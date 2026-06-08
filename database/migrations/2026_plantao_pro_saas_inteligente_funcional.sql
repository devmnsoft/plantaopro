-- PlantãoPro — SaaS inteligente funcional e comercialmente vendável
-- Migração incremental, idempotente e segura para PostgreSQL.
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.clientes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), razao_social varchar(180) NOT NULL, nome_fantasia varchar(180), cnpj varchar(20), status varchar(40) NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.planos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, descricao text, valor_mensal numeric(12,2) NOT NULL DEFAULT 0, limite_medicos int NOT NULL DEFAULT 0, limite_hospitais int NOT NULL DEFAULT 0, limite_plantoes_mes int NOT NULL DEFAULT 0, possui_mobile boolean NOT NULL DEFAULT false, possui_bi boolean NOT NULL DEFAULT false, possui_relatorios_avancados boolean NOT NULL DEFAULT false, suporte_prioritario boolean NOT NULL DEFAULT false, operacao_assistida boolean NOT NULL DEFAULT false, status varchar(30) NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.plano_recursos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, recurso varchar(120) NOT NULL, habilitado boolean NOT NULL DEFAULT true, limite int, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.assinaturas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, plano_id uuid NOT NULL, status varchar(40) NOT NULL DEFAULT 'ATIVA', data_inicio date NOT NULL DEFAULT current_date, data_fim date, valor_mensal numeric(12,2) NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid, cliente_id uuid NOT NULL, plano_id uuid, tipo varchar(60) NOT NULL, resumo text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid, cliente_id uuid NOT NULL, competencia varchar(7) NOT NULL, medicos_usados int NOT NULL DEFAULT 0, hospitais_usados int NOT NULL DEFAULT 0, plantoes_mes_usados int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.faturas_saas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, assinatura_id uuid, competencia varchar(7) NOT NULL, vencimento date NOT NULL, valor_total numeric(12,2) NOT NULL DEFAULT 0, status varchar(40) NOT NULL DEFAULT 'ABERTA', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.fatura_itens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), fatura_id uuid NOT NULL, descricao varchar(220) NOT NULL, quantidade numeric(12,2) NOT NULL DEFAULT 1, valor_unitario numeric(12,2) NOT NULL DEFAULT 0, valor_total numeric(12,2) NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.pagamentos_saas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), fatura_id uuid NOT NULL, cliente_id uuid NOT NULL, valor_pago numeric(12,2) NOT NULL DEFAULT 0, data_pagamento timestamptz, metodo varchar(40), status varchar(40) NOT NULL DEFAULT 'PENDENTE', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cobranca_eventos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, fatura_id uuid, tipo varchar(80) NOT NULL, mensagem text NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cliente_bloqueios (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, motivo text NOT NULL, origem varchar(80), ativo boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), resolvido_em timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.cliente_alertas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, severidade varchar(30) NOT NULL DEFAULT 'MEDIA', titulo varchar(180) NOT NULL, mensagem text NOT NULL, resolvido boolean NOT NULL DEFAULT false, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), resolvido_em timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.cliente_limites_uso (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, recurso varchar(80) NOT NULL, limite int NOT NULL DEFAULT 0, usado int NOT NULL DEFAULT 0, percentual numeric(8,2) NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cliente_saude_historico (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, score int NOT NULL, classificacao varchar(40) NOT NULL, riscos text, oportunidades text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.comercial_leads (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(160) NOT NULL, email varchar(180) NOT NULL, telefone varchar(40), empresa varchar(180), status varchar(40) NOT NULL DEFAULT 'NOVO', plano_recomendado varchar(120), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_oportunidades (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid, cliente_id uuid, nome varchar(180) NOT NULL, etapa varchar(60) NOT NULL DEFAULT 'ABERTA', valor_estimado numeric(12,2) NOT NULL DEFAULT 0, plano_recomendado varchar(120), motivo_perda text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_propostas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), oportunidade_id uuid NOT NULL, numero varchar(40) NOT NULL, valor_total numeric(12,2) NOT NULL DEFAULT 0, desconto_percentual numeric(6,2) NOT NULL DEFAULT 0, validade date NOT NULL, status varchar(40) NOT NULL DEFAULT 'RASCUNHO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_proposta_itens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), proposta_id uuid NOT NULL, descricao varchar(220) NOT NULL, quantidade numeric(12,2) NOT NULL DEFAULT 1, valor_unitario numeric(12,2) NOT NULL DEFAULT 0, valor_total numeric(12,2) NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_interacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), oportunidade_id uuid, lead_id uuid, tipo varchar(80) NOT NULL, resumo text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_motivos_perda (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), descricao varchar(180) NOT NULL, ativo boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_regras_desconto (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid, percentual_maximo numeric(6,2) NOT NULL DEFAULT 0, exige_admin_global boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL UNIQUE, etapa varchar(80) NOT NULL DEFAULT 'LEAD_CADASTRADO', responsavel varchar(160), proxima_acao text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_eventos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, resumo text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_tarefas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(180) NOT NULL, responsavel varchar(160), status varchar(40) NOT NULL DEFAULT 'PENDENTE', tipo varchar(80), vencimento timestamptz, concluida_em timestamptz, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_observacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, observacao text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_responsaveis (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, usuario_id uuid, nome varchar(160) NOT NULL, papel varchar(80) NOT NULL, ativo boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.customer_success_interacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, usuario_id uuid, tipo varchar(80) NOT NULL DEFAULT 'CONTATO', risco varchar(40), resumo varchar(220) NOT NULL, descricao text, proxima_acao text, data_interacao timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_planos_acao (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(180) NOT NULL, descricao text, responsavel varchar(120), status varchar(40) NOT NULL DEFAULT 'ABERTO', prioridade varchar(20) NOT NULL DEFAULT 'MEDIA', prazo date, concluido_em timestamptz, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_riscos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, severidade varchar(30) NOT NULL DEFAULT 'MEDIA', descricao text NOT NULL, status varchar(40) NOT NULL DEFAULT 'ABERTO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_tarefas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(180) NOT NULL, responsavel varchar(120), status varchar(40) NOT NULL DEFAULT 'PENDENTE', vencimento date, concluida_em timestamptz, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.lgpd_consentimentos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, finalidade varchar(160) NOT NULL, base_legal varchar(120) NOT NULL, versao_politica varchar(40) NOT NULL, consentido boolean NOT NULL DEFAULT true, ip varchar(80), user_agent text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_solicitacoes_titular (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, tipo varchar(80) NOT NULL, status varchar(40) NOT NULL DEFAULT 'ABERTA', descricao text NOT NULL, resposta text, respondida_em timestamptz, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_eventos_privacidade (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, acao varchar(120) NOT NULL, detalhes text, ip varchar(80), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_politicas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), versao varchar(40) NOT NULL, titulo varchar(180) NOT NULL, conteudo text NOT NULL, vigente_desde timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_bases_legais (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, descricao text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_retencao_dados (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), entidade varchar(120) NOT NULL, prazo_meses int NOT NULL, base_legal varchar(120) NOT NULL, observacao text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_exportacoes_dados (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, status varchar(40) NOT NULL DEFAULT 'GERADA', arquivo varchar(260), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_anonimizacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, motivo text NOT NULL, status varchar(40) NOT NULL DEFAULT 'SOLICITADA', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.ajuda_topicos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil varchar(80) NOT NULL DEFAULT 'TODOS', titulo varchar(160) NOT NULL, descricao text, ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_artigos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), topico_id uuid, perfil varchar(80) NOT NULL DEFAULT 'TODOS', titulo varchar(180) NOT NULL, conteudo text NOT NULL, link_acao varchar(260), ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_passos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), artigo_id uuid NOT NULL, titulo varchar(180) NOT NULL, descricao text NOT NULL, ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_checklists (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil varchar(80) NOT NULL, titulo varchar(180) NOT NULL, link_acao varchar(260), ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_feedbacks (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), artigo_id uuid NOT NULL, usuario_id uuid, util boolean NOT NULL, comentario text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.eventos_sistema (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, tipo varchar(120) NOT NULL, mensagem text NOT NULL, correlation_id varchar(120), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.logs_operacionais (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, perfil varchar(80), acao varchar(120) NOT NULL, entidade varchar(120), entidade_id uuid, ip varchar(80), user_agent text, sucesso boolean NOT NULL DEFAULT true, mensagem text, correlation_id varchar(120), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.auditoria_eventos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, perfil varchar(80), acao varchar(120) NOT NULL, entidade varchar(120) NOT NULL, entidade_id uuid, ip varchar(80), user_agent text, sucesso boolean NOT NULL DEFAULT true, mensagem text, dados_antes text, dados_depois text, correlation_id varchar(120), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.auditoria_lgpd_eventos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, acao varchar(120) NOT NULL, entidade varchar(120), entidade_id uuid, ip varchar(80), sucesso boolean NOT NULL DEFAULT true, mensagem text, correlation_id varchar(120), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS status varchar(40) NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS possui_mobile boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS possui_bi boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS possui_relatorios_avancados boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS valor_mensal numeric(12,2) NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.jornada_cliente_tarefas ADD COLUMN IF NOT EXISTS tipo varchar(80);

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_planos_valor_mensal_nao_negativo' AND conrelid = 'plantaopro.planos'::regclass) THEN
        ALTER TABLE plantaopro.planos ADD CONSTRAINT ck_planos_valor_mensal_nao_negativo CHECK (valor_mensal >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_faturas_saas_valor_total_nao_negativo' AND conrelid = 'plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_valor_total_nao_negativo CHECK (valor_total >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_comercial_propostas_desconto_valido' AND conrelid = 'plantaopro.comercial_propostas'::regclass) THEN
        ALTER TABLE plantaopro.comercial_propostas ADD CONSTRAINT ck_comercial_propostas_desconto_valido CHECK (desconto_percentual >= 0 AND desconto_percentual <= 100);
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_faturas_saas_cliente_competencia_ativa ON plantaopro.faturas_saas(cliente_id, competencia) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_assinaturas_cliente_ativa ON plantaopro.assinaturas(cliente_id) WHERE reg_status='A' AND status IN ('ATIVA','TRIAL');
CREATE INDEX IF NOT EXISTS ix_clientes_status_reg_date ON plantaopro.clientes(status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_assinaturas_cliente_status ON plantaopro.assinaturas(cliente_id, status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_cliente_status_vencimento ON plantaopro.faturas_saas(cliente_id, status, vencimento);
CREATE INDEX IF NOT EXISTS ix_pagamentos_saas_cliente_status ON plantaopro.pagamentos_saas(cliente_id, status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_bloqueios_cliente_tipo ON plantaopro.cliente_bloqueios(cliente_id, tipo, ativo);
CREATE INDEX IF NOT EXISTS ix_cliente_alertas_cliente_status_tipo ON plantaopro.cliente_alertas(cliente_id, resolvido, tipo, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_comercial_leads_status_reg_date ON plantaopro.comercial_leads(status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_comercial_oportunidades_status_reg_date ON plantaopro.comercial_oportunidades(etapa, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_comercial_propostas_status_validade ON plantaopro.comercial_propostas(status, validade);
CREATE INDEX IF NOT EXISTS ix_jornada_cliente_etapa_reg_date ON plantaopro.jornada_cliente(etapa, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_jornada_tarefas_cliente_status ON plantaopro.jornada_cliente_tarefas(cliente_id, status, vencimento);
CREATE INDEX IF NOT EXISTS ix_customer_success_riscos_cliente_status ON plantaopro.customer_success_riscos(cliente_id, status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_tarefas_cliente_status ON plantaopro.customer_success_tarefas(cliente_id, status, vencimento);
CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_usuario_finalidade ON plantaopro.lgpd_consentimentos(usuario_id, finalidade, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_lgpd_solicitacoes_cliente_status ON plantaopro.lgpd_solicitacoes_titular(cliente_id, status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_eventos_sistema_cliente_tipo ON plantaopro.eventos_sistema(cliente_id, tipo, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_logs_operacionais_cliente_acao ON plantaopro.logs_operacionais(cliente_id, acao, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_eventos_cliente_acao ON plantaopro.auditoria_eventos(cliente_id, acao, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_lgpd_eventos_cliente_acao ON plantaopro.auditoria_lgpd_eventos(cliente_id, acao, reg_date DESC);

INSERT INTO plantaopro.lgpd_politicas(id, versao, titulo, conteudo, vigente_desde, reg_status, reg_date)
SELECT gen_random_uuid(), '2026.06', 'Política de Privacidade PlantãoPro', 'Política operacional para consentimentos, direitos do titular, exportação e anonimização conforme bases legais aplicáveis.', now(), 'A', now()
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.lgpd_politicas WHERE versao='2026.06' AND reg_status='A');

INSERT INTO plantaopro.ajuda_topicos(id, perfil, titulo, descricao, ordem, reg_status, reg_date)
SELECT gen_random_uuid(), v.perfil, v.titulo, v.descricao, v.ordem, 'A', now()
FROM (VALUES
('ADMINISTRADOR_GLOBAL','SaaS comercial','Cliente, plano, assinatura, fatura, inteligência e relatórios.',1),
('COORDENACAO','Operação de plantões','Criar, publicar, convidar médicos e confirmar escalas.',2),
('MEDICO','Área do médico','Plantões disponíveis, convites, agenda e pagamentos.',3),
('FINANCEIRO','Financeiro','Gerar pagamentos, confirmar pagamentos e resolver contestações.',4),
('HOSPITAL','Hospital','Acompanhar plantões, escalas e comunicação com coordenação.',5)
) AS v(perfil,titulo,descricao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.ajuda_topicos t WHERE t.perfil=v.perfil AND t.titulo=v.titulo AND t.reg_status='A');
