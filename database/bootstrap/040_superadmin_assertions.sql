\if :{?bootstrap_admin_password_hash}
DO $assertions$
DECLARE
    matching_users integer;
    matching_profiles integer;
    permission_count integer;
BEGIN
    SELECT count(*) INTO matching_profiles FROM plantaopro.perfis
     WHERE tenant_id IS NULL AND cliente_id IS NULL AND lower(btrim(codigo))='administrador_global' AND status='ATIVO' AND reg_status='A';
    SELECT count(*) INTO matching_users FROM plantaopro.usuarios
     WHERE lower(coalesce(nullif(btrim(email_normalizado),''),btrim(email)))=lower(btrim(current_setting('plantaopro.bootstrap_admin_email'))) AND reg_status='A';
    SELECT count(*) INTO permission_count FROM plantaopro.perfil_permissoes pp
     JOIN plantaopro.perfis p ON p.id=pp.perfil_id
     WHERE lower(btrim(p.codigo))='administrador_global' AND p.tenant_id IS NULL AND pp.permitido AND pp.reg_status='A';
    IF matching_profiles <> 1 OR matching_users <> 1 OR permission_count = 0 THEN
        RAISE EXCEPTION 'Bootstrap inconsistente: perfis %, usuários %, permissões %', matching_profiles, matching_users, permission_count;
    END IF;
END $assertions$;

INSERT INTO plantaopro.auditoria(id,codigo,nome,status,dados,criado_em)
SELECT gen_random_uuid(),'BOOTSTRAP_ADMIN_RECONCILIADO','Bootstrap global validado','ATIVO',
       jsonb_build_object('email_normalizado',lower(btrim(:'bootstrap_admin_email')),'senha_alterada',false),now()
WHERE NOT EXISTS (
    SELECT 1 FROM plantaopro.auditoria
    WHERE codigo='BOOTSTRAP_ADMIN_RECONCILIADO'
      AND dados->>'email_normalizado'=lower(btrim(:'bootstrap_admin_email')));
\echo 'Bootstrap do superadministrador criado ou preservado e validado; segredos não foram exibidos.'
\endif
SELECT pg_advisory_unlock(hashtext('plantaopro.install.v1370'));
