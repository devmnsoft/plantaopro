namespace PlantaoPro.Web.Models;

public class AgendaOperacionalViewModel
{
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string? Status { get; set; }
    public IEnumerable<PlantaoResumoDto> Itens { get; set; } = Array.Empty<PlantaoResumoDto>();
    public long Total { get; set; }
    public string? ErrorMessage { get; set; }
}
