using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor + "," + RolesConstants.Suporte + "," + RolesConstants.Auditor)]
public sealed class PermissoesController : Controller
{
    private readonly IPermissionService permissionService;

    public PermissoesController(IPermissionService permissionService)
    {
        this.permissionService = permissionService;
    }

    public IActionResult Index() => View("Matriz", CriarMatriz());
    public IActionResult Matriz() => View(CriarMatriz());
    public IActionResult PorPerfil(string? perfil) => View("Matriz", CriarMatriz(perfil));
    public IActionResult PorUsuario(string? usuario) => View("Matriz", CriarMatriz());

    [HttpGet]
    public IActionResult TestarAcesso() => View(CriarMatriz());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TestarAcesso(string perfil, string modulo, string acao)
    {
        var model = CriarMatriz(perfil);
        model.PerfilTeste = perfil ?? string.Empty;
        model.ModuloTeste = modulo ?? string.Empty;
        model.AcaoTeste = acao ?? string.Empty;
        var permitido = SimularPerfil(perfil, modulo, acao);
        model.ResultadoTeste = permitido
            ? "Acesso permitido: perfil possui permissão e o módulo está disponível no contexto do tenant."
            : "Acesso bloqueado: regra de perfil, plano, módulo ou tenant não autoriza esta ação.";
        TempData[permitido ? "Success" : "Warning"] = model.ResultadoTeste;
        return View(model);
    }

    private bool SimularPerfil(string? perfil, string? modulo, string? acao)
    {
        if (string.IsNullOrWhiteSpace(perfil) || string.IsNullOrWhiteSpace(modulo)) return false;
        if (string.Equals(perfil, RolesConstants.AdministradorGlobal, StringComparison.OrdinalIgnoreCase)) return true;
        return CriarMatriz(perfil).Modulos.Any(m => string.Equals(m.Codigo, modulo, StringComparison.OrdinalIgnoreCase) && m.PermissoesPorPerfil.TryGetValue(perfil, out var acoes) && acoes.Contains(acao ?? "VER", StringComparer.OrdinalIgnoreCase));
    }

    private static PermissionMatrixViewModel CriarMatriz(string? perfilSelecionado = null)
    {
        var perfis = new List<string>
        {
            RolesConstants.AdministradorGlobal,
            RolesConstants.AdministradorCliente,
            RolesConstants.Coordenador,
            RolesConstants.Financeiro,
            RolesConstants.Medico,
            RolesConstants.Hospital,
            RolesConstants.Parceiro,
            RolesConstants.Suporte,
            RolesConstants.Auditor,
            RolesConstants.Comercial,
            RolesConstants.CustomerSuccess
        };

        var modulos = new List<PermissionModuleViewModel>
        {
            Modulo("ADMIN_SAAS", "Admin SaaS", new List<string>{"VER","GERENCIAR"}, new List<string>{RolesConstants.AdministradorGlobal, RolesConstants.Suporte, RolesConstants.Auditor}),
            Modulo("CLIENTE_PORTAL", "Portal Cliente", new List<string>{"VER","GERENCIAR"}, new List<string>{RolesConstants.AdministradorGlobal, RolesConstants.AdministradorCliente}),
            Modulo("CENTRAL_ESCALA", "Central de Escala", new List<string>{"VER","CRIAR","EDITAR"}, new List<string>{RolesConstants.AdministradorGlobal, RolesConstants.AdministradorCliente, RolesConstants.Coordenador}),
            Modulo("FINANCEIRO", "Financeiro", new List<string>{"VER","CONFIRMAR","EXPORTAR"}, new List<string>{RolesConstants.AdministradorGlobal, RolesConstants.AdministradorCliente, RolesConstants.Financeiro}),
            Modulo("MEDICO_AREA", "Área do Médico", new List<string>{"VER","ACEITAR_CONVITE","DISPONIBILIDADE"}, new List<string>{RolesConstants.Medico}),
            Modulo("WHITE_LABEL", "White label", new List<string>{"VER","EDITAR","PREVIEW"}, new List<string>{RolesConstants.AdministradorGlobal, RolesConstants.AdministradorCliente}),
            Modulo("PARCEIRO", "Portal Parceiro", new List<string>{"VER","PROPOSTAS","COMISSOES"}, new List<string>{RolesConstants.Parceiro, RolesConstants.AdministradorGlobal}),
            Modulo("CUSTOMER_SUCCESS", "Customer Success", new List<string>{"VER","PLANO_ACAO"}, new List<string>{RolesConstants.CustomerSuccess, RolesConstants.AdministradorGlobal})
        };

        if (!string.IsNullOrWhiteSpace(perfilSelecionado))
        {
            perfis = perfis.Where(p => string.Equals(p, perfilSelecionado, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return new PermissionMatrixViewModel { Perfis = perfis, Modulos = modulos };
    }

    private static PermissionModuleViewModel Modulo(string codigo, string nome, IList<string> acoes, IList<string> perfisPermitidos)
    {
        var module = new PermissionModuleViewModel { Codigo = codigo, Nome = nome, Acoes = acoes };
        foreach (var perfil in perfisPermitidos)
        {
            module.PermissoesPorPerfil[perfil] = new List<string>(acoes);
        }
        return module;
    }
}

[Authorize(Roles = RolesConstants.Operacao)]
public sealed class CoordenacaoController : Controller
{
    public IActionResult Index() => View();
}

[Authorize]
public sealed class MarketplaceController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Details(string id) => View("Index");
}
