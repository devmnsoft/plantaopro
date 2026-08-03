\if :{?bootstrap_admin_password_hash}
INSERT INTO plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_status,reg_date)
SELECT gen_random_uuid(),NULL,NULL,u.id,p.id,'A',now()
FROM plantaopro.usuarios u CROSS JOIN plantaopro.perfis p
WHERE lower(coalesce(nullif(btrim(u.email_normalizado),''),btrim(u.email)))=lower(btrim(:'bootstrap_admin_email'))
  AND u.reg_status='A' AND p.tenant_id IS NULL AND p.cliente_id IS NULL
  AND lower(btrim(p.codigo))='administrador_global' AND p.reg_status='A'
  AND NOT EXISTS (SELECT 1 FROM plantaopro.usuarios_perfis up WHERE up.usuario_id=u.id AND up.perfil_id=p.id AND up.reg_status='A');

INSERT INTO plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status,reg_date)
SELECT gen_random_uuid(),profile.id,permission.id,true,false,'A',now()
FROM plantaopro.perfis profile CROSS JOIN plantaopro.permissoes permission
WHERE profile.tenant_id IS NULL AND profile.cliente_id IS NULL AND lower(btrim(profile.codigo))='administrador_global'
  AND profile.reg_status='A' AND permission.reg_status='A'
  AND NOT EXISTS (SELECT 1 FROM plantaopro.perfil_permissoes pp WHERE pp.perfil_id=profile.id AND pp.permissao_id=permission.id AND pp.reg_status='A');
\endif
