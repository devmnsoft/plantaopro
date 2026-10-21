using System.Threading;
using System.Threading.Tasks;

namespace PlantaoPro.Application.Legislacao;

/// <summary>
/// R80 (bloco B4 - legislação brasileira): parâmetro legal da fonte única
/// <c>plantaopro.parametros_legais</c>. Valor em texto canônico (ponto decimal
/// para numérico, dígito puro para dia do mês); formatação de exibição fica a
/// cargo do consumidor. Fonte oficial citável é obrigatória por contrato do plano.
/// </summary>
public sealed record ParametroLegalDto(
    string Codigo,
    string Descricao,
    string Valor,
    string FonteOficial,
    int Ano,
    string VigenciaInicio,
    string VigenciaFim);

/// <summary>
/// Fonte única de leitura de parâmetros legais por país/ano (global, sem tenant).
/// Consultas servem os blocos seguintes (R81 tributação PF, R83 Simples, R84 calendário).
/// </summary>
public interface IParametrosLegaisRepository
{
    /// <summary>Todos os parâmetros ativos, opcionalmente filtrados por ano (null = qualquer ano).</summary>
    Task<List<ParametroLegalDto>> ListarAtivosAsync(int? ano, CancellationToken ct = default);

    /// <summary>Última linha ativa de um código (ano mais alto quando ano é null).</summary>
    Task<ParametroLegalDto?> ObterPorCodigoAsync(string codigo, int? ano, CancellationToken ct = default);
}
