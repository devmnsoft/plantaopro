# WS-A2 - reconciliacao historica (fase 2): varredura de inteiros suspeitos (>= 100) em colunas monetarias
# Heuristica: valor inteiro e grande em coluna monetaria pode ser o resquicio do bug "12,50" -> 1250.
# A classificacao (artefato de teste / seed demo / entrada legitima) vai para o documento.
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$db = @{ Host='127.0.0.1'; Port=5432; User='postgres'; Db='plantaopro_test' }

function Psql($sql) {
    & $psql -h $db.Host -p $db.Port -U $db.User -d $db.Db -w -X -P pager=off -t -A -F '|' -c $sql
}

Write-Output '=== M2.5: produto com prefixo 0d96cc88 e SKUs GATE-A2 ==='
Psql "SELECT 'produto', id, sku, nome, preco_custo, created_at FROM plantaopro.adm360_produtos WHERE id::text LIKE '0d96cc88%' OR sku ILIKE 'GATE-A2%' ORDER BY created_at DESC LIMIT 20;"

$colunas = @(
    'adm360_produtos|preco_custo',
    'adm360_lotes|custo_unitario',
    'adm360_orcamento_itens|preco_unitario',
    'adm360_orcamento_itens|desconto',
    'adm360_orcamento_itens|total',
    'adm360_orcamentos|total_geral',
    'adm360_orcamentos|total_produtos',
    'adm360_orcamentos|desconto_geral',
    'adm360_cotacao_itens|preco_unitario_ofertado',
    'adm360_cotacao_itens|preco_total_ofertado',
    'adm360_cotacao_itens|desconto',
    'adm360_pedido_itens|preco_unitario',
    'adm360_pedido_itens|desconto',
    'adm360_documentos_recebidos|valor_total',
    'adm360_documentos_recebidos|valor_produtos',
    'adm360_documento_itens|valor_unitario',
    'adm360_documento_itens|valor_total',
    'adm360_venda_itens|preco_unitario',
    'adm360_venda_itens|custo_unitario',
    'adm360_venda_itens|desconto',
    'adm360_venda_itens|subtotal',
    'adm360_vendas|total_bruto',
    'adm360_vendas|total_liquido',
    'adm360_vendas|total_custo',
    'adm360_vendas|comissao_prevista',
    'adm360_valorizacao_itens|preco_unitario',
    'adm360_valorizacao_itens|custo_unitario',
    'adm360_valorizacoes|total_bruto',
    'adm360_valorizacoes|total_liquido',
    'adm360_valorizacoes|total_custo',
    'adm360_valorizacoes|comissao_prevista',
    'adm360_vale_itens|preco_unitario',
    'adm360_movimentos_financeiros|valor',
    'adm360_caixa_fechamentos|total_entradas',
    'adm360_caixa_fechamentos|total_saidas',
    'adm360_titulos_receber|valor_principal',
    'adm360_titulos_receber|valor_recebido',
    'adm360_titulos_receber|valor_juros',
    'adm360_titulos_receber|valor_desconto',
    'adm360_titulos_pagar|valor_principal',
    'adm360_titulos_pagar|valor_pago',
    'adm360_titulos_pagar|valor_juros',
    'adm360_titulos_pagar|valor_desconto',
    'adm360_titulo_baixas|valor_recebido',
    'adm360_titulo_pagamentos|valor_pago',
    'adm360_titulo_estornos|valor_estornado',
    'adm360_pagamento_estornos|valor_estornado',
    'adm360_comissoes_apropriadas|valor_comissao'
)

foreach ($par in $colunas) {
    $t = $par.Split('|')[0]; $c = $par.Split('|')[1]
    $sql = "SELECT $c AS valor, count(*) AS vezes, to_char(min(created_at),'YYYY-MM-DD HH24:MI') AS desde, to_char(max(created_at),'YYYY-MM-DD HH24:MI') AS ate FROM plantaopro.`"$t`" WHERE $c = FLOOR($c) AND $c >= 100 GROUP BY 1 ORDER BY 2 DESC LIMIT 12;"
    $out = Psql $sql | Where-Object { $_ -ne '' }
    if ($out) {
        Write-Output "### $t.$c"
        foreach ($l in $out) { Write-Output "    $l" }
    } else {
        Write-Output "ok  $t.$c (nenhum inteiro >= 100)"
    }
}
