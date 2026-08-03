DO $assert$
DECLARE canonical uuid; duplicate_status char(1); users_count integer; history_count integer;
BEGIN
  SELECT id INTO canonical FROM plantaopro.perfis
   WHERE reg_status='A' AND tenant_id IS NULL AND lower(btrim(codigo))='administrador';
  IF canonical IS NULL THEN RAISE EXCEPTION 'Perfil canônico não encontrado'; END IF;
  SELECT reg_status INTO duplicate_status FROM plantaopro.perfis WHERE id='13600000-0000-0000-0000-000000000002';
  IF duplicate_status <> 'I' THEN RAISE EXCEPTION 'Perfil redundante não foi inativado'; END IF;
  SELECT count(DISTINCT usuario_id) INTO users_count FROM plantaopro.usuarios_perfis
   WHERE perfil_id=canonical AND reg_status='A' AND usuario_id IN
    ('13600000-0000-0000-0000-000000000011','13600000-0000-0000-0000-000000000012');
  IF users_count <> 2 THEN RAISE EXCEPTION 'Vínculos de usuários não foram preservados'; END IF;
  IF EXISTS (SELECT 1 FROM plantaopro.perfil_permissoes WHERE perfil_id=canonical AND reg_status='A'
             GROUP BY permissao_id HAVING bool_or(permitido) AND bool_or(NOT permitido)) THEN
    RAISE EXCEPTION 'Conflito de permissão permaneceu';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM plantaopro.perfil_permissoes WHERE perfil_id=canonical AND reg_status='A'
                 AND permitido=false AND bloqueado_por_plano=true) THEN
    RAISE EXCEPTION 'Menor privilégio não prevaleceu';
  END IF;
  SELECT count(*) INTO history_count FROM plantaopro.perfil_consolidacao_historico
   WHERE perfil_origem_id='13600000-0000-0000-0000-000000000002';
  IF history_count <> 1 THEN RAISE EXCEPTION 'Histórico idempotente ausente'; END IF;
END
$assert$;
