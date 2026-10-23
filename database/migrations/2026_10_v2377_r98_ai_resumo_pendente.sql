-- R98 — assistente Drael em linguagem simples (resumo do pendente + orientacao
-- ao usuario final): nova tarefa RESUMO_PENDENTE na camada canonica de IA.
--
-- Segue o padrao de v2308 (governanca): os checks de task_code em ai_config
-- (tarefas habilitaveis por tenant) e ai_usos (auditoria imutavel) aceitam
-- explicitamente as tarefas conhecidas. Sem estender ambos, salvar a
-- configuracao da tarefa ou registrar um uso falha no banco com erro nao
-- diagnostico. TESTAR_CONEXAO permanece somente em ai_usos (auditoria de
-- conexao, sem linha de configuracao por tenant).
ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_task_code_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_task_code_check
    CHECK (task_code IN ('MEU_DIA_RESUMO', 'COTACAO_ANALISE', 'RESUMO_PENDENTE'));

ALTER TABLE plantaopro.ai_usos
    DROP CONSTRAINT IF EXISTS ai_usos_task_code_check;
ALTER TABLE plantaopro.ai_usos
    ADD CONSTRAINT ai_usos_task_code_check
    CHECK (task_code IN ('MEU_DIA_RESUMO', 'COTACAO_ANALISE', 'TESTAR_CONEXAO', 'RESUMO_PENDENTE'));
