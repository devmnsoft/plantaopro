using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Infrastructure.Legislacao;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R100 (notificações pautadas no calendário fiscal + SLAs + escalonamento): o calendário
/// fiscal R84 vira pauta da caixa de notificações dos gestores — avisos antecipados para
/// obrigacoes com vencimento dentro da janela oficial, que vencem hoje ou atrasadas, com
/// nível de SLA derivado do estado (CRITICO/ALTA/MEDIA) e escalonamento ao dono da
/// organização quando o atraso atinge o limiar operacional da fonte única (v2378). Idempotente
/// pela chave canônica da caixa (usuario+origem+título), origem determinística por
/// tenant|obrigação|período|variante, e porta de entrada só com ADM360 efetivo no tenant.
/// Contrato travado: (1) o drill E2E API+Web com RESULTADO=PASSEI; (2) o dump t1 do banco
/// antes do gatilho explicito; (3) os fragmentos de código + as funções puras de SLA;
/// (4) o serviço em nível de banco com dois mundos determinísticos (com e sem ADM360).
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R100NotificacoesCalendarioFiscalContratoTests
{
    private const string PastaR100 = "docs/evidencias/2026-10-23-rodada-100";
    private const string ArquivoSaida = "r100a_drill_saida.txt";
    private const string ArquivoT1 = "r100a_t1_alertas_banco.txt";
    private const string VersaoMigracao = "2026_10_v2378_r100_notificacoes_calendario_fiscal";
    private const string ChecksumV2378Lf = "251866a1d02fe88f90b5aa0a80d010589f66b2288a145688a35043e4e9ff9f38";

    // =========================================================================
    // Fatos do drill (execucao canonica contra o banco vivo, API + Web no ar)
    // =========================================================================

    // Fato 1 - a execucao canonica ponta a ponta: dois mundos TRIAL pelo funil, ADM360 por
    // governanca real (habilitar-tenant), leitura lazy do calendario pautando a caixa,
    // gatilho explicito idempotente, caixa listando base+escalonados, periodo fora da janela
    // sem novas, conclusao sem duplicata, controle sem modulo e as superficies web do dono.
    [Fact]
    public void Drill_saida_marcadores_completos_e_resultado_passei()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR100 + "/" + ArquivoSaida));
        Assert.StartsWith("R100A_DRILL data=", linhas[0]);
        Assert.DoesNotContain(linhas, l => l.StartsWith("FALHA ", StringComparison.Ordinal));

        Assert.Equal("PORTAS_LIVRES_INICIO=SIM", ObterLinha(linhas, "PORTAS_LIVRES_INICIO"));
        var api = Parsear(ObterLinha(linhas, "ESTAGEM_1_API_NO_AR "));
        Assert.Equal("SIM", api["ok"]);
        Assert.True(int.TryParse(api.GetOrThrow("pid"), out var pidApi) && pidApi > 0, $"pid da API ausente: {api.GetOrThrow("pid")}.");
        var web = Parsear(ObterLinha(linhas, "ESTAGEM_1_WEB_NO_AR "));
        Assert.Equal("SIM", web["ok"]);
        Assert.True(int.TryParse(web.GetOrThrow("pid"), out var pidWeb) && pidWeb > 0, $"pid da Web ausente: {web.GetOrThrow("pid")}.");

        Assert.Equal("PRE_LIMPEZA_RERUN ok=SIM", ObterLinha(linhas, "PRE_LIMPEZA_RERUN"));

        var plano = Parsear(ObterLinha(linhas, "PA_PLANO_OK "));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", plano["plano"]);
        Assert.Equal("0", plano["vinculos"]);

        var mundos = Parsear(ObterLinha(linhas, "PA_MUNDOS_OK "));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", mundos["adm_tenant"]);
        Assert.Equal("TRIAL", mundos["adm_status"]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", mundos["ctrl_tenant"]);
        Assert.Equal("TRIAL", mundos["ctrl_status"]);

        Assert.StartsWith("PA_ADM360_OK ", ObterLinha(linhas, "PA_ADM360_OK "));
        Assert.Contains("origem=CONTRATO", ObterLinha(linhas, "PA_ADM360_OK "));
        Assert.Contains("historico=1", ObterLinha(linhas, "PA_ADM360_OK "));
        Assert.Contains("efetivos=[ADM360]", ObterLinha(linhas, "PA_ADM360_OK "));

        Assert.StartsWith("CALENDARIO_PAUTA_OK ", ObterLinha(linhas, "CALENDARIO_PAUTA_OK "));
        var gerar = Parsear(ObterLinha(linhas, "GERAR_IDEMPOTENTE_OK "));
        Assert.Equal("0", gerar["criadas"]);
        Assert.Equal("5", gerar["alertas"]);
        Assert.Equal("8", gerar["total_mantido"]);

        var caixa = Parsear(ObterLinha(linhas, "CAIXA_OK "));
        Assert.Equal("8", caixa["fiscais"]);
        Assert.Equal("SIM", caixa["escalonado"]);
        Assert.Equal("SIM", caixa["base"]);

        var nov = Parsear(ObterLinha(linhas, "NOVEMBRO_AGENDADO_OK "));
        Assert.Equal("5", nov["agendadas"]);
        Assert.Equal("0", nov["criadas"]);
        Assert.Equal("8", nov["total_mantido"]);

        var conc = Parsear(ObterLinha(linhas, "CONCLUIDA_OK "));
        Assert.Equal("4", conc["alertas_apos"]);
        Assert.Equal("8", conc["total_mantido"]);

        var ctrl = Parsear(ObterLinha(linhas, "CONTROLE_OK "));
        Assert.Equal("0", ctrl["criadas"]);
        Assert.Equal("5", ctrl["alertas"]);
        Assert.Equal("0", ctrl["total"]);

        var webCal = Parsear(ObterLinha(linhas, "WEB_CALENDARIO_OK "));
        Assert.Equal("200", webCal["http"]);
        Assert.Equal("SIM", webCal["cartao"]);
        Assert.Equal("SIM", webCal["escalonado"]);
        Assert.Equal("SIM", webCal["critico"]);

        Assert.StartsWith("WEB_NOTIFICACOES_OK http=200", ObterLinha(linhas, "WEB_NOTIFICACOES_OK "));
        Assert.Equal("PORTAS_LIVRES=SIM", ObterLinha(linhas, "PORTAS_LIVRES="));
        Assert.Equal("RESULTADO=PASSEI", ObterLinha(linhas, "RESULTADO"));
    }

    // Fato 2 - o dump t1 ANTES do gatilho explicito: a leitura lazy do calendario ja pautou a
    // caixa do mundo A (5 avisos base + 3 escalonados para atraso >= limiar da fonte unica,
    // todos nao lidos, origens deterministicas distintas) e o mundo B sem ADM360 seguiu a zero.
    [Fact]
    public void Drill_dump_t1_pauta_da_leitura_lazy_cobre_base_e_escalonamento()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR100 + "/" + ArquivoT1));
        Assert.True(linhas.Length == 2, $"esperadas duas linhas de dump (mundoA/mundoB): {linhas.Length}.");

        var mA = Parsear(linhas[0]);
        Assert.StartsWith("T1_MUNDO_A", linhas[0]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", mA["tenant"]);
        Assert.Equal("[ADM360]", mA["efetivos"]);
        Assert.Equal("5", mA["bases"]);
        Assert.Equal("3", mA["escal"]);
        Assert.Equal("8", mA["total"]);
        Assert.Equal("8", mA["lida_falsa"]);
        Assert.Equal("8", mA["dist_id"]);

        var mB = Parsear(linhas[1]);
        Assert.StartsWith("T1_MUNDO_B", linhas[1]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", mB["tenant"]);
        Assert.Equal("[vazio]", mB["efetivos"]);
        Assert.Equal("0", mB["bases"]);
        Assert.Equal("0", mB["escal"]);
        Assert.Equal("0", mB["total"]);
    }

    // =========================================================================
    // Fragmentos de codigo + funcoes puras (contrato estatico)
    // =========================================================================

    // Fato 3 - o contrato da rodada esta no codigo: SLA derivado do estado do reminder,
    // escalonamento pelo limiar da fonte unica (nenhum numero no codigo), origem
    // deterministica, porta ADM360, perfis gestores, dedupe canonica da caixa, rotas de API
    // (alertas/gerar + hook lazy no calendario), registro de dependencia, proxy web, view com
    // o cartao server-side e a migracao v2378 registrada no manifest.
    [Fact]
    public void Codigo_carrega_o_contrato_da_pauta_fiscal()
    {
        var svc = File.ReadAllText(Caminho("backend/PlantaoPro.Api/CalendarioFiscalAlertasService.cs"));
        Assert.Contains("\"OBRIGACOES_FISCAIS_ESCALONAMENTO_ATRASO_DIAS\"", svc);
        Assert.Contains("\"ATRASADA\" => \"CRITICO\"", svc);
        Assert.Contains("\"VENCE_HOJE\" => \"ALTA\"", svc);
        Assert.Contains("\"VENCE_EM_CURTO_PRAZO\" => \"MEDIA\"", svc);
        Assert.Contains("_ => \"NENHUM\"", svc);
        Assert.Contains("estado == \"ATRASADA\" && -diasRestantes >= limiarAtrasoDias", svc);
        Assert.Contains("private const string OrigemBase = \"OBRIGACAO_FISCAL\";", svc);
        Assert.Contains("private const string OrigemEscalonada = \"OBRIGACAO_FISCAL_ESCALONADA\";", svc);
        Assert.Contains("MD5.HashData", svc);
        Assert.Contains("upper(f.codigo)='ADM360'", svc);
        Assert.Contains("('ADMINISTRADOR_CLIENTE','ADMINISTRADOR')", svc);
        Assert.Contains("\"fiscal\", tx", svc);

        // Funcoes puras do SLA: mapa completo e fronteira exata do escalonamento (>= limiar).
        Assert.Equal("CRITICO", CalendarioFiscalAlertasService.NivelSla("ATRASADA"));
        Assert.Equal("ALTA", CalendarioFiscalAlertasService.NivelSla("VENCE_HOJE"));
        Assert.Equal("MEDIA", CalendarioFiscalAlertasService.NivelSla("VENCE_EM_CURTO_PRAZO"));
        Assert.Equal("NENHUM", CalendarioFiscalAlertasService.NivelSla("AGENDADA"));
        Assert.Equal("NENHUM", CalendarioFiscalAlertasService.NivelSla("CONCLUIDA"));
        Assert.Equal("NENHUM", CalendarioFiscalAlertasService.NivelSla("NAO_SE_APLICA"));
        Assert.True(CalendarioFiscalAlertasService.DeveEscalar("ATRASADA", -7, 7));
        Assert.False(CalendarioFiscalAlertasService.DeveEscalar("ATRASADA", -6, 7));
        Assert.False(CalendarioFiscalAlertasService.DeveEscalar("VENCE_HOJE", -7, 7));
        Assert.False(CalendarioFiscalAlertasService.DeveEscalar("ATRASADA", 0, 7));

        var ctrl = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/CalendarioFiscalController.cs"));
        Assert.Contains("[HttpGet(\"alertas\")]", ctrl);
        Assert.Contains("[HttpPost(\"notificacoes/gerar\")]", ctrl);
        Assert.Contains("try { await alertas.GerarAsync(Tenant(), mes, ano", ctrl);

        var prog = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Program.cs"));
        Assert.Contains("AddScoped<ICalendarioFiscalAlertasService>", prog);

        var ctrlWeb = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Controllers/CalendarioFiscalWebController.cs"));
        Assert.Contains("\"api/calendario-fiscal/alertas\"", ctrlWeb);
        Assert.Contains("CalendarioFiscalWebViewModel", ctrlWeb);

        var view = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/CalendarioFiscalWeb/Index.cshtml"));
        Assert.Contains("SLA e escalonamento", view);
        Assert.Contains("ESCALONADO", view);
        Assert.Contains("CRITICO", view);

        var mig = File.ReadAllText(Caminho("database/migrations/" + VersaoMigracao + ".sql"));
        Assert.Contains("'OBRIGACOES_FISCAIS_ESCALONAMENTO_ATRASO_DIAS'", mig);
        Assert.Contains("on conflict (codigo, ano) where reg_status = 'A' do nothing", mig);

        using var doc = JsonDocument.Parse(File.ReadAllText(Caminho("database/migration-manifest.json"), System.Text.Encoding.UTF8));
        var entradas = doc.RootElement.GetProperty("migrations").EnumerateArray().ToList();
        var v2378 = entradas.FirstOrDefault(e => e.GetProperty("version").GetString() == VersaoMigracao);
        Assert.NotEqual(default, v2378);
        Assert.Equal("database/migrations/" + VersaoMigracao + ".sql", v2378.GetProperty("source").GetString());
        Assert.Equal(ChecksumV2378Lf, v2378.GetProperty("checksum").GetString());
        Assert.Equal("active", v2378.GetProperty("status").GetString());
        Assert.Contains("2026_10_v2377_r98_ai_resumo_pendente", v2378.GetProperty("dependsOn").EnumerateArray().Select(e => e.GetString()).ToList());
    }

    // =========================================================================
    // Servico em nivel de banco (dois mundos deterministicos, crash-idempotentes)
    // =========================================================================

    // Fato 4 - o núcleo em nível de serviço com hoje fixado em 23/10/2026: no periodo exibido
    // 10/2026 as cinco obrigacoes mensais estao ATRASADA (vencimentos 07/15/15/20/20); tres
    // delas superam o limiar de escalonamento (atrasos 16/8/8 vs 7) e valem aviso proprio ao
    // dono. Reexecucao cria zero (dedupe canonico + origem deterministica); o periodo exibido
    // seguinte esta todo AGENDADA (fora da janela) e nao pauta nada; o mundo sem ADM360 tem a
    // visao de SLA mas a caixa nao recebe.
    [Fact]
    public async Task Servico_pauta_alertas_idempotentes_e_escalona_por_limiar()
    {
        await SementePronta;
        var svc = NovoSelfService();
        var pauta = NovoPauta();
        var hoje = new DateOnly(2026, 10, 23);

        var cnpjAdm = "99991010000199";
        var cnpjCtrl = "99991010000210";
        try
        {
            var plano = await PlanoDrillAsync("r100_teste_plano");
            var rAdm = await svc.FinalizarCadastroAsync(Pedido(cnpjAdm, "r100-pauta-adm@exemplo.test", plano), null, null);
            Assert.True(rAdm.Success, rAdm.Message);
            var idsAdm = rAdm.Data!;
            var rCtrl = await svc.FinalizarCadastroAsync(Pedido(cnpjCtrl, "r100-pauta-ctrl@exemplo.test", plano), null, null);
            Assert.True(rCtrl.Success, rCtrl.Message);
            var idsCtrl = rCtrl.Data!;

            await ConcederAdm360Async(idsAdm.TenantId);

            var res1 = await pauta.GerarAsync(idsAdm.TenantId, 10, 2026, hoje);
            Assert.Equal(7, res1.JanelaAlertaDias);
            Assert.Equal(7, res1.LimiarEscalonamentoDias);
            Assert.Equal(5, res1.Alertas.Count);
            Assert.All(res1.Alertas, a => Assert.Equal("ATRASADA", a.Estado));
            Assert.All(res1.Alertas, a => Assert.Equal("CRITICO", a.SlaNivel));
            Assert.Equal(3, res1.Alertas.Count(a => a.Escalonada));
            Assert.Equal(8, res1.NotificacoesCriadas);

            Assert.Equal(5, await ConsultarAsync<int>(
                "select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=@t and n.reg_status='A' and n.origem_tipo='OBRIGACAO_FISCAL'", new { t = idsAdm.TenantId }));
            Assert.Equal(3, await ConsultarAsync<int>(
                "select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=@t and n.reg_status='A' and n.origem_tipo='OBRIGACAO_FISCAL_ESCALONADA'", new { t = idsAdm.TenantId }));
            Assert.Equal(8, await ConsultarAsync<int>(
                "select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=@t and n.reg_status='A' and n.tipo='fiscal' and n.lida=false", new { t = idsAdm.TenantId }));

            // Idempotencia + origem deterministica: reexecucao cria zero e mantem 8 origens distintas.
            var res2 = await pauta.GerarAsync(idsAdm.TenantId, 10, 2026, hoje);
            Assert.Equal(0, res2.NotificacoesCriadas);
            Assert.Equal(8, await ConsultarAsync<int>(
                "select count(distinct n.origem_id) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=@t and n.reg_status='A' and n.origem_tipo like 'OBRIGACAO_FISCAL%'", new { t = idsAdm.TenantId }));

            // Periodo exibido seguinte: tudo AGENDADA fora da janela -> nenhuma pauta nova.
            var resNov = await pauta.GerarAsync(idsAdm.TenantId, 11, 2026, hoje);
            Assert.Empty(resNov.Alertas);
            Assert.Equal(0, resNov.NotificacoesCriadas);

            // Mundo sem ADM360: a visao de SLA existe, mas sem destinatarios a caixa nao recebe.
            var resC = await pauta.GerarAsync(idsCtrl.TenantId, 10, 2026, hoje);
            Assert.Equal(5, resC.Alertas.Count);
            Assert.Equal(3, resC.Alertas.Count(a => a.Escalonada));
            Assert.Equal(0, resC.NotificacoesCriadas);
            Assert.Equal(0, await ConsultarAsync<int>(
                "select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=@t and n.reg_status='A' and n.origem_tipo like 'OBRIGACAO_FISCAL%'", new { t = idsCtrl.TenantId }));
        }
        finally
        {
            await LimparPorCnpj(cnpjAdm);
            await LimparPorCnpj(cnpjCtrl);
            await RemoverPlanoPorCodigo("r100_teste_plano");
        }
    }

    // =========================================================================
    // Infraestrutura local
    // =========================================================================

    private readonly Task SementePronta = SementarAsync();

    private static async Task SementarAsync()
    {
        // crash-idempotencia (padrao R96/R99): remove tudo ligado aos CNPJs/planos propios antes.
        foreach (var cnpj in new[] { "99991010000199", "99991010000210" })
            await LimparPorCnpj(cnpj);
        await RemoverPlanoPorCodigo("r100_teste_plano");
    }

    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static SelfServiceSaasService NovoSelfService()
    {
        var cfg = BuildCfg();
        var audit = new R100FakeAuditService();
        return new SelfServiceSaasService(cfg, new TenantContextService(new HttpContextAccessor(), cfg, NullLogger<TenantContextService>.Instance),
            audit, new AssinaturaGuardService(cfg, audit, NullLogger<AssinaturaGuardService>.Instance),
            NullLogger<SelfServiceSaasService>.Instance,
            new CobrancaService(cfg, audit, Array.Empty<ICobrancaProvider>(), NullLogger<CobrancaService>.Instance));
    }

    private static CalendarioFiscalAlertasService NovoPauta()
    {
        var cnStr = TestDatabase.ConnectionString;
        var parametros = new ParametrosLegaisRepository(cnStr);
        var calendario = new CalendarioFiscalService(parametros, new ObrigacoesFiscaisRepository(cnStr));
        var caixa = new NotificacaoService(BuildCfg(), new R100FakeAuditService(), NullLogger<NotificacaoService>.Instance);
        return new CalendarioFiscalAlertasService(calendario, parametros, caixa, BuildCfg());
    }

    private static CadastroSelfServiceRequest Pedido(string cnpj, string email, Guid plano) => new()
    {
        Empresa = new CadastroEmpresaRequest
        {
            NomeFantasia = "R100 Pauta " + cnpj[..4],
            RazaoSocial = "R100 Pauta LTDA",
            Cnpj = cnpj,
            Cidade = "Recife",
            Uf = "PE",
            Telefone = "(81) 99999-0008",
            EmailCorporativo = "r100-emp@exemplo.test"
        },
        Plano = new CadastroPlanoRequest { PlanoId = plano, Periodicidade = "MENSAL", AceiteTermos = true, AceitePrivacidade = true, ConsentimentoLgpd = true },
        UsuarioAdmin = new CadastroUsuarioAdminRequest { Nome = "R100 Admin", Email = email, Senha = "R100senha123!" },
        StatusAssinatura = "TRIAL",
        DiasTrial = 5
    };

    /// <summary>Plano fixture deterministico sem vinculos — o ADM360 entra por governanca no tenant A.</summary>
    private static async Task<Guid> PlanoDrillAsync(string codigo)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync($@"
delete from plantaopro.plano_modulos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper('{codigo}'));
delete from plantaopro.planos where upper(codigo)=upper('{codigo}');
insert into plantaopro.planos(id,nome,codigo,valor_mensal,valor_anual,status,reg_status,criado_em,reg_date,dados,
 limite_medicos,limite_hospitais,limite_plantoes_mes,limite_usuarios,limite_convites_mes,
 permite_mobile,permite_bi,permite_relatorios_avancados,permite_integracoes,permite_operacao_assistida,permite_suporte_prioritario,
 permite_api,ordem_exibicao,possui_mobile,possui_bi,possui_relatorios_avancados,permite_trial,dias_trial,metadata,permite_relatorios)
values(gen_random_uuid(),'R100 Teste {codigo}','{codigo}',100.00,1250.00,'ATIVO','A',now(),now(),'{{}}'::jsonb,
 0,0,0,0,0,false,false,false,false,false,false,false,957,false,false,false,true,5,'{{}}'::jsonb,false);");
        return await cn.ExecuteScalarAsync<Guid>($@"select id from plantaopro.planos where upper(codigo)=upper('{codigo}')");
    }

    /// <summary>Contrato ADM360 no tenant no mesmo shape da governanca habilitar-tenant (CONTRATO/HABILITAR).</summary>
    private static async Task ConcederAdm360Async(Guid tenantId)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync(@"
insert into plantaopro.tenant_modulos(id,tenant_id,modulo_id,codigo_modulo,codigo,nome,habilitado,origem,status,preco_contratado,limite_contratado,ativado_em,desativado_em,reg_status,reg_date,created_by)
select gen_random_uuid(),@t,m.id,m.codigo,m.codigo,m.nome,true,'CONTRATO','ATIVO',0,0,now(),null,'A',now(),null
from plantaopro.modulos_sistema m where upper(m.codigo)='ADM360' and m.reg_status='A'
on conflict (tenant_id,modulo_id) where reg_status='A' and modulo_id is not null do update set habilitado=true,reg_update=now();
insert into plantaopro.tenant_modulos_historico(tenant_modulo_id,tenant_id,modulo_id,acao,antes,depois,usuario_id,ip_origem)
select tm.id,@t,tm.modulo_id,'HABILITAR',null,'{}'::jsonb,null,null from plantaopro.tenant_modulos tm where tm.tenant_id=@t and tm.reg_status='A' and upper(tm.codigo_modulo)='ADM360'", new { t = tenantId });
    }

    private static async Task RemoverPlanoPorCodigo(string codigo)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync($@"
delete from plantaopro.plano_recursos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper('{codigo}'));
delete from plantaopro.plano_faq where plano_id in (select id from plantaopro.planos where upper(codigo)=upper('{codigo}'));
delete from plantaopro.plano_modulos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper('{codigo}'));
delete from plantaopro.planos where upper(codigo)=upper('{codigo}');");
    }

    private static async Task<T> ConsultarAsync<T>(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    private static async Task LimparPorCnpj(string cnpj)
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.ExecuteAsync(@"
delete from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id in (select a.id from plantaopro.assinaturas a join plantaopro.clientes c on c.id=a.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.lgpd_consentimentos where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_onboarding_checklist where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_onboarding where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.adm_obrigacoes_fiscais where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.notificacoes where usuario_id in (select u.id from plantaopro.usuarios u join plantaopro.clientes c on c.id=u.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_modulos_historico where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_modulos where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_white_label where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.perfil_permissoes where perfil_id in (select p.id from plantaopro.perfis p join plantaopro.tenants t on t.id=p.tenant_id join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.perfis where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.usuarios_perfis where usuario_id in (select u.id from plantaopro.usuarios u join plantaopro.clientes c on c.id=u.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.usuarios where id in (select u.id from plantaopro.usuarios u join plantaopro.clientes c on c.id=u.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenants where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.assinaturas where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj;
delete from plantaopro.cadastro_cliente_solicitacoes where regexp_replace(cnpj,'\D','','g')=@cnpj;",
                new { cnpj });
        });
    }

    private sealed class R100FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao,
            object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;

        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao,
            string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string Caminho(string relativo) =>
        Path.Combine(RepositoryPathResolver.ResolveRoot(), relativo.Replace('/', Path.DirectorySeparatorChar));

    private static string ObterLinha(string[] linhas, string prefixo)
    {
        var linha = linhas.FirstOrDefault(l => l.StartsWith(prefixo, StringComparison.Ordinal));
        Assert.NotNull(linha);
        return linha!;
    }

    /// <summary>Parsa linha chave=valor separada por espacos (formato maquina-legivel do harness).</summary>
    private static Dictionary<string, string> Parsear(string linha)
    {
        var d = new Dictionary<string, string>();
        foreach (var token in linha.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = token.IndexOf('=');
            if (i > 0)
            {
                d[token.Substring(0, i)] = token.Substring(i + 1);
            }
        }
        return d;
    }
}
// GetOrThrow: reutiliza DicionarioExtensaoR96 (mesmo namespace; duas extensoes iguais em
// arquivos distintos do mesmo namespace viram CS0121 em toda a suite).
