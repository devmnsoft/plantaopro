using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PlantaoPro.Web.Services.Security;

public sealed class SaasRouteGuardFilter : IActionFilter
{
    private static readonly IReadOnlyDictionary<string, string> ControllerModules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["AdminSaas"] = "ADMIN_SAAS",
        ["Clientes"] = "CLIENTES",
        ["Planos"] = "PLANOS",
        ["Assinaturas"] = "ASSINATURAS",
        ["Billing"] = "BILLING",
        ["FaturamentoSaas"] = "BILLING",
        ["Marketplace"] = "MARKETPLACE",
        ["WhiteLabel"] = "WHITE_LABEL",
        ["Permissoes"] = "PERMISSOES",
        ["Observabilidade"] = "OBSERVABILIDADE_GLOBAL",
        ["Auditoria"] = "AUDITORIA",
        ["ClientePortal"] = "CLIENTE_PORTAL",
        ["Usuarios"] = "USUARIOS",
        ["Perfis"] = "PERFIS",
        ["Parametrizacoes"] = "CONFIGURACOES",
        ["Configuracoes"] = "CONFIGURACOES",
        ["CentralEscala"] = "CENTRAL_ESCALA",
        ["Plantoes"] = "PLANTOES",
        ["Convites"] = "CONVITES",
        ["Escalas"] = "ESCALAS",
        ["Medicos"] = "MEDICOS",
        ["Hospitais"] = "HOSPITAIS",
        ["Especialidades"] = "ESPECIALIDADES",
        ["Agenda"] = "AGENDA",
        ["Comunicacao"] = "COMUNICACAO",
        ["Financeiro"] = "FINANCEIRO",
        ["Pagamentos"] = "PAGAMENTOS",
        ["Relatorios"] = "RELATORIOS",
        ["MedicoArea"] = "MEDICO_AREA",
        ["MinhaAgenda"] = "MINHA_AGENDA",
        ["HospitalArea"] = "HOSPITAL_AREA",
        ["ParceiroPortal"] = "PARCEIRO",
        ["Comercial"] = "COMERCIAL",
        ["PropostasComerciais"] = "PROPOSTAS",
        ["CustomerSuccess"] = "CUSTOMER_SUCCESS",
        ["Onboarding"] = "ONBOARDING",
        ["Lgpd"] = "LGPD",
        ["Suporte"] = "SUPORTE"
    };

    private static readonly ISet<string> PublicControllers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Account",
        "Error",
        "Home",
        "PlanosPublicos",
        "Cadastro",
        "Demo"
    };

    private readonly IPermissionService permissions;
    private readonly ICurrentUserService currentUser;
    private readonly ILogger<SaasRouteGuardFilter> logger;

    public SaasRouteGuardFilter(IPermissionService permissions, ICurrentUserService currentUser, ILogger<SaasRouteGuardFilter> logger)
    {
        this.permissions = permissions;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return;
        }

        if (IsAnonymousAllowed(descriptor) || PublicControllers.Contains(descriptor.ControllerName))
        {
            return;
        }

        if (!ControllerModules.TryGetValue(descriptor.ControllerName, out var module))
        {
            return;
        }

        var action = ResolvePermissionAction(descriptor.ActionName);
        if (permissions.HasPermission(module, action))
        {
            return;
        }

        logger.LogWarning("Acesso negado pelo guard SaaS. Usuario:{UsuarioId} Roles:{Roles} Controller:{Controller} Action:{Action} Modulo:{Modulo} Acao:{Acao} Tenant:{TenantId} Cliente:{ClienteId}",
            currentUser.UserId,
            string.Join(',', currentUser.Roles()),
            descriptor.ControllerName,
            descriptor.ActionName,
            module,
            action,
            currentUser.TenantId,
            currentUser.ClienteId);

        context.HttpContext.Items["SaasAccessDeniedModule"] = module;
        context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = string.Empty });
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static bool IsAnonymousAllowed(ControllerActionDescriptor descriptor)
    {
        return descriptor.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length > 0
            || descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length > 0;
    }

    private static string ResolvePermissionAction(string actionName)
    {
        if (string.Equals(actionName, "Create", StringComparison.OrdinalIgnoreCase) || string.Equals(actionName, "Criar", StringComparison.OrdinalIgnoreCase)) return "CRIAR";
        if (string.Equals(actionName, "Edit", StringComparison.OrdinalIgnoreCase) || string.Equals(actionName, "Editar", StringComparison.OrdinalIgnoreCase)) return "EDITAR";
        if (string.Equals(actionName, "Delete", StringComparison.OrdinalIgnoreCase) || string.Equals(actionName, "Excluir", StringComparison.OrdinalIgnoreCase) || string.Equals(actionName, "Cancelar", StringComparison.OrdinalIgnoreCase)) return "EXCLUIR";
        if (string.Equals(actionName, "Exportar", StringComparison.OrdinalIgnoreCase)) return "EXPORTAR";
        if (string.Equals(actionName, "MarcarPaga", StringComparison.OrdinalIgnoreCase) || string.Equals(actionName, "Confirmar", StringComparison.OrdinalIgnoreCase)) return "CONFIRMAR";
        return "VER";
    }
}
