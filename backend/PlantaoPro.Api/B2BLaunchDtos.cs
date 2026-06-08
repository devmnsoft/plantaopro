using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api.Models;

public sealed class DeveloperOverviewDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Autenticacao { get; set; } = string.Empty;
    public IEnumerable<string> EscoposDisponiveis { get; set; } = Array.Empty<string>();
    public IEnumerable<string> EndpointsLiberados { get; set; } = Array.Empty<string>();
    public int LimiteRequisicoesMinuto { get; set; }
}

public sealed class ApiKeyCreateRequest
{
    [Required] public string Nome { get; set; } = string.Empty;
    [Required] public string[] Escopos { get; set; } = Array.Empty<string>();
}

public sealed class ApiKeyCreateResultDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string ChaveExibicaoUnica { get; set; } = string.Empty;
    public string Aviso { get; set; } = string.Empty;
}

public sealed class B2BResumoItemDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime? DataReferencia { get; set; }
}

public sealed class ContratoRequest
{
    [Required] public Guid PlanoId { get; set; }
    [Required] public string Numero { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public decimal TaxaSetup { get; set; }
    public string PropriedadeDados { get; set; } = string.Empty;
    public string SlaResumo { get; set; } = string.Empty;
}

public sealed class SlaIncidenteRequest
{
    [Required] public string Titulo { get; set; } = string.Empty;
    public string Severidade { get; set; } = "MEDIA";
    public string Descricao { get; set; } = string.Empty;
}

public sealed class SuporteChamadoRequest
{
    [Required] public string Assunto { get; set; } = string.Empty;
    public string Prioridade { get; set; } = "NORMAL";
    public string Descricao { get; set; } = string.Empty;
}

public sealed class SuporteInteracaoRequest
{
    [Required] public string Mensagem { get; set; } = string.Empty;
}

public sealed class BetaFeedbackRequest
{
    [Required] public string Titulo { get; set; } = string.Empty;
    public string Severidade { get; set; } = "MEDIA";
    public string Descricao { get; set; } = string.Empty;
}

public sealed class GoToMarketMaterialRequest
{
    [Required] public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "INTERNO";
    public string Conteudo { get; set; } = string.Empty;
}

public sealed class OperacionalLaunchDto
{
    public IEnumerable<B2BResumoItemDto> CentralEscala { get; set; } = Array.Empty<B2BResumoItemDto>();
    public IEnumerable<B2BResumoItemDto> AgendaMedico { get; set; } = Array.Empty<B2BResumoItemDto>();
    public IEnumerable<B2BResumoItemDto> Disponibilidades { get; set; } = Array.Empty<B2BResumoItemDto>();
    public IEnumerable<B2BResumoItemDto> Substituicoes { get; set; } = Array.Empty<B2BResumoItemDto>();
    public IEnumerable<B2BResumoItemDto> Relatorios { get; set; } = Array.Empty<B2BResumoItemDto>();
}
