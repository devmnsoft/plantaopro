using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R87 (LGPD + saúde): consentimento por escopo controlado de dados de saúde
/// (PRONTUARIO/COAM — dado sensível, LGPD art. 11, I), direitos do titular
/// (LISTAR/EXPORTAR/APAGAR com trilha em lgpd_eventos_privacidade, LGPD art. 18) e
/// retenção mínima legal do prontuário (Lei 13.787/2018, art. 6: 20 anos a partir
/// do último registro clínico; Resolução CFM 1.821/2007 arts. 7-8).
/// Vetores travados: soma de meses com truncamento de fim de mês, data apta de
/// eliminação (2005-01-15 -> 2025-01-15; 2015-06-30 -> 2035-06-30), bloqueio 409
/// dentro da janela, anonimização determinística fora dela e trilha completa.
/// </summary>
public sealed class R87LgpdSaudeContratoTests : IDisposable
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2370 = "database/migrations/2026_10_v2370_r87_lgpd_saude.sql";
    private const string MigrationVersion = "2026_10_v2370_r87_lgpd_saude";

    private static readonly Guid TenantR87 = Guid.Parse("b7000001-0000-4000-9000-8700000000c1");
    private static readonly Guid TenantOutro = Guid.Parse("b7000002-0000-4000-9000-8700000000c2");
    private static readonly Guid UsuarioR87 = Guid.Parse("b7000001-0000-4000-9000-8700000000a1");
    private static readonly Guid PacienteAtivo = Guid.Parse("b7000001-0000-4000-9000-8700000000b1");   // ultimo registro 2005-01-15 -> apto 2025-01-15
    private static readonly Guid PacienteRecente = Guid.Parse("b7000001-0000-4000-9000-8700000000b2"); // ultimo registro 2015-06-30 -> apto 2035-06-30
    private static readonly Guid PacienteNovo = Guid.Parse("b7000001-0000-4000-9000-8700000000b3");   // sem registros clinicos
    private static readonly Guid PacienteOutroTenant = Guid.Parse("b7000002-0000-4000-9000-8700000000b9");
    private static readonly Guid ConsultaAtiva = Guid.Parse("b7000001-0000-4000-9000-8700000000c9");
    private static readonly Guid ConsultaRecente = Guid.Parse("b7000001-0000-4000-9000-8700000000c8");
    private static readonly Guid DocumentoAtivo = Guid.Parse("b7000001-0000-4000-9000-8700000000d9");

    private const string NomeRecente = "Paciente Retencao Ativa";
    private const string CpfRecente = "11144477735";

    private readonly Task SementePronta = SementarAsync();

    static R87LgpdSaudeContratoTests() => DapperTypeHandlerRegistrar.RegistrarTodos();

    public void Dispose()
    {
        SementePronta.GetAwaiter().GetResult();
        PurgarAsync().GetAwaiter().GetResult();
    }

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r87_lgpd_saude_schema'));");
        try
        {
            var temParametros = await cn.ExecuteScalarAsync<bool>(
                "select exists(select 1 from information_schema.tables where table_schema='plantaopro' and table_name='parametros_legais')");
            if (!temParametros)
            {
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(Read(MigrationV2363)); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
            // v2370 é idempotente: reexecução não duplica (create/add if not exists + on conflict / where not exists).
            await cn.ExecuteAsync(Read(MigrationV2370));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r87_lgpd_saude_schema'));"); }
    }

    private static LgpdSaudeService Svc() => new(TestDatabase.ConnectionString);

    private static async Task<int> ContarTrilhaAsync(string acao, Guid pacienteId)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.lgpd_eventos_privacidade where acao=@a and detalhes like '%'||@pid||'%'",
            new { a = acao, pid = pacienteId.ToString("D") });
    }

    // ---------------------------------------------------------------------
    // Fato 1 - manifesto com a entrada v2370 real.
    // ---------------------------------------------------------------------
    [Fact]
    public void Manifesto_tem_entrada_v2370_com_hash_real_dependencia_v2369_e_status()
    {
        var sql = Read(MigrationV2370);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationV2370.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2369_r86_trilhos_pagamento" }, deps);

        foreach (var codigo in new[] { "LGPD_SA_BASE_SENSIVEL", "LGPD_SA_ESCOPOS_CONSENTIMENTO", "LGPD_SA_CANAIS", "LGPD_SA_RETENCAO_PRONTUARIO_MESES", "LGPD_SA_RETENCAO_FISCAL_MESES", "LGPD_SA_DIREITOS" })
            Assert.Contains(codigo, sql);
        Assert.Contains("create table if not exists plantaopro.lgpd_escopos_consentimento", sql);
        Assert.Contains("'PRONTUARIO'", sql);
        Assert.Contains("'COAM'", sql);
        Assert.Contains("'240'", sql);
        Assert.Contains("Lei Federal 13.787/2018, art. 6", sql);
        Assert.Contains("on conflict (codigo, ano)", sql);
    }

    // ---------------------------------------------------------------------
    // Fato 2 - schema e seeds idempotentes na fonte única, com fonte citável.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Schema_e_seeds_saude_idempotentes_na_fonte_unica_com_fonte_citavel()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        var colunas = (await cn.QueryAsync<string>("select column_name from information_schema.columns where table_schema='plantaopro' and table_name='lgpd_escopos_consentimento'")).ToList();
        foreach (var coluna in new[] { "codigo", "nome", "exige_consentimento_explicito", "fonte_oficial", "ativo" })
            Assert.Contains(coluna, colunas);

        var escopos = (await cn.QueryAsync<LgpdEscopoDto>("select codigo Codigo, nome Nome, descricao Descricao, exige_consentimento_explicito Exige, coalesce(fonte_oficial,'') Fonte from plantaopro.lgpd_escopos_consentimento where reg_status='A' order by codigo")).ToList();
        Assert.Equal(2, escopos.Count);
        Assert.Equal(new[] { "COAM", "PRONTUARIO" }, escopos.Select(e => e.Codigo).ToArray());
        foreach (var escopo in escopos)
        {
            Assert.True(escopo.Exige, "escopo de dados de saude exige consentimento explicito (LGPD art. 11, I): " + escopo.Codigo);
            Assert.True(escopo.Fonte.Length >= 10, "fonte oficial citavel exigida para " + escopo.Codigo);
            Assert.Contains("13.709/2018", escopo.Fonte);
        }

        var diagRows = await cn.QueryAsync<dynamic>("select categoria, coalesce(prazo_meses::text,'NULL') meses, count(*)::int n from plantaopro.lgpd_retencao_dados where upper(categoria)='PRONTUARIO' group by 1,2");
        var diag = new System.Text.StringBuilder();
        foreach (var r in diagRows) diag.Append($"[{r.categoria}|{r.meses} x{r.n}] ");
        var ret = await cn.QuerySingleAsync<dynamic>(
            "select prazo_meses meses, coalesce(fonte_oficial,'') fonte from plantaopro.lgpd_retencao_dados where categoria='Prontuario' and reg_status='A'");
        var meses = Convert.ToInt32(ret.meses);
        Assert.True(meses == 240, "valor real=" + ret.meses + " tipo=" + (ret.meses == null ? "null" : ret.meses.GetType().Name) + " linhas=" + diag);
        Assert.Contains("13.787/2018", (string)ret.fonte);
        Assert.Contains("1.821/2007", (string)ret.fonte);

        var financeiro = await cn.QuerySingleAsync<dynamic>(
            "select prazo_meses meses, coalesce(fonte_oficial,'') fonte from plantaopro.lgpd_retencao_dados where categoria='Financeiro' and reg_status='A'");
        Assert.Equal(60, Convert.ToInt32(financeiro.meses));
        Assert.Contains("194", (string)financeiro.fonte);

        var esperado = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LGPD_SA_BASE_SENSIVEL"] = "CONSENTIMENTO_EXPLICITO",
            ["LGPD_SA_ESCOPOS_CONSENTIMENTO"] = "PRONTUARIO|COAM",
            ["LGPD_SA_CANAIS"] = "RECEPCAO|TELEFONE|DIGITAL|PRONTUARIO",
            ["LGPD_SA_RETENCAO_PRONTUARIO_MESES"] = "240",
            ["LGPD_SA_RETENCAO_FISCAL_MESES"] = "60",
            ["LGPD_SA_DIREITOS"] = "LISTAR|EXPORTAR|APAGAR"
        };
        foreach (var par in esperado)
        {
            var linha = await cn.QuerySingleAsync<dynamic>(
                "select valor, fonte_oficial from plantaopro.parametros_legais where codigo=@c and ano=2026 and reg_status='A'",
                new { c = par.Key });
            Assert.Equal(par.Value, (string)linha.valor);
            Assert.True(((string)linha.fonte_oficial).Length >= 10, "fonte oficial citavel exigida para " + par.Key);
        }
        // O parâmetro confere com a constante pura usada nas regras (fonte única).
        Assert.Equal("240", LgpdSaudeRegras.PrazoProntuarioMeses.ToString(CultureInfo.InvariantCulture));

        // Idempotência: rodar a migração de novo não duplica nenhuma linha.
        await cn.ExecuteAsync(Read(MigrationV2370));
        Assert.Equal(2, await cn.ExecuteScalarAsync<int>("select count(*) from plantaopro.lgpd_escopos_consentimento"));
        Assert.Equal(4, await cn.ExecuteScalarAsync<int>("select count(*) from plantaopro.lgpd_retencao_dados where reg_status='A'"));
        Assert.Equal(6, await cn.ExecuteScalarAsync<int>("select count(*) from plantaopro.parametros_legais where codigo like 'LGPD\\_SA\\_%' escape '\\' and ano=2026 and reg_status='A'"));
    }

    // ---------------------------------------------------------------------
    // Fato 3 - retenção mínima: soma de meses com truncamento de fim de mês e data apta.
    // ---------------------------------------------------------------------
    [Fact]
    public void Retencao_minima_soma_de_meses_truncamento_e_data_apta_travados()
    {
        Assert.Equal(240, LgpdSaudeRegras.PrazoProntuarioMeses);
        Assert.Equal(new DateOnly(2025, 1, 15), LgpdSaudeRegras.AptoDesdeUltimoRegistro(new DateOnly(2005, 1, 15)));
        Assert.Equal(new DateOnly(2035, 6, 30), LgpdSaudeRegras.AptoDesdeUltimoRegistro(new DateOnly(2015, 6, 30)));

        // Truncamento de fim de mês: janeiro de 31 dias cai em fevereiro (ano bissexto 2004).
        Assert.Equal(new DateOnly(2004, 2, 29), LgpdSaudeRegras.AddMeses(new DateOnly(2004, 1, 31), 1));
        Assert.Equal(new DateOnly(2024, 1, 31), LgpdSaudeRegras.AddMeses(new DateOnly(2004, 1, 31), 240));
        Assert.Equal(new DateOnly(2035, 6, 30), LgpdSaudeRegras.AddMeses(new DateOnly(2015, 6, 30), 240));

        // Limite: na data apta os 20 anos já se completaram (dia igual cumpre o prazo).
        Assert.True(LgpdSaudeRegras.PodeEliminarEm(new DateOnly(2025, 1, 15), new DateOnly(2025, 1, 15)));
        Assert.False(LgpdSaudeRegras.PodeEliminarEm(new DateOnly(2025, 1, 14), new DateOnly(2025, 1, 15)));
        Assert.True(LgpdSaudeRegras.PodeEliminarEm(new DateOnly(2036, 1, 1), new DateOnly(2035, 6, 30)));

        Assert.Equal(new[] { "PRONTUARIO", "COAM" }, LgpdSaudeRegras.Escopos);
        Assert.Equal(new[] { "LISTAR", "EXPORTAR", "APAGAR" }, LgpdSaudeRegras.Direitos);
        Assert.Equal("LGPD_APAGAR_PACIENTE", LgpdSaudeRegras.EventoTrilha("APAGAR"));
        Assert.StartsWith("ANONIMIZADO-", LgpdSaudeRegras.RotuloAnonimo("semente"));
    }

    // ---------------------------------------------------------------------
    // Fato 4 - consentimento: validações, substituição/revoção e trilha por registro.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Consentimento_valida_escopo_canal_e_versao_substitui_e_trilha_por_registro()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var svc = Svc();
        const string ip = "10.0.0.1";

        // Canal fora do catálogo rastreável cai apontando o campo.
        var canalRuim = await svc.RegistrarConsentimentoAsync(TenantR87, UsuarioR87, ip,
            new LgpdConsentimentoCommand(PacienteAtivo, "PRONTUARIO", true, "WHATSAPP", "T-1.0"), default);
        Assert.False(canalRuim.Success);
        Assert.Equal(400, canalRuim.StatusCode);
        Assert.Contains(canalRuim.Errors!, e => e.Contains("Canal"));

        // Dado sensível concedido sem versão do termo cai (consentimento específico e destacado).
        var semVersao = await svc.RegistrarConsentimentoAsync(TenantR87, UsuarioR87, ip,
            new LgpdConsentimentoCommand(PacienteAtivo, "PRONTUARIO", true, "RECEPCAO", null), default);
        Assert.False(semVersao.Success);
        Assert.Equal(400, semVersao.StatusCode);
        Assert.Contains(semVersao.Errors!, e => e.Contains("Versão"));

        // Escopo livre-texto fora do catálogo cai.
        var escopoLivre = await svc.RegistrarConsentimentoAsync(TenantR87, UsuarioR87, ip,
            new LgpdConsentimentoCommand(PacienteAtivo, "MARKETING", true, "RECEPCAO", "T-1.0"), default);
        Assert.False(escopoLivre.Success);
        Assert.Equal(400, escopoLivre.StatusCode);

        // Concessões válidas nos dois escopos controlados.
        var grantP = await svc.RegistrarConsentimentoAsync(TenantR87, UsuarioR87, ip,
            new LgpdConsentimentoCommand(PacienteAtivo, "PRONTUARIO", true, "recepcao", "T-1.0"), default);
        Assert.True(grantP.Success, grantP.Message);
        Assert.Equal("PRONTUARIO", grantP.Data!.Finalidade);
        Assert.True(grantP.Data.Concedido);
        Assert.Equal("RECEPCAO", grantP.Data.Canal);
        Assert.Equal(LgpdSaudeRegras.BaseSensivel, grantP.Data.BaseLegal);
        Assert.Equal("VIGENTE", grantP.Data.Status);

        var grantC = await svc.RegistrarConsentimentoAsync(TenantR87, UsuarioR87, ip,
            new LgpdConsentimentoCommand(PacienteAtivo, "COAM", true, "PRONTUARIO", "C-2.0"), default);
        Assert.True(grantC.Success, grantC.Message);
        Assert.Equal("COAM", grantC.Data!.Finalidade);

        // Revogação: o registro vigente vira REVOGADO e entra um novo VIGENTE não concedido.
        var revoga = await svc.RegistrarConsentimentoAsync(TenantR87, UsuarioR87, ip,
            new LgpdConsentimentoCommand(PacienteAtivo, "prontuario", false, "DIGITAL", null), default);
        Assert.True(revoga.Success, revoga.Message);
        Assert.False(revoga.Data!.Concedido);
        Assert.Null(revoga.Data.VersaoTermo);

        var lista = (await svc.ConsentimentosAsync(TenantR87, PacienteAtivo, default)).Data!;
        Assert.Equal(3, lista.Count);
        var prontuarios = lista.Where(c => c.Finalidade == "PRONTUARIO").OrderBy(c => c.RegDate).ToList();
        Assert.Equal(2, prontuarios.Count);
        Assert.Equal("REVOGADO", prontuarios[0].Status);
        Assert.True(prontuarios[0].Concedido);
        Assert.Equal("VIGENTE", prontuarios[1].Status);
        Assert.False(prontuarios[1].Concedido);
        var coam = lista.Single(c => c.Finalidade == "COAM");
        Assert.Equal("VIGENTE", coam.Status);
        Assert.True(coam.Concedido);
        Assert.Equal("C-2.0", coam.VersaoTermo);

        // Cada registro válido deixou exatamente uma trilha (3 operações válidas acima).
        Assert.Equal(3, await ContarTrilhaAsync(LgpdSaudeRegras.EventoTrilhaConsentimento, PacienteAtivo));
    }

    // ---------------------------------------------------------------------
    // Fato 5 - LISTAR e EXPORTAR retornam o envelope e deixam trilha por exercício.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Direitos_listar_e_exportar_retornam_envelope_e_deixam_trilha()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var svc = Svc();
        const string ip = "10.0.0.2";

        var antes = await ContarTrilhaAsync("LGPD_LISTAR_PACIENTE", PacienteRecente);
        antes += await ContarTrilhaAsync("LGPD_EXPORTAR_PACIENTE", PacienteRecente);

        var listar = await svc.ExercerDireitoAsync(TenantR87, UsuarioR87, ip, new ExercerDiretoCommand(PacienteRecente, "LISTAR"), default);
        Assert.True(listar.Success, listar.Message);
        Assert.Equal(NomeRecente, listar.Data!.Nome);
        Assert.Equal(CpfRecente, listar.Data.Cpf);
        Assert.True(listar.Data.UltimoRegistro == new DateOnly(2015, 6, 30), "ultimo registro clinico do vetor recente");
        Assert.Empty(listar.Data.Consentimentos);
        Assert.True(listar.Data.Retencao.AptoDesde == new DateOnly(2035, 6, 30), "data apta do vetor recente");
        Assert.False(listar.Data.Retencao.PodeEliminarAgora);
        Assert.Equal(240, listar.Data.Retencao.PrazoMeses);
        Assert.Contains("13.787/2018", listar.Data.Retencao.Fonte);

        Assert.Equal(antes + 1, (await ContarTrilhaAsync("LGPD_LISTAR_PACIENTE", PacienteRecente)) + (await ContarTrilhaAsync("LGPD_EXPORTAR_PACIENTE", PacienteRecente)));

        var exportar = await svc.ExercerDireitoAsync(TenantR87, UsuarioR87, ip, new ExercerDiretoCommand(PacienteRecente, "EXPORTAR"), default);
        Assert.True(exportar.Success, exportar.Message);
        Assert.Equal(NomeRecente, exportar.Data!.Nome);
        Assert.True(exportar.Data.UltimoRegistro == new DateOnly(2015, 6, 30));

        Assert.Equal(antes + 2, (await ContarTrilhaAsync("LGPD_LISTAR_PACIENTE", PacienteRecente)) + (await ContarTrilhaAsync("LGPD_EXPORTAR_PACIENTE", PacienteRecente)));

        // Direito fora do catálogo cai antes de tocar no banco.
        var outro = await svc.ExercerDireitoAsync(TenantR87, UsuarioR87, ip, new ExercerDiretoCommand(PacienteRecente, "RETIFICAR"), default);
        Assert.False(outro.Success);
        Assert.Equal(400, outro.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Fato 6 - APAGAR dentro da retenção mínima legal: 409 com a data apta.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Apagar_dentro_da_retencao_minima_retorna_409_com_data_apta()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var svc = Svc();

        var apagar = await svc.ExercerDireitoAsync(TenantR87, UsuarioR87, "10.0.0.3", new ExercerDiretoCommand(PacienteRecente, "APAGAR"), default);
        Assert.False(apagar.Success);
        Assert.Equal(409, apagar.StatusCode);
        Assert.Contains(apagar.Errors!, e => e == LgpdSaudeRegras.MotivoRetencaoMinima);
        Assert.True(apagar.Data!.Retencao.AptoDesde == new DateOnly(2035, 6, 30), "data apta exposta no bloqueio");
        Assert.False(apagar.Data.Retencao.PodeEliminarAgora);
        Assert.Contains("2035-06-30", apagar.Message);

        // Nada foi anonimizado e nenhuma trilha de eliminação foi gravada.
        var leitura = await svc.ListarDadosAsync(TenantR87, PacienteRecente, default);
        Assert.True(leitura.Success, leitura.Message);
        Assert.Equal(NomeRecente, leitura.Data!.Nome);
        Assert.Equal(CpfRecente, leitura.Data.Cpf);
        Assert.Equal(0, await ContarTrilhaAsync("LGPD_APAGAR_PACIENTE", PacienteRecente));
    }

    // ---------------------------------------------------------------------
    // Fato 7 - APAGAR fora da retenção: anonimiza identificadores, preserva o histórico clínico e trilheia.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Apagar_fora_da_retencao_anonimiza_preserva_historico_e_trilha()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var svc = Svc();

        // Preview da retenção: o documento (2004-06-01) é anterior à consulta (2005-01-15); vence a maior.
        var preview = await svc.RetencaoAsync(TenantR87, PacienteAtivo, default);
        Assert.True(preview.Success, preview.Message);
        Assert.True(preview.Data!.UltimoRegistro == new DateOnly(2005, 1, 15), "ultimo registro = maior data entre consultas e documentos");
        Assert.True(preview.Data.AptoDesde == new DateOnly(2025, 1, 15), "apto em 20 anos exatos");
        Assert.True(preview.Data.PodeEliminarAgora, "vetor antigo ja cumpriu os 20 anos");
        Assert.Equal(240, preview.Data.PrazoMeses);

        var apagar = await svc.ExercerDireitoAsync(TenantR87, UsuarioR87, "10.0.0.4", new ExercerDiretoCommand(PacienteAtivo, "APAGAR"), default);
        Assert.True(apagar.Success, apagar.Message);
        var rotulo = LgpdSaudeRegras.RotuloAnonimo(PacienteAtivo.ToString("N"));
        Assert.Equal(rotulo, apagar.Data!.Nome);
        Assert.Null(apagar.Data.Cpf);
        Assert.Null(apagar.Data.Email);
        Assert.Null(apagar.Data.Telefone);
        Assert.Null(apagar.Data.DataNascimento);

        // O histórico clínico permanece íntegro (a eliminação é anonimização, não delete).
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        Assert.Equal(1, await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.consultas where paciente_id=@p and reg_status='A'", new { p = PacienteAtivo }));

        // Trilha da eliminação com a fonte legal nos detalhes.
        Assert.Equal(1, await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.lgpd_eventos_privacidade where acao='LGPD_APAGAR_PACIENTE' and detalhes like '%'||@pid||'%' and detalhes like '%13.787%'",
            new { pid = PacienteAtivo.ToString("D") }));

        // Segunda eliminação cai honestamente (já anonimizado).
        var deNovo = await svc.ExercerDireitoAsync(TenantR87, UsuarioR87, "10.0.0.4", new ExercerDiretoCommand(PacienteAtivo, "APAGAR"), default);
        Assert.False(deNovo.Success);
        Assert.Equal(409, deNovo.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Fato 8 - sem registros clínicos a eliminação é imediata; isolamento de tenant.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Sem_registros_clinicos_eliminacao_imediata_e_isolamento_de_tenant()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var svc = Svc();

        var novo = await svc.RetencaoAsync(TenantR87, PacienteNovo, default);
        Assert.True(novo.Success, novo.Message);
        Assert.Null(novo.Data!.UltimoRegistro);
        Assert.Null(novo.Data.AptoDesde);
        Assert.True(novo.Data.PodeEliminarAgora, "sem ultimo registro nao ha janela de retencao");

        // Paciente de outro tenant é invisível para este contexto (404 honesto, não vaza dados).
        var outro = await svc.ConsentimentosAsync(TenantR87, PacienteOutroTenant, default);
        Assert.False(outro.Success);
        Assert.Equal(404, outro.StatusCode);
        var exOutro = await svc.ExercerDireitoAsync(TenantR87, UsuarioR87, "10.0.0.5", new ExercerDiretoCommand(PacienteOutroTenant, "LISTAR"), default);
        Assert.Equal(404, exOutro.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Semente idempotente (purga + insert em uma transação), padrão B6.
    // ---------------------------------------------------------------------
    private const string PurgeSql = @"
delete from plantaopro.lgpd_eventos_privacidade where tenant_id in (@t, @t2);
delete from plantaopro.paciente_consentimentos where paciente_id in (@p1, @p2, @p3, @p9);
delete from plantaopro.consultas where id in (@c1, @c2);
delete from plantaopro.documentos_clinicos where id in (@d1);
delete from plantaopro.pacientes where id in (@p1, @p2, @p3, @p9);
delete from plantaopro.clientes where id in (@t, @t2);";

    private const string InsertSql = @"
insert into plantaopro.clientes (id, razao_social, cnpj, status, reg_status) values
  (@t, 'R87 Clinica Teste', '90871122000133', 'ATIVO', 'A'),
  (@t2, 'R87 Clinica Vizinha', '90872244000155', 'ATIVO', 'A');
insert into plantaopro.pacientes (id, cliente_id, tenant_id, nome_completo, nome, data_nascimento, sexo, documento, cpf, email, telefone, endereco, responsavel_nome, finalidade_tratamento, ver_dados_sensiveis, consentimento_lgpd, consentimento_lgpd_canal, status, dados, criado_em, created_at, reg_status, reg_date) values
  (@p1, @t, @t, 'Paciente Antigo', 'Paciente Antigo', '1950-02-20', 'M', '52998224725', '52998224725', 'antigo@exemplo.com.br', '11999990001', '', '', 'ATENDIMENTO', false, true, 'RECEPCAO', 'ATIVO', '{}'::jsonb, now(), now(), 'A', now()),
  (@p2, @t, @t, 'Paciente Retencao Ativa', 'Paciente Retencao Ativa', '1990-03-10', 'F', '22255588858', '11144477735', 'recente@exemplo.com.br', '11999990002', '', '', 'ATENDIMENTO', false, true, 'RECEPCAO', 'ATIVO', '{}'::jsonb, now(), now(), 'A', now()),
  (@p3, @t, @t, 'Paciente Novo', 'Paciente Novo', '2000-05-05', 'F', '33366699939', '33366699939', 'novo@exemplo.com.br', '11999990003', '', '', 'ATENDIMENTO', false, false, 'NENHUM', 'ATIVO', '{}'::jsonb, now(), now(), 'A', now()),
  (@p9, @t2, @t2, 'Paciente Outro Tenant', 'Paciente Outro Tenant', '1980-01-01', 'F', '44477700040', '44477700040', 'outro@exemplo.com.br', '11999990004', '', '', 'ATENDIMENTO', false, false, 'NENHUM', 'ATIVO', '{}'::jsonb, now(), now(), 'A', now());
insert into plantaopro.consultas (id, cliente_id, tenant_id, paciente_id, status, tipo, motivo_atendimento, resumo, observacoes, anamnese, exame_fisico, diagnostico, codigo_cid, conduta, orientacoes, dados, versao, inicio_em, finalizada_em, criado_em, reg_date, reg_status) values
  (@c1, @t, @t, @p1, 'FINALIZADA', 'CONSULTA', 'Teste', '', '', '', '', '', '', '', '', '{}'::jsonb, 1,
   '2005-01-15 09:00:00'::timestamp at time zone 'UTC', '2005-01-15 10:00:00'::timestamp at time zone 'UTC', now(), now(), 'A'),
  (@c2, @t, @t, @p2, 'FINALIZADA', 'CONSULTA', 'Teste', '', '', '', '', '', '', '', '', '{}'::jsonb, 1,
   '2015-06-30 09:00:00'::timestamp at time zone 'UTC', '2015-06-30 10:00:00'::timestamp at time zone 'UTC', now(), now(), 'A');
insert into plantaopro.documentos_clinicos (id, tenant_id, paciente_id, tipo, titulo, conteudo, status, versao, assinatura_status, cid_exibido, emitido_em, created_at) values
  (@d1, @t, @p1, 'ATESTADO', 'Documento antigo', 'Conteudo de teste.', 'EMITIDO', 1, 'PENDENTE', false,
   '2004-06-01 10:00:00'::timestamp at time zone 'UTC', now())";

    private static object SeedParams() => new
    {
        t = TenantR87,
        t2 = TenantOutro,
        p1 = PacienteAtivo,
        p2 = PacienteRecente,
        p3 = PacienteNovo,
        p9 = PacienteOutroTenant,
        c1 = ConsultaAtiva,
        c2 = ConsultaRecente,
        d1 = DocumentoAtivo
    };

    private static async Task SementarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r87_lgpd_saude_seed'));");
        try
        {
            await using var tx = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync(PurgeSql, SeedParams(), tx);
            await cn.ExecuteAsync(InsertSql, SeedParams(), tx);
            await tx.CommitAsync();
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r87_lgpd_saude_seed'));"); }
    }

    private static async Task PurgarAsync()
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r87_lgpd_saude_seed'));");
            try
            {
                await cn.ExecuteAsync(PurgeSql, SeedParams());
            }
            finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r87_lgpd_saude_seed'));"); }
        });
    }
}
