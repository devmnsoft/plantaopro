using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models;

public sealed class ClientModulesPageViewModel
{
    public IReadOnlyList<ClientModuleViewModel> Catalogo { get; set; } = Array.Empty<ClientModuleViewModel>();
    public IReadOnlyList<ClientModuleRequestViewModel> Solicitacoes { get; set; } = Array.Empty<ClientModuleRequestViewModel>();
    public ClientModuleReviewViewModel? Revisao { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class ClientModuleViewModel
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string[] Funcionalidades { get; set; } = Array.Empty<string>();
    public string[] Dependencias { get; set; } = Array.Empty<string>();
    public decimal? Preco { get; set; }
    public decimal? PrecoContratado { get; set; }
    public string? Periodicidade { get; set; }
    public int? LimiteContratado { get; set; }
    public DateTimeOffset? VigenciaInicio { get; set; }
    public DateTimeOffset? VigenciaFim { get; set; }
    public string Disponibilidade { get; set; } = string.Empty;
    public string EstadoContratual { get; set; } = string.Empty;
}

public class ClientModuleReviewInput
{
    [MinLength(1, ErrorMessage = "Selecione ao menos um módulo.")]
    public Guid[] ModuloIds { get; set; } = Array.Empty<Guid>();
    public DateTimeOffset? InicioPrevisto { get; set; }
}

public sealed class ClientModuleConfirmInput : ClientModuleReviewInput
{
    [Required] public string ConditionsVersion { get; set; } = string.Empty;
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class ClientModuleReviewViewModel
{
    public string ConditionsVersion { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset InicioPrevisto { get; set; }
    public string Periodicidade { get; set; } = string.Empty;
    public decimal? Total { get; set; }
    public bool RequerProposta { get; set; }
    public ClientModuleViewModel[] Itens { get; set; } = Array.Empty<ClientModuleViewModel>();
}

public sealed class ClientModuleRequestViewModel
{
    public Guid Id { get; set; }
    public string Protocolo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConditionsVersion { get; set; } = string.Empty;
    public decimal? Total { get; set; }
    public string Periodicidade { get; set; } = string.Empty;
    public DateTimeOffset InicioPrevisto { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? DecididoEm { get; set; }
    public string? Justificativa { get; set; }
    public ClientModuleViewModel[] Itens { get; set; } = Array.Empty<ClientModuleViewModel>();
}
