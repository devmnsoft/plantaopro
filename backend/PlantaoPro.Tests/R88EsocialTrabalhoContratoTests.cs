using System;
using System.Collections.Generic;
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
/// R88 (trabalhista / eSocial): catálogo oficial dos eventos eSocial da faixa
/// S-1000 a S-1299 (leiaute S-1.3, NT 06/2026 rev. 09/04/2026), registro em prévia
/// de eventos por colaborador (forma_envio fixa PREVIEW), análise de horas/jornada
/// semanal contra a carga contratual (adm_contratos_trabalho) e o teto legal de 44h
/// (CF/88 art. 7, XIII), estimativa de hora extra (CLT art. 59 §1º sobre salário/220h
/// - CLT art. 74 §2º) e aviso prévio proporcional (Lei 12.506/2011: base 30 dias +
/// 3 por ano completo, teto 90; NT MTE 184/2012 sobre o acréscimo após 1 ano).
/// Vetores travados: datas de aviso prévio, normalização de semana à segunda-feira,
/// situações de jornada, upsert de declaração semanal e round-trip de eventos com
/// validações e isolamento entre tenants.
/// </summary>
public sealed class R88EsocialTrabalhoContratoTests : IDisposable
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2371 = "database/migrations/2026_10_v2371_r88_esocial_trabalhista.sql";
    private const string MigrationVersion = "2026_10_v2371_r88_esocial_trabalhista";

    private static readonly Guid TenantR88 = Guid.Parse("b8000001-0000-4000-9000-8800000000c1");
    private static readonly Guid TenantOutro = Guid.Parse("b8000002-0000-4000-9000-8800000000c2");
    private static readonly Guid UsuarioR88 = Guid.Parse("b8000001-0000-4000-9000-8800000000a1");
    private static readonly Guid DepartamentoR88 = Guid.Parse("b8000001-0000-4000-9000-8800000000d1");
    private static readonly Guid CargoR88 = Guid.Parse("b8000001-0000-4000-9000-8800000000e1");
    private static readonly Guid ColaboradorA = Guid.Parse("b8000001-0000-4000-9000-8800000000f1"); // contrato CLT vigente 44h R$ 2200 desde 2020-03-02
    private static readonly Guid ColaboradorB = Guid.Parse("b8000001-0000-4000-9000-8800000000f2"); // sem contrato vigente
    private static readonly Guid ContratoA = Guid.Parse("b8000001-0000-4000-9000-8800000000f3");
    private static readonly Guid ColaboradorAusente = Guid.Parse("b8000001-0000-4000-9000-8800000000f9");

    private readonly Task SementePronta = SementarAsync();

    static R88EsocialTrabalhoContratoTests() => DapperTypeHandlerRegistrar.RegistrarTodos();

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
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r88_esocial_trabalho_schema'));");
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
            // v2371 é idempotente: reexecução não duplica (create if not exists + on conflict / where not exists).
            await cn.ExecuteAsync(Read(MigrationV2371));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r88_esocial_trabalho_schema'));"); }
    }

    private static EsocialTrabalhoService Svc() => new(TestDatabase.ConnectionString);

    private sealed record ParamTeste(string Codigo, string? Valor);

    // ---------------------------------------------------------------------
    // Fato 1 - manifesto com a entrada v2371 real.
    // ---------------------------------------------------------------------
    [Fact]
    public void Manifesto_tem_entrada_v2371_com_hash_real_dependencia_v2370_e_status()
    {
        var sql = Read(MigrationV2371);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationV2371.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2370_r87_lgpd_saude" }, deps);

        foreach (var tabela in new[]
        {
            "create table if not exists plantaopro.esocial_eventos_catalogo",
            "create table if not exists plantaopro.colaborador_eventos_esocial",
            "create table if not exists plantaopro.colaborador_jornadas_semanais"
        })
            Assert.Contains(tabela, sql);
        foreach (var codigo in new[]
        {
            "ESOCIAL_EVENTOS_CATALOGO", "ESOCIAL_VENCIMENTO_PERIODICO_DIA",
            "AVISO_PREVIO_BASE_DIAS", "AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS", "AVISO_PREVIO_TETO_TOTAL_DIAS",
            "JORNADA_SEMANAL_MAX_HORAS", "HORA_EXTRA_ACRESIMO_MIN_PCT", "SALARIO_DIVISOR_MENSAL_HORAS"
        })
            Assert.Contains(codigo, sql);
        Assert.Contains("'S-1000|S-1005|S-1010|S-1020|S-1070|S-1200|S-1202|S-1207|S-1210|S-1260|S-1270|S-1280|S-1298|S-1299'", sql);
        Assert.Contains("Lei 12.506/2011", sql);
        Assert.Contains("on conflict (codigo, ano)", sql);
    }

    // ---------------------------------------------------------------------
    // Fato 2 - schema e seeds idempotentes na fonte única, com fonte citável.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Schema_catalogo_e_parametros_idempotentes_na_fonte_unica_com_fonte_citavel()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        var codigos = (await cn.QueryAsync<string>(
            "select codigo from plantaopro.esocial_eventos_catalogo where reg_status='A' order by codigo")).ToList();
        Assert.Equal(new[] { "S-1000", "S-1005", "S-1010", "S-1020", "S-1070", "S-1200", "S-1202", "S-1207", "S-1210", "S-1260", "S-1270", "S-1280", "S-1298", "S-1299" }, codigos);

        var categorias = (await cn.QueryAsync<string>(
            "select distinct categoria from plantaopro.esocial_eventos_catalogo where reg_status='A' order by categoria")).ToList();
        Assert.Equal(new[] { "PERIODICO", "TABELA_EMPREGADOR" }, categorias);

        var semFonteOuNome = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.esocial_eventos_catalogo where reg_status='A' and (char_length(nome) < 5 or char_length(coalesce(fonte_oficial,'')) < 5 or char_length(vencimento) < 5)");
        Assert.Equal(0, semFonteOuNome);

        var periodo = await cn.ExecuteScalarAsync<string>(
            "select vencimento from plantaopro.esocial_eventos_catalogo where codigo='S-1299'");
        Assert.Contains("dia 15", (periodo ?? string.Empty).ToLowerInvariant());

        // idempotência: reexecutar a migração não duplica catálogo nem parâmetros.
        await cn.ExecuteAsync(Read(MigrationV2371));
        var totalCatalogo = await cn.ExecuteScalarAsync<int>("select count(*) from plantaopro.esocial_eventos_catalogo where reg_status='A'");
        Assert.Equal(14, totalCatalogo);

        var parametros = (await cn.QueryAsync<ParamTeste>(@"
select codigo Codigo, valor Valor from plantaopro.parametros_legais
where codigo in ('ESOCIAL_EVENTOS_CATALOGO','ESOCIAL_VENCIMENTO_PERIODICO_DIA','AVISO_PREVIO_BASE_DIAS',
 'AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS','AVISO_PREVIO_TETO_TOTAL_DIAS','JORNADA_SEMANAL_MAX_HORAS',
 'HORA_EXTRA_ACRESIMO_MIN_PCT','SALARIO_DIVISOR_MENSAL_HORAS') and reg_status='A'
order by ano desc")).ToList();
        Assert.Equal(8, parametros.Count);
        var mapa = parametros.ToDictionary(p => p.Codigo, p => p.Valor ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("30", mapa["AVISO_PREVIO_BASE_DIAS"]);
        Assert.Equal("3", mapa["AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS"]);
        Assert.Equal("90", mapa["AVISO_PREVIO_TETO_TOTAL_DIAS"]);
        Assert.Equal("44", mapa["JORNADA_SEMANAL_MAX_HORAS"]);
        Assert.Equal("50", mapa["HORA_EXTRA_ACRESIMO_MIN_PCT"]);
        Assert.Equal("220", mapa["SALARIO_DIVISOR_MENSAL_HORAS"]);
        Assert.Equal("15", mapa["ESOCIAL_VENCIMENTO_PERIODICO_DIA"]);
    }

    // ---------------------------------------------------------------------
    // Fato 3 - aviso prévio proporcional: vetores travados da Lei 12.506/2011.
    // ---------------------------------------------------------------------
    [Fact]
    public void Aviso_previo_proporcional_vetores_travados_com_base_acrescimo_e_teto()
    {
        // 1 ano completo -> acréscimo mínimo de 3 dias (33 no total).
        Assert.Equal(33, EsocialTrabalhoRegras.DiasAvisoPrevio(new DateOnly(2024, 1, 15), new DateOnly(2025, 1, 15)));
        // 364 dias (até 1 ano) -> somente a base de 30 dias.
        Assert.Equal(30, EsocialTrabalhoRegras.DiasAvisoPrevio(new DateOnly(2023, 6, 30), new DateOnly(2024, 6, 29)));
        // 10 anos -> 30 + 30 = 60.
        Assert.Equal(60, EsocialTrabalhoRegras.DiasAvisoPrevio(new DateOnly(2015, 6, 30), new DateOnly(2025, 6, 30)));
        // 36 anos -> estoura o teto: 90 dias no máximo.
        Assert.Equal(90, EsocialTrabalhoRegras.DiasAvisoPrevio(new DateOnly(1990, 5, 20), new DateOnly(2026, 10, 1)));
        // menos de 1 ano dentro do mesmo mês corrente -> base 30.
        Assert.Equal(30, EsocialTrabalhoRegras.DiasAvisoPrevio(new DateOnly(2026, 1, 5), new DateOnly(2026, 10, 22)));

        Assert.Equal(1, EsocialTrabalhoRegras.AnosCompletos(new DateOnly(2024, 1, 15), new DateOnly(2025, 1, 15)));
        Assert.Equal(0, EsocialTrabalhoRegras.AnosCompletos(new DateOnly(2023, 6, 30), new DateOnly(2024, 6, 29)));
        Assert.Equal(36, EsocialTrabalhoRegras.AnosCompletos(new DateOnly(1990, 5, 20), new DateOnly(2026, 10, 1)));
        Assert.Equal(0, EsocialTrabalhoRegras.AnosCompletos(new DateOnly(2025, 1, 15), new DateOnly(2024, 1, 15)));
    }

    // ---------------------------------------------------------------------
    // Fato 4 - semana de referência eSocial/CLT sempre normaliza à segunda-feira.
    // ---------------------------------------------------------------------
    [Fact]
    public void Semana_de_referencia_volt_a_segunda_feira_da_semana_contendo_a_data()
    {
        // 2026-10-22 é quinta-feira -> segunda da mesma semana 2026-10-19.
        Assert.Equal(new DateOnly(2026, 10, 19), EsocialTrabalhoRegras.SemanaInicio(new DateOnly(2026, 10, 22)));
        // 2026-01-01 é quinta-feira -> segunda anterior 2025-12-29.
        Assert.Equal(new DateOnly(2025, 12, 29), EsocialTrabalhoRegras.SemanaInicio(new DateOnly(2026, 1, 1)));
        // segunda permanece inalterada.
        Assert.Equal(new DateOnly(2026, 10, 19), EsocialTrabalhoRegras.SemanaInicio(new DateOnly(2026, 10, 19)));
        // domingo cai na segunda da mesma semana (não avança).
        Assert.Equal(new DateOnly(2026, 10, 19), EsocialTrabalhoRegras.SemanaInicio(new DateOnly(2026, 10, 25)));
    }

    // ---------------------------------------------------------------------
    // Fato 5 - análise semanal de jornada: situações, excedentes e estimativa.
    // ---------------------------------------------------------------------
    [Fact]
    public void Analise_semanal_de_jornada_situacoes_excedentes_e_estimativa_de_hora_extra()
    {
        var acimaDoTeto = EsocialTrabalhoRegras.ResumoSemanaJornada.Calcular(40m, 5m, 44m);
        Assert.True(acimaDoTeto.ExcedeLimiteLegal);
        Assert.Equal(true, acimaDoTeto.ExcedeContrato);
        Assert.Equal(45m, acimaDoTeto.Total);
        Assert.Equal(1m, acimaDoTeto.HorasExcedentes);
        Assert.Equal(EsocialTrabalhoRegras.SituacoesJornada.ExcedeLimiteLegal, acimaDoTeto.Situacao);

        var conforme = EsocialTrabalhoRegras.ResumoSemanaJornada.Calcular(40m, 0m, 44m);
        Assert.False(conforme.ExcedeLimiteLegal);
        Assert.Equal(false, conforme.ExcedeContrato);
        Assert.Equal(0m, conforme.HorasExcedentes);
        Assert.Equal(EsocialTrabalhoRegras.SituacoesJornada.Conforme, conforme.Situacao);

        var acimaDaContratada = EsocialTrabalhoRegras.ResumoSemanaJornada.Calcular(41m, 2m, 40m);
        Assert.False(acimaDaContratada.ExcedeLimiteLegal);
        Assert.Equal(true, acimaDaContratada.ExcedeContrato);
        Assert.Equal(43m, acimaDaContratada.Total);
        Assert.Equal(3m, acimaDaContratada.HorasExcedentes);
        Assert.Equal(EsocialTrabalhoRegras.SituacoesJornada.ExcedeContrato, acimaDaContratada.Situacao);

        var semContratoAcimaDe44 = EsocialTrabalhoRegras.ResumoSemanaJornada.Calcular(44m, 1m, null);
        Assert.True(semContratoAcimaDe44.ExcedeLimiteLegal);
        Assert.Null(semContratoAcimaDe44.ExcedeContrato);
        Assert.Equal(1m, semContratoAcimaDe44.HorasExcedentes);
        Assert.Equal(EsocialTrabalhoRegras.SituacoesJornada.ExcedeLimiteLegal, semContratoAcimaDe44.Situacao);

        var nada = EsocialTrabalhoRegras.ResumoSemanaJornada.Calcular(0m, 0m, 44m);
        Assert.Equal(EsocialTrabalhoRegras.SituacoesJornada.NaoDeclarada, nada.Situacao);

        // salário R$ 2200 / 220h = R$ 10,00/hora; +50% mínimo = R$ 15,00 a hora extra.
        Assert.Equal(15m, EsocialTrabalhoRegras.ValorHoraExtra(2200m));
        Assert.Null(EsocialTrabalhoRegras.ValorHoraExtra(null));
        Assert.Null(EsocialTrabalhoRegras.ValorHoraExtra(0m));
        Assert.Equal(30.00m, EsocialTrabalhoRegras.ValorHorasExtras(15m, 2m));
        Assert.Null(EsocialTrabalhoRegras.ValorHorasExtras(15m, 0m));
    }

    // ---------------------------------------------------------------------
    // Fato 6 - jornada mensal: cinco semanas em outubro/2026 e contexto contratual.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Jornada_do_mes_carrega_cinco_semanas_de_outubro_2026_com_contexto_contratual()
    {
        await SementePronta;
        await GarantirSchemaAsync();

        var r = await Svc().JornadaAsync(TenantR88, ColaboradorA, "2026-10", CancellationToken.None);
        Assert.True(r.Success);
        var d = r.Data!;
        Assert.Equal("CLT", d.TipoContrato);
        Assert.Equal(44, d.CargaHorariaSemanal);
        Assert.Equal(2200m, d.SalarioContratado);
        Assert.Equal(44m, d.JornadaSemanalMax);
        Assert.Equal(5, d.Semanas.Count);
        Assert.Equal(new DateOnly(2026, 9, 28), d.Semanas[0].SemanaInicio);
        Assert.Equal(new DateOnly(2026, 10, 26), d.Semanas[4].SemanaInicio);
        Assert.All(d.Semanas, s => Assert.Equal(EsocialTrabalhoRegras.SituacoesJornada.NaoDeclarada, s.Situacao));
        Assert.Equal(15m, d.ValorHoraExtra);
        Assert.NotNull(d.Fontes);
        Assert.Contains(d.Fontes, f => f.Contains("44"));

        var mesInvalido = await Svc().JornadaAsync(TenantR88, ColaboradorA, "2026/10", CancellationToken.None);
        Assert.Equal(400, mesInvalido.StatusCode);
        var colabAusente = await Svc().JornadaAsync(TenantR88, ColaboradorAusente, "2026-10", CancellationToken.None);
        Assert.Equal(404, colabAusente.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Fato 7 - declaração semanal faz upsert, valida totais e permite remoção.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Declarar_jornada_faz_upsert_normaliza_a_segunda_valida_totais_e_remove()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var s = Svc();
        var ct = CancellationToken.None;

        var ok1 = await s.DeclararJornadaAsync(TenantR88, UsuarioR88,
            new EsocialJornadaDeclararCommand(ColaboradorA, new DateOnly(2026, 10, 21), 40m, 5m, "escala estendida"), ct);
        Assert.True(ok1.Success);
        var d1 = ok1.Data!;
        var declaradas = d1.Semanas.Count(x => x.Declarada);
        Assert.Equal(1, declaradas);
        var semanaDeclarada = d1.Semanas.First(x => x.Declarada);
        // quinta 2026-10-21 normaliza para a segunda 2026-10-19.
        Assert.Equal(new DateOnly(2026, 10, 19), semanaDeclarada.SemanaInicio);
        Assert.Equal(EsocialTrabalhoRegras.SituacoesJornada.ExcedeLimiteLegal, semanaDeclarada.Situacao);
        Assert.Equal(45m, d1.TotalHoras);
        Assert.Equal(1m, d1.HorasExcedentesMes);
        Assert.Equal(15m, d1.ValorHoraExtra);
        Assert.Equal(15.00m, d1.ValorTotalHorasExtras);
        var id = semanaDeclarada.Id!.Value;

        // redeclarar a mesma semana atualiza em vez de duplicar (upsert).
        var ok2 = await s.DeclararJornadaAsync(TenantR88, UsuarioR88,
            new EsocialJornadaDeclararCommand(ColaboradorA, new DateOnly(2026, 10, 19), 40m, 0m, null), ct);
        Assert.True(ok2.Success);
        Assert.Equal(1, ok2.Data!.Semanas.Count(x => x.Declarada));
        Assert.Equal(0m, ok2.Data.HorasExcedentesMes);
        Assert.Equal(EsocialTrabalhoRegras.SituacoesJornada.Conforme, ok2.Data.Semanas.First(x => x.Declarada).Situacao);
        Assert.Equal(id, ok2.Data.Semanas.First(x => x.Declarada).Id);

        var negativas = await s.DeclararJornadaAsync(TenantR88, UsuarioR88,
            new EsocialJornadaDeclararCommand(ColaboradorA, new DateOnly(2026, 10, 19), -1m, 0m, null), ct);
        Assert.Equal(400, negativas.StatusCode);
        var acimaDoSanidade = await s.DeclararJornadaAsync(TenantR88, UsuarioR88,
            new EsocialJornadaDeclararCommand(ColaboradorA, new DateOnly(2026, 10, 19), 100m, 21m, null), ct);
        Assert.Equal(400, acimaDoSanidade.StatusCode);
        var observacaoLarga = await s.DeclararJornadaAsync(TenantR88, UsuarioR88,
            new EsocialJornadaDeclararCommand(ColaboradorA, new DateOnly(2026, 10, 19), 40m, 0m, new string('x', 401)), ct);
        Assert.Equal(400, observacaoLarga.StatusCode);
        var semColaborador = await s.DeclararJornadaAsync(TenantR88, UsuarioR88,
            new EsocialJornadaDeclararCommand(ColaboradorAusente, new DateOnly(2026, 10, 19), 40m, 0m, null), ct);
        Assert.Equal(404, semColaborador.StatusCode);

        var del = await s.ExcluirJornadaAsync(TenantR88, id, ct);
        Assert.True(del.Success);
        var delDeNovo = await s.ExcluirJornadaAsync(TenantR88, id, ct);
        Assert.Equal(404, delDeNovo.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Fato 8 - aviso prévio via serviço: números, fontes e validação de datas.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Aviso_previo_servico_devolve_base_acrescimo_teto_fontes_e_valida_duas()
    {
        await SementePronta;
        var s = Svc();
        var ct = CancellationToken.None;

        var ok = await s.AvisoPrevioAsync(TenantR88,
            new EsocialAvisoPrevioCommand(new DateOnly(2024, 1, 15), new DateOnly(2025, 1, 15)), ct);
        Assert.True(ok.Success);
        Assert.Equal(30, ok.Data!.DiasBase);
        Assert.Equal(1, ok.Data.AnosCompletos);
        Assert.Equal(3, ok.Data.DiasAcrecimo);
        Assert.Equal(33, ok.Data.DiasTotal);
        Assert.Contains(ok.Data.Fontes, f => f.Contains("Lei 12.506/2011"));
        Assert.Contains(ok.Data.Fontes, f => f.Contains("art. 488"));
        Assert.Contains(ok.Data.Fontes, f => f.Contains("Nota Técnica MTE"));

        var invertida = await s.AvisoPrevioAsync(TenantR88,
            new EsocialAvisoPrevioCommand(new DateOnly(2025, 1, 15), new DateOnly(2024, 1, 15)), ct);
        Assert.Equal(400, invertida.StatusCode);
        Assert.NotNull(invertida.Errors);
    }

    // ---------------------------------------------------------------------
    // Fato 9 - eventos em prévia: round-trip, validações e isolamento por tenant.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Eventos_em_previa_roundtrip_validacoes_e_isolamento_entre_tenants()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var s = Svc();
        var ct = CancellationToken.None;

        var ev1 = await s.DeclararEventoAsync(TenantR88, UsuarioR88,
            new EsocialEventoDeclararCommand(ColaboradorA, "S-1200", "2026-10", new DateOnly(2026, 10, 20), "folha de outubro"), ct);
        Assert.True(ev1.Success);
        Assert.Equal("PREVIEW", ev1.Data!.FormaEnvio);
        Assert.Contains("Remuneração", ev1.Data.NomeEvento);
        Assert.Equal("PERIODICO", ev1.Data.Categoria);
        Assert.Equal("R88 Colaborador A", ev1.Data.ColaboradorNome);

        var institucional = await s.DeclararEventoAsync(TenantR88, UsuarioR88,
            new EsocialEventoDeclararCommand(null, "S-1000", null, new DateOnly(2026, 10, 2), null), ct);
        Assert.True(institucional.Success);
        Assert.Null(institucional.Data!.ColaboradorNome);
        Assert.Equal("TABELA_EMPREGADOR", institucional.Data.Categoria);

        var codigoDesconhecido = await s.DeclararEventoAsync(TenantR88, UsuarioR88,
            new EsocialEventoDeclararCommand(null, "S-9999", null, new DateOnly(2026, 10, 2), null), ct);
        Assert.Equal(400, codigoDesconhecido.StatusCode);
        var periodicoSemPeriodo = await s.DeclararEventoAsync(TenantR88, UsuarioR88,
            new EsocialEventoDeclararCommand(ColaboradorA, "S-1200", null, new DateOnly(2026, 10, 2), null), ct);
        Assert.Equal(400, periodicoSemPeriodo.StatusCode);
        var colabEstranho = await s.DeclararEventoAsync(TenantR88, UsuarioR88,
            new EsocialEventoDeclararCommand(ColaboradorAusente, "S-1010", null, new DateOnly(2026, 10, 2), null), ct);
        Assert.Equal(404, colabEstranho.StatusCode);

        var lista = await s.EventosAsync(TenantR88, null, null, ct);
        Assert.True(lista.Success);
        Assert.Equal(2, lista.Data!.Count);

        var porColaborador = await s.EventosAsync(TenantR88, ColaboradorA, null, ct);
        Assert.Single(porColaborador.Data!);
        Assert.Equal("S-1200", porColaborador.Data![0].CodigoEvento);
        Assert.Equal("2026-10", porColaborador.Data![0].PeriodoApuracao);
        Assert.Equal("R88 Colaborador A", porColaborador.Data![0].ColaboradorNome);

        var porPeriodo = await s.EventosAsync(TenantR88, null, "2026-10", ct);
        Assert.Single(porPeriodo.Data!);

        // isolamento: evento do outro tenant não aparece aqui.
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(@"insert into plantaopro.colaborador_eventos_esocial (tenant_id, colaborador_id, codigo_evento, data_fato, forma_envio)
values (@t2, null, 'S-1005', '2026-10-05'::date, 'PREVIEW')", new { t2 = TenantOutro });
        var doOutro = await s.EventosAsync(TenantOutro, null, null, ct);
        Assert.Single(doOutro.Data!);
        Assert.Equal("S-1005", doOutro.Data![0].CodigoEvento);
        var listaDeNovo = await s.EventosAsync(TenantR88, null, null, ct);
        Assert.Equal(2, listaDeNovo.Data!.Count);

        var del = await s.ExcluirEventoAsync(TenantR88, ev1.Data.Id, ct);
        Assert.True(del.Success);
        var restante = await s.EventosAsync(TenantR88, null, null, ct);
        Assert.Single(restante.Data!);
        Assert.Equal("S-1000", restante.Data![0].CodigoEvento);
        var delDeNovo = await s.ExcluirEventoAsync(TenantR88, ev1.Data.Id, ct);
        Assert.Equal(404, delDeNovo.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Fato 10 - listagem de colaboradores carrega o contexto trabalhista.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Colaboradores_listagem_carrega_contrato_vigente_carga_salario_e_admissao()
    {
        await SementePronta;
        var s = Svc();
        var ct = CancellationToken.None;

        var lista = await s.ColaboradoresAsync(TenantR88, ct);
        Assert.True(lista.Success);
        var a = lista.Data!.First(c => c.Id == ColaboradorA);
        Assert.Equal("R88-0001", a.Matricula);
        Assert.True(a.ContratoVigente);
        Assert.Equal("CLT", a.TipoContrato);
        Assert.Equal(2200m, a.SalarioVigente);
        Assert.Equal((short)44, a.CargaHorariaSemanal);
        Assert.Equal(new DateOnly(2020, 3, 2), a.AdmissaoPrimeiroContrato);

        var b = lista.Data!.First(c => c.Id == ColaboradorB);
        Assert.False(b.ContratoVigente);
        Assert.Null(b.TipoContrato);
        Assert.Null(b.AdmissaoPrimeiroContrato);

        var outroTenant = await s.ColaboradoresAsync(TenantOutro, ct);
        Assert.Empty(outroTenant.Data!);
    }

    // ---------------------------------------------------------------------
    // Sementes transacionais com locks consultivos (padrão da casa).
    // ---------------------------------------------------------------------
    private const string PurgeSql = @"
delete from plantaopro.colaborador_jornadas_semanais where tenant_id in (@t, @t2);
delete from plantaopro.colaborador_eventos_esocial where tenant_id in (@t, @t2);
delete from plantaopro.adm_contratos_trabalho where id in (@ct);
delete from plantaopro.adm_colaboradores where id in (@c1, @c2);
delete from plantaopro.adm_cargos where id in (@cg);
delete from plantaopro.adm_departamentos where id in (@dep);
delete from plantaopro.tenants where id in (@t, @t2);";

    private const string InsertSql = @"
insert into plantaopro.tenants (id) values
  (@t), (@t2);
insert into plantaopro.adm_departamentos (id, tenant_id, codigo, nome) values
  (@dep, @t, 'R88-DEP', 'R88 Departamento Teste');
insert into plantaopro.adm_cargos (id, tenant_id, codigo, nome, departamento_id) values
  (@cg, @t, 'R88-CGO', 'R88 Cargo Teste', @dep);
insert into plantaopro.adm_colaboradores (id, tenant_id, matricula, nome, cpf, email, cargo_id) values
  (@c1, @t, 'R88-0001', 'R88 Colaborador A', '11122233344', 'r88colab1@exemplo.com.br', @cg),
  (@c2, @t, 'R88-0002', 'R88 Colaborador B', '55566677788', 'r88colab2@exemplo.com.br', @cg);
insert into plantaopro.adm_contratos_trabalho (id, tenant_id, colaborador_id, tipo, inicio, salario, carga_horaria_semanal) values
  (@ct, @t, @c1, 'CLT', @inicio, 2200.00, 44);";

    private static object SeedParams() => new
    {
        t = TenantR88,
        t2 = TenantOutro,
        dep = DepartamentoR88,
        cg = CargoR88,
        c1 = ColaboradorA,
        c2 = ColaboradorB,
        ct = ContratoA,
        inicio = new DateTime(2020, 3, 2)
    };

    private static async Task SementarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r88_esocial_trabalho_seed'));");
        try
        {
            await using var tx = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync(PurgeSql, SeedParams(), tx);
            await cn.ExecuteAsync(InsertSql, SeedParams(), tx);
            await tx.CommitAsync();
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r88_esocial_trabalho_seed'));"); }
    }

    private static async Task PurgarAsync()
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r88_esocial_trabalho_seed'));");
            try
            {
                await cn.ExecuteAsync(PurgeSql, SeedParams());
            }
            finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r88_esocial_trabalho_seed'));"); }
        });
    }
}
