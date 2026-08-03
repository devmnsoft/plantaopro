\if :{?bootstrap_admin_password_hash}
INSERT INTO plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria,reg_date)
SELECT gen_random_uuid(),NULL,NULL,:'bootstrap_admin_name',lower(btrim(:'bootstrap_admin_email')),
       lower(btrim(:'bootstrap_admin_email')),:'bootstrap_admin_password_hash','ATIVO','A',:'bootstrap_force_rotation'::boolean,now()
WHERE NOT EXISTS (
    SELECT 1 FROM plantaopro.usuarios
    WHERE lower(coalesce(nullif(btrim(email_normalizado),''),btrim(email)))=lower(btrim(:'bootstrap_admin_email')) AND reg_status='A');
\endif
