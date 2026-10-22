using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlantaoPro.Application.Legislacao;

/// <summary>
/// R90 (bloco B4 - fechamento da bateria de contratos legais): grupo de
/// parâmetros ativos da fonte unica por tema. O rotulo do tema e texto de
/// apresentacao, nao numero legal (valores/fontes/vigencia sao dos parametros).
/// </summary>
public sealed record ParametroLegalGrupoDto(string Tema, string TemaRotulo, IReadOnlyList<ParametroLegalDto> Parametros);

/// <summary>Bateria de parametros ativos do ano pedido, particionada por tema.</summary>
public sealed record BateriaLegalDto(int? Ano, int TotalParametros, IReadOnlyList<ParametroLegalGrupoDto> Grupos);

/// <summary>
/// R90: superficie de leitura da bateria legal 2026 (parâmetros das rodadas 80 a
/// 89) agrupada por tema para revisao e auditoria. Nao guarda nenhum numero
/// legal: valores, fontes oficiais e vigencias saem da fonte unica
/// parametros_legais via IParametrosLegaisRepository. Parametros ativos que nao
/// pertencem a nenhum tema do catalogo caem no grupo final "DIVERSOS".
/// </summary>
public interface IBateriaLegalService
{
    Task<BateriaLegalDto> ObterBateriaAsync(int? ano, CancellationToken ct = default);
}
