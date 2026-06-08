using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api.Models;

public sealed class TenantContextDto
{
    public Guid? TenantId { get; set; }
    public Guid? ClienteId { get; set; }
    public string TenantNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public IEnumerable<string> Modulos { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Permissoes { get; set; } = Array.Empty<string>();
}

public sealed class PlanoPublicoDto
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

public sealed class PlanoComparativoDto
{
    public string Grupo { get; set; } = string.Empty;
    public string Recurso { get; set; } = string.Empty;
    public Dictionary<string, string> ValoresPorPlano { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class CadastroEmpresaRequest
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
}

public sealed class CadastroPlanoRequest
{
    public Guid PlanoId { get; set; }
    public string Periodicidade { get; set; } = "MENSAL";
    public string[] ModulosAdicionais { get; set; } = Array.Empty<string>();
    public bool AceiteTermos { get; set; }
    public bool AceitePrivacidade { get; set; }
    public bool ConsentimentoLgpd { get; set; }
}

public sealed class CadastroUsuarioAdminRequest
{
    [Required] public string Nome { get; set; } = string.Empty;
    [Required] public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    [Required] public string Senha { get; set; } = string.Empty;
}

public sealed class CadastroSelfServiceRequest
{
    public CadastroEmpresaRequest Empresa { get; set; } = new CadastroEmpresaRequest();
    public CadastroPlanoRequest Plano { get; set; } = new CadastroPlanoRequest();
    public CadastroUsuarioAdminRequest UsuarioAdmin { get; set; } = new CadastroUsuarioAdminRequest();
}

public sealed class CadastroSelfServiceResultadoDto
{
    public Guid SolicitacaoId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid AssinaturaId { get; set; }
    public Guid UsuarioAdminId { get; set; }
    public string LoginUrl { get; set; } = string.Empty;
    public string OnboardingUrl { get; set; } = string.Empty;
}

public sealed class WhiteLabelConfiguracaoDto
{
    public Guid TenantId { get; set; }
    public string NomePlataforma { get; set; } = "PlantãoPro";
    public string ClienteNome { get; set; } = string.Empty;
    public string Slogan { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string LogoReduzidaUrl { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
    public string CorPrimaria { get; set; } = "#0d6efd";
    public string CorSecundaria { get; set; } = "#20c997";
    public string CorFundo { get; set; } = "#f8fafc";
    public string CorMenu { get; set; } = "#0f172a";
    public string Tema { get; set; } = "claro";
    public string EmailRemetente { get; set; } = string.Empty;
    public string TextoBoasVindas { get; set; } = string.Empty;
    public string TextoRodape { get; set; } = string.Empty;
    public string LoginBannerUrl { get; set; } = string.Empty;
}

public sealed class AssetUploadRequest
{
    public string NomeArquivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public string Url { get; set; } = string.Empty;
}

public sealed class ModuloSistemaDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public sealed class PermissaoDto
{
    public Guid Id { get; set; }
    public string Modulo { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool Sensivel { get; set; }
}

public sealed class PerfilDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ClienteId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool BaseSistema { get; set; }
    public bool Customizado { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class PerfilRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public sealed class PerfilPermissoesRequest
{
    public Guid[] PermissoesPermitidas { get; set; } = Array.Empty<Guid>();
}

public sealed class ParametrizacoesClienteDto
{
    public Guid TenantId { get; set; }
    public Dictionary<string, string> Operacionais { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Financeiras { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Notificacoes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Lgpd { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public WhiteLabelConfiguracaoDto WhiteLabel { get; set; } = new WhiteLabelConfiguracaoDto();
}

public sealed class ParametrosCategoriaRequest
{
    public Dictionary<string, string> Valores { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class OnboardingStatusDto
{
    public Guid TenantId { get; set; }
    public Guid ClienteId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progresso { get; set; }
    public string ProximaAcao { get; set; } = string.Empty;
}

public sealed class OnboardingChecklistItemDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Obrigatorio { get; set; }
    public bool Concluido { get; set; }
    public string LinkAcao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class MinhaAssinaturaDto
{
    public Guid AssinaturaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ValorContratado { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
}

public sealed class SolicitacaoMudancaPlanoRequest
{
    public Guid PlanoDestinoId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
