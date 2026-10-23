using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R96 (B6 onboarding self-service): o funil público (POST /api/public/cadastro/finalizar)
/// agora aceita statusAssinatura=TRIAL com diasTrial explícito (1..90): a assinatura nasce
/// TRIAL sem cobrança inicial e com data_fim = fim do teste + 30 dias de janela de conversao
/// (mesmo núcleo do provisionamento manual B2B). Status ausente preserva o comportamento
/// B5 (ATIVA com cobrança inicial). Achados documentados nesta rodada: (1) o login do admin
/// provisionado pelo self-service falhava com 401 porque o insert em usuarios nao gravava
/// tenant_id (LoginAsync resolve roles por user.TenantId ?? ClienteId; correção no núcleo);
/// (2) datas do trial eram gravadas em fuso UTC virando um dia a menos na coluna `date` do
/// banco America/Sao_Paulo (troca para o calendario local). Contrato travado: (1) o drill
/// E2E com RESULTADO=PASSEI nos marcadores maquina-legiveis; (2) os dumps de banco do trial
/// (mundo completo, sem cobrança) e da ATIVA legado (cobrança ABERTA visível em faturas);
/// (3) o DTO expõe os novos campos e MinhaAssinatura expõe DataTrialFim; (4) a rota pública
/// anônima + as validações do núcleo + o invariant de login do admin; (5) a migração v2375
/// ativa no manifest com checksum coerente e guarda idempotente; (6) o wizard web oferece o
/// teste grátis como opção com dias 1..90 e a tela de sucesso mostra as datas reais.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R96OnboardingSelfServiceContratoTests
{
    private const string PastaR96 = "docs/evidencias/2026-10-23-rodada-96";
    private const string ArquivoSaida = "r96a_drill_saida.txt";
    private const string ArquivoT1 = "r96a_t1_trial_banco.txt";
    private const string ArquivoT3 = "r96a_t3_ativa_banco.txt";

    // =========================================================================
    // Fatos do drill (execucao canonica contra o banco vivo)
    // =========================================================================

    // Fato 1 - a execucao canonica do drill E2E: API standalone no ar, catalogo publico com
    // o FAQ do teste gratis (v2375), TRIAL 14d com mundo completo sem cobrança inicial,
    // login do dono funcional (invariant que a correção de tenant_id garante), duplicata
    // 400 honesta, validações 400 sem resíduo e ATIVA legado com cobrança visível.
    [Fact]
    public void Drill_saida_marcadores_completos_e_resultado_passei()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR96 + "/" + ArquivoSaida));
        Assert.StartsWith("R96A_DRILL data=", linhas[0]);
        Assert.DoesNotContain(linhas, l => l.StartsWith("FALHA ", StringComparison.Ordinal));

        Assert.Equal("PORTAS_LIVRES_INICIO=SIM", ObterLinha(linhas, "PORTAS_LIVRES_INICIO"));
        var estagio1 = Parsear(ObterLinha(linhas, "ESTAGEM_1_API_NO_AR "));
        Assert.Equal("SIM", estagio1["ok"]);
        Assert.True(int.TryParse(estagio1.GetOrThrow("pid"), out var pid) && pid > 0,
            $"pid da API ausente: {estagio1.GetOrThrow("pid")}.");
        var estagio2 = Parsear(ObterLinha(linhas, "ESTAGEM_2_CATALOGO_OK "));
        Assert.Equal("4", estagio2["planos"]);
        Assert.Equal("SIM", estagio2["faq_trial"]);
        Assert.Equal("PRE_LIMPEZA_RERUN ok=SIM", ObterLinha(linhas, "PRE_LIMPEZA_RERUN"));

        var criado = Parsear(ObterLinha(linhas, "PA_TRIAL_CRIADO "));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", criado["tenant"]);
        var trial = Parsear(ObterLinha(linhas, "PA_TRIAL_OK "));
        Assert.Equal("TRIAL", trial["status"]);
        Assert.Equal("SIM", trial["trial_fim_ok"]);
        Assert.Equal("SIM", trial["fim_conv_ok"]);
        Assert.Equal("0", trial["cobranca_inicial"]);
        Assert.Equal("consentimento", trial["lgpd"]);
        Assert.Equal("EM_ANDAMENTO", trial["onboarding"]);
        Assert.True(int.Parse(trial["checklist"]) >= 1, "checklist de onboarding vazio.");

        var funcional = Parsear(ObterLinha(linhas, "FUNCIONAL_API_OK "));
        Assert.Equal("SIM", funcional["login_dono"]);
        Assert.Equal("200", funcional["onboarding_http"]);
        Assert.Equal("200", funcional["minha_assinatura_http"]);
        Assert.Equal("TRIAL", funcional["minha_status"]);

        var duplicata = Parsear(ObterLinha(linhas, "PA_DUPLICATA_OK "));
        Assert.Equal("400", duplicata["http"]);
        Assert.Equal("SIM", duplicata["msg_cnpj"]);
        Assert.Equal("0", duplicata["residuo"]);
        var validacao = Parsear(ObterLinha(linhas, "PA_VALIDACAO_OK "));
        Assert.Equal("4", validacao["casos"]);
        Assert.Equal("400", validacao["http_todos"]);
        Assert.Equal("0", validacao["residuos"]);

        var ativa = Parsear(ObterLinha(linhas, "PA_ATIVA_OK "));
        Assert.Equal("ATIVA", ativa["status"]);
        Assert.Equal("SIM", ativa["trial_fim_nulo"]);
        Assert.Equal("SIM", ativa["fim_1mes"]);
        Assert.Equal("ABERTO", ativa["cobranca"]);
        Assert.Equal("1", ativa["faturas_api"]);
        Assert.Equal("SIM", ativa["login_dono"]);

        Assert.Equal("PORTAS_LIVRES=SIM", ObterLinha(linhas, "PORTAS_LIVRES="));
        Assert.Equal("RESULTADO=PASSEI", ObterLinha(linhas, "RESULTADO"));
    }

    // Fato 2 - o dump do banco do mundo TRIAL: assinatura TRIAL com trial_fim = inicio + 14d,
    // data_fim = trial_fim + 30d (janela de conversao), ZERO linhas de cobrança inicial,
    // LGPD com base legal de consentimento e onboarding EM_ANDAMENTO com checklist materializado.
    [Fact]
    public void Drill_dump_trial_mundo_completo_sem_cobranca_inicial()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR96 + "/" + ArquivoT1));
        Assert.True(linhas.Length == 1, $"esperada uma unica linha de dump: {linhas.Length}.");
        var d = Parsear(linhas[0]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", d["assinatura_id"]);
        Assert.Equal("TRIAL", d["status"]);

        var inicio = DateOnly.Parse(d["data_inicio"], System.Globalization.CultureInfo.InvariantCulture);
        var trialFim = DateOnly.Parse(d["data_trial_fim"], System.Globalization.CultureInfo.InvariantCulture);
        var fim = DateOnly.Parse(d["data_fim"], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(trialFim == inicio.AddDays(14),
            $"trial_fim deve ser inicio+14d: {inicio} -> {trialFim}.");
        Assert.True(fim == trialFim.AddDays(30),
            $"data_fim deve ser trial_fim+30d: {trialFim} -> {fim}.");

        Assert.Equal("0", d["cobranca_inicial_linhas"]);
        Assert.Equal("consentimento", d["lgpd_base_legal"]);
        Assert.Equal("true", d["aceito"]);
        Assert.Equal("EM_ANDAMENTO", d["onboarding_status"]);
        Assert.True(int.Parse(d["checklist_linhas"]) >= 1, "checklist de onboarding vazio.");
    }

    // Fato 3 - o dump da rota legada B5 (status ausente): assinatura ATIVA sem trial_fim e a
    // cobrança inicial ABERTA visível em minha-assinatura/faturas (compatibilidade B5 intacta).
    [Fact]
    public void Drill_dump_ativa_legado_b5_com_cobranca_inicial_aberta()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR96 + "/" + ArquivoT3));
        Assert.True(linhas.Length == 1, $"esperada uma unica linha de dump: {linhas.Length}.");
        var d = Parsear(linhas[0]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", d["assinatura_id"]);
        Assert.Equal("ATIVA", d["status"]);
        Assert.Equal("null", d["data_trial_fim"]);
        Assert.Matches("^\\d{4}-\\d{2}-\\d{2}$", d["data_fim"]);
        Assert.Equal("ABERTO", d["cobranca_inicial_status"]);
        Assert.Equal("1", d["faturas_api_total"]);
        Assert.Equal("1", d["faturas_api_cadastro"]);
    }

    // =========================================================================
    // Fatos de código
    // =========================================================================

    // Fato 4 - o contrato do wire: o DTO público expõe StatusAssinatura (string?, ausente =
    // ATIVA legado) e DiasTrial (int?, explícita para TRIAL); MinhaAssinatura expõe a nova
    // DataTrialFim para o card de "fim do teste gratis" do tenant.
    [Fact]
    public void Dto_expo_statusassinatura_e_dias_trial_e_minha_assinatura_expo_data_trial_fim()
    {
        var pStatus = typeof(CadastroSelfServiceRequest).GetProperty("StatusAssinatura");
        Assert.NotNull(pStatus);
        Assert.Equal(typeof(string), pStatus!.PropertyType);
        var pDias = typeof(CadastroSelfServiceRequest).GetProperty("DiasTrial");
        Assert.NotNull(pDias);
        Assert.Equal(typeof(int?), pDias!.PropertyType);
        var pTrialFim = typeof(MinhaAssinaturaDto).GetProperty("DataTrialFim");
        Assert.NotNull(pTrialFim);
        Assert.Equal(typeof(DateTime?), pTrialFim!.PropertyType);
    }

    // Fato 5 - a rota de finalizacao segue pública/anônima e o núcleo trava as mensagens e o
    // alcance exatos do trial; e o insert do admin provisionado agora grava tenant_id
    // (invariant exigido pelo LoginAsync: sem isso o admin nasce com 401 no primeiro login —
    // defeito latente descoberto pelo drill e corrigido nesta rodada).
    [Fact]
    public void Rota_publica_anonima_validacoes_do_nucleo_e_admin_provisionado_logavel()
    {
        var ctrl = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/SelfServiceSaasController.cs"));
        foreach (var fragmento in new[]
        {
            "[AllowAnonymous]",
            "[Route(\"api/public\")]",
            "[HttpPost(\"cadastro/finalizar\")]",
        })
        {
            Assert.Contains(fragmento, ctrl);
        }

        var svc = File.ReadAllText(Caminho("backend/PlantaoPro.Api/SelfServiceServices.cs"));
        foreach (var fragmento in new[]
        {
            "Informe os dias de teste (entre 1 e 90)",
            "!request.DiasTrial.HasValue || request.DiasTrial.Value is < 1 or > 90",
            "Status da assinatura deve ser TRIAL ou ATIVA",
            // invariant de login: usuarios.grava tenant_id (chave de resolucao de roles).
            "preferencias_notificacao,cliente_id,tenant_id,status",
            "values(@usuarioId,@Nome,@Email,upper(@Email),@Telefone,@SenhaHash,false,'{}',@clienteId,@tenantId,'ATIVO','A',now())",
            // calendario local (fuso do banco/operacao do tenant), nunca UtcNow.Date.
            "DateTime.Today.AddDays(request.DiasTrial.Value)",
        })
        {
            Assert.Contains(fragmento, svc);
        }
    }

    // Fato 6 - a migração v2375 está ativa no manifest dependendo de v2374, o checksum casa
    // com o arquivo (mesma lógica CRLF->LF do runner) e o seed é idempotente (not-exists por
    // pergunta) com as três FAQ públicas do teste grátis.
    [Fact]
    public void Migracao_v2375_ativa_no_manifest_checksum_coerente_e_guarda_idempotente()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "database", "migration-manifest.json")));
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => string.Equals(m.GetProperty("version").GetString(),
                "2026_10_v2375_r96_onboarding_selfservice_trial", StringComparison.Ordinal));
        Assert.True(entrada.ValueKind == JsonValueKind.Object, "entrada v2375 ausente do manifest.");
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.Contains("2026_10_v2374_r93_erros_negocio",
            entrada.GetProperty("dependsOn").EnumerateArray().Select(e => e.GetString()).ToList());

        var source = entrada.GetProperty("source").GetString()!;
        var caminho = Path.Combine(root, source.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(caminho), $"arquivo ausente: {source}.");
        var bruto = File.ReadAllBytes(caminho);
        var lf = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(bruto).Replace("\r\n", "\n"));
        var esperado = entrada.GetProperty("checksum").GetString()!;
        Assert.True(
            string.Equals(Sha256Hex(lf), esperado, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Sha256Hex(bruto), esperado, StringComparison.OrdinalIgnoreCase),
            $"checksum dessincronizado do arquivo v2375: {esperado}.");

        var sql = File.ReadAllText(caminho);
        foreach (var fragmento in new[]
        {
            "insert into plantaopro.plano_faq",
            "not exists",
            "Como funciona o teste grátis?",
            "Quando começo a pagar no teste grátis?",
            "O que acontece quando o teste termina?",
        })
        {
            Assert.Contains(fragmento, sql);
        }
    }

    // Fato 7 - o wizard web oferece as duas opções de contrato (teste grátis como decisão de
    // produto e assinatura imediata), o campo de dias é 1..90 com show/hide, a tela de
    // sucesso lê o resumo do contrato em TempData e mostra as datas reais (calendário local,
    // igual ao gravado no banco), e a minha-assinatura web expõe o fim do teste.
    [Fact]
    public void Wizard_web_teste_gratis_e_sucesso_com_datas_reais()
    {
        var wizard = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Cadastro/Cadastro.cshtml"));
        foreach (var fragmento in new[]
        {
            "asp-for=\"TipoContrato\"",
            "value=\"TRIAL\"",
            "value=\"ATIVA\"",
            "data-trial-dias",
            "min=\"1\" max=\"90\"",
        })
        {
            Assert.Contains(fragmento, wizard);
        }

        var sucesso = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Cadastro/Sucesso.cshtml"));
        Assert.Contains("TempData[\"ContratoResumo\"]", sucesso);
        Assert.Contains("StartsWith(\"trial|\"", sucesso);
        Assert.Contains("Teste grátis até:", sucesso);

        var ctrlWeb = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Controllers/PublicSelfServiceWebControllers.cs"));
        Assert.Contains("\"trial|{DateTime.Today", ctrlWeb);

        var minha = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Controllers/MinhaAssinaturaController.cs"));
        Assert.Contains("DataTrialFim", minha);
    }

    // =========================================================================
    // Fatos de serviço (núcleo contra o banco vivo)
    // =========================================================================

    // Fato 8 - o serviço público cria o mundo TRIAL inteiro: assinatura TRIAL com trial_fim
    // exato no calendário local (+diasTrial) e data_fim = trial_fim + 30d, sem cobrança
    // inicial, onboarding EM_ANDAMENTO, hash verificável e o invariant de login: o admin
    // nasce com tenant_id alinhado ao link de perfil (sem isso o LoginAsync não resolve role).
    [Fact]
    public async Task Servico_publico_trial_mundo_completo_sem_cobranca_e_admin_logavel()
    {
        await SementePronta;
        var svc = NovoSelfService();
        var cnpj = "96964455000123";
        var email = "r96-s8-admin@exemplo.test";
        var plano = await PlanoDestaque();

        var r = await svc.FinalizarCadastroAsync(Pedido(cnpj, email, plano, "TRIAL", 14), null, null);
        Assert.True(r.Success, r.Message);
        var ids = r.Data!;
        try
        {
            Assert.Equal("TRIAL", await ConsultarAsync<string>(
                "select status from plantaopro.assinaturas where id=@id", new { id = ids.AssinaturaId }));
            var datas = await ConsultarAsync<DatasAssinatura>(
                "select data_inicio as \"DataInicio\", data_trial_fim as \"DataTrialFim\", data_fim as \"DataFim\" from plantaopro.assinaturas where id=@id",
                new { id = ids.AssinaturaId });
            Assert.True(datas.DataTrialFim == DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
                $"trial_fim fora do calendario local esperado: {datas.DataTrialFim}.");
            Assert.True(datas.DataFim == DateOnly.FromDateTime(DateTime.Today.AddDays(44)),
                $"data_fim deve ser hoje+44d (trial 14 + janela 30): {datas.DataFim}.");
            Assert.Equal(0, await ConsultarAsync<int>(
                "select count(*) from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id=@id",
                new { id = ids.AssinaturaId }));
            Assert.Equal("EM_ANDAMENTO", await ConsultarAsync<string>(
                "select status from plantaopro.tenant_onboarding where tenant_id=@id", new { id = ids.TenantId }));
            Assert.True(await ConsultarAsync<bool>(
                "select exists(select 1 from plantaopro.lgpd_consentimentos where tenant_id=@id)",
                new { id = ids.TenantId }));

            var hash = await ConsultarAsync<string>(
                "select senha_hash from plantaopro.usuarios where id=@id", new { id = ids.UsuarioAdminId });
            Assert.True(PlantaoPro.Api.Security.PasswordHashService.Verify("R96senha123!", hash));

            // invariant de login (defeito latente corrigido nesta rodada): o usuario grava
            // tenant_id e ele alinha com os links usuarios_perfis/perfis — exatamente a chave
            // que o filtro de roles do LoginAsync compara (user.TenantId ?? ClienteId).
            var tenantDoUsuario = await ConsultarAsync<Guid?>("select tenant_id from plantaopro.usuarios where id=@id",
                new { id = ids.UsuarioAdminId });
            Assert.True(tenantDoUsuario.HasValue && tenantDoUsuario.Value == ids.TenantId,
                "admin provisionado sem tenant_id alinhado ao tenant (login nasceria 401).");
            Assert.Equal(0, await ConsultarAsync<int>(
                @"select count(*) from plantaopro.usuarios_perfis up
                  join plantaopro.perfis p on p.id = up.perfil_id
                  where up.usuario_id = @id
                    and (up.tenant_id <> @t or p.tenant_id <> @t)",
                new { id = ids.UsuarioAdminId, t = ids.TenantId }));
        }
        finally
        {
            await LimparPorCnpj(cnpj);
        }
    }

    // Fato 9 - validações honestas sem resíduo: diasTrial ausente/0/91 pedem os dias (entre 1
    // e 90); status inválido pede TRIAL ou ATIVA; CNPJ duplicado após criação válida = 400.
    [Fact]
    public async Task Servico_publico_validacoes_dias_trial_status_invalido_e_cnpj_duplicado()
    {
        await SementePronta;
        var svc = NovoSelfService();
        var cnpj = "96966677000145";
        var email = "r96-s9-admin@exemplo.test";
        var plano = await PlanoDestaque();

        var ausente = await svc.FinalizarCadastroAsync(Pedido(cnpj, email, plano, "TRIAL", null), null, null);
        Assert.False(ausente.Success);
        Assert.Equal(400, ausente.StatusCode);
        Assert.Contains("dias de teste", ausente.Message);

        var zero = await svc.FinalizarCadastroAsync(Pedido(cnpj, email, plano, "TRIAL", 0), null, null);
        Assert.False(zero.Success);
        Assert.Equal(400, zero.StatusCode);
        Assert.Contains("dias de teste", zero.Message);

        var noventaEum = await svc.FinalizarCadastroAsync(Pedido(cnpj, email, plano, "TRIAL", 91), null, null);
        Assert.False(noventaEum.Success);
        Assert.Equal(400, noventaEum.StatusCode);
        Assert.Contains("dias de teste", noventaEum.Message);

        var piloto = await svc.FinalizarCadastroAsync(Pedido(cnpj, email, plano, "PILOTO", 30), null, null);
        Assert.False(piloto.Success);
        Assert.Equal(400, piloto.StatusCode);
        Assert.Contains("TRIAL ou ATIVA", piloto.Message);

        var ok = await svc.FinalizarCadastroAsync(Pedido(cnpj, email, plano, "TRIAL", 7), null, null);
        Assert.True(ok.Success, ok.Message);
        var duplicado = await svc.FinalizarCadastroAsync(Pedido(cnpj, email + "-2@exemplo.test", plano, null, null), null, null);
        Assert.False(duplicado.Success);
        Assert.Equal(400, duplicado.StatusCode);
        Assert.Contains("CNPJ", duplicado.Message);
        try
        {
            Assert.Equal(1, await ConsultarAsync<int>(
                "select count(*) from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')=@cnpj",
                new { cnpj }));
        }
        finally
        {
            await LimparPorCnpj(cnpj);
        }
    }

    // =========================================================================
    // Infraestrutura local
    // =========================================================================

    private readonly Task SementePronta = SementarAsync();

    private static async Task SementarAsync()
    {
        // crash-idempotência (padrão B5/r9k): remove tudo ligado aos CNPJs próprios antes.
        foreach (var cnpj in new[] { "96964455000123", "96966677000145" })
            await LimparPorCnpj(cnpj);
    }

    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static SelfServiceSaasService NovoSelfService()
    {
        var cfg = BuildCfg();
        var audit = new R96FakeAuditService();
        return new SelfServiceSaasService(cfg, new TenantContextService(new HttpContextAccessor(), cfg, NullLogger<TenantContextService>.Instance),
            audit, new AssinaturaGuardService(cfg, audit, NullLogger<AssinaturaGuardService>.Instance),
            NullLogger<SelfServiceSaasService>.Instance,
            new CobrancaService(cfg, audit, Array.Empty<ICobrancaProvider>(), NullLogger<CobrancaService>.Instance));
    }

    private static CadastroSelfServiceRequest Pedido(string cnpj, string email, Guid plano, string? status, int? dias) => new()
    {
        Empresa = new CadastroEmpresaRequest { NomeFantasia = "R96 Teste " + cnpj[..4], RazaoSocial = "R96 Teste LTDA", Cnpj = cnpj, Cidade = "Olinda", Uf = "PE", EmailCorporativo = "r96-emp@exemplo.test" },
        Plano = new CadastroPlanoRequest { PlanoId = plano, Periodicidade = "MENSAL", AceiteTermos = true, AceitePrivacidade = true, ConsentimentoLgpd = true },
        UsuarioAdmin = new CadastroUsuarioAdminRequest { Nome = "R96 Admin", Email = email, Senha = "R96senha123!" },
        StatusAssinatura = status,
        DiasTrial = dias
    };

    private static async Task<Guid> PlanoDestaque()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        var id = await cn.ExecuteScalarAsync<Guid?>(@"select id from plantaopro.planos
            where reg_status='A' and upper(coalesce(status,'ATIVO'))='ATIVO' and coalesce(publico,true)=true and destaque=true
            order by valor_mensal limit 1");
        if (id.HasValue) return id.Value;
        return await cn.ExecuteScalarAsync<Guid>(@"select id from plantaopro.planos
            where reg_status='A' and upper(coalesce(status,'ATIVO'))='ATIVO' and coalesce(publico,true)=true
            order by valor_mensal limit 1");
    }

    private static async Task<T> ConsultarAsync<T>(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    /// <summary>Campos `date` da assinatura para checagem no calendário local.</summary>
    private sealed class DatasAssinatura
    {
        public DateOnly DataInicio { get; set; }
        public DateOnly DataTrialFim { get; set; }
        public DateOnly DataFim { get; set; }
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
delete from plantaopro.tenant_white_label where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.usuarios_perfis where usuario_id in (select u.id from plantaopro.usuarios u join plantaopro.clientes c on c.id=u.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.perfis where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.usuarios where id in (select u.id from plantaopro.usuarios u join plantaopro.clientes c on c.id=u.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.assinaturas where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj;
delete from plantaopro.cadastro_cliente_solicitacoes where regexp_replace(cnpj,'\D','','g')=@cnpj;",
                new { cnpj });
        });
    }

    private sealed class R96FakeAuditService : IAuditService
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

    private static string Sha256Hex(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

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

internal static class DicionarioExtensaoR96
{
    public static string GetOrThrow(this Dictionary<string, string> d, string chave)
    {
        Assert.True(d.TryGetValue(chave, out var valor), $"chave ausente na linha do harness: {chave}.");
        return valor;
    }
}
