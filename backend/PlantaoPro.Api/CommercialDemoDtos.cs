using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api.Models;

public sealed class LandingContentDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public IEnumerable<string> Ctas { get; set; } = Array.Empty<string>();
    public IEnumerable<LandingSectionDto> Secoes { get; set; } = Array.Empty<LandingSectionDto>();
    public IEnumerable<LandingFaqDto> Faq { get; set; } = Array.Empty<LandingFaqDto>();
    public IEnumerable<UseCaseDto> CasosUso { get; set; } = Array.Empty<UseCaseDto>();
}

public sealed class LandingSectionDto
{
    public string Chave { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public IEnumerable<string> Beneficios { get; set; } = Array.Empty<string>();
}

public sealed class LandingFaqDto
{
    public string Pergunta { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
}

public sealed class UseCaseDto
{
    public string Segmento { get; set; } = string.Empty;
    public string Dor { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
}

public class PublicLeadRequest
{
    [Required] public string Nome { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Telefone { get; set; } = string.Empty;
    [Required] public string Empresa { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Origem { get; set; } = "LANDING";
    public string Mensagem { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
}

public sealed class DemoScheduleRequest : PublicLeadRequest
{
    public DateTime? DataPreferida { get; set; }
    public string TamanhoOperacao { get; set; } = string.Empty;
}

public sealed class PublicLeadDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class SimulatorQuestionDto
{
    public string Chave { get; set; } = string.Empty;
    public string Pergunta { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public IEnumerable<string> Opcoes { get; set; } = Array.Empty<string>();
}

public sealed class PlanSimulatorRequest
{
    public int Medicos { get; set; }
    public int Unidades { get; set; }
    public int PlantoesMes { get; set; }
    public int UsuariosAdministrativos { get; set; }
    public bool WhiteLabel { get; set; }
    public bool Api { get; set; }
    public bool Bi { get; set; }
    public bool OperacaoAssistida { get; set; }
    public bool SuportePrioritario { get; set; }
    public bool Revenda { get; set; }
    public bool DominioProprio { get; set; }
    public bool Integracoes { get; set; }
    public string Origem { get; set; } = "SIMULADOR";
}

public sealed class PlanSimulatorResultDto
{
    public Guid HistoricoId { get; set; }
    public string PlanoRecomendado { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
    public decimal MensalidadeEstimada { get; set; }
    public IEnumerable<string> Limites { get; set; } = Array.Empty<string>();
    public IEnumerable<string> RecursosInclusos { get; set; } = Array.Empty<string>();
    public IEnumerable<string> ModulosSugeridos { get; set; } = Array.Empty<string>();
    public string ProximoPasso { get; set; } = string.Empty;
}

public class CommercialProposalRequest
{
    [Required] public string ClienteNome { get; set; } = string.Empty;
    [Required, EmailAddress] public string ClienteEmail { get; set; } = string.Empty;
    [Required] public string Empresa { get; set; } = string.Empty;
    public string Plano { get; set; } = "PROFISSIONAL";
    public string[] Modulos { get; set; } = Array.Empty<string>();
    public decimal TaxaSetup { get; set; }
    public decimal Mensalidade { get; set; }
    public decimal DescontoPercentual { get; set; }
    public DateTime Validade { get; set; } = DateTime.UtcNow.Date.AddDays(15);
    public string Sla { get; set; } = "Suporte em horário comercial com SLA inicial de 8h úteis.";
    public string PrazoImplantacao { get; set; } = "7 a 15 dias úteis";
    public string CondicoesComerciais { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public string ResponsavelComercial { get; set; } = string.Empty;
}

public sealed class CommercialProposalDto : CommercialProposalRequest
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrimeiroMes { get; set; }
    public DateTime RegDate { get; set; }
    public Guid? TenantProvisionadoId { get; set; }
    public Guid? ClienteProvisionadoId { get; set; }
    public Guid? AssinaturaProvisionadaId { get; set; }
    public Guid? AdminClienteProvisionadoId { get; set; }
    public Guid? OnboardingProvisionadoId { get; set; }
    public IEnumerable<string> Timeline { get; set; } = Array.Empty<string>();
}

public sealed class CommercialProposalConversionDto
{
    public Guid PropostaId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid AssinaturaId { get; set; }
    public Guid AdminClienteId { get; set; }
    public Guid OnboardingId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ModoPagamento { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
    public IEnumerable<string> EtapasProvisionadas { get; set; } = Array.Empty<string>();
}

public sealed class RejectProposalRequest
{
    [Required] public string Motivo { get; set; } = string.Empty;
}

public sealed class PortalKpiDto
{
    public string Nome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Unidade { get; set; } = string.Empty;
    public string Severidade { get; set; } = string.Empty;
}

public sealed class PortalResumoDto
{
    public string Titulo { get; set; } = string.Empty;
    public IEnumerable<PortalKpiDto> Kpis { get; set; } = Array.Empty<PortalKpiDto>();
    public IEnumerable<string> Alertas { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Acoes { get; set; } = Array.Empty<string>();
}

public sealed class ModuleDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public bool Global { get; set; }
    public bool Beta { get; set; }
    public bool Oculto { get; set; }
    public bool Habilitado { get; set; }
}

public sealed class UpsertModuleRequest
{
    [Required] public string Codigo { get; set; } = string.Empty;
    [Required] public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = "OPERACIONAL";
    public bool Global { get; set; }
    public bool Beta { get; set; }
    public bool Oculto { get; set; }
    public bool Habilitado { get; set; } = true;
}

public sealed class TenantModuleRequest
{
    public Guid TenantId { get; set; }
    public string Justificativa { get; set; } = string.Empty;
}

public sealed class FeatureFlagDto
{
    public Guid Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public bool Habilitada { get; set; }
}

public sealed class UpdateFeatureFlagRequest
{
    public bool Habilitada { get; set; }
    public Guid? TenantId { get; set; }
    public string Justificativa { get; set; } = string.Empty;
}

public sealed class DemoStatusDto
{
    public bool DadosGerados { get; set; }
    public long TenantsDemo { get; set; }
    public long ClientesDemo { get; set; }
    public long MedicosDemo { get; set; }
    public long HospitaisDemo { get; set; }
    public long PlantoesDemo { get; set; }
    public long ConvitesDemo { get; set; }
    public long EscalasDemo { get; set; }
    public long PagamentosDemo { get; set; }
    public long FaturasDemo { get; set; }
    public long PropostasDemo { get; set; }
    public long ParceirosDemo { get; set; }
    public long LeadsDemo { get; set; }
    public DateTime? UltimaGeracao { get; set; }
    public IEnumerable<string> Roteiros { get; set; } = Array.Empty<string>();
}
