# WS-A2 - reconciliacao historica (fase 1): mapear colunas monetarias no schema
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
& $psql -h 127.0.0.1 -p 5432 -U postgres -d plantaopro_test -w -X -v ON_ERROR_STOP=1 -P pager=off -c @"
SELECT table_schema || '.' || table_name AS tabela, column_name, numeric_precision, numeric_scale
FROM information_schema.columns
WHERE data_type = 'numeric'
  AND (column_name ILIKE '%preco%' OR column_name ILIKE '%valor%' OR column_name ILIKE '%custo%'
       OR column_name ILIKE '%desconto%' OR column_name ILIKE '%comissao%' OR column_name ILIKE '%total%')
ORDER BY 1, 2;
"@
