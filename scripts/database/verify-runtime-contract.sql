DO $$ BEGIN
 IF NOT EXISTS(SELECT FROM plantaopro.usuarios u JOIN plantaopro.usuarios_perfis up ON up.usuario_id=u.id AND up.reg_status='A' JOIN plantaopro.perfis p ON p.id=up.perfil_id AND p.codigo='ADMINISTRADOR_GLOBAL' WHERE u.reg_status='A') THEN RAISE EXCEPTION 'Superadministrador runtime-ready ausente'; END IF;
 IF EXISTS(SELECT FROM plantaopro.usuarios_perfis up LEFT JOIN plantaopro.usuarios u ON u.id=up.usuario_id LEFT JOIN plantaopro.perfis p ON p.id=up.perfil_id WHERE up.reg_status='A' AND (u.id IS NULL OR p.id IS NULL)) THEN RAISE EXCEPTION 'Vínculo órfão'; END IF;
END $$;
