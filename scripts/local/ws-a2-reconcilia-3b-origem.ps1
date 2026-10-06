# WS-A2 - reconciliacao historica (fase 3b): quem criou as linhas suspeitas
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'

function Psql($sql) {
    & $psql -h 127.0.0.1 -p 5432 -U postgres -d plantaopro_test -w -X -P pager=off -t -A -F '|' -c $sql
}

Write-Output '=== Tenants (nome/criado) ==='
Psql "SELECT id, COALESCE(nome,'?'), to_char(created_at,'YYYY-MM-DD HH24:MI') FROM tenants ORDER BY created_at LIMIT 25;" | ForEach-Object { Write-Output $_ }

Write-Output ''
Write-Output '=== M2.5 WPB-001 + proximos produtos do mesmo periodo ==='
Psql @"
SELECT p.tenant_id, p.sku, p.nome, p.preco_custo, to_char(p.created_at,'YYYY-MM-DD HH24:MI:SS') AS criado
FROM plantaopro.adm360_produtos p
WHERE p.id = '0d96cc88-7577-4156-a5de-b94a4436092c'
   OR p.created_at BETWEEN '2026-09-24 17:30:00' AND '2026-09-25 11:00:00'
ORDER BY p.created_at LIMIT 40;
"@ | ForEach-Object { Write-Output $_ }

Write-Output ''
Write-Output '=== Linhas one-off (clusters manuais): dono por tenantedas ==='
Psql @"
SELECT 'documentos_recebidos' AS origem, d.tenant_id, to_char(d.created_at,'YYYY-MM-DD HH24:MI') AS criado
FROM plantaopro.adm360_documentos_recebidos d
WHERE d.created_at BETWEEN '2026-10-05 07:00:00' AND '2026-10-06 03:00:00'
UNION ALL
SELECT 'titulos_pagar', t.tenant_id, to_char(t.created_at,'YYYY-MM-DD HH24:MI')
FROM plantaopro.adm360_titulos_pagar t
WHERE t.valor_principal IN (350, 110, 150, 1200, 1500) AND t.created_at >= '2026-09-25 00:00:00'
UNION ALL
SELECT 'vendas', v.tenant_id, to_char(v.created_at,'YYYY-MM-DD HH24:MI')
FROM plantaopro.adm360_vendas v
WHERE v.total_bruto IN (1200, 2400) AND v.created_at >= '2026-09-28 00:00:00'
ORDER BY criado LIMIT 60;
"@ | ForEach-Object { Write-Output $_ }
