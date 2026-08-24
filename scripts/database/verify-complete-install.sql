-- Verificador fail-fast. Executar conectado ao banco instalado.
DO $verify$
DECLARE missing text; bad integer;
BEGIN
 IF current_setting('server_version_num')::int < 160000 THEN RAISE EXCEPTION 'PostgreSQL 16 ou superior é obrigatório'; END IF;
 IF pg_encoding_to_char((SELECT encoding FROM pg_database WHERE datname=current_database())) <> 'UTF8' THEN RAISE EXCEPTION 'Encoding deve ser UTF8'; END IF;
 IF current_schema IS NULL OR NOT EXISTS(SELECT FROM pg_namespace WHERE nspname='plantaopro') THEN RAISE EXCEPTION 'Schema plantaopro ausente'; END IF;
 SELECT string_agg(x,', ') INTO missing FROM unnest(ARRAY['usuarios','perfis','permissoes','perfil_permissoes','usuarios_perfis','modulos_sistema','acoes_sistema','politicas_senha','parametros_sistema','schema_migrations']) x WHERE to_regclass('plantaopro.'||x) IS NULL;
 IF missing IS NOT NULL THEN RAISE EXCEPTION 'Tabelas obrigatórias ausentes: %',missing; END IF;
 SELECT count(*) INTO bad FROM (SELECT lower(email_normalizado) FROM plantaopro.usuarios WHERE reg_status='A' GROUP BY 1 HAVING count(*)>1) d;
 IF bad>0 THEN RAISE EXCEPTION 'Usuários ativos duplicados'; END IF;
 SELECT count(*) INTO bad FROM (SELECT coalesce(tenant_id,'00000000-0000-0000-0000-000000000000'),lower(codigo) FROM plantaopro.perfis WHERE reg_status='A' GROUP BY 1,2 HAVING count(*)>1) d;
 IF bad>0 THEN RAISE EXCEPTION 'Perfis ativos duplicados'; END IF;
 IF EXISTS(SELECT FROM plantaopro.usuarios WHERE reg_status='A' AND (senha_hash IS NULL OR senha_hash='' OR senha_hash !~ '^[$]2[aby][$][0-9]{2}[$].{53}$')) THEN RAISE EXCEPTION 'Hash de senha inválido'; END IF;
 IF EXISTS(SELECT FROM plantaopro.schema_migrations WHERE status='FALHA') THEN RAISE EXCEPTION 'Migration em falha'; END IF;
 IF EXISTS(SELECT FROM plantaopro.perfis p WHERE p.base_sistema AND p.reg_status='A' AND NOT EXISTS(SELECT FROM plantaopro.perfil_permissoes pp WHERE pp.perfil_id=p.id AND pp.reg_status='A')) THEN RAISE EXCEPTION 'Perfil de sistema sem permissão'; END IF;
END $verify$;
SELECT current_database() banco, current_setting('server_version') postgresql,
 (SELECT count(*) FROM information_schema.tables WHERE table_schema='plantaopro') tabelas,
 (SELECT count(*) FROM plantaopro.modulos_sistema WHERE reg_status='A') modulos,
 (SELECT count(*) FROM plantaopro.permissoes WHERE reg_status='A') permissoes,
 (SELECT count(*) FROM plantaopro.perfis WHERE reg_status='A') perfis,
 (SELECT count(*) FROM plantaopro.parametros_sistema WHERE reg_status='A') parametros;
