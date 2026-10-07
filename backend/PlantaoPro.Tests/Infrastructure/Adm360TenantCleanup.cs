namespace PlantaoPro.Tests.Infrastructure;

/// <summary>
/// Limpeza ordenada (segura para FK) de TODAS as tabelas do Administrativo 360 dentro do
/// escopo dos tenants informados (WP-A4 — dados isolados + limpeza ordenada).
///
/// Origem da ordem: derivação topológica do grafo FK extraído de pg_constraint em
/// 2026-10-04 (PostgreSQL 18, banco plantaopro_test, migration v2308): 53 tabelas base adm360
/// (todas com tenant_id) + tenants. A visão derivada adm360_saldos não é limpa (não é base). Regra: filhos antes dos pais. Como o nível do pai é
/// sempre estritamente maior que o nível dos filhos, não há ciclos e tabelas do mesmo
/// nível jamais se referenciam — qualquer ordem dentro de um mesmo nível é válida.
///
/// Causa raiz corrigida (flake 23503): Administrativo360CadastrosSeletoresEUnicidadeTests
/// apagava apenas lotes/movimentos/produtos/locais/parceiros/tenants; estado residual de
/// uma execução anterior ou interrompida (ex.: adm360_orcamentos referenciando parceiros
/// desses tenants) quebrava o delete de parceiros em execuções subsequentes. Agora a
/// limpeza cobre o fechamento descendente completo e é idempotente.
/// </summary>
public static class Adm360TenantCleanup
{
    /// <summary>
    /// Todas as tabelas adm360 na ordem de exclusão (filhos antes dos pais). NÃO reordenar:
    /// a ordem é topológica sobre o grafo FK atual (v2308).
    /// </summary>
    private static readonly string[] TabelasNaOrdem =
    {
        // L0 — folhas (nenhuma tabela é referenciada por estas): excluem primeiro.
        "adm360_caixa_fechamentos",
        "adm360_capacidades_contratadas",
        "adm360_comissoes_apropriadas",
        "adm360_cotacao_anexos",
        "adm360_cotacao_envios",
        "adm360_cotacao_exportacoes",
        "adm360_cotacao_itens",
        "adm360_dfe_sincronizacoes",
        "adm360_documento_eventos",
        "adm360_documento_itens",
        "adm360_eventos",
        "adm360_inspecoes",
        "adm360_inventario_itens",
        "adm360_leituras",
        "adm360_mapeamentos_de_para",
        "adm360_nota_pre_emitida_itens",
        "adm360_ocorrencias",
        "adm360_operacoes",
        "adm360_orcamento_itens",
        "adm360_orcamento_revisoes",
        "adm360_pagamento_estornos",
        "adm360_pedido_itens",
        "adm360_titulo_estornos",
        "adm360_venda_itens",
        "adm360_valorizacao_itens",
        // L1.
        "adm360_cotacao_respostas",
        "adm360_documentos_recebidos",
        "adm360_inventarios",
        "adm360_notas_pre_emitidas",
        "adm360_recebimento_itens",
        "adm360_tarefas_coleta",
        "adm360_titulo_baixas",
        "adm360_titulo_pagamentos",
        "adm360_vale_eventos",
        // L2.
        "adm360_cotacoes",
        "adm360_movimentos",
        "adm360_movimentos_financeiros",
        "adm360_recebimentos",
        "adm360_titulos_pagar",
        "adm360_titulos_receber",
        "adm360_vale_itens",
        // L3.
        "adm360_contas_financeiras",
        "adm360_pedidos",
        "adm360_portal_contas",
        "adm360_reservas",
        "adm360_vendas",
        // L4.
        "adm360_estabelecimentos",
        "adm360_lotes",
        "adm360_valorizacoes",
        // L5.
        "adm360_produtos",
        "adm360_vales",
        // L6.
        "adm360_cirurgias",
        // L7.
        "adm360_locais",
        "adm360_orcamentos",
        // L8 — raízes do módulo (apenas referência tenants): excluem por último.
        "adm360_parametros_fiscais",
        "adm360_parceiros",
    };

    /// <summary>
    /// Bloco SQL de deletes ordenados (um delete por tabela + tenants por último), pronto
    /// para compor um único comando multi-statement atômico (delete+insert da semente).
    /// </summary>
    public static string SqlDelete(params Guid[] tenants)
    {
        if (tenants.Length == 0)
            throw new ArgumentException("Informe ao menos um tenant.", nameof(tenants));

        var ids = string.Join(",", tenants.Select(t => $"'{t}'"));
        var sb = new System.Text.StringBuilder();
        foreach (var tabela in TabelasNaOrdem)
            sb.Append($"delete from plantaopro.{tabela} where tenant_id in ({ids});\n");
        sb.Append($"delete from plantaopro.tenants where id in ({ids});\n");
        return sb.ToString();
    }
}
