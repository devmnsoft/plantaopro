-- PlantãoPro — seed básico de demonstração (reaplicável / idempotente).
-- Bloco E: realinhado ao schema atual (colunas v2200) e protegido contra
-- duplicação em re-aplicação:
--   * especialidades: coluna "descricao" removida do schema (guarda por nome);
--   * escalas: colunas "justificativa"/"reg_date" não existem mais (guarda global por status);
--   * pagamentos: guarda global (seed apenas se ainda não existir pagamento);
--   * notificacoes: formato novo (nome + dados jsonb, sem usuario_id/titulo/mensagem).
SET search_path TO plantaopro;

INSERT INTO especialidades(nome)
SELECT v.nome
FROM (VALUES ('Clínica Médica'), ('Pediatria'), ('Cardiologia')) AS v(nome)
WHERE NOT EXISTS (SELECT 1 FROM especialidades e WHERE e.nome = v.nome);


-- Escalas e financeiro (base vazia => 0 linhas; re-aplicação => 0 linhas).
-- O par (plantao_id, medico_id) de escala ativa é protegido pelo índice unique
-- parcial ux_escala_ocupacao_ativa; os inserts abaixo evitam pares já ocupados
-- e usam ORDER BY em uuid para seleção determinística entre execuções.
INSERT INTO escalas(id, plantao_id, medico_id, status, reg_status)
SELECT gen_random_uuid(), p.id, m.id, 'solicitado', 'A'
FROM plantoes p CROSS JOIN medicos m
WHERE p.id NOT IN (SELECT plantao_id FROM escalas WHERE reg_status = 'A' AND plantao_id IS NOT NULL)
  AND m.id NOT IN (SELECT medico_id FROM escalas WHERE reg_status = 'A' AND medico_id IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM escalas e WHERE e.status = 'solicitado' AND e.reg_status = 'A')
ORDER BY p.id, m.id
LIMIT 1;

INSERT INTO escalas(id, plantao_id, medico_id, status, reg_status)
SELECT gen_random_uuid(), p.id, m.id, 'confirmado', 'A'
FROM plantoes p CROSS JOIN medicos m
WHERE p.id NOT IN (SELECT plantao_id FROM escalas WHERE reg_status = 'A' AND plantao_id IS NOT NULL)
  AND m.id NOT IN (SELECT medico_id FROM escalas WHERE reg_status = 'A' AND medico_id IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM escalas e WHERE e.status = 'confirmado' AND e.reg_status = 'A')
ORDER BY p.id, m.id
LIMIT 1;

INSERT INTO escalas(id, plantao_id, medico_id, status, reg_status)
SELECT gen_random_uuid(), p.id, m.id, 'realizado', 'A'
FROM plantoes p CROSS JOIN medicos m
WHERE p.id NOT IN (SELECT plantao_id FROM escalas WHERE reg_status = 'A' AND plantao_id IS NOT NULL)
  AND m.id NOT IN (SELECT medico_id FROM escalas WHERE reg_status = 'A' AND medico_id IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM escalas e WHERE e.status = 'realizado' AND e.reg_status = 'A')
ORDER BY p.id, m.id
LIMIT 1;

INSERT INTO pagamentos(id, escala_id, medico_id, plantao_id, valor_previsto, status, data_prevista, reg_status, reg_date)
SELECT gen_random_uuid(), e.id, e.medico_id, e.plantao_id, COALESCE(p.valor, 0), 'pendente', CURRENT_DATE + 7, 'A', now()
FROM escalas e JOIN plantoes p ON p.id = e.plantao_id
WHERE lower(e.status) IN ('realizado', 'realizada')
  AND NOT EXISTS (SELECT 1 FROM pagamentos)
ORDER BY e.id
LIMIT 1;

INSERT INTO notificacoes(id, nome, dados)
SELECT gen_random_uuid(), 'Escala criada', '{"mensagem":"Sua escala foi criada","tipo":"escala","lida":false}'::jsonb
WHERE NOT EXISTS (SELECT 1 FROM notificacoes n WHERE n.nome = 'Escala criada');
