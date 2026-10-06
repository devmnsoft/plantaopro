# WS-A2 - reconciliacao historica (fase 4b): detalhe das linhas suspeitas da familia saude/financeiro
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'

$pares = @(
    'v115_regras_faturamento|valor_base',
    'v115_regras_repasse|valor_fixo',
    'clinica_contas_receber|valor_total',
    'clinica_contas_receber|valor_pendente',
    'clinica_contas_receber|valor_pago',
    'clinica_recebimentos|valor',
    'clinica_caixa|total_entradas',
    'v116_faturamento_lotes|valor',
    'v116_caixa_movimentos|valor'
)

foreach ($par in $pares) {
    $t = $par.Split('|')[0]; $c = $par.Split('|')[1]
    $sql = @"
SELECT COALESCE(t.tenant_id::text,'(sem tenant_id)'), t.$c, COALESCE(to_char(t.created_at,'YYYY-MM-DD HH24:MI'),'(sem created_at)')
FROM plantaopro.""$t"" t
WHERE t.$c IS NOT NULL AND t.$c = FLOOR(t.$c) AND t.$c >= 100
ORDER BY 3 DESC LIMIT 15;
"@
    $out = & $psql -h 127.0.0.1 -p 5432 -U postgres -d plantaopro_test -w -X -P pager=off -t -A -F '|' -c $sql 2>&1 | Where-Object { $_ -notmatch 'ERRO|LINHA' }
    Write-Output "### $t.$c"
    if ($out) { foreach ($l in $out) { Write-Output "    $l" } } else { Write-Output '    (nenhuma linha)' }
}
