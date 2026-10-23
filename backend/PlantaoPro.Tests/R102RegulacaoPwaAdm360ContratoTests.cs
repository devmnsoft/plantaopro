using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using PlantaoPro.Tests.Infrastructure;

namespace PlantaoPro.Tests;

/// <summary>
/// R102 (experiência PWA de triagem/recepção + regulação do fluxo assistencial + pendências
/// Administrativos 360): três pacotes numa única rodada. (C) Regulação — "regularizar" a
/// triagem finaliza o registro com os dados tipados e registra a decisão de regulação no
/// encaminhamento (médico de destino opcional + orientação obrigatória), avançando o
/// agendamento para AGUARDANDO_CONSULTA quando o paciente já está no fluxo pós-check-in.
/// (A) Cobrança — gerar-mensal respeita o ciclo anual (só fatura no mês de renovação e nunca
/// após data_fim), trial vencido vira VENCIDA via expirar-vencidos (idempotente, com histórico
/// TRIAL_EXPIRADO) e alterar-plano recalcula o valor pela fonte única PrecosPlano reiniciando
/// o ciclo só para ATIVA. (B) PWA — service worker v2 network-first com fallback offline,
/// página offline em PT, botão de instalação via beforeinstallprompt e wiring completo no
/// _Layout das telas da triagem/recepção. (D) Integridade adm360 (v2379) — limpeza
/// determinística das órfãs herdadas da R95 e VALIDATE CONSTRAINT das duas FKs NOT VALID.
/// Contrato travado: (1) o drill E2E API+Web com RESULTADO=PASSEI; (2) o dump t1 da resposta
/// da API do regularizar; (3) os fragmentos de código da API e do site; (4) a migracao v2379
/// + o registro no manifest.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R102RegulacaoPwaAdm360ContratoTests
{
    private const string PastaR102 = "docs/evidencias/2026-10-23-rodada-102";
    private const string ArquivoSaida = "r102a_drill_saida.txt";
    private const string ArquivoT1 = "r102a_t1_regularizar_api.json";
    private const string VersaoMigracao = "2026_10_v2379_r102_regulacao_triagem_integridade_adm360";
    private const string ChecksumV2379Lf = "df3dd62b4561e3359e3902609a33cefaf001d2bae01e0b2277fa07f44b611e53";

    // =========================================================================
    // Fatos do drill (execucao canonica contra o banco vivo, API + Web no ar)
    // =========================================================================

    // Fato 1 - o drill inteiro: migracao v2379 aplicada e verificada no banco (colunas de
    // regulacao + FKs validadas + zero orfaas), mundo de cobranca com quatro assinaturas
    // (mensal/anual/fim-período/trial vencido), regulação da triagem (409 sem check-in, 400
    // sem orientação, 200 com efeitos completos + auditoria), gerar-mensal com ciclo anual
    // e gate de fim de periodo, expirar-vencidos idempotente, alterar-plano pela fonte unica,
    // ativos PWA servidos e botao Regularizar na tela autenticada da triagem.
    [Fact]
    public void Drill_canonico_executa_a_rodada_inteira()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR102 + "/" + ArquivoSaida));
        Assert.StartsWith("R102A_DRILL ", linhas[0]);
        Assert.Equal("PORTAS_LIVRES_INICIO=SIM", ObterLinha(linhas, "PORTAS_LIVRES_INICIO="));

        Assert.StartsWith("MIGRACAO_OK v2379 ", ObterLinha(linhas, "MIGRACAO_OK "));
        var mig = Parsear(ObterLinha(linhas, "MIGRACAO_OK "));
        Assert.Equal("2", mig["colunas_regulacao"]);
        Assert.Equal("t", mig["fk_validadas"]);
        Assert.Equal("0|0", mig["orfaas"]);

        var plano = Parsear(ObterLinha(linhas, "PLANO_OK "));
        Assert.Equal("100/250", plano["mensal"]);
        Assert.Equal("1188/2880", plano["anual"]);

        Assert.Contains("origem=CONTRATO", ObterLinha(linhas, "REG_MODULO_OK "));
        Assert.StartsWith("REG_PERMISSAO_OK TRIAGEM.EDITAR", ObterLinha(linhas, "REG_PERMISSAO_OK "));

        var r409 = Parsear(ObterLinha(linhas, "REG_409_OK "));
        Assert.Equal("409", r409["http"]);
        Assert.Equal("sem_check-in", r409["motivo"]);

        var r400 = Parsear(ObterLinha(linhas, "REG_400_OK "));
        Assert.Equal("400", r400["http"]);
        Assert.Equal("sem_orientacao", r400["motivo"]);

        var rs = Parsear(ObterLinha(linhas, "REG_SUCESSO_OK "));
        Assert.Equal("200", rs["http"]);
        Assert.Equal("AGUARDANDO_CONSULTA", rs["agendamento"]);
        Assert.Equal("SIM", rs["medico_destino"]);
        Assert.Equal("1", rs["encaminhamento"]);
        Assert.Equal("1", rs["historico"]);
        Assert.Equal("+1", rs["auditoria"]);

        var t1 = Parsear(ObterLinha(linhas, "T1_DUMP_OK "));
        Assert.Equal("regularizar", t1["acao"]);
        Assert.Equal("FINALIZADA", t1["triagem"]);

        var cfg = Parsear(ObterLinha(linhas, "BIL_CFG_OK "));
        Assert.Equal("ATIVA_MENSAL", cfg["M"]);
        Assert.Equal("ATIVA_ANUAL", cfg["A"]);
        Assert.Equal("ATIVA_FIM_2026_09", cfg["F"]);
        Assert.Equal("TRIAL_VENCIDO", cfg["T"]);

        var set = Parsear(ObterLinha(linhas, "GERAR_SET_OK "));
        Assert.Equal("1x100", set["M"]);
        Assert.Equal("1x1188", set["A"]);
        Assert.Equal("1x100", set["F"]);
        Assert.Equal("0", set["T"]);

        var fora = Parsear(ObterLinha(linhas, "GERAR_OUT_CYCLE_OK "));
        Assert.Equal("1x100", fora["M"]);
        Assert.Equal("0", fora["A"]);
        Assert.Equal("0", fora["F"]);

        var exp = ObterLinha(linhas, "TRIAL_EXPIRAR_OK ");
        Assert.Contains("primeira>=1", exp);
        var expd = Parsear(exp);
        Assert.Equal("VENCIDA", expd["status"]);
        Assert.Equal("1", expd["historico"]);
        Assert.Equal("0", expd["segunda"]);
        Assert.Equal("1", expd["expiradas_primeira"]);

        var alt = Parsear(ObterLinha(linhas, "ALTERAR_PLANO_OK "));
        Assert.Equal("250+ciclo_1m", alt["M"]);
        Assert.Equal("2880+ciclo_1y", alt["A"]);
        Assert.Equal("datas_preservadas", alt["TRIAL"]);
        Assert.Equal("400", alt["VENCIDA"]);

        Assert.StartsWith("PWA_ASSETS_OK sw=v2_network_first", ObterLinha(linhas, "PWA_ASSETS_OK "));
        var webEdit = Parsear(ObterLinha(linhas, "WEB_REG_EDIT_OK "));
        Assert.Equal("200", webEdit["http"]);
        Assert.Equal("Regularizar", webEdit["botao"]);
        Assert.Equal("/Triagem/Regularizar", webEdit["acao"]);
        Assert.StartsWith("PWA_LAYOUT_OK ", ObterLinha(linhas, "PWA_LAYOUT_OK "));
        Assert.Equal("PORTAS_LIVRES=SIM", ObterLinha(linhas, "PORTAS_LIVRES="));
        Assert.Equal("RESULTADO=PASSEI", ObterLinha(linhas, "RESULTADO"));
    }

    // Fato 2 - o dump t1 (resposta canonica da API do POST /api/triagens/{id}/regularizar):
    // sucesso com 200, triagem FINALIZADA com finalizada_por gravado e a orientacao da
    // regulacao registrada nas observacoes do registro.
    [Fact]
    public void Drill_dump_t1_regularizar_finaliza_a_triagem_com_orientacao()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Caminho(PastaR102 + "/" + ArquivoT1)));
        var raiz = doc.RootElement;
        Assert.True(raiz.GetProperty("success").GetBoolean());
        Assert.Equal(200, raiz.GetProperty("statusCode").GetInt32());
        var dados = raiz.GetProperty("data");
        Assert.Equal("FINALIZADA", dados.GetProperty("status").GetString());
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", dados.GetProperty("pacienteId").GetString());
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", dados.GetProperty("agendamentoId").GetString());
        var registro = dados.GetProperty("dados");
        Assert.Equal("FINALIZADA", registro.GetProperty("status").GetString());
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", registro.GetProperty("finalizada_por").GetString());
        Assert.StartsWith("Regulado para", registro.GetProperty("observacoes").GetString());
    }

    // =========================================================================
    // Fragmentos de codigo (contrato estatico)
    // =========================================================================

    // Fato 3 - regulação na API: rota /regularizar com a mesma validacao tipada do finalizar
    // + orientação obrigatória; precheck do agendamento no fluxo pós-check-in (senão 409);
    // status forçado FINALIZADA; efeito no agendamento (AGUARDANDO_CONSULTA + medico de
    // destino opcional); upsert do encaminhamento CONSULTA na unique parcial existente;
    // historico enriquecido com medicoDestino/orientacao.
    [Fact]
    public void Codigo_api_expoena_a_rota_e_regras_de_regulacao()
    {
        var ctrl = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs"));
        Assert.Contains("[HttpPost(\"{id:guid}/regularizar\")]", ctrl);
        Assert.Contains("ClinicalMeasurements.Validar(request, true)", ctrl);
        Assert.Contains("Regulação exige orientação registrada.", ctrl);
        Assert.Contains("AcaoAsync(\"triagens\", id, \"regularizar\"", ctrl);

        var svc = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Saude360ClinicalService.cs"));
        Assert.Contains("string.Equals(acao, \"regularizar\", StringComparison.OrdinalIgnoreCase)) status = \"FINALIZADA\";", svc);
        Assert.Contains("\"CHECKIN_REALIZADO\", \"EM_TRIAGEM\", \"AGUARDANDO_CONSULTA\"", svc);
        Assert.Contains("Regulação exige o paciente no fluxo de atendimento (após o check-in).", svc);
        Assert.Contains("set status='AGUARDANDO_CONSULTA', medico_id=case when @medicoId is not null then @medicoId else medico_id end", svc);
        Assert.Contains("'CONSULTA', 'ENCAMINHADA', @medicoId, @obs", svc);
        Assert.Contains("on conflict (cliente_id,triagem_id,destino) where destino='CONSULTA' and reg_status='A'", svc);
        Assert.Contains("medicoDestino = request.MedicoId, orientacao = request.Observacoes", svc);
    }

    // Fato 4 - cobrança mensal com ciclo anual: o gerar-mensal mantém o literal de ATIVA
    // (contrato R97/CobrancaB6 C7) e ganha os dois gates do ciclo — ANUAL fatura somente no
    // mês de renovação (mês do inicio) e nenhuma fatura nasce após data_fim (comparação por
    // ordinal mês/ano, sem depender de fuso).
    [Fact]
    public void Codigo_api_trava_o_ciclo_anual_do_gerar_mensal()
    {
        var src = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs"));
        var posGerar = src.IndexOf("[HttpPost(\"gerar-mensal\")]", StringComparison.Ordinal);
        Assert.True(posGerar >= 0, "rota gerar-mensal ausente.");
        var corpo = src.Substring(posGerar);
        Assert.Contains("upper(a.status)='ATIVA'", corpo);
        Assert.Contains("(upper(coalesce(a.periodicidade,'MENSAL')) <> 'ANUAL' or extract(month from a.data_inicio::date) = extract(month from @competencia::date))", corpo);
        Assert.Contains("a.data_fim is null or extract(year from a.data_fim::date) * 12 + extract(month from a.data_fim::date) >= extract(year from @competencia::date) * 12 + extract(month from @competencia::date)", corpo);
    }

    // Fato 5 - trial vencido e mudança de plano: expirar-vencidos marca VENCIDA (valor
    // permitido pelo check ck_assinaturas_status_saas), grava histórico TRIAL_EXPIRADO e é
    // idempotente (a segunda chamada seleciona zero); alterar-plano só aceita ATIVA/TRIAL,
    // recalcula valor_contratado pela fonte unica PrecosPlano e reinicia o ciclo só para ATIVA
    // (TRIAL mantém as datas do teste).
    [Fact]
    public void Codigo_api_expira_trials_vencidos_e_altera_plano_pela_fonte_unica()
    {
        var src = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs"));
        Assert.Contains("[HttpPost(\"trials/expirar-vencidos\")]", src);
        Assert.Contains("update plantaopro.assinaturas set status='VENCIDA', reg_update=now()", src);
        Assert.Contains("upper(status)='TRIAL' and data_trial_fim is not null and data_trial_fim::date < @hoje", src);
        Assert.Contains("'TRIAL_EXPIRADO'", src);
        Assert.Contains("Apenas assinaturas ativas ou em trial aceitam mudança de plano.", src);
        Assert.Contains("PrecosPlano.ValorContratado(precoDestino.ValorMensal, precoDestino.ValorAnual, atual.Periodicidade)", src);
        Assert.Contains("data_inicio=case when upper(@statusAtual)='ATIVA' then @hoje else data_inicio end", src);
        Assert.Contains("data_fim=case when upper(@statusAtual)='ATIVA' then @fim else data_fim end", src);
    }

    // Fato 6 - experiência PWA no site: service worker v2 (cache renomeado, navegações
    // network-first com fallback para o cache ou para a pagina offline, estáticos
    // cache-first, skipWaiting/claim/message), pagina offline em PT, botão de instalação
    // discreto via beforeinstallprompt/appinstalled e todo o wiring no _Layout das telas do
    // app (manifest, meta iOS standalone e os dois scripts).
    [Fact]
    public void Codigo_web_oferece_a_experiencia_pwa_da_triagem()
    {
        var sw = File.ReadAllText(Caminho("backend/PlantaoPro.Web/wwwroot/service-worker.js"));
        Assert.Contains("const CACHE_NAME = 'plantaopro-static-v2';", sw);
        Assert.Contains("event.request.mode === 'navigate'", sw);
        Assert.Contains("caches.match('/offline.html')", sw);
        Assert.Contains("self.clients.claim()", sw);
        Assert.Contains("'SKIP_WAITING'", sw);

        var off = File.ReadAllText(Caminho("backend/PlantaoPro.Web/wwwroot/offline.html"));
        Assert.Contains("lang=\"pt-BR\"", off);
        Assert.Contains("conseguiu falar com o servidor. Verifique a rede", off);
        Assert.Contains("Tentar novamente", off);

        var inst = File.ReadAllText(Caminho("backend/PlantaoPro.Web/wwwroot/js/pwa-install.js"));
        Assert.Contains("beforeinstallprompt", inst);
        Assert.Contains("pwaInstallButton", inst);
        Assert.Contains("appinstalled", inst);

        var layout = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml"));
        Assert.Contains("<link rel=\"manifest\" href=\"~/manifest.json\" />", layout);
        Assert.Contains("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\" />", layout);
        Assert.Contains("~/js/service-worker-registration.js\" asp-append-version=\"true\"", layout);
        Assert.Contains("~/js/pwa-install.js\" asp-append-version=\"true\"", layout);
    }

    // Fato 7 - tela da triagem com o botão Regularizar ativo (submit POST /Triagem/Regularizar
    // via BFF com a validacao da orientação) e as novas chaves de i18n presentes nos quatro
    // resx (paridade de presença).
    [Fact]
    public void Codigo_web_ativa_o_botao_regularizar_e_as_chaves_de_i18n()
    {
        var view = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Saude360/Formulario.cshtml"));
        Assert.Contains("asp-controller=\"Triagem\" asp-action=\"Regularizar\"", view);
        Assert.Contains("Clinical.ButtonRegulateTriage", view);

        var web = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs"));
        Assert.Contains("RegularizarTriagemAsync(token, idTriagem, form)", web);
        Assert.Contains("form.Id.GetValueOrDefault()", web);
        Assert.Contains("Salve a triagem antes de regularizá-la.", web);

        foreach (var resx in new[]
        {
            "backend/PlantaoPro.Web/Localization/UiTexts.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.en.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.es.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.fr.resx",
        })
        {
            var txt = File.ReadAllText(Caminho(resx));
            Assert.True(txt.Contains("Clinical.ButtonRegulateTriage\"", StringComparison.Ordinal), $"chave ButtonRegulateTriage ausente em {resx}.");
            Assert.True(txt.Contains("Clinical.TriageReferTitle\"", StringComparison.Ordinal), $"chave TriageReferTitle ausente em {resx}.");
        }
    }

    // Fato 8 - migracao v2379 + manifest: colunas de regulacao idempotentes no
    // encaminhamento de triagem; limpeza deterministica das orfaas adm360 (itens sem vale e
    // envios sem resposta) e VALIDATE CONSTRAINT com guarda de existencia; registro no
    // manifest com checksum LF, dependencia da v2378 e transacional.
    [Fact]
    public void Migracao_v2379_e_manifest_carregam_o_contrato()
    {
        var mig = File.ReadAllText(Caminho("database/migrations/" + VersaoMigracao + ".sql"));
        Assert.Contains("add column if not exists medico_id uuid null", mig);
        Assert.Contains("add column if not exists observacao text not null default ''", mig);
        Assert.Contains("delete from plantaopro.adm360_vale_itens where not exists (select 1 from plantaopro.adm360_vales v where v.id = vale_id)", mig);
        Assert.Contains("delete from plantaopro.adm360_cotacao_envios where resposta_id is not null and not exists (select 1 from plantaopro.adm360_cotacao_respostas r where r.id = resposta_id)", mig);
        Assert.Contains("validate constraint adm360_vale_itens_vale_id_fkey", mig);
        Assert.Contains("validate constraint adm360_cotacao_envios_resposta_id_fkey", mig);

        using var doc = JsonDocument.Parse(File.ReadAllText(Caminho("database/migration-manifest.json"), System.Text.Encoding.UTF8));
        var entradas = doc.RootElement.GetProperty("migrations").EnumerateArray().ToList();
        var v = entradas.FirstOrDefault(e => e.GetProperty("version").GetString() == VersaoMigracao);
        Assert.NotEqual(default, v);
        Assert.Equal("database/migrations/" + VersaoMigracao + ".sql", v.GetProperty("source").GetString());
        Assert.Equal(ChecksumV2379Lf, v.GetProperty("checksum").GetString());
        Assert.Equal("active", v.GetProperty("status").GetString());
        Assert.True(v.GetProperty("transactional").GetBoolean());
        Assert.Contains("2026_10_v2378_r100_notificacoes_calendario_fiscal", v.GetProperty("dependsOn").EnumerateArray().Select(e => e.GetString()).ToList());
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
