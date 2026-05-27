using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models;

public sealed class PilotoResumoViewModel
{
    public string ClientePiloto { get; set; } = string.Empty;
    public int UsuariosAtivos { get; set; }
    public int PlantoesCriados { get; set; }
    public int PlantoesPublicados { get; set; }
    public int EscalasConfirmadas { get; set; }
    public int PagamentosConfirmados { get; set; }
    public int NotificacoesEnviadas { get; set; }
    public int OcorrenciasAbertas { get; set; }
    public int OcorrenciasResolvidas { get; set; }
    public int PendenciasImplantacao { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class PilotoChecklistViewModel
{
    public List<PilotoChecklistItemViewModel> Itens { get; set; } = new();
    public int Total { get; set; }
    public int Concluidos { get; set; }
    public int Percentual { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class PilotoChecklistItemViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public bool Concluido { get; set; }
}

public sealed class PilotoOcorrenciasViewModel
{
    public List<PilotoOcorrenciaViewModel> Ocorrencias { get; set; } = new();
    public NovaPilotoOcorrenciaViewModel NovaOcorrencia { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class PilotoOcorrenciaViewModel
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime DataAbertura { get; set; }
    public DateTime? DataResolucao { get; set; }
}

public sealed class NovaPilotoOcorrenciaViewModel
{
    [Required(ErrorMessage = "O tipo é obrigatório.")]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A prioridade é obrigatória.")]
    public string Prioridade { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    public string? Responsavel { get; set; }
}
