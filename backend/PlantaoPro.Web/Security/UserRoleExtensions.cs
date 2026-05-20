using System.Security.Claims;

namespace PlantaoPro.Web.Security;

public static class UserRoleExtensions
{
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Administrador);
    public static bool IsMedico(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Medico);
    public static bool IsFinanceiro(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Financeiro);
    public static bool IsCoordenacao(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Coordenacao);
    public static bool IsHospital(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Hospital);
    public static bool IsOperador(this ClaimsPrincipal user) => user.IsInRole(RolesConstants.Operador);
}
