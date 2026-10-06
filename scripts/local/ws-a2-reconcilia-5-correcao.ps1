# WS-A2 - correcao auditavel da linha M2.5 confirmada (produto WPB-001)
# Base: docs\evidencias\2026-10-05-wp-b-jornadas\jornadas.md finding #2 ("12,50" digitado virou 1250)
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'

Write-Output '=== ANTES ==='
& $psql -h 127.0.0.1 -p 5432 -U postgres -d plantaopro_test -w -X -P pager=off -t -A -F '|' -c @"
SELECT id, tenant_id, sku, nome, preco_custo FROM plantaopro.adm360_produtos
WHERE id = '0d96cc88-7577-4156-a5de-b94a4436092c';
"@ | ForEach-Object { Write-Output $_ }

& $psql -h 127.0.0.1 -p 5432 -U postgres -d plantaopro_test -w -X -P pager=off -t -A -F '|' -c @"
UPDATE plantaopro.adm360_produtos
SET preco_custo = 12.5000
WHERE id = '0d96cc88-7577-4156-a5de-b94a4436092c' AND preco_custo = 1250.0000;
"@ | ForEach-Object { Write-Output $_ }

Write-Output '=== DEPOIS ==='
& $psql -h 127.0.0.1 -p 5432 -U postgres -d plantaopro_test -w -X -P pager=off -t -A -F '|' -c @"
SELECT id, tenant_id, sku, nome, preco_custo, created_at FROM plantaopro.adm360_produtos
WHERE id = '0d96cc88-7577-4156-a5de-b94a4436092c';
"@ | ForEach-Object { Write-Output $_ }
