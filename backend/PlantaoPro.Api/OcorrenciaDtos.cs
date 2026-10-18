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
// NOTE: Dapper/Npgsql devolvem timestamptz como System.DateTime (mapeamento legado padrao).
// Parametros de construtor com DateTimeOffset quebram o match do Dapper em runtime
// ("A parameterless default constructor ... is required for ... materialization").
// R19 (r19b1 F7e): familia F7b auditada globalmente — records ligados ao Dapper usam DateTime;
// propriedades de CLASSE com DTOFF materializam normalmente (probe versionado em R19DapperBindingGuardTests).
public sealed record OcorrenciaDto(Guid Id, Guid TenantId, Guid UnidadeId, Guid? PlantaoId, string Titulo, string Descricao, string Categoria, string Prioridade, string Situacao, Guid SolicitanteId, Guid? ResponsavelId, DateTime? PrazoResolucao, string? Resolucao, int Versao, DateTime CriadoEm, DateTime AtualizadoEm);
public sealed record OcorrenciaEventoDto(Guid Id, string Tipo, Guid AutorId, string Descricao, bool VisivelSolicitante, DateTime CriadoEm);
public sealed record OcorrenciaPagina(IReadOnlyList<OcorrenciaDto> Itens, long Total, long Abertas, long SemResponsavel, long EmAtendimento, long ResolvidasPeriodo, long Vencidas, int Pagina, int Tamanho);
