namespace PlantaoPro.Web.Models;

public sealed class AjudaInterativaIndexViewModel
{
    public string Busca { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public List<AjudaInterativaTopicoViewModel> Topicos { get; set; } = new List<AjudaInterativaTopicoViewModel>();
    public List<AjudaInterativaArtigoViewModel> Artigos { get; set; } = new List<AjudaInterativaArtigoViewModel>();
}

public sealed class AjudaInterativaTopicoViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
}

public sealed class AjudaInterativaArtigoViewModel
{
    public string Id { get; set; } = string.Empty;
    public string TopicoSlug { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public List<string> Passos { get; set; } = new List<string>();
}

public sealed class AjudaInterativaChecklistViewModel
{
    public string Perfil { get; set; } = string.Empty;
    public List<AjudaInterativaArtigoViewModel> Artigos { get; set; } = new List<AjudaInterativaArtigoViewModel>();
}

public sealed class AjudaFeedbackWebViewModel
{
    public string ArtigoId { get; set; } = string.Empty;
    public bool Util { get; set; }
    public string Comentario { get; set; } = string.Empty;
}
