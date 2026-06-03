using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models;

public sealed class OperacaoAssistidaIndexViewModel
{
    public IEnumerable<OperacaoAssistidaClienteViewModel> Clientes { get; set; } = Array.Empty<OperacaoAssistidaClienteViewModel>();
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public bool HasItems => (Clientes ?? Array.Empty<OperacaoAssistidaClienteViewModel>()).Any();
}

public sealed class OperacaoAssistidaClienteViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime? InicioPrevisto { get; set; }
    public DateTime? GoLivePrevisto { get; set; }
    public int Percentual { get; set; }
    public string Risco { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public long OcorrenciasAbertas { get; set; }
    public long OcorrenciasCriticas { get; set; }
}

public sealed class OperacaoAssistidaDetalheViewModel
{
    public OperacaoAssistidaClienteViewModel Cliente { get; set; } = new();
    public IEnumerable<OperacaoAssistidaChecklistItemViewModel> Checklist { get; set; } = Array.Empty<OperacaoAssistidaChecklistItemViewModel>();
    public IEnumerable<OperacaoAssistidaOcorrenciaViewModel> Ocorrencias { get; set; } = Array.Empty<OperacaoAssistidaOcorrenciaViewModel>();
    public IEnumerable<OperacaoAssistidaTreinamentoViewModel> Treinamentos { get; set; } = Array.Empty<OperacaoAssistidaTreinamentoViewModel>();
    public string? ErrorMessage { get; set; }
}

public sealed class OperacaoAssistidaChecklistPageViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public IEnumerable<OperacaoAssistidaChecklistItemViewModel> Itens { get; set; } = Array.Empty<OperacaoAssistidaChecklistItemViewModel>();
    public int Percentual { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class OperacaoAssistidaChecklistItemViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public int Ordem { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Concluido { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    public string ConcluidoPor { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
}

public sealed class OperacaoAssistidaOcorrenciasPageViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Prioridade { get; set; }
    public IEnumerable<OperacaoAssistidaOcorrenciaViewModel> Ocorrencias { get; set; } = Array.Empty<OperacaoAssistidaOcorrenciaViewModel>();
    public NovaOperacaoAssistidaOcorrenciaViewModel NovaOcorrencia { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class OperacaoAssistidaOcorrenciaViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Solucao { get; set; } = string.Empty;
    public DateTime DataAbertura { get; set; }
    public DateTime? DataResolucao { get; set; }
}

public sealed class NovaOperacaoAssistidaOcorrenciaViewModel
{
    [Required]
    public Guid ClienteId { get; set; }
    [Required]
    public string Tipo { get; set; } = "DUVIDA";
    [Required]
    public string Prioridade { get; set; } = "MEDIA";
    [Required]
    [StringLength(1000)]
    public string Descricao { get; set; } = string.Empty;
    [StringLength(120)]
    public string? Responsavel { get; set; }
}

public sealed class OperacaoAssistidaTreinamentosPageViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public IEnumerable<OperacaoAssistidaTreinamentoViewModel> Treinamentos { get; set; } = Array.Empty<OperacaoAssistidaTreinamentoViewModel>();
    public NovoOperacaoAssistidaTreinamentoViewModel NovoTreinamento { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class OperacaoAssistidaTreinamentoViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Tema { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string Participantes { get; set; } = string.Empty;
    public DateTime RealizadoEm { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}

public sealed class NovoOperacaoAssistidaTreinamentoViewModel
{
    [Required]
    public Guid ClienteId { get; set; }
    [Required]
    public string Tema { get; set; } = string.Empty;
    public string? Perfil { get; set; }
    public string? Responsavel { get; set; }
    public string? Participantes { get; set; }
    public DateTime? RealizadoEm { get; set; }
    public string? Observacoes { get; set; }
}
