using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Infrastructure.Legislacao;

/// <summary>
/// R90 (bloco B4 - fechamento da bateria de contratos legais): agrupa os
/// parametros ativos da fonte unica parametros_legais por tema para a superficie
/// de revisao. O catalogo fixa apenas a classificacao (codigo -&gt; tema) e o
/// rotulo de apresentacao: nenhum numero legal vive aqui. A ordem do catalogo e
/// a ordem de apresentacao da bateria.
/// </summary>
public sealed class BateriaLegalService : IBateriaLegalService
{
    private static readonly (string Tema, string Rotulo, string[] Codigos)[] CatalogoTema =
    {
        ("PF_TRIBUTACAO", "Tributação PF: INSS, IRRF, tetos e piso", new[]
        {
            "SALARIO_MINIMO", "PISO_ISENCAO_IRRF_MENSAL", "INSS_TETO_CONTRIBUICAO",
            "INSS_FAIXA_1", "INSS_FAIXA_2", "INSS_FAIXA_3", "INSS_FAIXA_4",
            "IRRF_FAIXA_1", "IRRF_FAIXA_2", "IRRF_FAIXA_3", "IRRF_FAIXA_4"
        }),
        ("FOLHA_RESCISAO", "Folha, horas extras e rescisão", new[]
        {
            "HORA_EXTRA_ACRESCIMO_PCT", "HORA_EXTRA_ACRESIMO_MIN_PCT",
            "DIAS_MES_TRABALHADO_HORAS", "DIAS_CONTAGEM_MES_CREDITADO",
            "DEPENDENTE_DEDUCAO_MENSAL", "DEDUCAO_SIMPLIFICADA_MENSAL",
            "PISO_ISENCAO_IRRF_DECIMO_TERCEIRO"
        }),
        ("FGTS", "FGTS", new[]
        {
            "FGTS_ALIQUOTA_MENSAL", "FGTS_VENCIMENTO_DIA", "FGTS_MULTA_RESCISAO_PCT"
        }),
        ("SIMPLES_NACIONAL", "Simples Nacional", new[]
        {
            "SIMPLES_LIMITE_ME_RECEITA_ANUAL", "SIMPLES_LIMITE_EPP_RECEITA_ANUAL",
            "SIMPLES_DAS_DIA_VENCIMENTO", "DAS_SIMPLES_VENCIMENTO_DIA",
            "SIMPLES_ANEXO_1_FAIXA_1", "SIMPLES_ANEXO_1_FAIXA_2", "SIMPLES_ANEXO_1_FAIXA_3",
            "SIMPLES_ANEXO_1_FAIXA_4", "SIMPLES_ANEXO_1_FAIXA_5", "SIMPLES_ANEXO_1_FAIXA_6",
            "SIMPLES_ANEXO_2_FAIXA_1", "SIMPLES_ANEXO_2_FAIXA_2", "SIMPLES_ANEXO_2_FAIXA_3",
            "SIMPLES_ANEXO_2_FAIXA_4", "SIMPLES_ANEXO_2_FAIXA_5", "SIMPLES_ANEXO_2_FAIXA_6",
            "SIMPLES_ANEXO_3_FAIXA_1", "SIMPLES_ANEXO_3_FAIXA_2", "SIMPLES_ANEXO_3_FAIXA_3",
            "SIMPLES_ANEXO_3_FAIXA_4", "SIMPLES_ANEXO_3_FAIXA_5", "SIMPLES_ANEXO_3_FAIXA_6",
            "SIMPLES_ANEXO_4_FAIXA_1", "SIMPLES_ANEXO_4_FAIXA_2", "SIMPLES_ANEXO_4_FAIXA_3",
            "SIMPLES_ANEXO_4_FAIXA_4", "SIMPLES_ANEXO_4_FAIXA_5", "SIMPLES_ANEXO_4_FAIXA_6",
            "SIMPLES_ANEXO_5_FAIXA_1", "SIMPLES_ANEXO_5_FAIXA_2", "SIMPLES_ANEXO_5_FAIXA_3",
            "SIMPLES_ANEXO_5_FAIXA_4", "SIMPLES_ANEXO_5_FAIXA_5", "SIMPLES_ANEXO_5_FAIXA_6"
        }),
        ("CALENDARIO_FISCAL", "Calendário fiscal e obrigações", new[]
        {
            "OBRIGACAO_FISCAL_DAS_SIMPLES", "OBRIGACAO_FISCAL_DCTFWEB_MENSAL",
            "OBRIGACAO_FISCAL_EFD_ICMS_MENSAL", "OBRIGACAO_FISCAL_ESOCIAL_S1200",
            "OBRIGACAO_FISCAL_FGTS_MENSAL", "OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS",
            "OBRIGACOES_FISCAIS_JANELA_ALERTA_DIAS", "OBRIGACOES_FISCAIS_ESCALONAMENTO_ATRASO_DIAS",
            "DCTFWEB_VENCIMENTO_DIA"
        }),
        ("ESOCIAL_TRABALHO", "eSocial, jornada e aviso prévio", new[]
        {
            "AVISO_PREVIO_BASE_DIAS", "AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS",
            "AVISO_PREVIO_TETO_TOTAL_DIAS", "JORNADA_SEMANAL_MAX_HORAS",
            "SALARIO_DIVISOR_MENSAL_HORAS", "ESOCIAL_EVENTOS_CATALOGO",
            "ESOCIAL_VENCIMENTO_PERIODICO_DIA"
        }),
        ("DFE_SEFAZ", "DF-e e SEFAZ por unidade federativa", new[]
        {
            "DFE_LAYOUT_NFE", "DFE_LAYOUT_NFCE", "DFE_LAYOUT_CFE_SAT", "DFE_LAYOUT_CFE_SERVICOS",
            "SEFAZ_UF_AC", "SEFAZ_UF_AL", "SEFAZ_UF_AM", "SEFAZ_UF_AP", "SEFAZ_UF_BA",
            "SEFAZ_UF_CE", "SEFAZ_UF_DF", "SEFAZ_UF_ES", "SEFAZ_UF_GO", "SEFAZ_UF_MA",
            "SEFAZ_UF_MG", "SEFAZ_UF_MS", "SEFAZ_UF_MT", "SEFAZ_UF_PA", "SEFAZ_UF_PB",
            "SEFAZ_UF_PE", "SEFAZ_UF_PI", "SEFAZ_UF_PR", "SEFAZ_UF_RJ", "SEFAZ_UF_RN",
            "SEFAZ_UF_RO", "SEFAZ_UF_RR", "SEFAZ_UF_RS", "SEFAZ_UF_SC", "SEFAZ_UF_SE",
            "SEFAZ_UF_SP", "SEFAZ_UF_TO"
        }),
        ("TRILHOS_PAGAMENTO", "Trilhos de pagamento", new[]
        {
            "TRILHO_BOLETO_BASE_FATOR_DIA", "TRILHO_BOLETO_MOEDA", "TRILHO_CNAB_REMESSA_LAYOUT",
            "TRILHO_FEBEN_DOCUMENTOS", "TRILHO_PIX_GUI", "TRILHO_PIX_MCC_PADRAO"
        }),
        ("LGPD_SA", "LGPD e prontuário", new[]
        {
            "LGPD_SA_BASE_SENSIVEL", "LGPD_SA_CANAIS", "LGPD_SA_DIREITOS",
            "LGPD_SA_ESCOPOS_CONSENTIMENTO", "LGPD_SA_RETENCAO_FISCAL_MESES",
            "LGPD_SA_RETENCAO_PRONTUARIO_MESES"
        }),
        ("VENDA_INTERNACIONAL", "Venda internacional", new[]
        {
            "COTACAO_MOEDAS", "COTACAO_MOEDA_BASE", "NOTA_INTERNACIONAL_FORMA"
        })
    };

    private readonly IParametrosLegaisRepository repository;

    public BateriaLegalService(IParametrosLegaisRepository repository) => this.repository = repository;

    public async Task<BateriaLegalDto> ObterBateriaAsync(int? ano, CancellationToken ct = default)
    {
        var todos = await repository.ListarAtivosAsync(ano, ct);
        var classificados = new System.Collections.Generic.HashSet<string>();
        var grupos = new List<ParametroLegalGrupoDto>();
        foreach (var (tema, rotulo, codigos) in CatalogoTema)
        {
            var conjunto = new System.Collections.Generic.HashSet<string>(codigos);
            var itens = todos.Where(p => conjunto.Contains(p.Codigo)).OrderBy(p => p.Codigo).ToList();
            foreach (var item in itens) classificados.Add(item.Codigo);
            grupos.Add(new ParametroLegalGrupoDto(tema, rotulo, itens));
        }
        // Rede de seguranca: parametro ativo futuro fora do catalogo nao some da bateria.
        var outros = todos.Where(p => !classificados.Contains(p.Codigo)).OrderBy(p => p.Codigo).ToList();
        if (outros.Count > 0) grupos.Add(new ParametroLegalGrupoDto("DIVERSOS", "Outros", outros));
        return new BateriaLegalDto(ano, todos.Count, grupos);
    }
}
