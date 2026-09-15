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
    public string? Error { get; set; }
}
