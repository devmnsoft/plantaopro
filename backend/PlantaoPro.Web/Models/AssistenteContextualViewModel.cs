namespace PlantaoPro.Web.Models;

public sealed class AssistenteContextualViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string ProximaAcao { get; set; } = string.Empty;
    public string LinkProximaAcao { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public IEnumerable<string> Dicas { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Alertas { get; set; } = Array.Empty<string>();
}
