using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlantaoPro.Application.Folha;

/// <summary>
/// R82 (bloco B4 - folha de pagamento PF): comandos e resultados dos calculos de
/// referencia (mes, 13o, ferias, rescisao sem justa causa). Todos os parametros
/// legais vem da fonte unica parametros_legais via ITributacaoPfService /
/// IParametrosLegaisRepository - nenhum numero legal vive no codigo.
/// </summary>
public sealed record CalcularMesCommand(decimal SalarioBase, decimal HorasExtras, int Dependentes, bool DeducaoSimplificada, int? Ano);

public sealed record CalcularDecimoTerceiroCommand(decimal SalarioMensal, DateTime Admissao, int Parcela, int? Ano);

public sealed record CalcularFeriasCommand(decimal SalarioMensal, int Dias, int? Ano);

public sealed record CalcularRescisaoCommand(decimal SalarioMensal, DateTime Admissao, DateTime Demissao, bool AvisoPrevio, int? Ano);

/// <summary>Calculo do mes: base + horas extras (acrescimo legal), INSS, IRRF com dedução, FGTS.</summary>
public sealed record CalculoMesFolhaDto(
    int Ano,
    decimal SalarioBase,
    decimal HorasExtras,
    decimal ValorHoraReferencia,
    decimal ValorHorasExtras,
    decimal SalarioBruto,
    decimal DescontosDependentes,
    decimal DeducaoSimplificada,
    decimal BaseInss,
    decimal Inss,
    decimal BaseIrrf,
    decimal PisoIrrfMensal,
    decimal Irrf,
    decimal SalarioLiquido,
    decimal AliquotaFgtsPct,
    decimal FgtsDepositado);

/// <summary>Parcela do 13o (proporcionada pelos meses trabalhados no ano) com retenções.</summary>
public sealed record CalculoDecimoTerceiroDto(
    int Ano,
    DateTime Admissao,
    int MesesCreditados,
    decimal SalarioMensal,
    decimal ProRataAnual,
    int Parcela,
    decimal ValorParcela,
    decimal Inss,
    decimal ExcedentePisoDoisMinimos,
    decimal Irrf,
    decimal LiquidoParcela);

/// <summary>Férias (período proporcional em dias) + terço constitucional, com retenções.</summary>
public sealed record CalculoFeriasDto(
    int Ano,
    decimal SalarioMensal,
    int Dias,
    decimal ValorFerias,
    decimal TercoFerias,
    decimal TotalFerias,
    decimal Inss,
    decimal BaseIrrf,
    decimal Irrf,
    decimal Liquido);

/// <summary>Verba da rescisão com nome em linguagem leiga para a tela.</summary>
public sealed record VerbaRescisaoDetalhe(string Codigo, string NomeLeigo, decimal Valor, bool TributavelNaFonte);

/// <summary>Rescisão sem justa causa: verbas, retenções consolidadas, FGTS + multa de 40%.</summary>
public sealed record CalculoRescisaoDto(
    int Ano,
    DateTime Admissao,
    DateTime Demissao,
    decimal SalarioMensal,
    bool AvisoPrevio,
    IReadOnlyList<VerbaRescisaoDetalhe> Verbas,
    decimal TotalVerbasTributaveis,
    decimal InssRetido,
    decimal BaseIrrf,
    decimal IrrfRetido,
    decimal LiquidoVerbas,
    decimal AliquotaFgtsPct,
    int MesesFgtsDepositados,
    decimal FgtsTotal,
    decimal MultaPct,
    decimal Multa40,
    decimal TotalParaPerceber);

public interface IFolhaPfService
{
    Task<CalculoMesFolhaDto> CalcularMesAsync(CalcularMesCommand comando, CancellationToken ct = default);

    Task<CalculoDecimoTerceiroDto> CalcularDecimoTerceiroAsync(CalcularDecimoTerceiroCommand comando, CancellationToken ct = default);

    Task<CalculoFeriasDto> CalcularFeriasAsync(CalcularFeriasCommand comando, CancellationToken ct = default);

    Task<CalculoRescisaoDto> CalcularRescisaoAsync(CalcularRescisaoCommand comando, CancellationToken ct = default);
}
