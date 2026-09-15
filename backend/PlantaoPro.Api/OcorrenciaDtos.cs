using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api;

public sealed class CriarOcorrenciaRequest
{
    [Required] public Guid UnidadeId { get; set; }
    public Guid? PlantaoId { get; set; }
    [Required, StringLength(160, MinimumLength = 4)] public string Titulo { get; set; } = "";
    [Required, StringLength(4000, MinimumLength = 10)] public string Descricao { get; set; } = "";
    [Required] public string Categoria { get; set; } = "";
    [Required] public string Prioridade { get; set; } = "MEDIA";
}
public sealed class AtribuirOcorrenciaRequest { public Guid ResponsavelId { get; set; } public int Versao { get; set; } }
public sealed class TransicionarOcorrenciaRequest { public string Situacao { get; set; } = ""; public string? Descricao { get; set; } public int Versao { get; set; } }
public sealed class OcorrenciaFiltro { public string? Pesquisa { get; set; } public Guid? UnidadeId { get; set; } public string? Categoria { get; set; } public string? Situacao { get; set; } public Guid? ResponsavelId { get; set; } public bool Minhas { get; set; } public DateTimeOffset? De { get; set; } public DateTimeOffset? Ate { get; set; } public int Pagina { get; set; } = 1; public int Tamanho { get; set; } = 25; }
public sealed record OcorrenciaDto(Guid Id, Guid TenantId, Guid UnidadeId, Guid? PlantaoId, string Titulo, string Descricao, string Categoria, string Prioridade, string Situacao, Guid SolicitanteId, Guid? ResponsavelId, DateTimeOffset? PrazoResolucao, string? Resolucao, int Versao, DateTimeOffset CriadoEm, DateTimeOffset AtualizadoEm);
public sealed record OcorrenciaEventoDto(Guid Id, string Tipo, Guid AutorId, string Descricao, bool VisivelSolicitante, DateTimeOffset CriadoEm);
public sealed record OcorrenciaPagina(IReadOnlyList<OcorrenciaDto> Itens, long Total, long Abertas, long SemResponsavel, long EmAtendimento, long ResolvidasPeriodo, long? Vencidas, int Pagina, int Tamanho);
