CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Incremental complementar da rodada PLANTÃOPRO SAAS INTELIGENTE.
-- Idempotente e seguro para bancos que já possuem parte das estruturas de rodadas anteriores.

CREATE TABLE IF NOT EXISTS plantaopro.clientes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), razao_social varchar(200), nome_fantasia varchar(200), cnpj varchar(30), status varchar(40) NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.planos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, descricao text, valor_mensal numeric(12,2) NOT NULL DEFAULT 0, status varchar(40) NOT NULL DEFAULT 'ATIVO', limite_medicos int NOT NULL DEFAULT 0, limite_hospitais int NOT NULL DEFAULT 0, limite_plantoes_mes int NOT NULL DEFAULT 0, permite_mobile boolean NOT NULL DEFAULT false, permite_bi boolean NOT NULL DEFAULT false, permite_relatorios_avancados boolean NOT NULL DEFAULT false, permite_api boolean NOT NULL DEFAULT false, permite_integracoes boolean NOT NULL DEFAULT false, permite_relatorios boolean NOT NULL DEFAULT false, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.plano_recursos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, recurso varchar(80) NOT NULL, limite int, habilitado boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.assinaturas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, plano_id uuid NOT NULL, status varchar(40) NOT NULL DEFAULT 'ATIVA', data_inicio date NOT NULL DEFAULT current_date, data_fim date NOT NULL DEFAULT (current_date + interval '1 month'), data_trial_fim date, valor_contratado numeric(12,2) NOT NULL DEFAULT 0, dia_vencimento int NOT NULL DEFAULT 5, periodicidade varchar(20) NOT NULL DEFAULT 'MENSAL', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, cliente_id uuid NOT NULL, status_anterior varchar(40), status_novo varchar(40), motivo text, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid, cliente_id uuid NOT NULL, recurso varchar(80) NOT NULL, competencia date NOT NULL, quantidade int NOT NULL DEFAULT 0, limite_contratado int, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.faturas_saas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, assinatura_id uuid, competencia date NOT NULL, vencimento date NOT NULL, status varchar(40) NOT NULL DEFAULT 'ABERTA', valor numeric(12,2) NOT NULL DEFAULT 0, valor_pago numeric(12,2), paga_em timestamp, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.fatura_itens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), fatura_id uuid NOT NULL, descricao varchar(220) NOT NULL, quantidade numeric(12,2) NOT NULL DEFAULT 1, valor_unitario numeric(12,2) NOT NULL DEFAULT 0, valor_total numeric(12,2) NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.pagamentos_saas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), fatura_id uuid NOT NULL, cliente_id uuid NOT NULL, valor numeric(12,2) NOT NULL DEFAULT 0, status varchar(40) NOT NULL DEFAULT 'PENDENTE', metodo varchar(60), referencia varchar(120), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.cobranca_eventos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, fatura_id uuid, tipo varchar(80) NOT NULL, mensagem text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cliente_bloqueios (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, motivo text NOT NULL, origem varchar(80) NOT NULL DEFAULT 'SISTEMA', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cliente_alertas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, severidade varchar(40) NOT NULL DEFAULT 'MEDIA', titulo varchar(160) NOT NULL, mensagem text NOT NULL, resolvido boolean NOT NULL DEFAULT false, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.cliente_limites_uso (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, competencia date NOT NULL, recurso varchar(80) NOT NULL, usado int NOT NULL DEFAULT 0, limite_contratado int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.cliente_saude_historico (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, classificacao varchar(40) NOT NULL, score int NOT NULL DEFAULT 0, riscos text, oportunidades text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, etapa varchar(60) NOT NULL, responsavel varchar(160), proxima_acao text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_eventos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, resumo text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_tarefas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(220) NOT NULL, responsavel varchar(160), tipo varchar(80), status varchar(40) NOT NULL DEFAULT 'PENDENTE', vencimento timestamp, concluida_em timestamp, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_observacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, observacao text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_responsaveis (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, usuario_id uuid, nome varchar(160) NOT NULL, papel varchar(80) NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.comercial_leads (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(180) NOT NULL, email varchar(180) NOT NULL, telefone varchar(60), empresa varchar(180), status varchar(40) NOT NULL DEFAULT 'NOVO', plano_recomendado varchar(80), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_oportunidades (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid, nome varchar(180) NOT NULL, etapa varchar(60) NOT NULL DEFAULT 'QUALIFICACAO', valor_estimado numeric(12,2) NOT NULL DEFAULT 0, plano_recomendado varchar(80), motivo_perda text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_propostas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), oportunidade_id uuid NOT NULL, numero varchar(60) NOT NULL, validade date NOT NULL, valor_total numeric(12,2) NOT NULL DEFAULT 0, desconto_percentual numeric(5,2) NOT NULL DEFAULT 0, status varchar(40) NOT NULL DEFAULT 'RASCUNHO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_proposta_itens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), proposta_id uuid NOT NULL, descricao varchar(220) NOT NULL, quantidade numeric(12,2) NOT NULL DEFAULT 1, valor_unitario numeric(12,2) NOT NULL DEFAULT 0, valor_total numeric(12,2) NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_interacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), oportunidade_id uuid, lead_id uuid, tipo varchar(80) NOT NULL, resumo text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_motivos_perda (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, descricao text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_regras_desconto (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil varchar(80) NOT NULL, limite_percentual numeric(5,2) NOT NULL DEFAULT 0, exige_aprovacao boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.customer_success_interacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, resumo text NOT NULL, data_interacao timestamp NOT NULL DEFAULT now(), usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_planos_acao (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(180) NOT NULL, status varchar(40) NOT NULL DEFAULT 'ABERTO', objetivo text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_riscos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, severidade varchar(40) NOT NULL DEFAULT 'MEDIA', status varchar(40) NOT NULL DEFAULT 'ABERTO', descricao text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_tarefas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(180) NOT NULL, responsavel varchar(160), status varchar(40) NOT NULL DEFAULT 'PENDENTE', vencimento timestamp, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);

CREATE TABLE IF NOT EXISTS plantaopro.lgpd_consentimentos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, finalidade varchar(160) NOT NULL, base_legal varchar(120) NOT NULL, versao_politica varchar(30) NOT NULL, consentido boolean NOT NULL DEFAULT true, ip varchar(80), user_agent text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_solicitacoes_titular (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, tipo varchar(40) NOT NULL, status varchar(40) NOT NULL DEFAULT 'ABERTA', descricao text NOT NULL, resposta text, respondida_por uuid, respondida_em timestamp, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_eventos_privacidade (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, acao varchar(120) NOT NULL, detalhes text, ip varchar(80), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_politicas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), versao varchar(30) NOT NULL, titulo varchar(200) NOT NULL, conteudo text NOT NULL, publicada boolean NOT NULL DEFAULT false, vigente_desde timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_bases_legais (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, descricao text NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_retencao_dados (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), categoria varchar(120) NOT NULL, base_legal varchar(120) NOT NULL, prazo varchar(120) NOT NULL, regra text NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_exportacoes_dados (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, status varchar(40) NOT NULL, ip varchar(80), arquivo_url text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_anonimizacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid NOT NULL, motivo text NOT NULL, status varchar(40) NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.ajuda_topicos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil varchar(80) NOT NULL DEFAULT 'TODOS', titulo varchar(160) NOT NULL, descricao text, ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_artigos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), topico_id uuid, perfil varchar(80) NOT NULL DEFAULT 'TODOS', titulo varchar(180) NOT NULL, conteudo text NOT NULL, link_acao varchar(220), ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_passos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), artigo_id uuid NOT NULL, titulo varchar(180) NOT NULL, descricao text NOT NULL, ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_checklists (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil varchar(80) NOT NULL, titulo varchar(180) NOT NULL, descricao text NOT NULL, concluido_padrao boolean NOT NULL DEFAULT false, ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_feedbacks (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), artigo_id uuid NOT NULL, usuario_id uuid, util boolean NOT NULL, comentario text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

ALTER TABLE plantaopro.jornada_cliente_tarefas ADD COLUMN IF NOT EXISTS tipo varchar(80);
ALTER TABLE plantaopro.cliente_alertas ADD COLUMN IF NOT EXISTS resolvido boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.cliente_alertas ADD COLUMN IF NOT EXISTS reg_update timestamp;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS data_trial_fim date;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_relatorios_avancados boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_mobile boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_bi boolean NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS ix_saas_clientes_status_reg_date ON plantaopro.clientes(status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_saas_assinaturas_cliente_status ON plantaopro.assinaturas(cliente_id, status, data_fim);
CREATE INDEX IF NOT EXISTS ix_saas_faturas_cliente_vencimento ON plantaopro.faturas_saas(cliente_id, vencimento, status);
CREATE INDEX IF NOT EXISTS ix_saas_bloqueios_cliente_tipo ON plantaopro.cliente_bloqueios(cliente_id, tipo, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_saas_alertas_cliente_tipo ON plantaopro.cliente_alertas(cliente_id, tipo, resolvido, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_jornada_cliente_tarefas_tipo_status ON plantaopro.jornada_cliente_tarefas(cliente_id, tipo, status, vencimento);
CREATE INDEX IF NOT EXISTS ix_comercial_leads_status_reg_date_inteligencia ON plantaopro.comercial_leads(status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_comercial_propostas_validade_status_inteligencia ON plantaopro.comercial_propostas(validade, status);
CREATE INDEX IF NOT EXISTS ix_lgpd_solicitacoes_tipo_status_inteligencia ON plantaopro.lgpd_solicitacoes_titular(tipo, status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_ajuda_artigos_busca_inteligencia ON plantaopro.ajuda_artigos(perfil, titulo);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='uq_jornada_cliente_cliente_inteligencia' AND conrelid='plantaopro.jornada_cliente'::regclass) THEN
        ALTER TABLE plantaopro.jornada_cliente ADD CONSTRAINT uq_jornada_cliente_cliente_inteligencia UNIQUE (cliente_id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_planos_valor_nao_negativo_inteligencia' AND conrelid='plantaopro.planos'::regclass) THEN
        ALTER TABLE plantaopro.planos ADD CONSTRAINT ck_planos_valor_nao_negativo_inteligencia CHECK (valor_mensal >= 0);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_faturas_saas_pagamento_valido_inteligencia' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_pagamento_valido_inteligencia CHECK (valor >= 0 AND (valor_pago IS NULL OR valor_pago > 0));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_propostas_desconto_inteligencia' AND conrelid='plantaopro.comercial_propostas'::regclass) THEN
        ALTER TABLE plantaopro.comercial_propostas ADD CONSTRAINT ck_propostas_desconto_inteligencia CHECK (valor_total > 0 AND desconto_percentual >= 0 AND desconto_percentual <= 100);
    END IF;
END $$;

INSERT INTO plantaopro.ajuda_topicos(perfil, titulo, descricao, ordem)
SELECT perfil, titulo, descricao, ordem
FROM (VALUES
('ADMINISTRADOR_GLOBAL','SaaS Executivo','Clientes, planos, assinaturas, inteligência e relatórios.',1),
('COORDENACAO','Operação de Plantões','Publicação, convites, escalas e central operacional.',2),
('MEDICO','Área do Médico','Plantões disponíveis, convites, agenda e pagamentos.',3),
('FINANCEIRO','Financeiro','Pagamentos médicos, faturas SaaS e relatórios financeiros.',4),
('HOSPITAL','Hospital','Acompanhamento de plantões e escalas.',5)
) AS x(perfil,titulo,descricao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.ajuda_topicos t WHERE t.perfil=x.perfil AND t.titulo=x.titulo);

INSERT INTO plantaopro.ajuda_artigos(perfil, titulo, conteudo, link_acao, ordem)
SELECT perfil, titulo, conteudo, link_acao, ordem
FROM (VALUES
('ADMINISTRADOR_GLOBAL','Como acompanhar clientes em risco','Use o dashboard SaaS e a inteligência para priorizar clientes com alertas, inadimplência ou queda de uso.','/SaasDashboard',1),
('ADMINISTRADOR_GLOBAL','Como usar inteligência SaaS','Recalcule saúde, revise justificativas e acione Customer Success ou upgrade conforme recomendação determinística.','/Inteligencia',2),
('COORDENACAO','Como publicar plantão','Cadastre o plantão, valide limites do plano e publique somente quando a assinatura estiver ativa.','/Plantoes',1),
('MEDICO','Como aceitar convite','Acesse o mobile ou a área do médico, consulte detalhes do plantão e responda ao convite.','/MinhaAgenda',1),
('FINANCEIRO','Como confirmar pagamento','Revise o valor, confirme o pagamento e acompanhe a auditoria financeira.','/Financeiro',1),
('HOSPITAL','Como acompanhar plantões','Consulte escalas e plantões do seu contexto com isolamento multiempresa.','/Operacao',1)
) AS x(perfil,titulo,conteudo,link_acao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.ajuda_artigos a WHERE a.perfil=x.perfil AND a.titulo=x.titulo);
