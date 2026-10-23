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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R97 (planos/precos configuraveis + entitlements): o catalogo comercial deixa de ser
/// fixo — o admin configura codigo canunico (estilo HOMOLOGACAO_360), slug, visibilidade
/// (publico/destaque/ordem), preco anual e os parametros de teste gratis (permite_trial/
/// dias_trial, 0 = sem teto) por api/planos, e o funil self-service passa a respeitar
/// esses entitlements com mensagens honestas. O preco contratado segue a fonte unica
/// PrecosPlano (ANUAL usa valor_anual quando &gt; 0, senao 12x mensal — antes a coluna
/// valor_anual era morta e o ANUAL contritava o valor mensal). A conversao trial-&gt;pago
/// ganha rota propria (converter-para-paga): vira ATIVA, limpa o fim do teste, recalcula
/// datas/preco pelo plano de destino e materializa CONVERSAO_TRIAL_PAGO — fechando a
/// pendencia herdada da R96 em que alterar-plano/editar nunca viravam o status nem
/// limparam data_trial_fim e o cliente convertido seguia barrado pelo gate do trial vencido.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R97PlanosConfiguraveisEntitlementsContratoTests
{
    private const string PastaR97 = "docs/evidencias/2026-10-23-rodada-97";
    private const string ArquivoSaida = "r97a_drill_saida.txt";

    // =========================================================================
    // Fato 1 - a execucao canonica do drill E2E
    // =========================================================================

    // API standalone no ar; admin cria/edita plano com codigo/preco anual/dias de teste;
    // duplicata de codigo 409; catalogo publico expoe o preco anual efetivo e oculta os
    // internos; o self-service respeita os entitlements de trial (teto por plano e
    // permite_trial=false), contrata o preco anual quando ANUAL, e a conversao trial->pago
    // abre o gate que o trial vencido havia fechado (pendencia herdada da R96).
    [Fact]
    public void Drill_saida_marcadores_completos_e_resultado_passei()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR97 + "/" + ArquivoSaida));
        Assert.StartsWith("R97A_DRILL data=", linhas[0]);
        Assert.DoesNotContain(linhas, l => l.StartsWith("FALHA ", StringComparison.Ordinal));

        Assert.Equal("PORTAS_LIVRES_INICIO=SIM", ObterLinha(linhas, "PORTAS_LIVRES_INICIO"));
        var estagio1 = Parsear(ObterLinha(linhas, "ESTAGEM_1_API_NO_AR "));
        Assert.Equal("SIM", estagio1["ok"]);
        var pidBruto = estagio1.GetOrThrow("pid");
        Assert.True(int.TryParse(pidBruto, out var pid) && pid > 0, $"pid da API ausente: {pidBruto}.");
        Assert.Equal("PRE_LIMPEZA_RERUN ok=SIM", ObterLinha(linhas, "PRE_LIMPEZA_RERUN"));

        var criado = Parsear(ObterLinha(linhas, "PA_PLANO_ADMIN_OK "));
        Assert.Equal("200", criado["http"]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", criado.GetOrThrow("plano"));
        var duplo = Parsear(ObterLinha(linhas, "PA_DUPLICA_CODIGO_OK "));
        Assert.Equal("409", duplo["http"]);
        Assert.Equal("SIM", duplo["msg_em_uso"]);
        var ed1 = Parsear(ObterLinha(linhas, "PA_PLANO_EDIT_OK "));
        Assert.Equal("200", ed1["http"]);
        Assert.Equal("1250.00", ed1["valor_anual"]);
        Assert.Equal("7", ed1["dias_trial"]);
        var ed2 = Parsear(ObterLinha(linhas, "PA_PLANO_EDIT2_OK "));
        Assert.Equal("200", ed2["http"]);
        Assert.Equal("5", ed2["dias_trial"]);

        var catalogo = Parsear(ObterLinha(linhas, "PA_CATALOGO_OK "));
        var totalBruto = catalogo["total"];
        Assert.True(int.TryParse(totalBruto, out var total) && total >= 1, $"catalogo vazio no drill: {totalBruto}.");
        Assert.Equal("SIM", catalogo["tem_r97"]);
        Assert.Equal("1250.00", catalogo["anual_efetivo"]);
        Assert.Equal("NAO", catalogo["interno"]);

        var teto = Parsear(ObterLinha(linhas, "PA_TRIAL_TETO_OK "));
        Assert.Equal("400", teto["http"]);
        Assert.Equal("SIM", teto["msg_teto"]);
        Assert.Equal("0", teto["residuo"]);
        var semTrial = Parsear(ObterLinha(linhas, "PA_TRIAL_SEM_PERMISSAO_OK "));
        Assert.Equal("400", semTrial["http"]);
        Assert.Equal("SIM", semTrial["msg_sem_trial"]);
        Assert.Equal("0", semTrial["residuo"]);

        var trial = Parsear(ObterLinha(linhas, "PA_TRIAL_OK "));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", trial.GetOrThrow("assinatura"));
        Assert.Equal("TRIAL", trial["status"]);
        Assert.Equal("SIM", trial["trial_fim_ok"]);
        Assert.Equal("0", trial["cob_inicial"]);

        var anual = Parsear(ObterLinha(linhas, "PA_ANUAL_OK "));
        Assert.Equal("ATIVA", anual["status"]);
        Assert.Equal("ANUAL", anual["periodicidade"]);
        Assert.Equal("1250.00", anual["valor"]);
        Assert.Equal("1250.00", anual["cob"]);
        Assert.Equal("SIM", anual["fim_1ano"]);

        var gateAtivo = Parsear(ObterLinha(linhas, "PA_GATE_ATIVO_OK "));
        Assert.Equal("200", gateAtivo["minha_http"]);
        Assert.Equal("TRIAL", gateAtivo["minha_status"]);
        Assert.Equal("200", gateAtivo["uso_http"]);
        var gateVencido = Parsear(ObterLinha(linhas, "PA_GATE_VENCIDO_OK "));
        Assert.Equal("403", gateVencido["uso_http"]);
        Assert.Equal("SIM", gateVencido["msg_encerrado"]);

        var conv = Parsear(ObterLinha(linhas, "PA_CONVERSAO_OK "));
        Assert.Equal("200", conv["http"]);
        Assert.Equal("ATIVA", conv["status"]);
        Assert.Equal("nulo", conv["trial_fim"]);
        Assert.Equal("100.00", conv["valor"]);
        Assert.Equal("SIM", conv["fim_1mes"]);
        Assert.Equal("1", conv["historico"]);
        var duploConv = Parsear(ObterLinha(linhas, "PA_CONVERSAO_DUPLO_OK "));
        Assert.Equal("400", duploConv["http"]);
        Assert.Equal("SIM", duploConv["msg_apenas_trial"]);

        var gateAberto = Parsear(ObterLinha(linhas, "PA_GATE_ABERTO_OK "));
        Assert.Equal("200", gateAberto["minha_http"]);
        Assert.Equal("ATIVA", gateAberto["minha_status"]);
        Assert.Equal("200", gateAberto["uso_http"]);

        Assert.Equal("PORTAS_LIVRES=SIM", ObterLinha(linhas, "PORTAS_LIVRES="));
        Assert.Equal("RESULTADO=PASSEI", ObterLinha(linhas, "RESULTADO"));
    }

    // =========================================================================
    // Fato 2 - a migracao v2376: codigo unico entre ativos + backfill + dias_trial 0
    // =========================================================================

    [Fact]
    public async Task Migracao_v2376_ativa_no_manifest_codigo_unico_backfill_e_dias_trial_sem_teto()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "database", "migration-manifest.json")));
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => string.Equals(m.GetProperty("version").GetString(),
                "2026_10_v2376_r97_planos_configuraveis_entitlements", StringComparison.Ordinal));
        Assert.True(entrada.ValueKind == JsonValueKind.Object, "entrada v2376 ausente do manifest.");
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.Contains("2026_10_v2375_r96_onboarding_selfservice_trial",
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
            $"checksum dessincronizado do arquivo v2376: {esperado}.");

        var sql = File.ReadAllText(caminho);
        foreach (var fragmento in new[]
        {
            "create unique index if not exists ux_planos_codigo",
            "upper(codigo)",
            "dias_trial = 0",
        })
        {
            Assert.Contains(fragmento, sql);
        }

        // Efeito real no banco vivo: indice presente, backfill de codigo completo e o
        // padrao legado (14 dias) convertido para "sem teto" nos planos semeados.
        Assert.Equal(1, await ConsultarAsync<int>(
            "select count(*) from pg_indexes where schemaname='plantaopro' and indexname='ux_planos_codigo'"));
        Assert.Equal(0, await ConsultarAsync<int>(
            @"select count(*) from plantaopro.planos
              where reg_status='A' and slug is not null and btrim(coalesce(slug,'')) <> '' and codigo is null"));
        foreach (var codigo in new[] { "ESSENCIAL", "HOMOLOGACAO_360" })
        {
            Assert.Equal(0, await ConsultarAsync<int>(
                "select coalesce(dias_trial,0) from plantaopro.planos where upper(codigo)=@codigo",
                new { codigo }));
        }
    }

    // =========================================================================
    // Fato 3 - fonte unica de precos por periodicidade
    // =========================================================================

    [Fact]
    public void Precos_plano_resolver_unico_por_periodicidade_com_fallback_dezesseis_x_zero()
    {
        // ANUAL usa o valor_anual configurado quando > 0 ...
        Assert.Equal(1188m, PrecosPlano.ValorContratado(100m, 1188m, "ANUAL"));
        // ... aceita a periodicidade sem capitalizacao/espacos ...
        Assert.Equal(1200m, PrecosPlano.ValorContratado(100m, 0m, "  anual "));
        // ... e com valor_anual zerado (nao configurado) cai em 12x mensal (nunca preco inventado);
        // MENSAL/ausente sempre usa o mensal.
        Assert.Equal(100m, PrecosPlano.ValorContratado(100m, 1188m, "MENSAL"));
        Assert.Equal(399m, PrecosPlano.ValorContratado(399m, 0m, null));
        Assert.Equal(4788.00m, PrecosPlano.ValorContratado(399m, 0m, "ANUAL"));

        // O nucle do provisionamento e a conversao passam a chamar a fonte unica.
        var svc = File.ReadAllText(Caminho("backend/PlantaoPro.Api/SelfServiceServices.cs"));
        Assert.Contains("PrecosPlano.ValorContratado(", svc);
        var conv = File.ReadAllText(Caminho("backend/PlantaoPro.Api/ConversaoTrialService.cs"));
        Assert.Contains("PrecosPlano.ValorContratado(", conv);
    }

    // =========================================================================
    // Fato 4 - o CRUD administrativo configura catalogo, precos e trial
    // =========================================================================

    [Fact]
    public async Task Admin_cria_e_edita_plano_com_codigo_precos_e_trial_configuraveis()
    {
        await SementePronta;
        var ctrl = NovoPlanosController();
        try
        {
            var req = NovoRequest(codigo: "R97_CRUD_CONTRATO", slug: "r97-crud-contrato", valorAnual: 1150m, diasTrial: 5);
            var criado = await ctrl.Criar(req);
            Assert.Equal(200, StatusDe(criado));
            var payload = (ApiResponse<Guid>)((OkObjectResult)criado).Value!;
            Assert.True(payload.Success, payload.Message);
            // Id lido de volta por codigo canonico (deterministico e sem depender do formato do wire).
            var id = await ConsultarAsync<Guid>("select id from plantaopro.planos where upper(codigo)='R97_CRUD_CONTRATO'");

            var lido = await ConsultarAsync<LendoPlano>(
                @"select codigo as ""Codigo"", round(valor_anual::numeric,2) as ""ValorAnual"", dias_trial as ""DiasTrial"",
                   permite_trial as ""PermiteTrial"", publico as ""Publico"", ordem as ""Ordem""
                  from plantaopro.planos where id=@id", new { id });
            Assert.Equal("R97_CRUD_CONTRATO", lido.Codigo);
            Assert.Equal(1150.00m, lido.ValorAnual);
            Assert.Equal(5, lido.DiasTrial);
            Assert.True(lido.PermiteTrial && lido.Publico && lido.Ordem == 950,
                $"campos de catalogo/entitlement nao persistidos: {lido}.");

            // Edicao move preco anual e teto de teste (o mesmo codigo proprio nao conflita).
            var edicao = NovoRequest(codigo: "R97_CRUD_CONTRATO", slug: "r97-crud-contrato", valorAnual: 1250m, diasTrial: 7);
            var editado = await ctrl.Editar(id, edicao);
            Assert.Equal(200, StatusDe(editado));
            var relido = await ConsultarAsync<LendoPlano>(
                @"select codigo as ""Codigo"", round(valor_anual::numeric,2) as ""ValorAnual"", dias_trial as ""DiasTrial"",
                   permite_trial as ""PermiteTrial"", publico as ""Publico"", ordem as ""Ordem""
                  from plantaopro.planos where id=@id", new { id });
            Assert.Equal(1250.00m, relido.ValorAnual);
            Assert.Equal(7, relido.DiasTrial);

            // Duplicata de codigo ativo -> 409 honesto (indice unico ux_planos_codigo).
            var duplicado = await ctrl.Criar(NovoRequest(codigo: "r97_crud_contrato", slug: "r97-crud-contrato-bis", valorAnual: 0m, diasTrial: 0));
            Assert.Equal(409, StatusDe(duplicado));
            Assert.Contains("em uso", Mensagem(duplicado) ?? "");

            // Validacoes honestas: codigo fora do formato e teto de dias fora da faixa.
            var invalido = await ctrl.Criar(NovoRequest(codigo: "r97 x", slug: null, valorAnual: 0m, diasTrial: 0));
            Assert.Equal(400, StatusDe(invalido));
            Assert.NotNull(Erros(invalido));
            Assert.Contains(Erros(invalido)!, e => e.Contains("código", StringComparison.OrdinalIgnoreCase));

            var tetoRuim = await ctrl.Criar(NovoRequest(codigo: "R97_CRUD_TETO", slug: null, valorAnual: 0m, diasTrial: 91));
            Assert.Equal(400, StatusDe(tetoRuim));
            Assert.Contains(Erros(tetoRuim)!, e => e.Contains("entre 0 (sem teto) e 90", StringComparison.Ordinal));
        }
        finally
        {
            await LimparPorCodigo("R97_CRUD_CONTRATO");
        }
    }

    // =========================================================================
    // Fato 5 - o catalogo publico expoe o preco anual efetivo e os entitlements
    // =========================================================================

    [Fact]
    public async Task Catalogo_publico_expo_preco_anual_efetivo_entitlements_e_oculta_internos()
    {
        await SementePronta;
        var cfg = BuildCfg();
        var audit = new R97FakeAuditService();
        var svc = new SelfServiceSaasService(cfg,
            new TenantContextService(new HttpContextAccessor(), cfg, NullLogger<TenantContextService>.Instance),
            audit, new AssinaturaGuardService(cfg, audit, NullLogger<AssinaturaGuardService>.Instance),
            NullLogger<SelfServiceSaasService>.Instance,
            new CobrancaService(cfg, audit, Array.Empty<ICobrancaProvider>(), NullLogger<CobrancaService>.Instance));

        var criado = await CriarPlanoAsync("R97_CAT_CONTRATO", "R97 Cat Contrato", 100m, 1100m, true, 0, true);
        try
        {
            var r = await svc.ListarPlanosPublicosAsync();
            Assert.True(r.Success, r.Message);
            var lista = r.Data!.ToList();

            var proprio = lista.FirstOrDefault(p => p.Slug == "r97-cat-contrato");
            Assert.NotNull(proprio);
            Assert.Equal(1100m, proprio!.ValorAnualEfetivo);
            Assert.Equal(1100m, proprio.ValorAnual);
            Assert.True(proprio.PermiteTrial, "entitlement permite_trial ausente do catalogo.");
            Assert.Equal(0, proprio.DiasTrial);

            // Sem preco anual configurado, o efetivo cai em 12x mensal no servidor (399*12).
            var essencial = lista.FirstOrDefault(p => p.Slug == "essencial");
            Assert.NotNull(essencial);
            Assert.Equal(4788.00m, essencial!.ValorAnualEfetivo);

            // O plano interno HOMOLOGACAO_360 (publico=false) nao aparece no catalogo.
            var homId = await ConsultarAsync<Guid>(
                "select id from plantaopro.planos where upper(codigo)='HOMOLOGACAO_360'");
            Assert.False(lista.Any(p => p.Id == homId), "plano interno vazou para o catalogo publico.");
        }
        finally
        {
            await LimparPorCodigo("R97_CAT_CONTRATO");
        }
    }

    // =========================================================================
    // Fato 6 - o self-service respeita os entitlements de trial do PLANO
    // =========================================================================

    [Fact]
    public async Task Selfservice_trial_respeita_entitlements_do_plano_sem_residuo()
    {
        await SementePronta;
        var svc = NovoSelfService();

        var sem = await CriarPlanoAsync("R97_TRIAL_SEM", "R97 Trial Sem", 100m, 0m, false, 0, false);
        var teto = await CriarPlanoAsync("R97_TRIAL_TETO", "R97 Trial Teto", 100m, 0m, true, 5, false);
        try
        {
            // Plano sem trial: 400 honesto e nenhum resíduo de mundo.
            var bloqueado = await svc.FinalizarCadastroAsync(Pedido("97971001000111", "r97-s6-a@exemplo.test", sem, "TRIAL", 5), null, null);
            Assert.False(bloqueado.Success);
            Assert.Equal(400, bloqueado.StatusCode);
            Assert.Contains("não oferece teste gratuito", bloqueado.Message);
            Assert.Equal(0, await ConsultarAsync<int>(
                "select count(*) from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='97971001000111'"));

            // Teto por plano: 6 dias num plano de 5 dias -> 400 honesto sem resíduo.
            var acima = await svc.FinalizarCadastroAsync(Pedido("97972002000122", "r97-s6-b@exemplo.test", teto, "TRIAL", 6), null, null);
            Assert.False(acima.Success);
            Assert.Equal(400, acima.StatusCode);
            Assert.Contains("até 5 dias", acima.Message);
            Assert.Equal(0, await ConsultarAsync<int>(
                "select count(*) from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='97972002000122'"));

            // Dentro do teto: mundo TRIAL completo com trial_fim exato.
            var ok = await svc.FinalizarCadastroAsync(Pedido("97973003000133", "r97-s6-c@exemplo.test", teto, "TRIAL", 5), null, null);
            Assert.True(ok.Success, ok.Message);
            try
            {
                var dados = await ConsultarAsync<DatasAssinatura>(
                    "select data_inicio as \"DataInicio\", data_trial_fim as \"DataTrialFim\", data_fim as \"DataFim\" from plantaopro.assinaturas where id=@id",
                    new { id = ok.Data!.AssinaturaId });
                Assert.True(dados.DataTrialFim == DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                    $"trial_fim fora do esperado: {dados.DataTrialFim}.");
            }
            finally
            {
                await LimparPorCnpj("97973003000133");
            }
        }
        finally
        {
            await LimparPorCodigo("R97_TRIAL_SEM");
            await LimparPorCodigo("R97_TRIAL_TETO");
        }
    }

    // =========================================================================
    // Fato 7 - o ANUAL contrata o preco anual configurado (e o ciclo de 1 ano)
    // =========================================================================

    [Fact]
    public async Task Selfservice_anual_contrata_valor_anual_e_ciclo_de_um_ano()
    {
        await SementePronta;
        var svc = NovoSelfService();
        var plano = await CriarPlanoAsync("R97_ANUAL_CONTRATO", "R97 Anual Contrato", 100m, 1188m, true, 0, true);
        try
        {
            var anual = await svc.FinalizarCadastroAsync(Pedido("97974004000144", "r97-s7-a@exemplo.test", plano, "ATIVA", null, "ANUAL"), null, null);
            Assert.True(anual.Success, anual.Message);
            var mensal = await svc.FinalizarCadastroAsync(Pedido("97975005000155", "r97-s7-b@exemplo.test", plano, "ATIVA", null, "MENSAL"), null, null);
            Assert.True(mensal.Success, mensal.Message);
            try
            {
                var a = anual.Data!;
                var m = mensal.Data!;
                Assert.Equal("ANUAL", await ConsultarAsync<string>(
                    "select coalesce(periodicidade,'') from plantaopro.assinaturas where id=@id", new { id = a.AssinaturaId }));
                Assert.Equal(1188m, await ConsultarAsync<decimal>(
                    "select valor_contratado from plantaopro.assinaturas where id=@id", new { id = a.AssinaturaId }));
                Assert.Equal(1188m, await ConsultarAsync<decimal>(
                    "select sum(valor) from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id=@id", new { id = a.AssinaturaId }));
                var fimAnual = await ConsultarAsync<string>(
                    "select data_fim::text||' | inicio='||data_inicio::text||' | trial='||coalesce(data_trial_fim::text,'null') from plantaopro.assinaturas where id=@id", new { id = a.AssinaturaId });
                Assert.True(fimAnual.StartsWith(DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"), StringComparison.Ordinal),
                    $"data_fim ANUAL fora do ciclo de 1 ano: {fimAnual}.");

                Assert.Equal("MENSAL", await ConsultarAsync<string>(
                    "select coalesce(periodicidade,'') from plantaopro.assinaturas where id=@id", new { id = m.AssinaturaId }));
                Assert.Equal(100m, await ConsultarAsync<decimal>(
                    "select valor_contratado from plantaopro.assinaturas where id=@id", new { id = m.AssinaturaId }));
                var fimMensal = await ConsultarAsync<string>(
                    "select data_fim::text from plantaopro.assinaturas where id=@id", new { id = m.AssinaturaId });
                Assert.True(fimMensal == DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"),
                    $"data_fim MENSAL fora do ciclo de 1 mes: {fimMensal}.");
            }
            finally
            {
                await LimparPorCnpj("97974004000144");
                await LimparPorCnpj("97975005000155");
            }
        }
        finally
        {
            await LimparPorCodigo("R97_ANUAL_CONTRATO");
        }
    }

    // =========================================================================
    // Fato 8 - a conversao trial->pago abre o gate que o trial vencido fechou
    // =========================================================================

    [Fact]
    public async Task Conversao_trial_para_pago_atoma_gate_datas_preco_e_historico()
    {
        await SementePronta;
        var cfg = BuildCfg();
        var audit = new R97FakeAuditService();
        var svc = NovoSelfService();
        var guard = new AssinaturaGuardService(cfg, audit, NullLogger<AssinaturaGuardService>.Instance);
        var conversor = new ConversaoTrialService(cfg, audit, NullLogger<ConversaoTrialService>.Instance);
        var plano = await CriarPlanoAsync("R97_CONV_CONTRATO", "R97 Conv Contrato", 150m, 1500m, true, 10, false);
        var cnpj = "97976006000166";
        try
        {
            var criado = await svc.FinalizarCadastroAsync(Pedido(cnpj, "r97-s8-admin@exemplo.test", plano, "TRIAL", 10), null, null);
            Assert.True(criado.Success, criado.Message);
            var ids = criado.Data!;
            var clienteId = await ConsultarAsync<Guid>(
                "select cliente_id from plantaopro.assinaturas where id=@id", new { id = ids.AssinaturaId });

            // Corrida herdada da R96: trial vencido fecha o gate (403 experimental encerrado).
            await ExecutarSqlAsync("update plantaopro.assinaturas set data_trial_fim = current_date - 1 where id=@id",
                new { id = ids.AssinaturaId });
            var vencido = await guard.ObterUsoPlanoAsync(clienteId);
            Assert.False(vencido.Success);
            Assert.Equal(403, vencido.StatusCode);
            Assert.Contains("experimental encerrado", vencido.Message);

            // A conversao vira ATIVA, limpa o fim do teste, recalcula preco pelo plano e
            // materializa o historico — e o gate abre na mesma chamada seguinte.
            var conv = await conversor.ConverterAsync(ids.AssinaturaId, null, null, "conversao por contrato r97", Guid.NewGuid(), "127.0.0.1");
            Assert.True(conv.Success, conv.Message);
            Assert.Equal("ATIVA", conv.Data!.Status);

            var estado = await ConsultarAsync<EstadoPosConversao>(
                @"select (data_trial_fim is null) as ""TrialNulo"", coalesce(periodicidade,'') as ""Periodicidade"",
                   valor_contratado as ""Valor"", data_fim::date as ""Fim""
                  from plantaopro.assinaturas where id=@id", new { id = ids.AssinaturaId });
            Assert.True(estado.TrialNulo, "data_trial_fim nao foi limpa na conversao.");
            Assert.Equal("MENSAL", estado.Periodicidade);
            Assert.Equal(150m, estado.Valor);
            Assert.True(estado.Fim == DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
                $"data_fim da conversao fora de 1 mes: {estado.Fim}.");
            Assert.Equal(1, await ConsultarAsync<int>(
                "select count(*) from plantaopro.assinatura_historico where assinatura_id=@id and acao='CONVERSAO_TRIAL_PAGO'",
                new { id = ids.AssinaturaId }));

            var aberto = await guard.ObterUsoPlanoAsync(clienteId);
            Assert.True(aberto.Success, aberto.Message);

            // Segunda conversao da mesma assinatura: apenas TRIAL converte.
            var deNovo = await conversor.ConverterAsync(ids.AssinaturaId, null, null, "tentativa duplicada", Guid.NewGuid(), "127.0.0.1");
            Assert.False(deNovo.Success);
            Assert.Equal(400, deNovo.StatusCode);
            Assert.Contains("assinaturas em teste", deNovo.Message);

            // Justificativa obrigatoria: sem ela, 400 antes de tocar no banco.
            var semJustificativa = await conversor.ConverterAsync(ids.AssinaturaId, null, null, "  ", Guid.NewGuid(), "127.0.0.1");
            Assert.False(semJustificativa.Success);
            Assert.Equal(400, semJustificativa.StatusCode);
        }
        finally
        {
            await LimparPorCnpj(cnpj);
            await LimparPorCodigo("R97_CONV_CONTRATO");
        }
    }

    // =========================================================================
    // Fato 9 - conversao para OUTRO plano com ANUAL recalcula tudo pelo destino
    // =========================================================================

    [Fact]
    public async Task Conversao_para_outro_plano_anual_recalcula_preco_periodicidade_e_fim()
    {
        await SementePronta;
        var audit = new R97FakeAuditService();
        var cfg = BuildCfg();
        var svc = NovoSelfService();
        var conversor = new ConversaoTrialService(cfg, audit, NullLogger<ConversaoTrialService>.Instance);
        var origem = await CriarPlanoAsync("R97_CONV_DES", "R97 Conv Origem", 120m, 0m, true, 10, false);
        var destino = await CriarPlanoAsync("R97_CONV_DEST", "R97 Conv Destino", 200m, 2100m, true, 0, false);
        var cnpj = "97977007000177";
        try
        {
            var criado = await svc.FinalizarCadastroAsync(Pedido(cnpj, "r97-s9-admin@exemplo.test", origem, "TRIAL", 10), null, null);
            Assert.True(criado.Success, criado.Message);
            var ids = criado.Data!;

            var conv = await conversor.ConverterAsync(ids.AssinaturaId, destino, "ANUAL", "upgrade no fim do teste", Guid.NewGuid(), "127.0.0.1");
            Assert.True(conv.Success, conv.Message);
            Assert.Equal(destino, conv.Data!.PlanoId);

            var estado = await ConsultarAsync<EstadoDestinoConversao>(
                @"select plano_id as ""PlanoId"", (data_trial_fim is null) as ""TrialNulo"", coalesce(periodicidade,'') as ""Periodicidade"",
                   valor_contratado as ""Valor"", data_fim::date as ""Fim""
                  from plantaopro.assinaturas where id=@id", new { id = ids.AssinaturaId });
            Assert.Equal(destino, estado.PlanoId);
            Assert.True(estado.TrialNulo);
            Assert.Equal("ANUAL", estado.Periodicidade);
            Assert.Equal(2100m, estado.Valor);
            Assert.True(estado.Fim == DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
                $"data_fim ANUAL da conversao fora de 1 ano: {estado.Fim}.");
        }
        finally
        {
            await LimparPorCnpj(cnpj);
            await LimparPorCodigo("R97_CONV_DES");
            await LimparPorCodigo("R97_CONV_DEST");
        }
    }

    // =========================================================================
    // Fato 10 - o wire: rota de conversao, mensagens do nucleo e wizard com entitlements
    // =========================================================================

    [Fact]
    public void Wire_rota_de_conversao_mensagens_do_nucleo_e_wizard_com_entitlements()
    {
        var comercial = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs"));
        Assert.Contains("[HttpPost(\"{id:guid}/converter-para-paga\")]", comercial);
        Assert.Contains("[Route(\"api/assinaturas\")]", comercial);

        var nucleo = File.ReadAllText(Caminho("backend/PlantaoPro.Api/SelfServiceServices.cs"));
        foreach (var fragmento in new[]
        {
            "não oferece teste gratuito.",
            "oferece teste gratuito de até",
            "coalesce(permite_trial,true)",
            "AddYears(1)",
        })
        {
            Assert.Contains(fragmento, nucleo);
        }

        var conv = File.ReadAllText(Caminho("backend/PlantaoPro.Api/ConversaoTrialService.cs"));
        foreach (var fragmento in new[]
        {
            "pg_advisory_xact_lock",
            "'CONVERSAO_TRIAL_PAGO'",
            "data_trial_fim=null",
        })
        {
            Assert.Contains(fragmento, conv);
        }

        var wizard = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Cadastro/Cadastro.cshtml"));
        foreach (var fragmento in new[]
        {
            "data-plano-select",
            "data-permite-trial",
            "data-dias-trial",
            "data-anual",
            "data-nome",
            "/ano",
            "Este plano não oferece teste gratuito",
        })
        {
            Assert.Contains(fragmento, wizard);
        }
    }

    // =========================================================================
    // Infraestrutura local
    // =========================================================================

    private static readonly string[] TodosCnpjs =
    {
        "97971001000111", "97972002000122", "97973003000133", "97974004000144",
        "97975005000155", "97976006000166", "97977007000177"
    };

    private static readonly string[] TodosCodigos =
    {
        "R97_CRUD_CONTRATO", "R97_CRUD_TETO", "R97_CAT_CONTRATO", "R97_TRIAL_SEM",
        "R97_TRIAL_TETO", "R97_ANUAL_CONTRATO", "R97_CONV_CONTRATO", "R97_CONV_DES", "R97_CONV_DEST"
    };

    private readonly Task SementePronta = SementarAsync();

    private static async Task SementarAsync()
    {
        // crash-idempotencia: remove antes tudo ligado aos CNPJs/codigos proprios.
        foreach (var cnpj in TodosCnpjs) await LimparPorCnpj(cnpj);
        foreach (var codigo in TodosCodigos) await LimparPorCodigo(codigo);
    }

    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static SelfServiceSaasService NovoSelfService()
    {
        var cfg = BuildCfg();
        var audit = new R97FakeAuditService();
        return new SelfServiceSaasService(cfg, new TenantContextService(new HttpContextAccessor(), cfg, NullLogger<TenantContextService>.Instance),
            audit, new AssinaturaGuardService(cfg, audit, NullLogger<AssinaturaGuardService>.Instance),
            NullLogger<SelfServiceSaasService>.Instance,
            new CobrancaService(cfg, audit, Array.Empty<ICobrancaProvider>(), NullLogger<CobrancaService>.Instance));
    }

    private static PlanosController NovoPlanosController()
    {
        var cfg = BuildCfg();
        var audit = new R97FakeAuditService();
        return new PlanosController(cfg, audit, new R97FakeCurrentUser(), NullLogger<PlanosController>.Instance)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    }

    private static PlanoComercialRequest NovoRequest(string? codigo, string? slug, decimal valorAnual, int diasTrial) =>
        new("R97 Plano", "plano de contrato r97", 100m, 0, 0, 0, false, false, false, false,
            Codigo: codigo, Slug: slug, ValorAnual: valorAnual, Publico: true, Destaque: false,
            Ordem: 950, PermiteWhiteLabel: false, PermiteTrial: true, DiasTrial: diasTrial);

    private static CadastroSelfServiceRequest Pedido(string cnpj, string email, Guid plano, string? status, int? dias, string periodicidade = "MENSAL") => new()
    {
        Empresa = new CadastroEmpresaRequest { NomeFantasia = "R97 Teste " + cnpj[..4], RazaoSocial = "R97 Teste LTDA", Cnpj = cnpj, Cidade = "Olinda", Uf = "PE", EmailCorporativo = "r97-emp@exemplo.test" },
        Plano = new CadastroPlanoRequest { PlanoId = plano, Periodicidade = periodicidade, AceiteTermos = true, AceitePrivacidade = true, ConsentimentoLgpd = true },
        UsuarioAdmin = new CadastroUsuarioAdminRequest { Nome = "R97 Admin", Email = email, Senha = "R97senha123!" },
        StatusAssinatura = status,
        DiasTrial = dias
    };

    private static async Task<Guid> CriarPlanoAsync(string codigo, string nome, decimal mensal, decimal anual, bool permiteTrial, int dias, bool publico)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.ExecuteScalarAsync<Guid>(@"insert into plantaopro.planos(id,codigo,slug,nome,descricao,valor_mensal,valor_anual,publico,destaque,ordem,permite_trial,dias_trial,limite_medicos,limite_hospitais,limite_plantoes_mes,limite_usuarios,limite_convites_mes,status,reg_status,reg_date)
values(gen_random_uuid(), @codigo, lower(replace(@codigo,'_','-')), @nome, 'plano de contrato r97', @mensal, @anual, @publico, false, 950, @permiteTrial, @dias, 0, 0, 0, 0, 0, 'ATIVO', 'A', now())
returning id", new { codigo, nome, mensal, anual, publico, permiteTrial, dias });
    }

    private static async Task LimparPorCodigo(string codigo)
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.ExecuteAsync(@"
delete from plantaopro.plano_recursos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.plano_modulos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.plano_faq where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.assinatura_historico where assinatura_id in (select id from plantaopro.assinaturas where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo)));
delete from plantaopro.assinaturas where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.planos where upper(codigo)=upper(@codigo);",
                new { codigo });
        });
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
delete from plantaopro.assinatura_historico where assinatura_id in (select a.id from plantaopro.assinaturas a join plantaopro.clientes c on c.id=a.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.cliente_alertas where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.cliente_bloqueios where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.assinatura_uso where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.faturas_saas where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.paineis_publicos where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.painel_publico_tokens where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.assinaturas where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj;
delete from plantaopro.cadastro_cliente_solicitacoes where regexp_replace(cnpj,'\D','','g')=@cnpj;",
                new { cnpj });
        });
    }

    private static async Task<T> ConsultarAsync<T>(string sql, object? parametros = null)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    private static Task ExecutarSqlAsync(string sql, object parametros)
    {
        return TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.ExecuteAsync(sql, parametros);
        });
    }

    private sealed class LendoPlano
    {
        public string Codigo { get; set; } = string.Empty;
        public decimal ValorAnual { get; set; }
        public int DiasTrial { get; set; }
        public bool PermiteTrial { get; set; }
        public bool Publico { get; set; }
        public int Ordem { get; set; }
    }

    /// <summary>Campos `date` da assinatura para checagem no calendário local.</summary>
    private sealed class DatasAssinatura
    {
        public DateOnly DataInicio { get; set; }
        public DateOnly DataTrialFim { get; set; }
        public DateOnly DataFim { get; set; }
    }

    private sealed class EstadoPosConversao
    {
        public bool TrialNulo { get; set; }
        public string Periodicidade { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateOnly Fim { get; set; }
    }

    private sealed class EstadoDestinoConversao
    {
        public Guid PlanoId { get; set; }
        public bool TrialNulo { get; set; }
        public string Periodicidade { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateOnly Fim { get; set; }
    }

    private sealed class R97FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao,
            object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;

        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao,
            string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
    }

    private sealed class R97FakeCurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.Parse("6ba7b810-9dad-41d1-80b4-00c04fd430c0");
        public Guid? TenantId => null;
        public Guid? ClienteId => null;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; } = new[] { "ADMINISTRADOR_GLOBAL" };
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => true;
        public bool IsTenantAdmin() => false;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => role.Equals("ADMINISTRADOR_GLOBAL", StringComparison.OrdinalIgnoreCase);
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

    private static int StatusDe(IActionResult resultado) => resultado switch
    {
        OkObjectResult => 200,
        BadRequestObjectResult => 400,
        NotFoundObjectResult => 404,
        ConflictObjectResult => 409,
        ObjectResult objectResult => objectResult.StatusCode ?? 500,
        _ => -1
    };

    private static string? Mensagem(IActionResult resultado) =>
        resultado is ObjectResult objectResult && objectResult.Value is ApiResponse<string> apMensagem ? apMensagem.Message : null;

    private static IEnumerable<string>? Erros(IActionResult resultado) =>
        resultado is ObjectResult objectResult && objectResult.Value is ApiResponse<string> apErros ? apErros.Errors : null;
}

