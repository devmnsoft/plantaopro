using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Models;

// ============================================================================
// B5 - Convites de equipe (Web): lista/criação no tenant + aceite público.
// O token sai SÓ na criação (TempData uma vez); depois nunca é exibido.
// ============================================================================

public sealed class ConviteEquipeWebViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string[] PerfilNomes { get; set; } = Array.Empty<string>();
    public DateTime ExpiraEm { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

public sealed class ConvitesEquipeIndexViewModel
{
    public IReadOnlyList<ConviteEquipeWebViewModel> Convites { get; set; } = Array.Empty<ConviteEquipeWebViewModel>();
    public IEnumerable<PerfilOpcaoUsuarioViewModel> Perfis { get; set; } = Array.Empty<PerfilOpcaoUsuarioViewModel>();
    public string? TokenCriado { get; set; }
    public string? EmailConvidado { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>R5-A3: área global sem cliente ativo não tem equipe; aviso honesto no lugar de erro 403.</summary>
    public bool SemContextoCliente { get; set; }
    public string? InfoMessage { get; set; }
}

public sealed class ConviteAceiteWebViewModel
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TenantNome { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public bool Valido { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
