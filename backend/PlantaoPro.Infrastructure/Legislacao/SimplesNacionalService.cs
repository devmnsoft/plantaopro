using System.Globalization;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Infrastructure.Legislacao;

/// <summary>
/// R83 (bloco B4): Simples Nacional com todos os numeros legais lidos da fonte unica
/// (parametros_legais, v2366). A faixa e escolhida pela receita bruta acumulada nos 12 meses
/// anteriores a competencia (teto da faixa inclusivo); alíquota efetiva = (RBT12 x aliquota
/// - parcela a deduzir) / RBT12 (LC 123/2006 art. 18 paragrafos 1o, 1o-A e 3o); o valor do
/// DAS e efetiva x receita do mes. Sem acumulado informado (empresa nova), estima 12x a
/// receita do mes (flag no resultado); acumulado zerado aplica a aliquota plana da faixa 1
/// (parcela zero). Arredondamento comercial em 2 casas.
/// </summary>
public sealed class SimplesNacionalService : ISimplesNacionalService
{
    private static readonly NumberFormatInfo Decimais = CultureInfo.InvariantCulture.NumberFormat;

    // Texto explicativo do enquadramento por atividade (LC 123 art. 18 paragrafos 4o e 5o-B a 5o-I).
    private static readonly (string Nome, string AtividadesExemplo)[] NomesAnexos =
    {
        ("Comércio por revenda ou distribuição", "revender produtos de terceiros: distribuidoras e lojas sem produção própria"),
        ("Indústria e fabricação pela própria empresa", "fabricar produtos para venda, com ou sem comércio misto"),
        ("Serviços em geral, incluindo saúde e educação", "clínicas, consultórios, escolas e serviços não listados nos outros anexos"),
        ("Construção civil, limpeza urbana, vigilância, advocacia e construção de uso próprio", "construtoras, empresas de limpeza e segurança, escritórios de advocacia"),
        ("Consultoria, auditoria, veterinária, engenharia, publicidade, tradução e agenciamento", "consultores, auditores, engenheiros, agências de publicidade e tradutores")
    };

    private readonly IParametrosLegaisRepository parametros;

    public SimplesNacionalService(IParametrosLegaisRepository parametros) => this.parametros = parametros;

    public async Task<TabelaSimplesNacionalDto> ObterTabelaAsync(int? ano, CancellationToken ct = default)
    {
        var linhas = await parametros.ListarAtivosAsync(ano, ct);
        var anoEfetivo = TributacaoPfService.ResolveAno(linhas, ano);
        var limiteMe = TributacaoPfService.ValorObrigatorio(linhas, "SIMPLES_LIMITE_ME_RECEITA_ANUAL");
        var limiteEpp = TributacaoPfService.ValorObrigatorio(linhas, "SIMPLES_LIMITE_EPP_RECEITA_ANUAL");
        var diaVencimento = (int)TributacaoPfService.ValorObrigatorio(linhas, "SIMPLES_DAS_DIA_VENCIMENTO");

        var anexos = new List<AnexoSimplesDto>();
        for (var anexo = 1; anexo <= 5; anexo++)
        {
            var faixas = TributacaoPfService.Faixas(linhas, $"SIMPLES_ANEXO_{anexo}_FAIXA_");
            if (faixas.Count != 6)
                throw new InvalidOperationException($"Tabela do anexo {anexo} do Simples incompleta na fonte única (esperadas 6 faixas).");
            anexos.Add(new AnexoSimplesDto(anexo, NomesAnexos[anexo - 1].Nome, NomesAnexos[anexo - 1].AtividadesExemplo, faixas));
        }

        return new TabelaSimplesNacionalDto(anoEfetivo, limiteMe, limiteEpp, diaVencimento, anexos);
    }

    public async Task<PreviaDasDto> GerarPreviaDasAsync(PreviaDasCommand comando, CancellationToken ct = default)
    {
        if (comando.Anexo is < 1 or > 5)
            throw new ArgumentException("Selecione o anexo da atividade principal (de 1 a 5).", nameof(comando));
        if (comando.MesCompetencia is < 1 or > 12)
            throw new ArgumentException("Informe o mês de competência (de 1 a 12).", nameof(comando));
        if (comando.AnoCompetencia is < 2000 or > 2100)
            throw new ArgumentException("Informe um ano de competência válido.", nameof(comando));
        if (comando.ReceitaBrutaMensal <= 0m)
            throw new ArgumentException("Informe a receita bruta do mês maior que zero.", nameof(comando));
        if (comando.ReceitaBrutaAcumulada12Meses is < 0m)
            throw new ArgumentException("A receita bruta acumulada em 12 meses não pode ser negativa.", nameof(comando));

        var tabela = await ObterTabelaAsync(comando.Ano, ct);
        var anexo = tabela.Anexos.Single(a => a.Numero == comando.Anexo);

        // RBT12: sem acumulado informado (empresa com menos de 12 meses), estima 12x a receita do mes.
        var acumulado = comando.ReceitaBrutaAcumulada12Meses ?? Redondo(comando.ReceitaBrutaMensal * 12m);
        var estimado = comando.ReceitaBrutaAcumulada12Meses is null;
        if (acumulado > tabela.LimiteEppReceitaAnual)
            throw new ArgumentException($"Receita acumulada acima do limite do Simples Nacional ({Moeda(tabela.LimiteEppReceitaAnual)} por ano): a empresa ultrapassa o regime.", nameof(comando));

        var faixa = SelecionarFaixa(anexo.Faixas, acumulado);
        var aliquotaEfetivaPct = AliquotaEfetivaPct(faixa, acumulado);
        var valorDas = Redondo(comando.ReceitaBrutaMensal * aliquotaEfetivaPct / 100m);
        var vencimento = new DateTime(comando.AnoCompetencia, comando.MesCompetencia, 1).AddMonths(1);

        return new PreviaDasDto(
            tabela.Ano,
            comando.Anexo,
            anexo.Nome,
            acumulado <= tabela.LimiteMeReceitaAnual ? "ME" : "EPP",
            comando.MesCompetencia,
            comando.AnoCompetencia,
            Redondo(comando.ReceitaBrutaMensal),
            Redondo(acumulado),
            estimado,
            FaixaNumero(faixa),
            ParseLimite(faixa),
            faixa.AliquotaPercentual,
            faixa.ParcelaADeduzir,
            aliquotaEfetivaPct,
            valorDas,
            tabela.DasDiaVencimento,
            vencimento.Month,
            vencimento.Year);
    }

    /// <summary>Faixa escolhida pela receita acumulada nos 12 meses anteriores (teto da faixa é inclusivo).</summary>
    internal static FaixaTributoLegalDto SelecionarFaixa(IReadOnlyList<FaixaTributoLegalDto> faixas, decimal acumulado12Meses)
    {
        foreach (var faixa in faixas)
        {
            if (acumulado12Meses <= ParseLimite(faixa))
                return faixa;
        }
        throw new InvalidOperationException("Receita acumulada acima da última faixa com teto definido - revisão da fonte única.");
    }

    /// <summary>
    /// LC 123/2006 art. 18 paragrafos 1o, 1o-A e 3o: alíquota efetiva = (RBT12 x aliquota -
    /// parcela a deduzir) / RBT12, em porcentagem com 4 casas. Sem acumulado, aliquota plana
    /// da faixa (a primeira tem parcela zero).
    /// </summary>
    internal static decimal AliquotaEfetivaPct(FaixaTributoLegalDto faixa, decimal acumulado12Meses)
    {
        if (acumulado12Meses <= 0m)
            return Redondo4(faixa.AliquotaPercentual);

        var efetiva = (acumulado12Meses * faixa.AliquotaPercentual / 100m - faixa.ParcelaADeduzir) / acumulado12Meses * 100m;
        return Redondo4(efetiva);
    }

    internal static int FaixaNumero(FaixaTributoLegalDto faixa) =>
        int.Parse(faixa.Faixa[(faixa.Faixa.LastIndexOf('_') + 1)..], Decimais);

    internal static decimal ParseLimite(FaixaTributoLegalDto faixa) => decimal.Parse(faixa.LimiteSuperior, Decimais);

    private static string Moeda(decimal valor) => valor.ToString("N2", new CultureInfo("pt-BR"));

    private static decimal Redondo(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static decimal Redondo4(decimal valor) => Math.Round(valor, 4, MidpointRounding.AwayFromZero);
}
