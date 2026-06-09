using System.Security.Claims;

namespace PlantaoPro.Web.Security;

public static class UserRoleExtensions
{
    public static bool HasRole(this ClaimsPrincipal user, string role) => user.IsInRole(role);
    public static bool IsGlobalAdmin(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.AdministradorGlobal);
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsGlobalAdmin() || user.IsInRole(RolesConstants.Administrador) || user.IsInRole(RolesConstants.AdministradorCliente);
    public static bool IsTenantAdmin(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Administrador) || user.IsInRole(RolesConstants.AdministradorCliente) || user.IsInRole(RolesConstants.Diretor);
    public static bool IsMedico(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Medico);
    public static bool IsFinanceiro(this ClaimsPrincipal user) => user.IsGlobalAdmin() || user.IsInRole(RolesConstants.Financeiro);
    public static bool IsCoordenacao(this ClaimsPrincipal user) => user.IsGlobalAdmin() || user.IsInRole(RolesConstants.Coordenacao) || user.IsInRole(RolesConstants.Coordenador);
    public static bool IsHospital(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Hospital);
    public static bool IsOperador(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Operador);
    public static bool IsParceiro(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Parceiro);
    public static bool IsSuporte(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Suporte);
    public static bool IsAuditor(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Auditor);
    public static bool IsComercial(this ClaimsPrincipal user) => user.IsGlobalAdmin() || user.IsInRole(RolesConstants.Comercial);
    public static bool IsCustomerSuccess(this ClaimsPrincipal user) => user.IsGlobalAdmin() || user.IsInRole(RolesConstants.CustomerSuccess);

    public static string PrimaryRole(this ClaimsPrincipal user)
    {
        var priority = new List<string>
        {
            RolesConstants.AdministradorGlobal,
            RolesConstants.Administrador,
            RolesConstants.AdministradorCliente,
            RolesConstants.Diretor,
            RolesConstants.CustomerSuccess,
            RolesConstants.Comercial,
            RolesConstants.Suporte,
            RolesConstants.Auditor,
            RolesConstants.Parceiro,
            RolesConstants.Financeiro,
            RolesConstants.Coordenacao,
            RolesConstants.Coordenador,
            RolesConstants.Operador,
            RolesConstants.Hospital,
            RolesConstants.Medico
        };

        return priority.FirstOrDefault(user.IsInRole) ?? "USUARIO";
    }
}
