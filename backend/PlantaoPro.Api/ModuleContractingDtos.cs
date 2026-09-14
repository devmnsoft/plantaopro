using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api.Models;

public sealed class CommercialModuleDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string[] Funcionalidades { get; set; } = Array.Empty<string>();
    public string[] Dependencias { get; set; } = Array.Empty<string>();
    public decimal? Preco { get; set; }
    public string? Periodicidade { get; set; }
    public string Disponibilidade { get; set; } = string.Empty;
    public string EstadoContratual { get; set; } = string.Empty;
}

public class ContractReviewRequest
{
    [MinLength(1)] public Guid[] ModuloIds { get; set; } = Array.Empty<Guid>();
    public DateTimeOffset? InicioPrevisto { get; set; }
}

public sealed class ContractReviewDto
{
    public string ConditionsVersion { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset InicioPrevisto { get; set; }
    public string Periodicidade { get; set; } = string.Empty;
    public decimal? Total { get; set; }
    public bool RequerProposta { get; set; }
    public CommercialModuleDto[] Itens { get; set; } = Array.Empty<CommercialModuleDto>();
}

public sealed class ConfirmContractRequest : ContractReviewRequest
{
    [Required] public string ConditionsVersion { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class ContractRequestDto
{
    public Guid Id { get; set; }
    public string Protocolo { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConditionsVersion { get; set; } = string.Empty;
    public decimal? Total { get; set; }
    public string Periodicidade { get; set; } = string.Empty;
    public DateTimeOffset InicioPrevisto { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? DecididoEm { get; set; }
    public string? Justificativa { get; set; }
    public CommercialModuleDto[] Itens { get; set; } = Array.Empty<CommercialModuleDto>();
}

public sealed class ContractDecisionRequest
{
    [Required] public string ConditionsVersion { get; set; } = string.Empty;
    [MaxLength(500)] public string Justificativa { get; set; } = string.Empty;
}
