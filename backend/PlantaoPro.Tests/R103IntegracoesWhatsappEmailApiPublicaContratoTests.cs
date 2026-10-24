using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using PlantaoPro.Tests.Infrastructure;

namespace PlantaoPro.Tests;

/// <summary>
/// R103 (integrações: WhatsApp opt-in, e-mail transacional e API pública com chaves): a linha
/// 98 do plano fechada num pacote único. (1) Mensageria — caixa de saída única
/// (mensagens_saida) com opt-in por paciente seguindo o consentimento LGPD
/// (consentimento_lgpd + canal), provider default LOCAL e gatilho transacional no check-in.
/// (2) API pública com chaves — esquema ApiChave por header X-Api-Key sobre o hash SHA256 em
/// api_chaves, escopo por rota, janelas de rate limit (1 min / 1 dia) sobre a trilha real em
/// api_chaves_usos e leitura sempre no escopo da instituição da chave (cliente_id). (3) Web —
/// tela de preferências renderiza o outbox dos canais externos no servidor com degradação
/// graciosa. Contrato travado: (1) o drill E2E API+Web com RESULTADO=PASSEI; (2) o dump t1 da
/// resposta da situacao pública; (3) os fragmentos de código da API, do site e do B2B; (4) a
/// migração v2380 + o registro no manifest.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R103IntegracoesWhatsappEmailApiPublicaContratoTests
{
    private const string PastaR103 = "docs/evidencias/2026-10-24-rodada-103";
    private const string ArquivoSaida = "r103a_drill_saida.txt";
    private const string ArquivoT1 = "r103a_t1_publico_situacao.json";
    private const string VersaoMigracao = "2026_10_v2380_r103_integracoes_whatsapp_email_api_publica";
    private const string ChecksumV2380Lf = "d5f010023096d8c5d9aa4dd59505c1f953c157ee1dafb6a82dbf42709609e897";

    // =========================================================================
    // Fatos do drill (execucao canonica contra o banco vivo, API + Web no ar)
    // =========================================================================

    // Fato 1 - o drill inteiro: migracao v2380 aplicada (caixa de saida + trilha de chaves +
    // colunas de limite/ultimo_uso_em), plano drill com modulo de integracoes, fixtures clinicas,
    // opt-in LGPD gravando canal + auditoria, envio manual nos tres desfechos
    // (ENVIADA/SEM_DESTINO/BLOQUEADA_SEM_CONSENTIMENTO), chave pp_live_* com hash SHA256
    // conferido no banco, 401 inexistente, identidade + situacao (dump t1 AGENDADO) + paciente
    // por escopo, 403 sem escopo, 429 pela janela de minuto com limite restaurado, trilha=3 +
    // logs portal=3 + revogada 401, check-in via fluxo real do kernel (confirmar e depois
    // checkin) com gatilho notificando os dois canais com opt-in e a tela web renderizando o
    // outbox (resumo enviado=4).
    [Fact]
    public void Drill_canonico_executa_a_rodada_inteira()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR103 + "/" + ArquivoSaida));
        Assert.StartsWith("R103A_DRILL ", linhas[0]);
        Assert.Equal("PORTAS_LIVRES_INICIO=SIM", ObterLinha(linhas, "PORTAS_LIVRES_INICIO="));

        Assert.StartsWith("MIGRACAO_OK v2380 ", ObterLinha(linhas, "MIGRACAO_OK "));
        var mig = Parsear(ObterLinha(linhas, "MIGRACAO_OK "));
        Assert.Equal("SIM", mig["mensagens_saida"]);
        Assert.Equal("SIM", mig["api_chaves_usos"]);
        Assert.Equal("3", mig["rate_limit_cols"]);

        var plano = Parsear(ObterLinha(linhas, "PLANO_OK "));
        Assert.Equal("100", plano["mensal"]);
        Assert.Equal("1188", plano["anual"]);
        Assert.Equal("SIM", plano["integracoes"]);

        var modulos = ObterLinha(linhas, "REG_MODULO_OK ");
        Assert.Contains("saude360=CONTRATO", modulos);
        Assert.Contains("notificacoes=CONTRATO", modulos);
        Assert.StartsWith("REG_PERMISSAO_OK NOTIFICACOES.VER", ObterLinha(linhas, "REG_PERMISSAO_OK "));

        var fix = Parsear(ObterLinha(linhas, "REG_FIXTURE_OK "));
        foreach (var campo in new[] { "medico", "pac1", "pac2", "pac3", "agendamento" })
            Assert.Matches(GuidRegex, fix[campo]);

        var optin = Parsear(ObterLinha(linhas, "OPTIN_OK "));
        Assert.Equal("EMAIL,WHATSAPP", optin["canal_banco"]);
        Assert.Equal("2", optin["auditoria"]);

        var msg = Parsear(ObterLinha(linhas, "MSG_OK "));
        Assert.Equal("2", msg["enviada"]);
        Assert.Equal("1", msg["sem_destino"]);
        Assert.Equal("1", msg["bloqueada_sem_consentimento"]);
        Assert.Equal("LOCAL", msg["provider"]);
        Assert.Equal("4", msg["auditoria"]);

        var criar = Parsear(ObterLinha(linhas, "CHAVE_CRIAR_OK "));
        Assert.Matches(GuidRegex, criar["id"]);
        Assert.Equal("OK", criar["hash_sha256"]);
        Assert.Equal("120min/10000dia", criar["limites"]);

        var r401 = Parsear(ObterLinha(linhas, "CHAVE_401_OK "));
        Assert.Equal("http=401", r401["chave_inexistente"]);

        var idt = Parsear(ObterLinha(linhas, "CHAVE_IDENTIDADE_OK "));
        Assert.Equal("confere", idt["chaveId"]);
        Assert.Equal("2", idt["escopos"]);

        var t1 = Parsear(ObterLinha(linhas, "T1_DUMP_OK "));
        Assert.Equal(ArquivoT1, t1["arquivo"]);
        Assert.Equal("AGENDADO", t1["status"]);

        Assert.Contains("leitura_por_escopo_pacientes_read", ObterLinha(linhas, "CHAVE_PACIENTE_OK "));

        var r403 = Parsear(ObterLinha(linhas, "CHAVE_403_OK "));
        Assert.Equal("403", r403["sem_escopo_agendamentos_http"]);

        var r429 = Parsear(ObterLinha(linhas, "CHAVE_429_OK "));
        Assert.Equal("429", r429["janela_de_minuto_excedida_http"]);
        Assert.Equal("120", r429["limite_restaurado"]);

        var usos = Parsear(ObterLinha(linhas, "CHAVE_USOS_OK "));
        Assert.Equal("3", usos["trilha"]);
        Assert.Equal("3", usos["logs_portal"]);
        Assert.Equal("SIM", usos["revogada_401"]);

        var chk = Parsear(ObterLinha(linhas, "CHECKIN_OK "));
        Assert.Equal("200", chk["http"]);
        Assert.Equal("CHECKIN_REALIZADO", chk["status"]);
        Assert.Equal("1", chk["registro"]);

        var trig = Parsear(ObterLinha(linhas, "CHECKIN_TRIGGER_OK "));
        Assert.Equal("2", trig["enviadas"]);
        Assert.Equal("SIM", trig["destinos_confirmados"]);

        var web = Parsear(ObterLinha(linhas, "WEB_OUTBOX_OK "));
        Assert.Equal("4", web["secao+tabela+linhas+resumo_enviado"]);

        Assert.Equal("PORTAS_LIVRES_FIM=SIM", ObterLinha(linhas, "PORTAS_LIVRES_FIM="));
        Assert.Equal("RESULTADO=PASSEI", ObterLinha(linhas, "RESULTADO"));
    }

    // Fato 2 - o dump t1 (resposta canonica de GET /api/publico/agendamentos/{id}/situacao
    // autenticada por X-Api-Key): sucesso com 200 e a leitura ja escopada pela instituicao da
    // chave (o agendamento e do mundo do drill, com o nome do paciente resolvido).
    [Fact]
    public void Drill_dump_t1_situacao_publica_leva_a_leitura_da_chave()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Caminho(PastaR103 + "/" + ArquivoT1)));
        var raiz = doc.RootElement;
        Assert.True(raiz.GetProperty("success").GetBoolean());
        Assert.Equal(200, raiz.GetProperty("statusCode").GetInt32());
        var dados = raiz.GetProperty("data");
        Assert.Matches(GuidRegex, dados.GetProperty("agendamentoId").GetString());
        Assert.Equal("AGENDADO", dados.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(dados.GetProperty("dataInicio").GetString()));
        Assert.Contains("R103", dados.GetProperty("pacienteNome").GetString() ?? string.Empty);
    }

    // =========================================================================
    // Fragmentos de codigo (contrato estatico)
    // =========================================================================

    // Fato 3 - autenticacao por chave no esquema ApiChave: header X-Api-Key obrigatorio para o
    // esquema participar (sem ele, NoResult e o JWT segue), hash SHA256 hex em minusculas sobre
    // a api_key_hash, chave ATIVA + reg_status A e instituicao nao suspensa (join tenants),
    // claims chave_id/chave_nome/tenant_id/cliente_id/limite_minuto/limite_dia/escopo e
    // atualizacao best effort de ultimo_uso_em. Registro do esquema no Program ao lado do JWT.
    [Fact]
    public void Codigo_api_autentica_por_chave_sha256_com_instituicao_nao_suspensa()
    {
        var handler = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Security/ApiKeyAuthenticationHandler.cs"));
        Assert.Contains("public const string SchemeName = \"ApiChave\";", handler);
        Assert.Contains("public const string HeaderName = \"X-Api-Key\";", handler);
        Assert.Contains("Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave))).ToLowerInvariant()", handler);
        Assert.Contains("left join plantaopro.tenants t on t.id=c.tenant_id and t.reg_status='A'", handler);
        Assert.Contains("where c.reg_status='A' and upper(c.status)='ATIVA' and c.api_key_hash=@hash", handler);
        Assert.Contains("\"Chave de API não reconhecida ou revogada.\"", handler);
        Assert.Contains("\"Instituição suspensa. Regularize a assinatura para continuar.\"", handler);
        Assert.Contains("new Claim(\"cliente_id\", linha.ClienteId.Value.ToString())", handler);
        Assert.Contains("update plantaopro.api_chaves set ultimo_uso_em=now() where id=@id", handler);

        var program = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Program.cs"));
        Assert.Contains(".AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, o => { });", program);
    }

    // Fato 4 - filtro EscopoChave das rotas publicas: 401 sem principal do esquema, 403 sem o
    // escopo exigido pela rota, 429 pelas janelas de 1 minuto / 1 dia contadas sobre a trilha
    // api_chaves_usos (limites vindos dos claims); apos o action, trilha best effort
    // (metodo, rota, status HTTP, IP) que alimenta os logs do portal do desenvolvedor. As rotas
    // declaram o escopo: agendamentos:read, pacientes:read e identidade sem escopo.
    [Fact]
    public void Codigo_api_filtra_escopo_rate_limit_e_registra_trilha()
    {
        var handler = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Security/ApiKeyAuthenticationHandler.cs"));
        Assert.Contains("public sealed class EscopoChaveAttribute : Attribute, IAsyncActionFilter", handler);
        Assert.Contains("\"Chave de API ausente ou inválida.\", 401", handler);
        Assert.Contains("$\"A chave não possui o escopo {_escopo}.\", 403", handler);
        Assert.Contains("reg_date > now() - interval '1 minute'", handler);
        Assert.Contains("reg_date > now() - interval '1 day'", handler);
        Assert.Contains("\"Limite de requisições da chave excedido.\", 429", handler);
        Assert.Contains("insert into plantaopro.api_chaves_usos(id, chave_id, tenant_id, cliente_id, metodo, rota, status_http, ip_origem, reg_date, reg_status)", handler);

        var ctrl = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/IntegracoesControllers.cs"));
        Assert.Contains("[Authorize(AuthenticationSchemes = Security.ApiKeyAuthenticationHandler.SchemeName)]", ctrl);
        Assert.Contains("[Route(\"api/publico\")]", ctrl);
        Assert.Contains("[EscopoChave(\"agendamentos:read\")]", ctrl);
        Assert.Contains("[EscopoChave(\"pacientes:read\")]", ctrl);
        Assert.Contains("[EscopoChave(\"\")]", ctrl);
    }

    // Fato 5 - mensageria: canais WHATSAPP/EMAIL validados, provider efetivo LOCAL por padrao
    // (config Integracoes:MensageriaProvider entra sem DDL), opt-in gravando consentimento_lgpd
    // + canal + data de consentimento com auditoria MENSAGERIA_OPTIN, envio com os tres
    // desfechos de status (SEM_DESTINO sem telefone/e-mail, BLOQUEADA_SEM_CONSENTIMENTO sem
    // opt-in, ENVIADA com provider), outbox/resumo escopados pelo contexto clinico e auditoria
    // MENSAGENS_SAIDA. Audiencia clinica SAUDE360 + gate de modulo; DI registrada.
    [Fact]
    public void Codigo_api_mensageria_optin_caixa_de_saida_e_provedor_local()
    {
        var svc = File.ReadAllText(Caminho("backend/PlantaoPro.Api/IntegracoesServices.cs"));
        Assert.Contains("private static readonly string[] CanaisPermitidos = { \"WHATSAPP\", \"EMAIL\" };", svc);
        Assert.Contains("string.IsNullOrWhiteSpace(valor) ? \"LOCAL\" : valor.Trim().ToUpperInvariant();", svc);
        Assert.Contains("\"BLOQUEADA_SEM_CONSENTIMENTO\"", svc);
        Assert.Contains("\"SEM_DESTINO\"", svc);
        Assert.Contains("consentimento_lgpd_canal=@canais", svc);
        Assert.Contains("\"MENSAGERIA_OPTIN\"", svc);
        Assert.Contains("\"MENSAGENS_SAIDA\"", svc);

        var ctrl = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/IntegracoesControllers.cs"));
        Assert.Contains("[Authorize(Roles = RolesConstants.Saude360Assistencial)]", ctrl);
        Assert.Contains("[Saude360Module]", ctrl);
        Assert.Contains("[Route(\"api/mensageria\")]", ctrl);
        Assert.Contains("[HttpPost(\"opt-in\")]", ctrl);
        Assert.Contains("[HttpPost(\"enviar\")]", ctrl);
        Assert.Contains("[HttpGet(\"outbox\")]", ctrl);
        Assert.Contains("[HttpGet(\"resumo\")]", ctrl);

        var program = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Program.cs"));
        Assert.Contains("builder.Services.AddScoped<MensageriaService>();", program);
        Assert.Contains("builder.Services.AddScoped<IntegracoesPublicaService>();", program);
    }

    // Fato 6 - API publica: leitura somente, sempre no escopo da instituicao da chave (claim
    // cliente_id = clientes.id, a mesma semantica de escopo clinico do kernel) — agendamento e
    // paciente fora do escopo viram 404 como se nao existissem; chave sem vinculo vira 403;
    // paciente legado somente com nome cai no fallback nullif(nome_completo,'').
    [Fact]
    public void Codigo_api_publica_sempre_no_escopo_da_instituicao_da_chave()
    {
        var svc = File.ReadAllText(Caminho("backend/PlantaoPro.Api/IntegracoesServices.cs"));
        Assert.Contains("user.FindFirstValue(\"cliente_id\")", svc);
        Assert.Contains("\"Chave sem vínculo de instituição.\", 403", svc);
        Assert.Contains("coalesce(nullif(p.nome_completo,''), p.nome, '') as PacienteNome", svc);
        Assert.Contains("where a.reg_status='A' and a.id=@id and a.cliente_id=@tenantId", svc);
        Assert.Contains("where p.reg_status='A' and p.id=@id and p.cliente_id=@tenantId", svc);
        Assert.Contains("\"Agendamento não encontrado para esta chave.\", 404", svc);
        Assert.Contains("\"Paciente não encontrado para esta chave.\", 404", svc);

        var ctrl = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/IntegracoesControllers.cs"));
        Assert.Contains("[HttpGet(\"identidade\")]", ctrl);
        Assert.Contains("[HttpGet(\"agendamentos/{id:guid}/situacao\")]", ctrl);
        Assert.Contains("[HttpGet(\"pacientes/{id:guid}\")]", ctrl);
    }

    // Fato 7 - gatilho transacional do check-in: apos o audit da acao (e so no checkin do
    // agendamento), melhor esforco — falha de mensageria nunca quebra a acao clinica — le o
    // paciente por projecao tipada (o acesso dinamico do Dapper e case-sensitive e o Postgres
    // retorna aliases em minusculas), respeita o consentimento LGPD + canais e grava uma
    // mensagem por canal com opt-in na caixa de saida (CHECKIN_REALIZADO, assunto em EMAIL).
    [Fact]
    public void Codigo_api_checkin_notifica_os_canais_com_optin_best_effort()
    {
        var svc = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Saude360ClinicalService.cs"));
        Assert.Contains("string.Equals(acao, \"checkin\", StringComparison.OrdinalIgnoreCase)", svc);
        Assert.Contains("TentarNotificarCheckInAsync(cn, id)", svc);
        Assert.Contains("Notificacao de check-in falhou (best effort)", svc);
        Assert.Contains("private sealed record CheckInPacienteLinha(string Nome, string Telefone, string Email, bool Consentimento, string Canais);", svc);
        Assert.Contains("coalesce(p.consentimento_lgpd,false) as Consentimento", svc);
        Assert.Contains("if (linha is null || !linha.Consentimento) return;", svc);
        Assert.Contains("'CHECKIN_REALIZADO'", svc);
        Assert.Contains("\"Check-in realizado no PlantãoPro\"", svc);
        Assert.Contains("insert into plantaopro.mensagens_saida", svc);
    }

    // Fato 8 - logs do portal do desenvolvedor: ListarApiKeyLogsAsync parou de responder lista
    // vazia e agora le a trilha real de api_chaves_usos, escopada pelo tenant do contexto,
    // mais recentes primeiro, teto de 100 por chave.
    [Fact]
    public void Codigo_api_b2b_logs_leram_a_trilha_real_da_chave()
    {
        var src = File.ReadAllText(Caminho("backend/PlantaoPro.Api/B2BLaunchServices.cs"));
        var pos = src.IndexOf("ListarApiKeyLogsAsync(Guid id)", StringComparison.Ordinal);
        Assert.True(pos >= 0, "ListarApiKeyLogsAsync ausente.");
        var corpo = src.Substring(pos);
        Assert.Contains("from plantaopro.api_chaves_usos", corpo);
        Assert.Contains("where reg_status='A' and chave_id=@id and tenant_id=@tenantId", corpo);
        Assert.Contains("order by reg_date desc limit 100", corpo);
    }

    // Fato 9 - web: Preferencias busca o outbox (limite 30) + resumo no servidor com
    // degradacao graciosa (falha de comunicacao loga aviso e a tela renderiza o modelo vazio),
    // a view tem a secao do outbox com os data-attributes canonicos e o resumo de enviados, e as
    // 12 chaves Integrations.* estao presentes com paridade nos quatro resx.
    [Fact]
    public void Codigo_web_renderiza_o_outbox_com_degradacao_graciosa()
    {
        var ctrl = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Controllers/NotificacoesController.cs"));
        Assert.Contains("api/mensageria/outbox?limite=30", ctrl);
        Assert.Contains("api/mensageria/resumo", ctrl);
        Assert.Contains("modelo.Resumo.Enviadas = LerNumero(resumo, \"enviadas\");", ctrl);
        Assert.Contains("Falha ao carregar o outbox de mensageria na tela de preferencias", ctrl);

        var view = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Notificacoes/Preferencias.cshtml"));
        Assert.Contains("data-mensageria-outbox", view);
        Assert.Contains("data-mensageria-tabela", view);
        Assert.Contains("data-resumo-sent>@Model.Resumo.Enviadas", view);

        var vm = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Models/MensageriaViewModels.cs"));
        Assert.Contains("MensageriaPreferenciasViewModel", vm);

        foreach (var resx in new[]
        {
            "backend/PlantaoPro.Web/Localization/UiTexts.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.en.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.es.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.fr.resx",
        })
        {
            var txt = File.ReadAllText(Caminho(resx));
            var total = txt.Split("name=\"Integrations.", StringSplitOptions.None).Length - 1;
            Assert.Equal(12, total);
        }
    }

    // Fato 10 - migracao v2380 + manifest: caixa de saida com constraints de canal/status,
    // trilha de uso de chaves, colunas de limite da chave e ultimo_uso_em (correcao de estado:
    // o insert B2B em api_rate_limits e o update da autenticacao falhavam no banco vivo), tudo
    // idempotente; registro no manifest com checksum LF, dependencia da v2379 e transacional,
    // mantendo as 150 entradas.
    [Fact]
    public void Migracao_v2380_e_manifest_carregam_o_contrato()
    {
        var mig = File.ReadAllText(Caminho("database/migrations/" + VersaoMigracao + ".sql"));
        Assert.Contains("create table if not exists plantaopro.mensagens_saida", mig);
        Assert.Contains("constraint ck_mensagens_saida_canal check (canal in ('WHATSAPP', 'EMAIL'))", mig);
        Assert.Contains("constraint ck_mensagens_saida_status check (status in ('ENVIADA', 'FALHOU', 'BLOQUEADA_SEM_CONSENTIMENTO', 'SEM_DESTINO'))", mig);
        Assert.Contains("create table if not exists plantaopro.api_chaves_usos", mig);
        Assert.Contains("alter table plantaopro.api_rate_limits add column if not exists api_chave_id uuid null", mig);
        Assert.Contains("alter table plantaopro.api_rate_limits add column if not exists limite_minuto integer not null default 120", mig);
        Assert.Contains("alter table plantaopro.api_rate_limits add column if not exists limite_dia integer not null default 10000", mig);
        Assert.Contains("alter table plantaopro.api_chaves add column if not exists ultimo_uso_em timestamptz null", mig);
        Assert.Contains("ix_api_rate_limits_chave", mig);

        using var doc = JsonDocument.Parse(File.ReadAllText(Caminho("database/migration-manifest.json"), System.Text.Encoding.UTF8));
        var entradas = doc.RootElement.GetProperty("migrations").EnumerateArray().ToList();
        Assert.Equal(150, entradas.Count);
        var v = entradas.FirstOrDefault(e => e.GetProperty("version").GetString() == VersaoMigracao);
        Assert.NotEqual(default, v);
        Assert.Equal("database/migrations/" + VersaoMigracao + ".sql", v.GetProperty("source").GetString());
        Assert.Equal(ChecksumV2380Lf, v.GetProperty("checksum").GetString());
        Assert.Equal("active", v.GetProperty("status").GetString());
        Assert.True(v.GetProperty("transactional").GetBoolean());
        Assert.Contains("2026_10_v2379_r102_regulacao_triagem_integridade_adm360", v.GetProperty("dependsOn").EnumerateArray().Select(e => e.GetString()).ToList());
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private const string GuidRegex = "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    private static string Caminho(string relativo) =>
        Path.Combine(RepositoryPathResolver.ResolveRoot(), relativo.Replace('/', Path.DirectorySeparatorChar));

    private static string ObterLinha(string[] linhas, string prefixo)
    {
        var linha = linhas.FirstOrDefault(l => l.StartsWith(prefixo, StringComparison.Ordinal));
        Assert.NotNull(linha);
        return linha!;
    }

    /// <summary>Parsa linha chave=valor separada por espacos (formato maquina-legivel do harness).</summary>
    private static System.Collections.Generic.Dictionary<string, string> Parsear(string linha)
    {
        var d = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var token in linha.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
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
