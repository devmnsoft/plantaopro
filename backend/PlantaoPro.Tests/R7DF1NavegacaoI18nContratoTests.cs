using System;
using System.IO;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R7-D-F1: navegação global (sidebar + topbar) passa a usar recursos localizados.
/// Contrato de fonte (mesmo padrão dos contratos R7-E1): os partials não podem voltar a
/// carregar literais pt-BR das seções/itens/estados de navegação e o session-health.js
/// não pode embutir texto pt-BR — ele lê os estados localizados dos data-attrs da view.
/// </summary>
public sealed class R7DF1NavegacaoI18nContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();

    private static string Read(params string[] partes) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(partes).ToArray()));

    [Fact]
    public void Sidebar_UseRecursosENaoCarregaLiteraisPtBr()
    {
        var sidebar = Read("backend", "PlantaoPro.Web", "Views", "Shared", "_AppSidebar.cshtml");

        foreach (var chave in new[]
                 {
                     "Nav.HeaderAria", "Nav.Collapse", "Nav.OpenSearch", "Nav.SearchLabel",
                     "Nav.SectionOverview", "Nav.MyCentral", "Nav.MyDay", "Nav.Dashboard", "Nav.Pendencias",
                     "Nav.SectionOperation", "Nav.Plantoes", "Nav.Agenda", "Nav.Cobertura", "Nav.Convites",
                     "Nav.Escalas", "Nav.Fechamentos",
                     "Nav.SectionClinical", "Nav.JornadaClinica", "Nav.Pacientes", "Nav.Agendamentos",
                     "Nav.Triagem", "Nav.Consultas", "Nav.UnidadesClinica",
                     "Nav.SectionFinance", "Nav.VisaoFinanceira", "Nav.Pagamentos", "Nav.Caixa",
                     "Nav.SectionClientAdmin", "Nav.Administrativo360", "Nav.Relatorios", "Nav.Profissionais",
                     "Nav.Unidades", "Nav.Usuarios", "Nav.Perfis", "Nav.Permissoes", "Nav.Configuracoes",
                     "Nav.SectionMnsOfAdmin", "Nav.CentralGlobal", "Nav.Clientes", "Nav.PlanosComerciais",
                     "Nav.Assinaturas", "Nav.CobrancasSaas", "Nav.CatalogoModulos", "Nav.UsuariosEAcessos",
                     "Nav.PerfisGlobais", "Nav.LogsESaude", "Nav.Auditoria",
                     "Nav.SectionMonitoring", "Nav.Notificacoes",
                     "Nav.SectionSupport", "Nav.CentralAjuda", "Nav.PrivacidadeLgpd",
                     "Nav.FallbackTenant", "Nav.FallbackProfile"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", sidebar);
        }

        foreach (var literal in new[]
                 {
                     "<span>Visão geral</span>", "<span>Minha Central</span>", "<span>Meu dia</span>",
                     "<span>Central de Pendências</span>", "<span>Operação</span>", "<span>Plantões</span>",
                     "<span>Cobertura</span>", "<span>Escalas</span>", "<span>Saúde 360</span>",
                     "<span>Jornada clínica</span>", "<span>Pacientes</span>", "<span>Agendamentos</span>",
                     "<span>Triagem</span>", "<span>Consultas</span>", "<span>Financeiro</span>",
                     "<span>Visão financeira</span>", "<span>Pagamentos</span>",
                     "<span>Administração do Cliente</span>", "<span>Administrativo 360</span>",
                     "<span>Relatórios</span>", "<span>Usuários</span>", "<span>Perfis</span>",
                     "<span>Administração MNSOFT</span>", "<span>Central Global</span>",
                     "<span>Planos comerciais</span>", "<span>Assinaturas</span>",
                     "<span>Cobranças SaaS</span>", "<span>Catálogo de módulos</span>",
                     "<span>Logs e saúde</span>", "<span>Auditoria</span>",
                     "<span>Acompanhamento</span>", "<span>Notificações</span>",
                     "<span class=\"nav-section-title\">Suporte</span>",
                     "<span>Central de ajuda</span>", "<span>Privacidade e LGPD</span>",
                     "aria-label=\"Navegação principal\"", "aria-label=\"Recolher menu lateral\"",
                     "aria-label=\"Abrir busca rápida\"", "<span>Busca rápida</span>",
                     "?? \"Organização\"", "?? \"Profissional\""
                 })
        {
            Assert.DoesNotContain(literal, sidebar);
        }
    }

    [Fact]
    public void Topbar_UseRecursosENaoCarregaLiteraisPtBr()
    {
        var topbar = Read("backend", "PlantaoPro.Web", "Views", "Shared", "_AppTopbar.cshtml");

        foreach (var chave in new[]
                 {
                     "Nav.TopbarDefaultSubtitle", "Nav.TopbarOpenMenu", "Nav.TopbarBreadcrumbAria",
                     "Nav.TopbarLiveContext", "Nav.TopbarConnection", "Nav.TopbarConnectionSecure",
                     "Nav.TopbarConnectionLost", "Nav.TopbarConnectionAriaOnline", "Nav.TopbarConnectionAriaOffline",
                     "Nav.TopbarSearchAria", "Nav.TopbarSearchLabel", "Nav.TopbarHelp"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", topbar);
        }

        foreach (var literal in new[]
                 {
                     "aria-label=\"Abrir menu\"", "aria-label=\"Você está em\"",
                     "Ambiente operacional", "aria-label=\"Buscar no PlantãoPro\"",
                     "<span>Buscar no PlantãoPro</span>", "title=\"Ajuda contextual\"",
                     "<small>Conexão</small><strong data-session-health-label>Protegida</strong>",
                     "\"Acompanhe prioridades e continue sua jornada.\""
                 })
        {
            Assert.DoesNotContain(literal, topbar);
        }
    }

    [Fact]
    public void SessionHealthJs_SemTextoPtBr_E_LiEstadosDosDataAttrsDaView()
    {
        var js = Read("backend", "PlantaoPro.Web", "wwwroot", "js", "session-health.js");
        Assert.Contains("data-online-text", js);
        Assert.Contains("data-offline-text", js);
        Assert.Contains("data-aria-online", js);
        Assert.Contains("data-aria-offline", js);
        Assert.DoesNotContain("\"Protegida\"", js);
        Assert.DoesNotContain("Sem conexão", js);
        Assert.DoesNotContain("Conexão protegida com a plataforma", js);
    }

    [Fact]
    public void RecursosContemAsChavesDeNavegacaoEmTodosOsQuatroIdiomas()
    {
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = Read("backend", "PlantaoPro.Web", "Localization", arquivo);
            foreach (var chave in new[]
                     {
                         "Nav.SectionOverview", "Nav.SectionOperation", "Nav.SectionClinical",
                         "Nav.SectionFinance", "Nav.SectionClientAdmin", "Nav.SectionMnsOfAdmin",
                         "Nav.SectionMonitoring", "Nav.SectionSupport", "Nav.MyCentral", "Nav.Cobertura",
                         "Nav.JornadaClinica", "Nav.Pendencias", "Nav.CentralAjuda", "Nav.PrivacidadeLgpd",
                         "Nav.TopbarOpenMenu", "Nav.TopbarBreadcrumbAria", "Nav.TopbarLiveContext",
                         "Nav.TopbarConnectionLost", "Nav.TopbarConnectionAriaOffline", "Nav.TopbarHelp",
                         "Nav.TopbarDefaultSubtitle"
                     })
            {
                Assert.Contains($"<data name=\"{chave}\"", resx, StringComparison.Ordinal);
            }
        }
    }
}
