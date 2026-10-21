using System.Globalization;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Infrastructure.Legislacao;

/// <summary>
/// R84 (bloco B4): calendario fiscal e motor de obrigacoes/reminders por tenant. Catalogo,
/// dias de vencimento e janela de alerta sao lidos da fonte unica (parametros_legais, v2367) -
/// nenhum numero legal vive no codigo. MENSAL: o vencimento cai no dia fixo do MES EXIBIDO e a
/// competencia (mes de referencia) e o mes exibido menos o offset (ex.: DAS do mes anterior
/// vence no dia 20 do mes exibido). ANUAL: aparece somente na exibicao de dezembro e
/// encerra o exercicio do ano exibido em dezembro. Estados: NAO_SE_APLICA &gt; CONCLUIDA &gt;
/// ATRASADA &gt; VENCE_HOJE &gt; VENCE_EM_CURTO_PRAZO (janela oficial, inclusiva) &gt; AGENDADA.
/// </summary>
public sealed class CalendarioFiscalService : ICalendarioFiscalService
{
    private static readonly NumberFormatInfo Decimais = CultureInfo.InvariantCulture.NumberFormat;

    // Texto explicativo do catalogo (nomes e descricao para a linguagem leiga da tela) -
    // os parametros numericos oficiais vivem somente em parametros_legais.
    private static readonly (string Codigo, string Nome, string Descricao)[] Catalogo =
    {
        ("OBRIGACAO_FISCAL_DAS_SIMPLES", "DAS - Simples Nacional (PGDAS-D)", "Documento unico que recolhe os tributos do regime no mes seguinte a competencia"),
        ("OBRIGACAO_FISCAL_FGTS_MENSAL", "FGTS mensal", "Deposito mensal do fundo de garantia dos contratos CLT"),
        ("OBRIGACAO_FISCAL_DCTFWEB_MENSAL", "DCTFWeb consolidada", "Declaracao dos tributos retidos e noretidos do mes anterior"),
        ("OBRIGACAO_FISCAL_ESOCIAL_S1200", "eSocial - fechamento mensal (S-1200)", "Transmissao dos eventos da folha do mes anterior"),
        ("OBRIGACAO_FISCAL_EFD_ICMS_MENSAL", "EFD ICms/IPI (DEF)", "Livro digital eletronicos de entradas e saidas do mes anterior"),
        ("OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS", "Encerramento anual dos livros contabeis", "Fechamento do exercicio fiscal nos livros contabeis")
    };

    private readonly IParametrosLegaisRepository parametros;
    private readonly ObrigacoesFiscaisRepository obreigacoes;

    public CalendarioFiscalService(IParametrosLegaisRepository parametros, ObrigacoesFiscaisRepository obreigacoes)
    {
        this.parametros = parametros;
        this.obreigacoes = obreigacoes;
    }

    public async Task<CalendarioFiscalDto> ObterCalendarioAsync(Guid tenantId, int mesExibicao, int anoExibicao, DateOnly hoje, CancellationToken ct = default)
    {
        ValidarPeriodo(mesExibicao, anoExibicao);
        ValidarTenant(tenantId);

        var linhas = await parametros.ListarAtivosAsync(null, ct);
        var janela = (int)TributacaoPfService.ValorObrigatorio(linhas, "OBRIGACOES_FISCAIS_JANELA_ALERTA_DIAS");
        var inicio = new DateTime(anoExibicao, mesExibicao, 1);

        var itens = new List<ItemObrigacaoFiscalDto>();
        foreach (var (codigo, nome, descricao) in Catalogo)
        {
            var partes = TextoObrigatorio(linhas, codigo).Split('|');
            if (partes.Length != 4)
                throw new InvalidOperationException($"Obrigacao {codigo} malformada na fonte unica: esperado \"periodicidade|offset_periodos|dia_vencimento|regime\".");
            var periodicidade = partes[0].Trim().ToUpperInvariant();
            var offset = int.Parse(partes[1].Trim(), Decimais);
            var dia = int.Parse(partes[2].Trim(), Decimais);
            var regime = partes[3].Trim().ToUpperInvariant();
            if (dia is < 1 or > 31 || offset is < 0 or > 12)
                throw new InvalidOperationException($"Data oficial da obrigacao {codigo} fora da faixa valida na fonte unica.");

            int mesReferencia, anoReferencia, vencMes, vencAno;
            if (periodicidade == "MENSAL")
            {
                // O vencimento cai no dia da competencia do MES EXIBIDO (ex.: DAS do mes de
                // setembro vence em 20/outubro); a competencia e o mes exibido menos o offset.
                var referencia = inicio.AddMonths(-offset);
                mesReferencia = referencia.Month;
                anoReferencia = referencia.Year;
                vencMes = mesExibicao;
                vencAno = anoExibicao;
            }
            else if (periodicidade == "ANUAL")
            {
                if (mesExibicao != 12) continue; // convencao: item anual so aparece na exibicao de dezembro
                mesReferencia = 12;
                anoReferencia = anoExibicao + offset;
                vencMes = 12;
                vencAno = anoExibicao + offset;
            }
            else
            {
                throw new InvalidOperationException($"Periodicidade desconhecida '{partes[0]}' na obrigacao {codigo} da fonte unica.");
            }

            var diaUso = Math.Min(dia, DiasNoMes(vencAno, vencMes));
            var dataVencimento = new DateOnly(vencAno, vencMes, diaUso);
            var estado = await obreigacoes.ObterEstadoAsync(tenantId, codigo, mesReferencia, anoReferencia, ct);
            var ativa = estado?.Ativa ?? true;
            var concluidaEm = estado?.ConcluidaEm;
            var diasRestantes = (new DateTime(vencAno, vencMes, diaUso) - new DateTime(hoje.Year, hoje.Month, hoje.Day)).Days;

            itens.Add(new ItemObrigacaoFiscalDto(
                codigo,
                nome,
                descricao,
                periodicidade,
                regime,
                mesReferencia,
                anoReferencia,
                dia,
                dataVencimento,
                ativa,
                concluidaEm is not null,
                concluidaEm,
                Estado(ativa, concluidaEm is not null, diasRestantes, janela),
                diasRestantes,
                Fonte(linhas, codigo)));
        }

        return new CalendarioFiscalDto(
            mesExibicao,
            anoExibicao,
            janela,
            itens.OrderBy(i => i.DataVencimento).ThenBy(i => i.Codigo, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public async Task<ItemObrigacaoFiscalDto> AlterarAtivaAsync(AlterarAtivaObrigacaoCommand comando, Guid tenantId, DateOnly hoje, CancellationToken ct = default)
    {
        if (comando is null) throw new ArgumentException("Comando vazio.", nameof(comando));
        ValidarTenant(tenantId);

        var linhas = await parametros.ListarAtivosAsync(null, ct);
        var (codigo, mesReferencia, anoReferencia) = ResolverPeriodo(linhas, comando.Codigo, comando.MesPeriodo, comando.AnoPeriodo);
        await obreigacoes.RegistrarAtivaAsync(tenantId, codigo, mesReferencia, anoReferencia, comando.Ativa, ct);

        var calendario = await ObterCalendarioAsync(tenantId, comando.MesPeriodo, comando.AnoPeriodo, hoje, ct);
        return calendario.Itens.Single(i => i.Codigo == codigo);
    }

    public async Task<ItemObrigacaoFiscalDto> AlternarConclusaoAsync(AlternarConclusaoObrigacaoCommand comando, Guid tenantId, string? usuario, DateOnly hoje, CancellationToken ct = default)
    {
        if (comando is null) throw new ArgumentException("Comando vazio.", nameof(comando));
        ValidarTenant(tenantId);

        var linhas = await parametros.ListarAtivosAsync(null, ct);
        var (codigo, mesReferencia, anoReferencia) = ResolverPeriodo(linhas, comando.Codigo, comando.MesPeriodo, comando.AnoPeriodo);
        await obreigacoes.AlternarConclusaoAsync(tenantId, codigo, mesReferencia, anoReferencia, usuario, ct);

        var calendario = await ObterCalendarioAsync(tenantId, comando.MesPeriodo, comando.AnoPeriodo, hoje, ct);
        return calendario.Itens.Single(i => i.Codigo == codigo);
    }

    /// <summary>Periodo de referencia de uma obrigacao para o periodo exibido (offset do catalogo).</summary>
    internal static (string Codigo, int MesReferencia, int AnoReferencia) ResolverPeriodo(List<ParametroLegalDto> linhas, string codigo, int mesExibicao, int anoExibicao)
    {
        ValidarPeriodo(mesExibicao, anoExibicao);
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("Informe a obrigacao fiscal.", nameof(codigo));

        var meta = Catalogo.FirstOrDefault(c => c.Codigo == codigo.Trim().ToUpperInvariant());
        if (meta.Codigo is null)
            throw new ArgumentException("Obrigacao nao encontrada no catalogo oficial.", nameof(codigo));

        var partes = TextoObrigatorio(linhas, meta.Codigo).Split('|');
        if (partes.Length != 4)
            throw new InvalidOperationException($"Obrigacao {meta.Codigo} malformada na fonte unica: esperado \"periodicidade|offset_periodos|dia_vencimento|regime\".");
        var periodicidade = partes[0].Trim().ToUpperInvariant();
        var offset = int.Parse(partes[1].Trim(), Decimais);
        if (offset is < 0 or > 12)
            throw new InvalidOperationException($"Offset da obrigacao {meta.Codigo} fora da faixa valida na fonte unica.");

        if (periodicidade == "MENSAL")
        {
            var referencia = new DateTime(anoExibicao, mesExibicao, 1).AddMonths(-offset);
            return (meta.Codigo, referencia.Month, referencia.Year);
        }
        if (periodicidade == "ANUAL")
        {
            if (mesExibicao != 12)
                throw new ArgumentException("Obrigacoes anuais sao gerenciadas na exibicao de dezembro.", nameof(mesExibicao));
            return (meta.Codigo, 12, anoExibicao + offset);
        }
        throw new InvalidOperationException($"Periodicidade desconhecida '{partes[0]}' na obrigacao {meta.Codigo} da fonte unica.");
    }

    /// <summary>Estado do reminder (janela oficial inclusiva): ver resumo da classe.</summary>
    internal static string Estado(bool ativa, bool concluida, int diasRestantes, int janelaAlertaDias)
    {
        if (!ativa) return "NAO_SE_APLICA";
        if (concluida) return "CONCLUIDA";
        if (diasRestantes < 0) return "ATRASADA";
        if (diasRestantes == 0) return "VENCE_HOJE";
        if (diasRestantes <= janelaAlertaDias) return "VENCE_EM_CURTO_PRAZO";
        return "AGENDADA";
    }

    private static void ValidarPeriodo(int mes, int ano)
    {
        if (mes is < 1 or > 12)
            throw new ArgumentException("Informe o mes do calendario (de 1 a 12).", nameof(mes));
        if (ano is < 2000 or > 2100)
            throw new ArgumentException("Informe um ano valido.", nameof(ano));
    }

    private static void ValidarTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant da sessao indefinido.", nameof(tenantId));
    }

    private static int DiasNoMes(int ano, int mes) => DateTime.DaysInMonth(ano, mes);

    private static string TextoObrigatorio(List<ParametroLegalDto> linhas, string codigo)
    {
        var linha = linhas.FirstOrDefault(l => string.Equals(l.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        if (linha is null || string.IsNullOrWhiteSpace(linha.Valor))
            throw new InvalidOperationException($"Parametro legal {codigo} ausente da fonte unica.");
        return linha.Valor.Trim();
    }

    private static string Fonte(List<ParametroLegalDto> linhas, string codigo)
    {
        var linha = linhas.FirstOrDefault(l => string.Equals(l.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        return linha?.FonteOficial ?? string.Empty;
    }
}
