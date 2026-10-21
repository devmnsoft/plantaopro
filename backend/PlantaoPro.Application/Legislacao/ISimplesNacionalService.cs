using System.Threading;

namespace PlantaoPro.Application.Legislacao;

/// <summary>
/// R83 (bloco B4): anexo do Simples Nacional (I a V) com suas faixas cumulativas lidas da
/// fonte unica (tabelas oficiais LC 123/2006 na redacao LC 155/2016). Nome e atividades de
/// exemplo sao texto explicativo do enquadramento (art. 18 paragrafos 4o e 5o-B a 5o-I);
/// numeros legais vivem somente em parametros_legais.
/// </summary>
public sealed record AnexoSimplesDto(
    int Numero,
    string Nome,
    string AtividadesExemplo,
    IReadOnlyList<FaixaTributoLegalDto> Faixas);

/// <summary>Tabela vigente do Simples de um ano: tetos ME/EPP, dia de vencimento do DAS e os cinco anexos.</summary>
public sealed record TabelaSimplesNacionalDto(
    int Ano,
    decimal LimiteMeReceitaAnual,
    decimal LimiteEppReceitaAnual,
    int DasDiaVencimento,
    IReadOnlyList<AnexoSimplesDto> Anexos);

/// <summary>
/// Solicitação de previa de DAS. ReceitaBrutaAcumulada12Meses nula = empresa com menos de
/// um ano de faturamento: o servico estima acumulado = mensal x 12 (flag no resultado).
/// Competencia e o mes/ano da receita declarada (define o vencimento padrao).
/// </summary>
public sealed record PreviaDasCommand(
    int Anexo,
    int MesCompetencia,
    int AnoCompetencia,
    decimal? ReceitaBrutaAcumulada12Meses,
    decimal ReceitaBrutaMensal,
    int? Ano);

/// <summary>
/// Previa do DAS campo a campo: a faixa e escolhida pela receita bruta acumulada nos 12
/// meses anteriores a competencia; alíquota efetiva = (RBT12 x aliquota - parcela) / RBT12
/// (LC 123 art. 18 paragrafos 1o, 1o-A e 3o); valor = efetiva x receita do mes (arredondado
/// comercial em 2 casas). ClasseReceita classifica ME/EPP pelos tetos da fonte unica.
/// </summary>
public sealed record PreviaDasDto(
    int Ano,
    int Anexo,
    string AnexoNome,
    string ClasseReceita,
    int MesCompetencia,
    int AnoCompetencia,
    decimal ReceitaBrutaMensal,
    decimal ReceitaBrutaAcumulada12Meses,
    bool ReceitaAcumuladaEstimada,
    int FaixaNumero,
    decimal FaixaLimiteSuperior,
    decimal AliquotaFaixaPct,
    decimal ParcelaADeduzir,
    decimal AliquotaEfetivaPct,
    decimal ValorDas,
    int DasDiaVencimento,
    int VencimentoMes,
    int VencimentoAno);

public interface ISimplesNacionalService
{
    Task<TabelaSimplesNacionalDto> ObterTabelaAsync(int? ano, CancellationToken ct = default);

    Task<PreviaDasDto> GerarPreviaDasAsync(PreviaDasCommand comando, CancellationToken ct = default);
}
