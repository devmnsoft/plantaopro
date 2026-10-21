using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlantaoPro.Application.Folha;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Infrastructure.Legislacao;

namespace PlantaoPro.Infrastructure.Folha;

/// <summary>
/// R82 (bloco B4): folha de pagamento PF em linguagem leiga. Regras oficiais fixas:
/// - Meses contam como cheios nas proporcionais a partir de 15 dias trabalhados (fonte unica).
/// - Hora extra: hora normal = base / jornada mensal canonica (220h) + acrescimo legal minimo.
/// - 13o: proporcional pelo ano; cada parcela tributa por si; IRRF apenas sobre o excedente
///   de 2x minimo (RIR art. 788), isento ate o piso da fonte unica.
/// - Ferias: periodo proporcional em dias + terco; tributa como evento unico.
/// - Rescisao sem justa causa: saldo do mes, 13o proporcional, ferias vencidas + proporcionais
///   (cada +1/3), aviso previo indenizado quando nao dado; INSS/IRRF consolidados em evento
///   unico sobre o total tributavel; FGTS mensal + multa de 40% (fonte unica).
/// Previa simplificada: sem abonos, media de variaveis e acréscimo do dobro na terceira ferias.
/// </summary>
public sealed class FolhaPfService : IFolhaPfService
{
    private static readonly NumberFormatInfo Decimais = CultureInfo.InvariantCulture.NumberFormat;

    private readonly IParametrosLegaisRepository parametros;
    private readonly ITributacaoPfService tributacao;

    public FolhaPfService(IParametrosLegaisRepository parametros, ITributacaoPfService tributacao)
    {
        this.parametros = parametros;
        this.tributacao = tributacao;
    }

    public async Task<CalculoMesFolhaDto> CalcularMesAsync(CalcularMesCommand comando, CancellationToken ct = default)
    {
        if (comando.SalarioBase <= 0m) throw new ArgumentException("Informe um salario base maior que zero.", nameof(comando));
        if (comando.HorasExtras < 0m) throw new ArgumentException("Horas extras nao pode ser negativo.", nameof(comando));
        if (comando.Dependentes < 0 || comando.Dependentes > 10) throw new ArgumentException("Quantidade de dependentes deve ficar entre zero e dez.", nameof(comando));
        if (comando.Dependentes > 0 && comando.DeducaoSimplificada)
            throw new ArgumentException("Escolha apenas uma das deducoes: dependentes ou deducao simplificada.", nameof(comando));

        var tabela = await tributacao.ObterTabelaAsync(comando.Ano, ct);
        var linhas = await parametros.ListarAtivosAsync(comando.Ano, ct);
        var jornadaMes = Parametro(linhas, "DIAS_MES_TRABALHADO_HORAS");
        var acrescimoHe = Parametro(linhas, "HORA_EXTRA_ACRESCIMO_PCT");
        var dedDependente = Parametro(linhas, "DEPENDENTE_DEDUCAO_MENSAL");
        var dedSimplificada = Parametro(linhas, "DEDUCAO_SIMPLIFICADA_MENSAL");
        var aliqFgts = Parametro(linhas, "FGTS_ALIQUOTA_MENSAL");

        // Hora normal exata (base / jornada); o arredondamento acontece no total da HE.
        var valorHora = comando.SalarioBase / jornadaMes;
        var valorHe = Redondo(comando.HorasExtras * valorHora * (1m + acrescimoHe / 100m));
        var bruto = Redondo(comando.SalarioBase + valorHe);

        var descontoDependentes = comando.Dependentes * dedDependente;
        var deducaoSimp = comando.DeducaoSimplificada ? dedSimplificada : 0m;

        var calc = await tributacao.CalcularAsync(comando.Ano, bruto, descontoDependentes, deducaoSimp, ct);
        var fgts = Redondo(bruto * aliqFgts / 100m);

        return new CalculoMesFolhaDto(
            tabela.Ano, comando.SalarioBase, comando.HorasExtras, Redondo(valorHora), valorHe, bruto,
            Redondo(descontoDependentes), deducaoSimp, calc.BaseInss, calc.Inss, calc.BaseIrrf,
            tabela.PisoIrrfMensal, calc.Irrf, calc.SalarioLiquido, aliqFgts, fgts);
    }

    public async Task<CalculoDecimoTerceiroDto> CalcularDecimoTerceiroAsync(CalcularDecimoTerceiroCommand comando, CancellationToken ct = default)
    {
        if (comando.SalarioMensal <= 0m) throw new ArgumentException("Informe um salario mensal maior que zero.", nameof(comando));
        if (comando.Parcela is not (1 or 2)) throw new ArgumentException("Parcela do 13o deve ser 1 ou 2.", nameof(comando));

        var linhas = await parametros.ListarAtivosAsync(comando.Ano, ct);
        var anoEfetivo = ResolverAno(linhas, comando.Ano);
        var pisoDoisMinimos = Parametro(linhas, $"PISO_ISENCAO_IRRF_DECIMO_TERCEIRO", anoEfetivo);
        var tabela = await tributacao.ObterTabelaAsync(anoEfetivo, ct);

        var inicioAno = new DateTime(anoEfetivo, 1, 1);
        var fimAno = new DateTime(anoEfetivo, 12, 31);
        var diasLimiar = DiasLimiar(linhas);
        var meses = MesesCreditados(inicioAno, fimAno, comando.Admissao.Date, diasLimiar);
        var proRata = Redondo(comando.SalarioMensal * meses / 12m);
        var primeira = Redondo(proRata / 2m);
        var valorParcela = comando.Parcela == 1 ? primeira : Redondo(proRata - primeira);

        var inss = InssEvento(tabela, valorParcela);
        var excedente = valorParcela - pisoDoisMinimos;
        if (excedente < 0m) excedente = 0m;
        var irrf = excedente <= 0m ? 0m : TributacaoPfService.AplicarFaixa(tabela.FaixasIrrf, excedente, clampNoLimiteFinal: false);

        return new CalculoDecimoTerceiroDto(
            anoEfetivo, comando.Admissao.Date, meses, comando.SalarioMensal, proRata,
            comando.Parcela, valorParcela, inss, Redondo(excedente), irrf, Redondo(valorParcela - inss - irrf));
    }

    public async Task<CalculoFeriasDto> CalcularFeriasAsync(CalcularFeriasCommand comando, CancellationToken ct = default)
    {
        if (comando.SalarioMensal <= 0m) throw new ArgumentException("Informe um salario mensal maior que zero.", nameof(comando));
        if (comando.Dias < 1 || comando.Dias > 30) throw new ArgumentException("As ferias usam entre 1 e 30 dias.", nameof(comando));

        var tabela = await tributacao.ObterTabelaAsync(comando.Ano, ct);

        var valorFerias = Redondo(comando.SalarioMensal * comando.Dias / 30m);
        var terco = Redondo(valorFerias / 3m);
        var total = Redondo(valorFerias + terco);
        var inss = InssEvento(tabela, total);
        var baseIrrf = Redondo(total - inss);
        var irrf = baseIrrf <= tabela.PisoIrrfMensal ? 0m : TributacaoPfService.AplicarFaixa(tabela.FaixasIrrf, baseIrrf, clampNoLimiteFinal: false);

        return new CalculoFeriasDto(tabela.Ano, comando.SalarioMensal, comando.Dias, valorFerias, terco, total, inss, baseIrrf, irrf, Redondo(total - inss - irrf));
    }

    public async Task<CalculoRescisaoDto> CalcularRescisaoAsync(CalcularRescisaoCommand comando, CancellationToken ct = default)
    {
        if (comando.SalarioMensal <= 0m) throw new ArgumentException("Informe um salario mensal maior que zero.", nameof(comando));
        if (comando.Admissao.Date > comando.Demissao.Date)
            throw new ArgumentException("A data de demissao nao pode ser anterior a admissao.", nameof(comando));

        var linhas = await parametros.ListarAtivosAsync(comando.Ano, ct);
        var anoEfetivo = ResolverAno(linhas, comando.Ano);
        var diasCreditados = DiasLimiar(linhas);
        var aliqFgts = Parametro(linhas, "FGTS_ALIQUOTA_MENSAL");
        var multaPct = Parametro(linhas, "FGTS_MULTA_RESCISAO_PCT");
        var tabela = await tributacao.ObterTabelaAsync(anoEfetivo, ct);

        var salario = comando.SalarioMensal;
        var demissao = comando.Demissao.Date;

        // Saldo do mes de demissao: fração do salario pelos dias do mes (mes canonico de 30 dias).
        var diasNoMes = Math.Min(30, demissao.Day);
        var saldo = Redondo(salario * diasNoMes / 30m);

        // 13o proporcional do ano da demissao (meses com 15+ dias contam como cheios).
        var inicioAnoDemissao = new DateTime(demissao.Year, 1, 1);
        var mesesAno = MesesCreditados(inicioAnoDemissao, demissao, comando.Admissao.Date, diasCreditados);
        var decimo = Redondo(salario * mesesAno / 12m);

        // Ferias: periodos de 12 meses vencidos (ate 2) + periodo proporcional em andamento.
        var vencidos = 0;
        while (vencidos < 2 && comando.Admissao.Date.AddMonths(12 * (vencidos + 1)).AddDays(-1) <= demissao) vencidos++;
        var feriasVencidasValor = Redondo(salario * vencidos);
        var tercoVencidas = Redondo(feriasVencidasValor / 3m);

        var inicioProporcional = comando.Admissao.Date.AddMonths(12 * vencidos);
        var diasDecorridos = (demissao - inicioProporcional).Days + 1;
        var fatorProporcional = Math.Min(1m, diasDecorridos / 30m);
        var feriasPropValor = fatorProporcional <= 0m ? 0m : Redondo(salario * fatorProporcional);
        var tercoProp = Redondo(feriasPropValor / 3m);

        var aviso = comando.AvisoPrevio ? 0m : Redondo(salario);

        var verbas = new List<VerbaRescisaoDetalhe>
        {
            new("SALDO", "Saldo do mes trabalhado", saldo, true),
            new("DECIMO_PROPORCIONAL", $"13o proporcional ({mesesAno} mes(es) do ano)", decimo, true),
            new("FERIAS_VENCIDAS", $"Férias vencidas ({vencidos} período(s))", feriasVencidasValor, true),
            new("TERCO_VENCIDAS", "Terço constitucional das férias vencidas", tercoVencidas, true),
            new("FERIAS_PROPORCIONAL", "Férias proporcionais (período em andamento)", feriasPropValor, true),
            new("TERCO_PROPORCIONAL", "Terço constitucional das férias proporcionais", tercoProp, true)
        };
        if (!comando.AvisoPrevio)
            verbas.Add(new VerbaRescisaoDetalhe("AVISO_PREVIO", "Aviso prévio indenizado (1 mês)", aviso, true));

        var totalTributavel = Redondo(verbas.Where(v => v.TributavelNaFonte).Sum(v => v.Valor));
        var inss = InssEvento(tabela, totalTributavel);
        var baseIrrf = Redondo(Math.Max(0m, totalTributavel - inss));
        var irrf = baseIrrf <= tabela.PisoIrrfMensal ? 0m : TributacaoPfService.AplicarFaixa(tabela.FaixasIrrf, baseIrrf, clampNoLimiteFinal: false);
        var liquidoVerbas = Redondo(totalTributavel - inss - irrf);

        var mesesFgts = MesesCreditados(comando.Admissao.Date, demissao, comando.Admissao.Date, diasCreditados) + (comando.AvisoPrevio ? 0 : 1);
        var fgtsMensal = Redondo(salario * aliqFgts / 100m);
        var fgtsTotal = fgtsMensal * mesesFgts;
        var multa40 = Redondo(fgtsTotal * multaPct / 100m);

        return new CalculoRescisaoDto(
            anoEfetivo, comando.Admissao.Date, demissao, salario, comando.AvisoPrevio, verbas,
            totalTributavel, inss, baseIrrf, irrf, liquidoVerbas,
            aliqFgts, mesesFgts, fgtsTotal, multaPct, multa40,
            Redondo(liquidoVerbas + fgtsTotal + multa40));
    }

    /// <summary>INSS progressivo sobre um evento avulso (13o, ferias, rescisao), base limitada ao teto.</summary>
    private static decimal InssEvento(TabelaTributacaoPfDto tabela, decimal valorEvento)
    {
        if (valorEvento <= 0m) return 0m;
        var baseInss = decimal.Min(valorEvento, tabela.TetoInss);
        return TributacaoPfService.AplicarFaixa(tabela.FaixasInss, baseInss, clampNoLimiteFinal: true);
    }

    /// <summary>
    /// Meses creditados no intervalo [de, ate]: cada mes trabalhado a partir do limiar
    /// canonical (fonte unica) conta como cheio; mes da admissao entra pela mesma regra.
    /// </summary>
    internal static int MesesCreditados(DateTime de, DateTime ate, DateTime admissao, int diasParaCreditar)
    {
        var inicio = admissao > de ? admissao : de;
        if (inicio > ate) return 0;
        var creditados = 0;
        var cursor = new DateTime(inicio.Year, inicio.Month, 1);
        while (cursor <= ate && cursor.Year * 12 + cursor.Month <= ate.Year * 12 + ate.Month)
        {
            var fimMes = new DateTime(cursor.Year, cursor.Month, 1).AddMonths(1).AddDays(-1);
            var trabalhoInicio = inicio > cursor ? inicio : cursor;
            var trabalhoFim = fimMes < ate ? fimMes : ate;
            if (trabalhoInicio <= trabalhoFim)
            {
                var dias = (trabalhoFim - trabalhoInicio).Days + 1;
                if (dias >= diasParaCreditar) creditados++;
            }
            cursor = cursor.AddMonths(1);
        }
        return creditados;
    }

    private static int DiasLimiar(IReadOnlyList<ParametroLegalDto> linhas)
        => (int)Math.Round(Parametro(linhas, "DIAS_CONTAGEM_MES_CREDITADO"), MidpointRounding.AwayFromZero);

    private static int ResolverAno(List<ParametroLegalDto> linhas, int? pedido)
    {
        if (pedido is not null)
        {
            if (!linhas.Exists(l => l.Ano == pedido))
                throw new InvalidOperationException($"Nenhum parametro legal ativo para o ano {pedido} na fonte unica.");
            return pedido.Value;
        }
        return linhas.Count == 0
            ? throw new InvalidOperationException("Nenhum parametro legal ativo na fonte unica.")
            : linhas.Max(l => l.Ano);
    }

    /// <summary>Parametro legal obrigatorio por codigo e ano (valor canônico, ponto decimal).</summary>
    private static decimal Parametro(IEnumerable<ParametroLegalDto> linhas, string codigo, int? ano = null)
    {
        var linha = linhas.FirstOrDefault(l =>
            string.Equals(l.Codigo, codigo, StringComparison.OrdinalIgnoreCase)
            && (ano is null || l.Ano == ano));
        if (linha is null) throw new InvalidOperationException($"Parametro legal {codigo} ausente da fonte unica.");
        return decimal.Parse(linha.Valor, Decimais);
    }

    private static decimal Redondo(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
