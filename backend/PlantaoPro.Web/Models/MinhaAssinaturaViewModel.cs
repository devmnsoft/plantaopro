namespace PlantaoPro.Web.Models;

public sealed class MinhaAssinaturaViewModel
{
    public Guid AssinaturaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ValorContratado { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public string? ErrorMessage { get; set; }
    public bool Disponivel => AssinaturaId != Guid.Empty;
}
