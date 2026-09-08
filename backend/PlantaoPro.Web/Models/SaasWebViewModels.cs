using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models;

public sealed class UsuarioSaasViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ClienteId { get; set; }
    public string TenantNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RegStatus { get; set; } = string.Empty;
    public DateTime? UltimoLogin { get; set; }
    public DateTime? BloqueadoAte { get; set; }
    public DateTime RegDate { get; set; }
    public string Perfis { get; set; } = string.Empty;
}

public sealed class UsuarioEditorViewModel : IValidatableObject
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string TenantNome { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o nome do usuário."), StringLength(160, MinimumLength = 3, ErrorMessage = "Informe um nome entre 3 e 160 caracteres.")]
    public string Nome { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(254)]
    public string Email { get; set; } = string.Empty;
    [Phone(ErrorMessage = "Informe um telefone válido."), StringLength(30)]
    public string? Telefone { get; set; }
    [DataType(DataType.Password), StringLength(128)]
    public string? SenhaTemporaria { get; set; }
    public Guid[] PerfilIds { get; set; } = Array.Empty<Guid>();
    public IEnumerable<PerfilOpcaoUsuarioViewModel> PerfisDisponiveis { get; set; } = Array.Empty<PerfilOpcaoUsuarioViewModel>();
    public IEnumerable<ClienteDto> Clientes { get; set; } = Array.Empty<ClienteDto>();
    public bool IsGlobalAdmin { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id == Guid.Empty && string.IsNullOrWhiteSpace(SenhaTemporaria))
            yield return new ValidationResult("Informe uma senha temporária.", new[] { nameof(SenhaTemporaria) });
        if (!string.IsNullOrWhiteSpace(SenhaTemporaria) &&
            (SenhaTemporaria.Length < 10 || !SenhaTemporaria.Any(char.IsUpper) || !SenhaTemporaria.Any(char.IsLower) || !SenhaTemporaria.Any(char.IsDigit) || !SenhaTemporaria.Any(ch => !char.IsLetterOrDigit(ch))))
            yield return new ValidationResult("Use ao menos 10 caracteres, com maiúscula, minúscula, número e símbolo.", new[] { nameof(SenhaTemporaria) });
        if (PerfilIds is null || PerfilIds.Length == 0)
            yield return new ValidationResult("Selecione ao menos um perfil.", new[] { nameof(PerfilIds) });
        if (IsGlobalAdmin && !TenantId.HasValue)
            yield return new ValidationResult("Selecione o cliente do usuário.", new[] { nameof(TenantId) });
    }
}

public sealed class PerfilOpcaoUsuarioViewModel
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool BaseSistema { get; set; }
}

public sealed class SaasModuleViewModel
{
    public Guid Id { get; set; }
    [Required, RegularExpression("^[A-Z][A-Z0-9_]{1,79}$", ErrorMessage = "Use letras maiúsculas, números e sublinhado.")]
    public string Codigo { get; set; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 3)]
    public string Nome { get; set; } = string.Empty;
    [Required, StringLength(2000, MinimumLength = 10)]
    public string Descricao { get; set; } = string.Empty;
    [Required]
    public string Categoria { get; set; } = "OPERACAO";
    [Range(typeof(decimal), "0", "10000000")]
    public decimal PrecoBase { get; set; }
    public bool Essencial { get; set; }
    public string Status { get; set; } = "ATIVO";
    public string FuncionalidadesJson { get; set; } = "[]";
    public string FuncionalidadesTexto { get; set; } = string.Empty;
    [Range(0, int.MaxValue)]
    public int? LimitePadrao { get; set; }
    public long ClientesAtivos { get; set; }
    public bool Contratado { get; set; }
    public bool Habilitado { get; set; }
    public decimal? PrecoContratado { get; set; }
    public int? LimiteContratado { get; set; }
}

public sealed class TenantModulePageViewModel
{
    public Guid TenantId { get; set; }
    public string TenantNome { get; set; } = string.Empty;
    public IEnumerable<SaasModuleViewModel> Modules { get; set; } = Array.Empty<SaasModuleViewModel>();
}

public sealed class TenantModuleActionViewModel
{
    [NonEmptyGuid]
    public Guid TenantId { get; set; }
    [NonEmptyGuid]
    public Guid ModuleId { get; set; }
    public bool Enabled { get; set; }
    [Range(typeof(decimal), "0", "10000000")]
    public decimal? PrecoContratado { get; set; }
    [Range(0, int.MaxValue)]
    public int? LimiteContratado { get; set; }
    [Required, StringLength(500, MinimumLength = 5)]
    public string Motivo { get; set; } = string.Empty;
}

public sealed class PlanoSaasViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public int LimiteMedicos { get; set; }
    public int LimiteHospitais { get; set; }
    public int LimitePlantoesMes { get; set; }
    public int LimiteUsuarios { get; set; }
    public int LimiteConvitesMes { get; set; }
    public bool PermiteMobile { get; set; }
    public bool PermiteBi { get; set; }
    public bool PermiteRelatoriosAvancados { get; set; }
    public bool PermiteIntegracoes { get; set; }
    public bool PermiteOperacaoAssistida { get; set; }
    public bool PermiteSuportePrioritario { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class PlanoRecursoSaasViewModel
{
    public Guid Id { get; set; }
    public Guid PlanoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Habilitado { get; set; }
    public int? Limite { get; set; }
}

public sealed class PlanosSaasIndexViewModel
{
    public PagedResult<PlanoSaasViewModel> Planos { get; set; } = PagedResult<PlanoSaasViewModel>.Empty();
    public string? Status { get; set; }
}

public sealed class AssinaturaSaasViewModel : IValidatableObject
{
    public Guid Id { get; set; }
    [NonEmptyGuid(ErrorMessage = "Selecione um cliente válido.")]
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    [NonEmptyGuid(ErrorMessage = "Selecione um plano válido.")]
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe a data de início.")]
    public DateTime DataInicio { get; set; }
    [Required(ErrorMessage = "Informe a data de término.")]
    public DateTime DataFim { get; set; }
    public string Status { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Informe um valor entre R$ 0,00 e R$ 10.000.000,00.")]
    public decimal ValorContratado { get; set; }
    [Range(1, 31, ErrorMessage = "Informe um dia de vencimento entre 1 e 31.")]
    public int DiaVencimento { get; set; }
    [StringLength(1000, ErrorMessage = "As observações devem ter no máximo 1.000 caracteres.")]
    public string Observacoes { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataInicio == default)
            yield return new ValidationResult("Informe uma data de início válida.", new[] { nameof(DataInicio) });
        if (DataFim == default)
            yield return new ValidationResult("Informe uma data de término válida.", new[] { nameof(DataFim) });
        if (DataInicio != default && DataFim != default && DataFim < DataInicio)
            yield return new ValidationResult("A data de término deve ser igual ou posterior à data de início.", new[] { nameof(DataFim) });
    }
}

public sealed class AssinaturasSaasIndexViewModel
{
    public PagedResult<AssinaturaSaasViewModel> Assinaturas { get; set; } = PagedResult<AssinaturaSaasViewModel>.Empty();
    public string? Status { get; set; }
}

public sealed class SaasResumoExecutivoViewModel
{
    public long ClientesAtivos { get; set; }
    public long ClientesTrial { get; set; }
    public long ClientesSuspensos { get; set; }
    public long ClientesCancelados { get; set; }
    public long ClientesRisco { get; set; }
    public long ClientesCriticos { get; set; }
    public decimal ReceitaPrevistaMes { get; set; }
    public decimal ReceitaRecebidaMes { get; set; }
    public long FaturasAbertas { get; set; }
    public long FaturasVencidas { get; set; }
    public decimal MrrEstimado { get; set; }
    public decimal ChurnEstimado { get; set; }
    public long ClientesProximosLimite { get; set; }
    public long OportunidadesUpgrade { get; set; }
    public long AlertasAbertos { get; set; }
}

public sealed class ClienteSaudeSaasViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Classificacao { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool Inadimplente { get; set; }
    public bool UsoAlto { get; set; }
    public bool Inativo { get; set; }
    public bool ElegivelUpgrade { get; set; }
    public IEnumerable<string> Riscos { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Oportunidades { get; set; } = Array.Empty<string>();
    public IEnumerable<string> AcoesRecomendadas { get; set; } = Array.Empty<string>();
}

public sealed class ClienteAlertaSaasViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Severidade { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public bool Resolvido { get; set; }
    public DateTime RegDate { get; set; }
}

public sealed class CustomerSuccessIndexViewModel
{
    public SaasResumoExecutivoViewModel Resumo { get; set; } = new SaasResumoExecutivoViewModel();
    public IEnumerable<ClienteAlertaSaasViewModel> Alertas { get; set; } = Array.Empty<ClienteAlertaSaasViewModel>();
}

public sealed class RelatorioSaasLinhaViewModel
{
    public Guid? ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Classificacao { get; set; } = string.Empty;
    public long Quantidade { get; set; }
    public decimal Valor { get; set; }
    public DateOnly? Competencia { get; set; }
}

public sealed class UsoPlanoViewModel
{
    public Guid ClienteId { get; set; }
    public Guid AssinaturaId { get; set; }
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public string AssinaturaStatus { get; set; } = string.Empty;
    public int MedicosUsados { get; set; }
    public int MedicosLimite { get; set; }
    public int HospitaisUsados { get; set; }
    public int HospitaisLimite { get; set; }
    public int PlantoesMesUsados { get; set; }
    public int PlantoesMesLimite { get; set; }
    public int UsuariosUsados { get; set; }
    public int UsuariosLimite { get; set; }
    public int ConvitesMesUsados { get; set; }
    public int ConvitesMesLimite { get; set; }
    public bool PermiteMobile { get; set; }
    public bool PermiteBi { get; set; }
    public bool PermiteRelatoriosAvancados { get; set; }
    public bool PermiteIntegracoes { get; set; }
    public bool PermiteOperacaoAssistida { get; set; }
    public bool PermiteSuportePrioritario { get; set; }
}

public sealed class SaasRecomendacaoViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
}
