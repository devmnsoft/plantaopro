# WS-A2 - reconciliacao historica (fase 3): descobrir colunas + atribuicao de tenant nas linhas suspeitas
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'

function Psql($sql) {
    & $psql -h 127.0.0.1 -p 5432 -U postgres -d plantaopro_test -w -X -P pager=off -t -A -F '|' -c $sql
}

Write-Output '=== Colunas de adm360_produtos ==='
Psql "SELECT column_name, data_type FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='adm360_produtos' ORDER BY ordinal_position;" | ForEach-Object { Write-Output $_ }

Write-Output ''
Write-Output '=== Tabelas com nome parecido a tenant/cliente ==='
Psql "SELECT table_name FROM information_schema.tables WHERE table_schema='plantaopro' AND (table_name ILIKE '%tenant%' OR table_name ILIKE '%client%' OR table_name ILIKE '%hospit%') ORDER BY 1;" | ForEach-Object { Write-Output $_ }
