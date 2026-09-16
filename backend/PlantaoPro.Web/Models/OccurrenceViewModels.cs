namespace PlantaoPro.Web.Models;

public sealed class OccurrenceDetailViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Titulo { get => Title; set => Title = value; }
    public string Description { get; set; } = string.Empty;
    public string Descricao { get => Description; set => Description = value; }
    public string Categoria { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Situacao { get; set; } = string.Empty;
    public Guid? ResponsavelId { get; set; }
    public DateTimeOffset? PrazoResolucao { get; set; }
    public int Versao { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
    public IReadOnlyList<OccurrenceEventViewModel> Historico { get; set; } = Array.Empty<OccurrenceEventViewModel>();
    public string? Error { get; set; }
}

public sealed class OccurrenceEventViewModel
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public Guid AutorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool VisivelSolicitante { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
