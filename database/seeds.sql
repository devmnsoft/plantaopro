SET search_path TO plantaopro; INSERT INTO especialidades(nome,descricao) VALUES ('Clínica Médica','Atendimento geral'),('Pediatria','Atendimento infantil'),('Cardiologia','Especialidade cardíaca') ON CONFLICT(nome) DO NOTHING;


-- Escalas e financeiro
INSERT INTO escalas(id,plantao_id,medico_id,status,justificativa,reg_status,reg_date)
SELECT gen_random_uuid(),p.id,m.id,'solicitado','seed','A',now()
FROM plantoes p CROSS JOIN medicos m LIMIT 1;

INSERT INTO escalas(id,plantao_id,medico_id,status,justificativa,reg_status,reg_date)
SELECT gen_random_uuid(),p.id,m.id,'confirmado','seed','A',now()
FROM plantoes p CROSS JOIN medicos m OFFSET 1 LIMIT 1;

INSERT INTO escalas(id,plantao_id,medico_id,status,justificativa,reg_status,reg_date)
SELECT gen_random_uuid(),p.id,m.id,'realizado','seed','A',now()
FROM plantoes p CROSS JOIN medicos m OFFSET 2 LIMIT 1;

INSERT INTO pagamentos(id,escala_id,medico_id,plantao_id,valor_previsto,status,data_prevista,reg_status,reg_date)
SELECT gen_random_uuid(),e.id,e.medico_id,e.plantao_id,p.valor,'pendente',CURRENT_DATE + 7,'A',now()
FROM escalas e JOIN plantoes p ON p.id=e.plantao_id
WHERE e.status='realizado'
LIMIT 1;

INSERT INTO notificacoes(id,usuario_id,titulo,mensagem,tipo,lida,reg_status,reg_date)
SELECT gen_random_uuid(),u.id,'Escala criada','Sua escala foi criada','escala',false,'A',now()
FROM usuarios u LIMIT 1;
