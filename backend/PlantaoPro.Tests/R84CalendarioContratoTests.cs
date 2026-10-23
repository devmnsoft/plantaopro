using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Infrastructure.Legislacao;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R84 (bloco B4 - legislacao brasileira): calendario fiscal/obrigacoes legais com motor de
/// obrigacoes/reminders por tenant. Contrato travado nas regras oficiais do plano r66a0:
/// catalogo oficial na fonte unica (DAS dia 20, FGTS dia 20, DCTFWeb dia 15, eSocial dia 7,
/// EFD dia 15, livros contabeis dia 31 em dezembro), competencia = mes exibido menos o offset,
/// estados de reminder com janela oficial inclusiva, item anual somente na exibicao de
/// dezembro e estado por tenant isolado e persistido. Todos os numeros leem da fonte unica.
/// </summary>
public sealed class R84CalendarioContratoTests
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2366 = "database/migrations/2026_10_v2366_r83_simples_nacional_2026.sql";
    private const string MigrationV2367 = "database/migrations/2026_10_v2367_r84_calendario_fiscal_2026.sql";
    private const string MigrationVersion = "2026_10_v2367_r84_calendario_fiscal_2026";
    private static readonly DateOnly HojeFixo = new(2026, 10, 21);

    // Catalogo oficial travado: codigo -> "periodicidade|offset_periodos|dia_vencimento|regime".
    private static readonly Dictionary<string, string> EsperadoCatalogo = new(StringComparer.Ordinal)
    {
        ["OBRIGACAO_FISCAL_DAS_SIMPLES"] = "MENSAL|1|20|SIMPLES_NACIONAL",
        ["OBRIGACAO_FISCAL_FGTS_MENSAL"] = "MENSAL|1|20|CLT",
        ["OBRIGACAO_FISCAL_DCTFWEB_MENSAL"] = "MENSAL|1|15|GERAL",
        ["OBRIGACAO_FISCAL_ESOCIAL_S1200"] = "MENSAL|1|7|CLT",
        ["OBRIGACAO_FISCAL_EFD_ICMS_MENSAL"] = "MENSAL|1|15|GERAL",
        ["OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS"] = "ANUAL|0|31|GERAL"
    };

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r84_calendario_schema'));");
        try
        {
            var temTabela = await cn.ExecuteScalarAsync<bool>(
                "select exists(select 1 from information_schema.tables where table_schema='plantaopro' and table_name='parametros_legais')");
            if (!temTabela)
            {
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(Read(MigrationV2363)); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
            // v2366/v2367 sao idempotentes: reexecucao nao duplica (on conflict do nothing).
            await cn.ExecuteAsync(Read(MigrationV2366));
            await cn.ExecuteAsync(Read(MigrationV2367));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r84_calendario_schema'));"); }
    }

    private static ICalendarioFiscalService Servico()
    {
        var repo = new ParametrosLegaisRepository(TestDatabase.ConnectionString);
        return new CalendarioFiscalService(repo, new ObrigacoesFiscaisRepository(TestDatabase.ConnectionString));
    }

    [Fact]
    public void Manifesto_tem_entrada_v2367_com_hash_real_dependencia_e_status()
    {
        var sql = Read(MigrationV2367);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationV2367.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2366_r83_simples_nacional_2026" }, deps);

        foreach (var codigo in EsperadoCatalogo.Keys)
            Assert.Contains(codigo, sql);
        Assert.Contains("OBRIGACOES_FISCAIS_JANELA_ALERTA_DIAS", sql);
        Assert.Contains("create table if not exists plantaopro.adm_obrigacoes_fiscais", sql);
    }

    [Fact]
    public async Task Catalogo_oficial_de_obrigacoes_semeado_na_fonte_unica_com_fonte_citavel()
    {
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        async Task<string> Valor(string codigo) =>
            (await cn.QuerySingleAsync<string>("select valor from plantaopro.parametros_legais where codigo=@c and ano=2026 and reg_status='A'", new { c = codigo }));

        foreach (var par in EsperadoCatalogo)
            Assert.Equal(par.Value, await Valor(par.Key));
        Assert.Equal("7", await Valor("OBRIGACOES_FISCAIS_JANELA_ALERTA_DIAS"));

        // Coerencia cruzada com os dias ja semeados nas rodadas anteriores (fonte unica so tem uma verdade):
        // o dia do catalogo tem que bater com os parametros avulsos de vencimento existentes.
        var diaDas = (await Valor("OBRIGACAO_FISCAL_DAS_SIMPLES")).Split('|')[2];
        Assert.Equal(await Valor("SIMPLES_DAS_DIA_VENCIMENTO"), diaDas);
        Assert.Equal(await Valor("DAS_SIMPLES_VENCIMENTO_DIA"), diaDas);
        Assert.Equal((await Valor("OBRIGACAO_FISCAL_FGTS_MENSAL")).Split('|')[2], await Valor("FGTS_VENCIMENTO_DIA"));
        Assert.Equal((await Valor("OBRIGACAO_FISCAL_DCTFWEB_MENSAL")).Split('|')[2], await Valor("DCTFWEB_VENCIMENTO_DIA"));

        var semFonte = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where ano=2026 and reg_status='A' and codigo like 'OBRIGAC%' and (fonte_oficial is null or btrim(fonte_oficial)='')");
        Assert.Equal(0, semFonte);

        // Regras fixas do plano (datas consolidadas e janelas operacionais): todas com vigencia nula.
        var fixos = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where ano=2026 and reg_status='A' and codigo like 'OBRIGAC%' and vigencia_inicio is null and vigencia_fim is null");
        Assert.Equal(8, fixos);
    }

    [Fact]
    public async Task Calendario_do_mes_exibe_as_mensais_com_competencia_e_vencimento_oficiais()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        var outubro = await servico.ObterCalendarioAsync(Guid.NewGuid(), 10, 2026, HojeFixo);
        Assert.Equal(7, outubro.JanelaAlertaDias);
        Assert.Equal(5, outubro.Itens.Count);

        // Todas as mensais referem-se ao mes anterior ao exibido (offset 1).
        Assert.All(outubro.Itens, i =>
        {
            Assert.Equal(9, i.MesReferencia);
            Assert.Equal(2026, i.AnoReferencia);
            Assert.True(i.Ativa);
            Assert.False(i.Concluida);
        });

        // Ordem por vencimento (empate quebra por codigo) e datas oficiais do catalogo:
        // eSocial 7, DCTFWeb 15, EFD 15, DAS 20, FGTS 20.
        Assert.Equal(new[] { "OBRIGACAO_FISCAL_ESOCIAL_S1200", "OBRIGACAO_FISCAL_DCTFWEB_MENSAL", "OBRIGACAO_FISCAL_EFD_ICMS_MENSAL", "OBRIGACAO_FISCAL_DAS_SIMPLES", "OBRIGACAO_FISCAL_FGTS_MENSAL" },
            outubro.Itens.Select(i => i.Codigo).ToArray());
        Assert.Equal(new DateOnly(2026, 10, 7), outubro.Itens[0].DataVencimento);
        Assert.Equal(new DateOnly(2026, 10, 15), outubro.Itens[1].DataVencimento);
        Assert.Equal(new DateOnly(2026, 10, 15), outubro.Itens[2].DataVencimento);
        Assert.Equal(new DateOnly(2026, 10, 20), outubro.Itens[3].DataVencimento);
        Assert.Equal(new DateOnly(2026, 10, 20), outubro.Itens[4].DataVencimento);

        // Estados contra a data atual (21/10/2026): tudo atrasado nesse cenario.
        Assert.Equal(new[] { "ATRASADA", "ATRASADA", "ATRASADA", "ATRASADA", "ATRASADA" }, outubro.Itens.Select(i => i.Estado).ToArray());
        Assert.Equal(-14, outubro.Itens[0].DiasRestantes);
        Assert.Equal(-6, outubro.Itens[1].DiasRestantes);
        Assert.Equal(-1, outubro.Itens[4].DiasRestantes);

        // Janeiro: as mensais referem-se a dezembro do ano anterior e vencem em janeiro (ano vira).
        var janeiro = await servico.ObterCalendarioAsync(Guid.NewGuid(), 1, 2027, HojeFixo);
        Assert.All(janeiro.Itens, i =>
        {
            Assert.Equal(12, i.MesReferencia);
            Assert.Equal(2026, i.AnoReferencia);
            Assert.Equal(2027, i.DataVencimento.Year);
            Assert.Equal(1, i.DataVencimento.Month);
        });
        Assert.Equal(new DateOnly(2027, 1, 7), janeiro.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_ESOCIAL_S1200").DataVencimento);
        Assert.Equal(new DateOnly(2027, 1, 20), janeiro.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_DAS_SIMPLES").DataVencimento);
    }

    [Fact]
    public async Task Estados_de_reminder_seguem_a_janela_oficial_inclusiva()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        // Hoje 01/10: eSocial vence em 6 dias (dentro da janela de 7, inclusiva); DCTFWeb em 14 (fora).
        var inicioMes = await servico.ObterCalendarioAsync(Guid.NewGuid(), 10, 2026, new DateOnly(2026, 10, 1));
        var esocial = inicioMes.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_ESOCIAL_S1200");
        Assert.Equal(6, esocial.DiasRestantes);
        Assert.Equal("VENCE_EM_CURTO_PRAZO", esocial.Estado);
        Assert.Equal("AGENDADA", inicioMes.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_DCTFWEB_MENSAL").Estado);

        // No proprio dia do vencimento: vence hoje com zero dias restantes.
        var diaVencimento = await servico.ObterCalendarioAsync(Guid.NewGuid(), 10, 2026, new DateOnly(2026, 10, 7));
        Assert.Equal(0, diaVencimento.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_ESOCIAL_S1200").DiasRestantes);
        Assert.Equal("VENCE_HOJE", diaVencimento.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_ESOCIAL_S1200").Estado);

        // Borda exata da janela: 7 dias restantes continua alerta; 8 volta para agendada.
        var bordaDentro = await servico.ObterCalendarioAsync(Guid.NewGuid(), 10, 2026, new DateOnly(2026, 10, 13));
        Assert.Equal(7, bordaDentro.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_DAS_SIMPLES").DiasRestantes);
        Assert.Equal("VENCE_EM_CURTO_PRAZO", bordaDentro.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_DAS_SIMPLES").Estado);
        var bordaFora = await servico.ObterCalendarioAsync(Guid.NewGuid(), 10, 2026, new DateOnly(2026, 10, 12));
        Assert.Equal(8, bordaFora.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_DAS_SIMPLES").DiasRestantes);
        Assert.Equal("AGENDADA", bordaFora.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_DAS_SIMPLES").Estado);
    }

    [Fact]
    public async Task Dezembro_inclui_o_item_anual_de_livros_e_os_outros_meses_nao_incluem()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        var dezembro = await servico.ObterCalendarioAsync(Guid.NewGuid(), 12, 2026, HojeFixo);
        Assert.Equal(6, dezembro.Itens.Count);
        var livros = dezembro.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS");
        Assert.Equal("ANUAL", livros.Periodicidade);
        Assert.Equal(12, livros.MesReferencia);
        Assert.Equal(2026, livros.AnoReferencia);
        Assert.Equal(new DateOnly(2026, 12, 31), livros.DataVencimento);
        // As mensais de dezembro referem-se a novembro.
        Assert.All(dezembro.Itens.Where(i => i.Periodicidade == "MENSAL"), i =>
        {
            Assert.Equal(11, i.MesReferencia);
            Assert.Equal(2026, i.AnoReferencia);
        });

        // Em junho o item anual nao aparece (convencao do encerramento anual).
        var junho = await servico.ObterCalendarioAsync(Guid.NewGuid(), 6, 2026, HojeFixo);
        Assert.Equal(5, junho.Itens.Count);
        Assert.DoesNotContain(junho.Itens, i => i.Codigo == "OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS");
    }

    [Fact]
    public async Task Estado_por_tenant_e_independente_e_a_conclusao_alternada_persiste()
    {
        await GarantirSchemaAsync();
        var servico = Servico();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Periodo 03/2027 (vencimentos em mar/2027): contra o hoje fixo tudo fica agendado.
        var baseA = await servico.ObterCalendarioAsync(tenantA, 3, 2027, HojeFixo);
        var das = baseA.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_DAS_SIMPLES");
        Assert.Equal("AGENDADA", das.Estado);

        var concluida = await servico.AlternarConclusaoAsync(new AlternarConclusaoObrigacaoCommand("OBRIGACAO_FISCAL_DAS_SIMPLES", 3, 2027), tenantA, "teste-r84", HojeFixo);
        Assert.True(concluida.Concluida);
        Assert.Equal("CONCLUIDA", concluida.Estado);
        Assert.NotNull(concluida.ConcluidaEm);

        // Alternar de novo desmarca (toggle) e o estado volta a considerar a data.
        var desmarcada = await servico.AlternarConclusaoAsync(new AlternarConclusaoObrigacaoCommand("OBRIGACAO_FISCAL_DAS_SIMPLES", 3, 2027), tenantA, "teste-r84", HojeFixo);
        Assert.False(desmarcada.Concluida);
        Assert.Equal("AGENDADA", desmarcada.Estado);
        Assert.Null(desmarcada.ConcluidaEm);

        // Desativar uma obrigacao para o periodo: o estado vira NAO_SE_APLICA so para o tenant A.
        var inativa = await servico.AlterarAtivaAsync(new AlterarAtivaObrigacaoCommand("OBRIGACAO_FISCAL_ESOCIAL_S1200", 3, 2027, false), tenantA, HojeFixo);
        Assert.False(inativa.Ativa);
        Assert.Equal("NAO_SE_APLICA", inativa.Estado);

        var baseB = await servico.ObterCalendarioAsync(tenantB, 3, 2027, HojeFixo);
        var esocialB = baseB.Itens.Single(i => i.Codigo == "OBRIGACAO_FISCAL_ESOCIAL_S1200");
        Assert.True(esocialB.Ativa);
        Assert.Equal("AGENDADA", esocialB.Estado);

        // Reativar nao duplica linha (upsert na chave por tenant/codigo/referencia).
        var reativada = await servico.AlterarAtivaAsync(new AlterarAtivaObrigacaoCommand("OBRIGACAO_FISCAL_ESOCIAL_S1200", 3, 2027, true), tenantA, HojeFixo);
        Assert.True(reativada.Ativa);
        Assert.Equal("AGENDADA", reativada.Estado);

        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var linhasDas = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.adm_obrigacoes_fiscais where tenant_id=@t and codigo_obrigacao='OBRIGACAO_FISCAL_DAS_SIMPLES' and mes_referencia=2 and ano_referencia=2027 and reg_status='A'",
            new { t = tenantA });
        Assert.Equal(1, linhasDas);
        var linhasEsocial = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.adm_obrigacoes_fiscais where tenant_id=@t and codigo_obrigacao='OBRIGACAO_FISCAL_ESOCIAL_S1200' and mes_referencia=2 and ano_referencia=2027 and reg_status='A'",
            new { t = tenantA });
        Assert.Equal(1, linhasEsocial);
    }

    [Fact]
    public async Task Validacoes_explicitas_do_calendario_sao_erros_diretos()
    {
        await GarantirSchemaAsync();
        var servico = Servico();
        var tenant = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => servico.ObterCalendarioAsync(tenant, 0, 2026, HojeFixo));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.ObterCalendarioAsync(tenant, 13, 2026, HojeFixo));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.ObterCalendarioAsync(tenant, 10, 1999, HojeFixo));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.ObterCalendarioAsync(tenant, 10, 2101, HojeFixo));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.ObterCalendarioAsync(Guid.Empty, 10, 2026, HojeFixo));

        await Assert.ThrowsAsync<ArgumentException>(() => servico.AlterarAtivaAsync(new AlterarAtivaObrigacaoCommand("NAO_EXISTE", 3, 2027, true), tenant, HojeFixo));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.AlternarConclusaoAsync(new AlternarConclusaoObrigacaoCommand("NAO_EXISTE", 3, 2027), tenant, "teste-r84", HojeFixo));
        // Obrigacao anual so e gerenciada na exibicao de dezembro.
        await Assert.ThrowsAsync<ArgumentException>(() => servico.AlterarAtivaAsync(new AlterarAtivaObrigacaoCommand("OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS", 6, 2026, false), tenant, HojeFixo));
    }
}
