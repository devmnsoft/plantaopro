using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R7-D-F6: Admin 360 (cotações + dashboard + resumo organizacional) em recursos localizados
/// + convenção de que TODO TempData["Success"/"Error"/aliases] escrito por Administrativo360*
/// passa por UiText.L. Escopo da fatia (r7d0 §6): 3 views (Cotacoes/Index, Gestao/Dashboard,
/// Administrativo360/Index) + os 9 partials do controller que escrevem mensagens de toast
/// (ComprasRecebimentos, Financeiro, Vendas, Vales, Cadastros, Cirurgias, Organizacao,
/// Orcamentos, EstoqueValorizacao). Convenções adotadas e travadas aqui:
///   • fallbacks `resp.Error ?? UiText.L(...)` mantêm a mensagem da API como prioritária
///     (texto dinâmico da API é dívida documentada, padrão F5);
///   • as ações financeiras/comerciais continuam usando os aliases de toast
///     "SuccessMessage"/"ErrorMessage" (unificação de chaves = backlog pós-MVP do _ToastMessages),
///     apenas o literal do ramo de sucesso foi keyizado;
///   • mensagens interpoladas viraram chaves condicionais (Cadastros ativado/inativado;
///     Vales evento CONSUMO/RETORNO/PERDA) — valores de máquina permanecem só nas rotas.
/// </summary>
public sealed class R7DF6Admin360I18nContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();

    private static string Read(params string[] partes) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(partes).ToArray()));

    private static readonly string[] Parciais =
    {
        "Administrativo360Controller.ComprasRecebimentos.cs",
        "Administrativo360Controller.Financeiro.cs",
        "Administrativo360Controller.Vendas.cs",
        "Administrativo360Controller.Vales.cs",
        "Administrativo360Controller.Cadastros.cs",
        "Administrativo360Controller.Cirurgias.cs",
        "Administrativo360Controller.Organizacao.cs",
        "Administrativo360Controller.Orcamentos.cs",
        "Administrativo360Controller.EstoqueValorizacao.cs"
    };

    [Fact]
    public void ControllersAdm360_TempDataSemLiteralsERotulosPorRecursos()
    {
        foreach (var parcial in Parciais)
        {
            var fonte = Read("backend", "PlantaoPro.Web", "Controllers", parcial);
            // nenhum literal novo pode voltar nos dois canais do toast
            Assert.DoesNotContain("TempData[\"Success\"] = \"", fonte);
            Assert.DoesNotContain("TempData[\"Error\"] = \"", fonte);
            Assert.Contains("UiText.L(\"Adm360.", fonte);
        }

        var compras = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.ComprasRecebimentos.cs");
        foreach (var chave in new[] { "Adm360.Compras.ErrSupplierProductRequired", "Adm360.Compras.OkOrderApproved", "Adm360.Compras.FallbackRegisterInspection" })
            Assert.Contains($"UiText.L(\"{chave}\")", compras);
        foreach (var literal in new[] { "Pedido de compra aprovado com sucesso. Valores congelados.", "A reprovação exige justificativa técnica obrigatória.", "Falha ao registrar decisão da inspeção." })
            Assert.DoesNotContain(literal, compras);

        var financeiro = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Financeiro.cs");
        foreach (var chave in new[] { "Adm360.Financeiro.ErrReversalReasonRequired", "Adm360.Financeiro.OkManualReceipt", "Adm360.Financeiro.OkCashReopened", "Adm360.Financeiro.FallbackPayableTitleNotFound" })
            Assert.Contains($"UiText.L(\"{chave}\")", financeiro);
        // aliases do toast preservados (decisão documentada) com literal do sucesso já keyizado
        Assert.Contains("TempData[ok ? \"SuccessMessage\" : \"ErrorMessage\"] = ok ? UiText.L(\"Adm360.Financeiro.OkManualReceipt\") : resp.Error;", financeiro);
        Assert.Contains("TempData[ok ? \"SuccessMessage\" : \"ErrorMessage\"] = ok ? UiText.L(\"Adm360.Financeiro.OkCashReopened\") : resp.Error;", financeiro);
        foreach (var literal in new[] { "O motivo do estorno é obrigatório.", "Baixa estornada com sucesso. Saldo e caixa revertidos.", "Título a pagar não encontrado.", "Caixa fechado com sucesso." })
            Assert.DoesNotContain(literal, financeiro);

        var vendas = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Vendas.cs");
        Assert.Contains("UiText.L(\"Adm360.Vendas.OkSaleCancelled\")", vendas);
        Assert.Contains("TempData[ok ? \"SuccessMessage\" : \"ErrorMessage\"] = ok ? UiText.L(\"Adm360.Vendas.OkSaleCancelled\") : resp.Error;", vendas);
        foreach (var literal in new[] { "Venda cancelada com sucesso.", "Venda não encontrada.", "O motivo do cancelamento é obrigatório." })
            Assert.DoesNotContain(literal, vendas);

        var vales = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Vales.cs");
        foreach (var chave in new[] { "Adm360.Vales.OkCreated", "Adm360.Vales.FallbackDispatch" })
            Assert.Contains($"UiText.L(\"{chave}\")", vales);
        // mensagens de evento: chaves condicionais em tuple switch por tipo (CONSUMO/RETORNO/PERDA)
        Assert.Contains("\"CONSUMO\" => (\"Adm360.Vales.OkEventConsumption\", \"Adm360.Vales.FallbackEventConsumption\")", vales);
        Assert.Contains("\"PERDA\" => (\"Adm360.Vales.OkEventLoss\", \"Adm360.Vales.FallbackEventLoss\")", vales);
        // interpolação antiga eliminada: chaves condicionais por tipo de evento
        Assert.DoesNotContain("$\"Evento de {tipo} registrado com sucesso.\"", vales);
        Assert.DoesNotContain($"$\"Falha ao registrar evento de {{tipo}}.\"", vales);
        Assert.Contains("(\"Adm360.Vales.OkEventReturn\", \"Adm360.Vales.FallbackEventReturn\")", vales);
        foreach (var literal in new[] { "Vale de consignação criado com sucesso.", "Falha ao expedir vale.", "Evento de consumo registrado com sucesso." })
            Assert.DoesNotContain(literal, vales);

        var cadastros = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Cadastros.cs");
        foreach (var chave in new[] { "Adm360.Cadastros.OkPartnerSaved", "Adm360.Cadastros.OkPartnerActivated", "Adm360.Cadastros.OkPartnerDeactivated", "Adm360.Cadastros.OkProductDeactivated", "Adm360.Cadastros.FallbackSaveLocation" })
            Assert.Contains($"UiText.L(\"{chave}\")", cadastros);
        Assert.DoesNotContain("$\"Parceiro {(ativo", cadastros);
        Assert.DoesNotContain("$\"Produto {(ativo", cadastros);
        Assert.Contains("ativo ? UiText.L(\"Adm360.Cadastros.OkPartnerActivated\") : UiText.L(\"Adm360.Cadastros.OkPartnerDeactivated\")", cadastros);
        foreach (var literal in new[] { "Parceiro cadastrado/atualizado com sucesso.", "Falha ao salvar local.", "Produto inativado com sucesso." })
            Assert.DoesNotContain(literal, cadastros);

        var cirurgias = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Cirurgias.cs");
        Assert.Contains("UiText.L(\"Adm360.Cirurgias.OkScheduled\")", cirurgias);
        Assert.Contains("resp.Error ?? UiText.L(\"Adm360.Cirurgias.FallbackCancel\")", cirurgias);
        foreach (var literal in new[] { "Cirurgia operacional agendada com sucesso.", "Falha ao cancelar cirurgia." })
            Assert.DoesNotContain(literal, cirurgias);

        var organizacao = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Organizacao.cs");
        foreach (var chave in new[] { "Adm360.Organizacao.OkDepartmentDeactivated", "Adm360.Organizacao.OkRoleReactivated", "Adm360.Organizacao.OkStatusTerminated", "Adm360.Organizacao.OkContractCancelled", "Adm360.Organizacao.ErrEmployeeStatusInvalid", "Adm360.Organizacao.FallbackOperationFailed" })
            Assert.Contains($"UiText.L(\"{chave}\")", organizacao);
        Assert.Contains("\"ATIVO\" => UiText.L(\"Adm360.Organizacao.OkStatusReactived\")", organizacao);
        Assert.Contains("erro ?? UiText.L(\"Adm360.Organizacao.FallbackOperationFailed\")", organizacao);
        foreach (var literal in new[] { "Departamento inativado. Cargos e colaboradores", "Colaborador desligado. Encerre os contratos", "Não foi possível concluir a operação. Tente novamente.", "Contrato encerrado. O histórico permanece disponível." })
            Assert.DoesNotContain(literal, organizacao);

        var orcamentos = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Orcamentos.cs");
        foreach (var chave in new[] { "Adm360.Orcamentos.OkApproved", "Adm360.Orcamentos.FallbackNotFound", "Adm360.Orcamentos.FallbackNotFoundOrNotApproved", "Adm360.Orcamentos.FallbackCancelReservation" })
            Assert.Contains($"UiText.L(\"{chave}\")", orcamentos);
        // fallback reutilizado nas 3 ações onde o orçamento não existe
        Assert.Equal(3, Regex.Matches(orcamentos, "UiText\\.L\\(\"Adm360\\.Orcamentos\\.FallbackNotFound\"\\)").Count);
        foreach (var literal in new[] { "Orçamento aprovado com sucesso!", "Orçamento não encontrado ou ainda não aprovado", "Falha ao cancelar reserva." })
            Assert.DoesNotContain(literal, orcamentos);

        var estoque = Read("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.EstoqueValorizacao.cs");
        foreach (var chave in new[] { "Adm360.Estoque.OkInventoryOpened", "Adm360.Estoque.ErrBarcodeAndQuantity", "Adm360.Estoque.FallbackValueVoucher", "Adm360.Estoque.FallbackValueVoucherSaleError" })
            Assert.Contains($"UiText.L(\"{chave}\")", estoque);
        foreach (var literal in new[] { "Inventário aberto com sucesso.", "Vale valorizado, mas houve erro ao gerar a venda", "Código de barras e quantidade positiva são obrigatórios." })
            Assert.DoesNotContain(literal, estoque);
    }

    [Fact]
    public void ViewsAdm360_TresTelasPorRecursosEMaquinasIntegras()
    {
        var cotacoes = Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Cotacoes", "Index.cshtml");
        foreach (var chave in new[]
                 {
                     "Adm360.Cotacoes.PageTitle", "Adm360.Cotacoes.Heading", "Adm360.Cotacoes.Subheading",
                     "Adm360.Cotacoes.ButtonDeParaGeral", "Adm360.Cotacoes.ButtonConfigContas", "Adm360.Cotacoes.GuideTitle",
                     "Adm360.Cotacoes.GuideStep1Title", "Adm360.Cotacoes.GuideStep1Body", "Adm360.Cotacoes.GuideStep2Title",
                     "Adm360.Cotacoes.GuideStep2Body", "Adm360.Cotacoes.GuideStep3Title", "Adm360.Cotacoes.GuideStep3Body",
                     "Adm360.Cotacoes.GuideStep4Title", "Adm360.Cotacoes.GuideStep4Body", "Implantacao.CloseAria",
                     "Adm360.LabelProvedor", "Adm360.Cotacoes.OptAllPortals", "Adm360.Cotacoes.OptManualImport",
                     "Adm360.LabelStatusInterno", "Adm360.Cotacoes.OptAllStatuses", "Adm360.Cotacoes.StatusRecebida",
                     "Adm360.Cotacoes.StatusEmRelacionamento", "Adm360.Cotacoes.StatusEmOrcamento", "Adm360.Cotacoes.StatusAguardandoAprovacao",
                     "Adm360.Cotacoes.StatusProntaEnvio", "Adm360.Cotacoes.StatusRespondida", "Adm360.Cotacoes.StatusExpirada",
                     "Adm360.Cotacoes.StatusBadgeProntaEnvio", "Adm360.ButtonFiltrar", "Adm360.ButtonLimpar", "Adm360.Cotacoes.RegisteredTitle",
                     "Adm360.Cotacoes.ThIdExternalRev", "Adm360.Cotacoes.ThHospitalRequester", "Adm360.Cotacoes.ThProcedure",
                     "Adm360.Cotacoes.ThResponseDeadline", "Adm360.Cotacoes.ThItems", "Adm360.Cotacoes.ThQuote", "Shift.ThActions",
                     "Adm360.Cotacoes.EmptyState", "Adm360.Cotacoes.NotSpecified", "Adm360.Cotacoes.BadgeDeadlineExpired",
                     "Adm360.Cotacoes.UnitItems", "Adm360.Cotacoes.UnitPending", "Adm360.Cotacoes.BadgeMatchingComplete",
                     "Adm360.Cotacoes.QuoteLinked", "Adm360.Cotacoes.QuotePending", "Adm360.Cotacoes.ButtonOpenQuote"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", cotacoes);
        }
        // valores de máquina de provedor/status permanecem íntegros (rotas, selects, switch)
        Assert.Contains("value=\"OPMENEXO\"", cotacoes);
        Assert.Contains("value=\"INPART\"", cotacoes);
        Assert.Contains("value=\"IMPORTACAO_MANUAL\"", cotacoes);
        Assert.Contains("case \"RECEBIDA\":", cotacoes);
        Assert.Contains("selected=\"@(Model.Status == \"PRONTA_PARA_ENVIO\")\"", cotacoes);
        foreach (var literal in new[]
                 {
                     "Central de Cotações Pré-Cirúrgicas", "Captação, relacionamento De/Para", "Como Funciona o Ciclo de Cotação",
                     "Configurar Contas</a>", "Todas as situações", "Prazo de Resposta", "Nenhuma cotação encontrada para os critérios selecionados.",
                     "Prazo Expirado", "Abrir Cotação", "100% De/Para"
                 })
        {
            Assert.DoesNotContain(literal, cotacoes);
        }

        var dashboard = Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Gestao", "Dashboard.cshtml");
        foreach (var chave in new[]
                 {
                     "Adm360.Dash.PageTitle", "Adm360.Dash.Heading", "Adm360.Dash.Subheading", "Adm360.Dash.ButtonExportQuotes",
                     "Adm360.Dash.ButtonExportDocuments", "Adm360.LabelProvedor", "Adm360.Cotacoes.OptAllPortals", "Adm360.Dash.LabelStartDate",
                     "Adm360.Dash.LabelEndDate", "Adm360.ButtonFiltrar", "Adm360.ButtonLimpar", "Adm360.Dash.CapabilitiesTitle",
                     "Adm360.Dash.CapabilitiesIsolationBadge", "Adm360.Dash.CapQuotePortalsTitle", "Adm360.Dash.CapQuotePortalsDesc",
                     "Adm360.Dash.CapXmlCenterTitle", "Adm360.Dash.CapXmlCenterDesc", "Adm360.Dash.CapDashboardTitle", "Adm360.Dash.CapDashboardDesc",
                     "Adm360.Dash.CapabilityEnabled", "Adm360.Dash.CapabilityDisabled", "Adm360.Dash.ButtonEnable", "Adm360.Dash.ButtonDisable",
                     "Adm360.Dash.GroupQuotesTitle", "Adm360.Dash.KpiQuotesReceived", "Adm360.Dash.LinkViewFullList", "Adm360.Dash.KpiAwaitingMatching",
                     "Adm360.Dash.KpiAwaitingMatchingDesc", "Adm360.Dash.KpiDueSoon24h", "Adm360.Dash.KpiDueSoonDesc", "Adm360.Dash.KpiLinkedQuotes",
                     "Adm360.Dash.KpiLinkedQuotesDesc", "Adm360.Dash.KpiAcceptedResponses", "Adm360.Dash.KpiAcceptedResponsesDesc",
                     "Adm360.Dash.KpiWinningProposals", "Adm360.Dash.LostLabel", "Adm360.Dash.KpiSuccessRate", "Adm360.Dash.KpiSuccessRateDesc",
                     "Adm360.Dash.KpiAvgResponseTime", "Adm360.Dash.IntegrationFailuresLabel", "Adm360.Dash.GroupXmlTitle", "Adm360.Dash.KpiTotalDocuments",
                     "Adm360.Dash.UnitCompleteDocs", "Adm360.Dash.UnitSummaryDocs", "Adm360.Dash.KpiPendingVerification", "Adm360.Dash.LinkRunVerification",
                     "Adm360.Dash.KpiLinkedQuarantine", "Adm360.Dash.UnitInQuarantine", "Adm360.Dash.GroupIntegrationsTitle",
                     "Adm360.Dash.KpiActivePortalAccounts", "Adm360.Dash.BlockedLabel", "Adm360.Dash.KpiFailuresLast24h",
                     "Adm360.Dash.KpiFailuresLast24hDesc", "Adm360.Dash.KpiLastGlobalSync", "Adm360.Dash.NoneValue", "Adm360.Dash.LinkOpenFullMonitor"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", dashboard);
        }
        // capacidade contratada é dado de máquina: comparações e nome do hidden intactos
        Assert.Contains("Model.CapacidadesAtivas.Contains(\"PORTAIS_COTACAO\")", dashboard);
        Assert.Contains("<input type=\"hidden\" name=\"capacidade\" value=\"XML_RECEBIDOS\" />", dashboard);
        Assert.Contains("name=\"capacidade\" value=\"DASHBOARD_GERENCIAL\"", dashboard);
        Assert.Contains("/api/administrativo360/gestao/exportar-cotacoes", dashboard);
        foreach (var literal in new[]
                 {
                     "Dashboard Gerencial & Estatístico 360", "Indicadores reais e consolidados", "Exportar Cotações (CSV)",
                     "Capacidades Contratadas no Tenant", "Isolamento Multi-tenant Ativo", "Portais de Cotação", "Central XML Recebidos",
                     "Indicadores de Cotações Pré-Cirúrgicas", "Taxa de Sucesso / Conversão", "Tempo Médio de Resposta",
                     "Pendentes de Conferência", "Última Sincronização Geral", "Abrir monitor completo"
                 })
        {
            Assert.DoesNotContain(literal, dashboard);
        }

        var organizacional = Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Index.cshtml");
        foreach (var chave in new[]
                 {
                     "Adm360.Org.PageTitle", "Adm360.Org.Heading", "Adm360.Org.Subheading", "Implantacao.CloseAria",
                     "Adm360.Org.KpiDepartments", "Adm360.Org.KpiRoles", "Adm360.Org.KpiActiveEmployees", "Adm360.Org.KpiActiveContracts",
                     "Adm360.Org.AreasTitle", "Adm360.Org.AreaDepartments", "Adm360.Org.AreaDepartmentsDesc", "Adm360.Org.AreaRoles",
                     "Adm360.Org.AreaRolesDesc", "Adm360.Org.AreaEmployees", "Adm360.Org.AreaEmployeesDesc", "Adm360.Org.AreaContracts",
                     "Adm360.Org.AreaContractsDesc", "Adm360.Org.HistoryNote"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", organizacional);
        }
        // rotas de navegação por tag helper permanecem intactas
        Assert.Contains("asp-action=\"Departamentos\"", organizacional);
        Assert.Contains("asp-action=\"Cargos\"", organizacional);
        Assert.Contains("asp-action=\"Colaboradores\"", organizacional);
        Assert.Contains("asp-action=\"Contratos\"", organizacional);
        foreach (var literal in new[]
                 {
                     "Gestão Organizacional", "Departamentos, cargos, colaboradores e contratos de trabalho", "Áreas de gestão",
                     "Departamentos cadastrados", "Inativar um departamento, cargo ou colaborador"
                 })
        {
            Assert.DoesNotContain(literal, organizacional);
        }
    }

    [Fact]
    public void ViewsAdm360_NaoRestouLiteralPtBrNosTresArquivosAlvo()
    {
        var arquivos = new[]
        {
            Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Cotacoes", "Index.cshtml"),
            Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Gestao", "Dashboard.cshtml"),
            Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Index.cshtml")
        };
        // palavras-chave de acentuação que só existiam em rótulos removidos
        var literaisRestantes = new[]
        {
            "Pré-Cirúrgicas", "Relacionamento</option>", "Aguardando Aprovação</option>", "Orçamentos Vinculados</div>",
            "Respostas Aceitas (Portal)</div>", "Sincronização Geral</div>", "em quarentena</span>", "cadastro</div>",
            "vinculados preservam", "estrutura organizacional</small>", "matrículas, vínculos e status</small>", "vigentes · admissão"
        };
        foreach (var arquivo in arquivos)
            foreach (var literal in literaisRestantes)
                Assert.DoesNotContain(literal, arquivo);
    }

    [Fact]
    public void RecursosContemAs502ChavesAdm360EmTodosOsQuatroIdiomas()
    {
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = Read("backend", "PlantaoPro.Web", "Localization", arquivo);
            var totalAdm360 = Regex.Matches(resx, "<data name=\"Adm360\\.").Count;
            Assert.Equal(502, totalAdm360);
            foreach (var chave in new[]
                     {
                         // views
                         "Adm360.Cotacoes.PageTitle", "Adm360.Cotacoes.Heading", "Adm360.Cotacoes.GuideStep4Body", "Adm360.Cotacoes.RegisteredTitle",
                         "Adm360.Cotacoes.ThResponseDeadline", "Adm360.Cotacoes.EmptyState", "Adm360.Cotacoes.BadgeMatchingComplete",
                         "Adm360.Cotacoes.StatusBadgeProntaEnvio", "Adm360.Cotacoes.ButtonOpenQuote", "Adm360.LabelProvedor",
                         "Adm360.LabelStatusInterno", "Adm360.ButtonFiltrar", "Adm360.ButtonLimpar",
                         "Adm360.Dash.PageTitle", "Adm360.Dash.Heading", "Adm360.Dash.CapabilitiesIsolationBadge", "Adm360.Dash.KpiDueSoon24h",
                         "Adm360.Dash.LostLabel", "Adm360.Dash.UnitInQuarantine", "Adm360.Dash.NoneValue", "Adm360.Dash.LinkOpenFullMonitor",
                         "Adm360.Org.PageTitle", "Adm360.Org.KpiActiveContracts", "Adm360.Org.AreaDepartmentsDesc", "Adm360.Org.HistoryNote",
                         // mensagens (uma amostra por domínio)
                         "Adm360.Compras.OkOrderApproved", "Adm360.Compras.FallbackRegisterInspection",
                         "Adm360.Financeiro.ErrReversalReasonRequired", "Adm360.Financeiro.OkPaymentReversed", "Adm360.Financeiro.OkCashClosed",
                         "Adm360.Vendas.OkSaleCancelled", "Adm360.Vendas.FallbackSaleNotFound",
                         "Adm360.Vales.OkDispatched", "Adm360.Vales.OkEventReturn", "Adm360.Vales.FallbackEventLoss",
                         "Adm360.Cadastros.OkPartnerActivated", "Adm360.Cadastros.FallbackProductStatus",
                         "Adm360.Cirurgias.OkScheduled", "Adm360.Organizacao.OkDepartmentDeactivated", "Adm360.Organizacao.OkStatusOnLeave",
                         "Adm360.Organizacao.OkContractCancelled", "Adm360.Organizacao.FallbackOperationFailed",
                         "Adm360.Orcamentos.OkApproved", "Adm360.Orcamentos.FallbackNotFound", "Adm360.Orcamentos.FallbackNotFoundOrNotApproved",
                          "Adm360.Estoque.OkVoucherValued", "Adm360.Estoque.FallbackValueVoucherSaleError",
                          // r9h: fluxos ajustados (relatorios financeiros por cliente + DRE + secoes do formulario de produto)
                          "Adm360.RelFin.PageTitle", "Adm360.RelFin.TabDre", "Adm360.RelFin.ExportCsv",
                          "Adm360.RelFin.DreTableTitle", "Adm360.RelFin.DreThResultadoGerencial", "Adm360.RelFin.DreConciliacaoTitulo",
                          "Adm360.Produtos.SecIdentificacao", "Adm360.Produtos.SecPrecificacao",
                          // r9j: completude RelFin (abas/indicadores/guia), Produtos (tela + modal + JS) e bloco ANVISA do detalhe da cotação
                          "Adm360.RelFin.VendasCardTitle", "Adm360.RelFin.ThCompetencia", "Adm360.RelFin.ThReceitaLiquida",
                          "Adm360.RelFin.VencidoBadge", "Adm360.RelFin.DreDrillCliente", "Adm360.RelFin.DreConciliadoTitle", "Adm360.RelFin.DreConcCaixaBody",
                          "Adm360.RelFin.IndLbLimitacoes", "Adm360.RelFin.IndDreLimitacoes", "Adm360.RelFin.GuideTitle", "Adm360.RelFin.GuideSec5Body",
                          "Adm360.Produtos.PageTitle", "Adm360.Produtos.ThUnidade", "Adm360.Produtos.GuiaSkuUnico", "Adm360.Produtos.LoteAviso",
                          "Adm360.Produtos.ConfContinuar", "Adm360.Produtos.JsMargemNeg", "Adm360.Produtos.JsPrevLocal",
                          "Adm360.CotDet.AnvisaBadgeTit", "Adm360.CotDet.AnvisaBadgeNaTxt", "Adm360.CotDet.AnvisaBadgePendTxt",
                          "Adm360.CotDet.SelectAjuda", "Adm360.CotDet.OptSelecioneProduto", "Adm360.CotDet.JsPendenteTexto"
                     })
            {
                Assert.Contains($"<data name=\"{chave}\"", resx, StringComparison.Ordinal);
            }
        }

        // valores pt-BR canônicos travados no arquivo-mestre
        var master = Read("backend", "PlantaoPro.Web", "Localization", "UiTexts.resx");
        Assert.Contains("<value>Central de Cotações — Administrativo 360</value>", master);
        Assert.Contains("<value>Pedido de compra aprovado com sucesso. Valores congelados.</value>", master);
        Assert.Contains("<value>Pronta p/ Envio</value>", master);
        Assert.Contains("<value>Não foi possível concluir a operação. Tente novamente.</value>", master);
        Assert.Contains("<value>cadastrados · estrutura organizacional</value>", master);
        Assert.Contains("<value>Vale valorizado com sucesso e venda interna gerada!</value>", master);
        // r9h
        Assert.Contains("<value>DRE Gerencial por Cliente — competência (NÃO é fluxo de caixa)</value>", master);
        Assert.Contains("<value>Vencimentos por Parcela</value>", master);
        // ampersand escapado no título bilingue do guia (pt-BR canônico)
        Assert.Contains("<value>Captação &amp; Entrada</value>", master);
        // traduções presentes nas demais culturas (amostra)
        var en = Read("backend", "PlantaoPro.Web", "Localization", "UiTexts.en.resx");
        Assert.Contains("<value>Capture &amp; Intake</value>", en);
        Assert.Contains("<value>Purchase order approved successfully. Values frozen.</value>", en);
        // r9h
        Assert.Contains("<value>Management P&amp;L by Client — accrual basis (NOT cash flow)</value>", en);
        Assert.Contains("<value>Due Dates by Installment</value>", en);
        // r9j: pt-BR canônico (inclui <code>/<strong> escapados no resx e espaços preservados)
        Assert.Contains("<value>Vendas Faturadas no Período</value>", master);
        Assert.Contains("<value>&lt;strong&gt;SKU Único:&lt;/strong&gt; O código de identificação do item deve ser exclusivo no seu cliente.</value>", master);
        Assert.Contains("<value> · Vigência regulatória: </value>", master);
        // r9j: amostras en/es/fr
        var es = Read("backend", "PlantaoPro.Web", "Localization", "UiTexts.es.resx");
        var fr = Read("backend", "PlantaoPro.Web", "Localization", "UiTexts.fr.resx");
        Assert.Contains("<value>Billed Sales in Period</value>", en);
        Assert.Contains("<value>ANVISA pending</value>", en);
        Assert.Contains("<value>Ventas Facturadas en el Período</value>", es);
        Assert.Contains("<value>Ventes facturées sur la période</value>", fr);
    }

    [Fact]
    public void ViewsAdm360_R9jRelFinProdutosEAnvisaDoCotDetPorRecursosSemLiterals()
    {
        var relfin = Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "RelatoriosFinanceiros.cshtml");
        foreach (var chave in new[]
                 {
                     "Adm360.RelFin.VendasCardTitle", "Adm360.RelFin.UnitVendas", "Adm360.RelFin.ThCompetencia", "Adm360.RelFin.ThReceitaLiquida",
                     "Adm360.RelFin.ComissoesTotalLabel", "Adm360.RelFin.MargemCardTitle", "Adm360.RelFin.VencimentosCardTitle",
                     "Adm360.RelFin.VencidoTooltip", "Adm360.RelFin.VencidoBadge", "Adm360.RelFin.DreDrillCliente", "Adm360.RelFin.DreDrillOrfa",
                     "Adm360.RelFin.DreConciliadoTitle", "Adm360.RelFin.DreConcBate", "Adm360.RelFin.DreConcCaixaBody",
                     "Adm360.RelFin.IndLbFonte", "Adm360.RelFin.IndVendasFonte", "Adm360.RelFin.IndDreLimitacoes",
                     "Adm360.RelFin.GuideTitle", "Adm360.RelFin.GuideSec1Title", "Adm360.RelFin.GuideSec5Body"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", relfin);
        }
        // formatos/valores de máquina preservados
        Assert.Contains(".ToString(\"C2\", ptBr)", relfin);
        Assert.Contains("<input type=\"hidden\" name=\"aba\" value=\"@Model.AbaAtiva\" />", relfin);
        foreach (var literal in new[]
                 {
                     "Vendas Faturadas no Período", "Nenhuma venda encontrada no período.",
                     "Total Apropriado (líquido de estornos — apenas APROPRIADA):", "Sem saldo vencido na data do relatório",
                     "bate — delta zero", "por construção (sem divisão por cliente nesta base)",
                     "Entendendo os Relatórios Financeiros", "Ver as vendas que compõem esta linha"
                 })
        {
            Assert.DoesNotContain(literal, relfin);
        }

        var produtos = Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Produtos.cshtml");
        foreach (var chave in new[]
                 {
                     "Adm360.Produtos.PageTitle", "Adm360.Produtos.Heading", "Adm360.Produtos.GuiaSkuUnico", "Adm360.Produtos.LoteAviso",
                     "Adm360.Produtos.ThUnidade", "Adm360.Produtos.PlOpcional", "Adm360.Produtos.ConfInativarTitulo",
                     "Adm360.Produtos.ConfContinuar", "Adm360.Produtos.AriaPaginacao", "Adm360.Produtos.JsMargemNeg",
                     "Adm360.Produtos.JsPrevLocal", "Adm360.Produtos.JsProcessando",
                     "Implantacao.CloseAria", "Clinical.ButtonCancel", "Clinical.ButtonSave"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", produtos);
        }
        // estados de máquina e formatos preservados
        Assert.Contains("value=\"NAO_APLICAVEL\"", produtos);
        Assert.Contains(".ToString(\"N2\", new System.Globalization.CultureInfo(\"pt-BR\"))", produtos);
        foreach (var literal in new[]
                 {
                     "Novo Produto", "Editar Produto", "Processando...", "margem negativa", "MARGEM NEGATIVA",
                     "Regras de Cadastro e Integridade de Estoque", "Nenhum produto encontrado com os filtros aplicados.",
                     "Deseja continuar?"
                 })
        {
            Assert.DoesNotContain(literal, produtos);
        }

        var cotdet = Read("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Cotacoes", "Detalhes.cshtml");
        foreach (var chave in new[]
                 {
                     "Adm360.CotDet.AnvisaBadgeTit", "Adm360.CotDet.AnvisaBadgeNaTxt", "Adm360.CotDet.AnvisaBadgePendTxt",
                     "Adm360.CotDet.AnvisaBadgePendTit", "Adm360.CotDet.OptSelecioneProduto", "Adm360.CotDet.LblIdentificadorManual",
                     "Adm360.CotDet.SelectAjuda", "Adm360.CotDet.JsSemProduto", "Adm360.CotDet.JsVigenciaLbl",
                     "Adm360.CotDet.JsRegistroManual", "Adm360.CotDet.JsAnvisaNaTxt", "Adm360.CotDet.JsPendenteTexto"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", cotdet);
        }
        // classes/estados dos badges preservados
        Assert.Contains("pp-badge--neutral", cotdet);
        Assert.Contains("pp-badge--warning", cotdet);
        foreach (var literal in new[]
                 {
                     "Registro ANVISA informado no cadastro do produto",
                     "Registro ANVISA pendente de informação no cadastro do produto",
                     "-- Selecione o Produto --", "ou identificador manual (UUID)",
                     "Selecione no catálogo ou informe o identificador", "Sem produto relacionado — regulação ANVISA",
                     "(registro manual no cadastro)", "número ainda não informado no cadastro do produto."
                 })
        {
            Assert.DoesNotContain(literal, cotdet);
        }
    }
}
