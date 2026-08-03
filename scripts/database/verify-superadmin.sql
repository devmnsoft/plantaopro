\if :{?bootstrap_admin_email}
\else
  \echo 'ERRO: bootstrap_admin_email deve ser informado.'
  \quit 3
\endif
SELECT set_config('plantaopro.verify_email', :'bootstrap_admin_email', false);
DO $verify$
DECLARE
    users integer;
    profiles integer;
    links integer;
    permissions integer;
BEGIN
    SELECT count(*) INTO users FROM plantaopro.usuarios
     WHERE lower(coalesce(nullif(btrim(email_normalizado),''),btrim(email)))=lower(btrim(current_setting('plantaopro.verify_email',true))) AND reg_status='A';
    SELECT count(*) INTO profiles FROM plantaopro.perfis
     WHERE tenant_id IS NULL AND cliente_id IS NULL AND lower(btrim(codigo))='administrador_global' AND reg_status='A';
    SELECT count(*) INTO links FROM plantaopro.usuarios_perfis up JOIN plantaopro.usuarios u ON u.id=up.usuario_id JOIN plantaopro.perfis p ON p.id=up.perfil_id
     WHERE lower(coalesce(nullif(btrim(u.email_normalizado),''),btrim(u.email)))=lower(btrim(current_setting('plantaopro.verify_email',true)))
       AND lower(btrim(p.codigo))='administrador_global' AND up.reg_status='A';
    SELECT count(*) INTO permissions FROM plantaopro.perfil_permissoes pp JOIN plantaopro.perfis p ON p.id=pp.perfil_id
     WHERE lower(btrim(p.codigo))='administrador_global' AND pp.permitido AND pp.reg_status='A';
    IF users <> 1 OR profiles <> 1 OR links <> 1 OR permissions = 0 THEN
        RAISE EXCEPTION 'Verificação do superadministrador falhou.';
    END IF;
END $verify$;
