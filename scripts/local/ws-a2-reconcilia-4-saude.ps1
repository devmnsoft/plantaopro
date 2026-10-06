# WS-A2 - reconciliacao historica (fase 4): varredura clinica_/v11x_/pagamentos_
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'

$sql = @'
DO
$$
DECLARE
    r record;
    q text;
BEGIN
    FOR r IN
        SELECT c.table_name AS t, c.column_name AS c
        FROM information_schema.columns c
        WHERE c.table_schema = 'plantaopro'
          AND c.data_type = 'numeric'
          AND (c.table_name LIKE 'clinica\_%'
               OR c.table_name LIKE 'v113\_%'
               OR c.table_name LIKE 'v115\_%'
               OR c.table_name LIKE 'v116\_%'
               OR c.table_name LIKE 'pagamentos\_%')
          AND (c.column_name ILIKE '%preco%'
               OR c.column_name ILIKE '%valor%'
               OR c.column_name ILIKE '%custo%'
               OR c.column_name ILIKE '%desconto%'
               OR c.column_name ILIKE '%total%')
    LOOP
        BEGIN
            EXECUTE format(
                'SELECT count(*) FROM plantaopro.%I WHERE %I IS NOT NULL AND %I = FLOOR(%I) AND %I >= 100',
                r.t, r.c, r.c, r.c, r.c)
            INTO q;
            IF q::bigint > 0 THEN
                RAISE NOTICE 'SUSPEITO %.% : % linha(s)', r.t, r.c, q;
            END IF;
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE 'ERRO %.% : %', r.t, r.c, SQLERRM;
        END;
    END LOOP;
END
$$;
'@

& $psql -h 127.0.0.1 -p 5432 -U postgres -d plantaopro_test -w -X -P pager=off -c $sql 2>&1 | ForEach-Object { Write-Output $_ }
