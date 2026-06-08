using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models;

public sealed class PlanoPublicoWebViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public int LimiteMedicos { get; set; }
    public int LimiteHospitais { get; set; }
    public int LimitePlantoesMes { get; set; }
    public int LimiteUsuarios { get; set; }
    public bool PermiteMobile { get; set; }
    public bool PermiteBi { get; set; }
    public bool PermiteWhiteLabel { get; set; }
    public bool Destaque { get; set; }
    public IEnumerable<string> Recursos { get; set; } = Array.Empty<string>();
}

public sealed class PlanoFaqWebViewModel
{
    public string Pergunta { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
}

public sealed class CadastroSelfServiceWebViewModel
{
    [Required] public string NomeFantasia { get; set; } = string.Empty;
    [Required] public string RazaoSocial { get; set; } = string.Empty;
    [Required] public string Cnpj { get; set; } = string.Empty;
    public string Segmento { get; set; } = string.Empty;
    public int QuantidadeMedicos { get; set; }
    public int QuantidadeHospitais { get; set; }
    public int VolumePlantoesMes { get; set; }
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    [Required] public string EmailCorporativo { get; set; } = string.Empty;
    public Guid PlanoId { get; set; }
    public string Periodicidade { get; set; } = "MENSAL";
    [Required] public string ResponsavelNome { get; set; } = string.Empty;
    [Required] public string ResponsavelEmail { get; set; } = string.Empty;
    public string ResponsavelTelefone { get; set; } = string.Empty;
    public string ResponsavelCargo { get; set; } = string.Empty;
    [Required] public string Senha { get; set; } = string.Empty;
    public bool AceiteTermos { get; set; }
    public bool AceitePrivacidade { get; set; }
    public bool ConsentimentoLgpd { get; set; }
    public IEnumerable<PlanoPublicoWebViewModel> Planos { get; set; } = Array.Empty<PlanoPublicoWebViewModel>();
}

public sealed class WhiteLabelWebViewModel
{
    public Guid TenantId { get; set; }
    public string NomePlataforma { get; set; } = "PlantãoPro";
    public string ClienteNome { get; set; } = string.Empty;
    public string Slogan { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
    public string CorPrimaria { get; set; } = "#0d6efd";
    public string CorSecundaria { get; set; } = "#20c997";
    public string CorFundo { get; set; } = "#f8fafc";
    public string CorMenu { get; set; } = "#0f172a";
    public string Tema { get; set; } = "claro";
    public string EmailRemetente { get; set; } = string.Empty;
    public string TextoBoasVindas { get; set; } = string.Empty;
    public string TextoRodape { get; set; } = string.Empty;
}

public sealed class PerfilWebViewModel
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool BaseSistema { get; set; }
    public bool Customizado { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ParametrizacoesWebViewModel
{
    public Dictionary<string, string> Operacionais { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Financeiras { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Notificacoes { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Lgpd { get; set; } = new Dictionary<string, string>();
}
