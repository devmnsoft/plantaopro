-- Fixture de upgrade: executar somente em banco descartável após a instalação canônica.
DROP INDEX IF EXISTS plantaopro.ux_perfis_tenant_codigo;

INSERT INTO plantaopro.perfis(id, tenant_id, codigo, nome, base_sistema, customizado, status, reg_status, reg_date)
VALUES
('13600000-0000-0000-0000-000000000001', NULL, 'ADMINISTRADOR', 'Administrador legado A', true, false, 'ATIVO', 'A', '2024-01-01'),
('13600000-0000-0000-0000-000000000002', NULL, ' administrador ', 'Administrador legado B', false, true, 'ATIVO', 'A', '2024-02-01');

INSERT INTO plantaopro.usuarios(id, nome, email, email_normalizado, senha_hash, status, reg_status)
VALUES
('13600000-0000-0000-0000-000000000011', 'Usuário legado A', 'legacy-a@invalid.local', 'LEGACY-A@INVALID.LOCAL', 'fixture-not-a-password', 'ATIVO', 'A'),
('13600000-0000-0000-0000-000000000012', 'Usuário legado B', 'legacy-b@invalid.local', 'LEGACY-B@INVALID.LOCAL', 'fixture-not-a-password', 'ATIVO', 'A');

INSERT INTO plantaopro.usuarios_perfis(usuario_id, perfil_id, reg_status, reg_date)
VALUES
('13600000-0000-0000-0000-000000000011', '13600000-0000-0000-0000-000000000001', 'A', '2024-01-01'),
('13600000-0000-0000-0000-000000000012', '13600000-0000-0000-0000-000000000002', 'A', '2024-02-01');

DO $fixture$
DECLARE permission_one uuid; permission_two uuid;
BEGIN
    SELECT id INTO permission_one FROM plantaopro.permissoes WHERE reg_status = 'A' ORDER BY id LIMIT 1;
    SELECT id INTO permission_two FROM plantaopro.permissoes WHERE reg_status = 'A' AND id <> permission_one ORDER BY id LIMIT 1;
    IF permission_one IS NULL OR permission_two IS NULL THEN
        RAISE EXCEPTION 'Fixture requer ao menos duas permissões canônicas';
    END IF;
    INSERT INTO plantaopro.perfil_permissoes(perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status)
    VALUES
      ('13600000-0000-0000-0000-000000000001', permission_one, true, false, 'A'),
      ('13600000-0000-0000-0000-000000000002', permission_one, false, true, 'A'),
      ('13600000-0000-0000-0000-000000000002', permission_two, true, false, 'A');
END
$fixture$;
