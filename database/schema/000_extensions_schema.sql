-- v1.18.7 extensões e schema canônico
CREATE EXTENSION IF NOT EXISTS pgcrypto;
DO $$
DECLARE
    v_schema text;
    v_relocatable boolean;
BEGIN
    SELECT n.nspname, e.extrelocatable
      INTO v_schema, v_relocatable
      FROM pg_extension e
      JOIN pg_namespace n ON n.oid = e.extnamespace
     WHERE e.extname = 'unaccent';

    IF v_schema IS NULL THEN
        CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
    ELSIF v_schema <> 'public' AND coalesce(v_relocatable, false) THEN
        ALTER EXTENSION unaccent SET SCHEMA public;
    END IF;
END $$;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE SCHEMA IF NOT EXISTS plantaopro;
SET search_path TO plantaopro, public;
