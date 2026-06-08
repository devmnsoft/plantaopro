namespace PlantaoPro.Web.Models;

public sealed class B2BLaunchCardViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
}

public sealed class B2BLaunchPageViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public IEnumerable<B2BLaunchCardViewModel> Cards { get; set; } = Array.Empty<B2BLaunchCardViewModel>();
}
