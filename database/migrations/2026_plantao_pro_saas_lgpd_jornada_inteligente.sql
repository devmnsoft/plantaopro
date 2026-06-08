-- ============================================================================
-- PlantãoPro SaaS Inteligente, LGPD, Jornada Comercial e Ajuda Interativa
-- Script incremental idempotente para PostgreSQL.
-- ============================================================================

CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.clientes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    razao_social varchar(180) NOT NULL,
    nome_fantasia varchar(180),
    cnpj varchar(30),
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz
);

CREATE TABLE IF NOT EXISTS plantaopro.planos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome varchar(120) NOT NULL,
    valor_mensal numeric(14,2) NOT NULL DEFAULT 0,
    limite_medicos int,
    limite_hospitais int,
    limite_plantoes_mes int,
    status varchar(30) NOT NULL DEFAULT 'ATIVO',
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz
);

CREATE TABLE IF NOT EXISTS plantaopro.assinaturas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    plano_id uuid NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'ATIVA',
    inicio_vigencia date NOT NULL DEFAULT current_date,
    fim_vigencia date,
    valor_contratado numeric(14,2) NOT NULL DEFAULT 0,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz
);

ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS responsavel_nome varchar(180);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS responsavel_email varchar(180);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS responsavel_telefone varchar(40);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS observacoes text;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS data_inicio_contrato date;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS data_cancelamento timestamptz;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS motivo_cancelamento text;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS data_suspensao timestamptz;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS motivo_suspensao text;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS saude_status varchar(20) NOT NULL DEFAULT 'ATENCAO';
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS saude_score int NOT NULL DEFAULT 50;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS ultimo_recalculo_saude timestamptz;

ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS valor_anual numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS limite_usuarios int;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS limite_convites_mes int;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_mobile boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_bi boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_api boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_integracoes boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_relatorios_avancados boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_suporte_prioritario boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_operacao_assistida boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS ordem_exibicao int NOT NULL DEFAULT 0;

ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS data_trial_fim timestamptz;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS periodicidade varchar(20) NOT NULL DEFAULT 'MENSAL';
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS motivo_suspensao text;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS data_suspensao timestamptz;

CREATE TABLE IF NOT EXISTS plantaopro.plano_recursos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, codigo varchar(80) NOT NULL, nome varchar(140) NOT NULL, descricao text, habilitado boolean NOT NULL DEFAULT true, limite int, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, cliente_id uuid NOT NULL, plano_id_anterior uuid, plano_id_novo uuid, status_anterior varchar(30), status_novo varchar(30), acao varchar(60) NOT NULL, justificativa text, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid, cliente_id uuid NOT NULL, recurso varchar(80) NOT NULL, quantidade int NOT NULL DEFAULT 0, competencia date NOT NULL DEFAULT date_trunc('month', now())::date, origem varchar(80), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.faturas_saas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, assinatura_id uuid NOT NULL, competencia date NOT NULL, valor numeric(14,2) NOT NULL DEFAULT 0, vencimento date NOT NULL, status varchar(30) NOT NULL DEFAULT 'ABERTA', valor_pago numeric(14,2), data_pagamento date, forma_pagamento varchar(60), motivo_cancelamento text, motivo_contestacao text, resposta_contestacao text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.fatura_itens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), fatura_id uuid NOT NULL, descricao varchar(180) NOT NULL, quantidade numeric(12,2) NOT NULL DEFAULT 1, valor_unitario numeric(14,2) NOT NULL DEFAULT 0, valor_total numeric(14,2) NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.pagamentos_saas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), fatura_id uuid NOT NULL, cliente_id uuid, valor_pago numeric(14,2) NOT NULL, data_pagamento date NOT NULL, forma_pagamento varchar(60) NOT NULL, observacoes text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cobranca_eventos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), fatura_id uuid NOT NULL, cliente_id uuid NOT NULL, tipo varchar(60) NOT NULL, canal varchar(40), mensagem text, sucesso boolean NOT NULL DEFAULT true, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cliente_bloqueios (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, motivo text NOT NULL, origem varchar(80) NOT NULL DEFAULT 'SISTEMA', usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cliente_alertas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(60) NOT NULL, severidade varchar(20) NOT NULL DEFAULT 'MEDIA', titulo varchar(180) NOT NULL, mensagem text NOT NULL, resolvido boolean NOT NULL DEFAULT false, resolvido_em timestamptz, usuario_resolucao_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.cliente_limites_uso (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, assinatura_id uuid, recurso varchar(80) NOT NULL, usado int NOT NULL DEFAULT 0, limite int, percentual numeric(8,4) NOT NULL DEFAULT 0, competencia date NOT NULL DEFAULT date_trunc('month', now())::date, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.cliente_saude_historico (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, score int NOT NULL DEFAULT 0, classificacao varchar(20) NOT NULL, riscos text, oportunidades text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.customer_success_interacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, usuario_id uuid, tipo varchar(40) NOT NULL DEFAULT 'CONTATO', risco varchar(30), resumo varchar(220) NOT NULL, descricao text, proxima_acao text, data_interacao timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_planos_acao (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(180) NOT NULL, descricao text, responsavel varchar(120), status varchar(30) NOT NULL DEFAULT 'ABERTO', prioridade varchar(20) NOT NULL DEFAULT 'MEDIA', prazo date, concluido_em timestamptz, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_riscos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, severidade varchar(20) NOT NULL DEFAULT 'MEDIA', descricao text NOT NULL, status varchar(30) NOT NULL DEFAULT 'ABERTO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_tarefas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(180) NOT NULL, responsavel varchar(120), status varchar(30) NOT NULL DEFAULT 'PENDENTE', vencimento date, concluida_em timestamptz, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, etapa varchar(60) NOT NULL, responsavel varchar(160), proxima_acao text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_eventos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, tipo varchar(80) NOT NULL, resumo text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_tarefas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, titulo varchar(220) NOT NULL, responsavel varchar(160), status varchar(40) NOT NULL DEFAULT 'PENDENTE', vencimento timestamp, concluida_em timestamp, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_observacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, observacao text NOT NULL, usuario_id uuid, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.jornada_cliente_responsaveis (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, usuario_id uuid, nome varchar(160) NOT NULL, papel varchar(80) NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.comercial_leads (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(180) NOT NULL, email varchar(180) NOT NULL, telefone varchar(60), empresa varchar(180), status varchar(40) NOT NULL DEFAULT 'NOVO', plano_recomendado varchar(80), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_oportunidades (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid, nome varchar(180) NOT NULL, etapa varchar(60) NOT NULL, valor_estimado numeric(12,2) NOT NULL DEFAULT 0, plano_recomendado varchar(80), motivo_perda text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_propostas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), oportunidade_id uuid NOT NULL, numero varchar(60) NOT NULL, valor_total numeric(12,2) NOT NULL, desconto_percentual numeric(5,2) NOT NULL DEFAULT 0, validade date NOT NULL, status varchar(40) NOT NULL DEFAULT 'RASCUNHO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.comercial_proposta_itens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), proposta_id uuid NOT NULL, descricao varchar(220) NOT NULL, quantidade int NOT NULL DEFAULT 1, valor_unitario numeric(12,2) NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_interacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), oportunidade_id uuid, lead_id uuid, tipo varchar(80) NOT NULL, resumo text NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_motivos_perda (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.comercial_regras_desconto (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil varchar(80) NOT NULL, limite_percentual numeric(5,2) NOT NULL, exige_aprovacao boolean NOT NULL DEFAULT false, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.lgpd_consentimentos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, finalidade varchar(160) NOT NULL, base_legal varchar(120) NOT NULL, versao_politica varchar(30) NOT NULL, consentido boolean NOT NULL DEFAULT true, ip varchar(80), user_agent text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_solicitacoes_titular (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, tipo varchar(40) NOT NULL, status varchar(40) NOT NULL DEFAULT 'ABERTA', descricao text NOT NULL, resposta text, respondida_por uuid, respondida_em timestamp, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_eventos_privacidade (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, acao varchar(120) NOT NULL, detalhes text, ip varchar(80), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_politicas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), versao varchar(30) NOT NULL, titulo varchar(200) NOT NULL, conteudo text NOT NULL, publicada boolean NOT NULL DEFAULT false, vigente_desde timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_bases_legais (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, descricao text NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_retencao_dados (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), categoria varchar(120) NOT NULL, base_legal varchar(120) NOT NULL, prazo varchar(120) NOT NULL, regra text NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_exportacoes_dados (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid, cliente_id uuid, status varchar(40) NOT NULL, ip varchar(80), arquivo_url text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_anonimizacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid NOT NULL, motivo text NOT NULL, status varchar(40) NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS plantaopro.ajuda_topicos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil varchar(80) NOT NULL, titulo varchar(180) NOT NULL, descricao text NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_artigos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), topico_id uuid NOT NULL, perfil varchar(80) NOT NULL, titulo varchar(180) NOT NULL, conteudo text NOT NULL, link_acao varchar(220), ordem int NOT NULL DEFAULT 0, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_passos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), artigo_id uuid NOT NULL, ordem int NOT NULL, titulo varchar(180) NOT NULL, descricao text NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_checklists (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil varchar(80) NOT NULL, titulo varchar(180) NOT NULL, concluido boolean NOT NULL DEFAULT false, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.ajuda_feedbacks (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), artigo_id uuid NOT NULL, usuario_id uuid, util boolean NOT NULL, comentario text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamp NOT NULL DEFAULT now());

CREATE INDEX IF NOT EXISTS ix_clientes_status_reg_date ON plantaopro.clientes(status, reg_date);
CREATE INDEX IF NOT EXISTS ix_planos_status_reg_date ON plantaopro.planos(status, reg_date);
CREATE INDEX IF NOT EXISTS ix_assinaturas_cliente_status_reg_date ON plantaopro.assinaturas(cliente_id, status, reg_date);
CREATE INDEX IF NOT EXISTS ix_assinatura_uso_cliente_competencia ON plantaopro.assinatura_uso(cliente_id, competencia, recurso);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_cliente_status ON plantaopro.faturas_saas(cliente_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_vencimento_status ON plantaopro.faturas_saas(vencimento, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_cobranca_eventos_cliente_reg_date ON plantaopro.cobranca_eventos(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_bloqueios_cliente_reg_date ON plantaopro.cliente_bloqueios(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_alertas_tipo_reg_date ON plantaopro.cliente_alertas(tipo, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_limites_uso_cliente_competencia ON plantaopro.cliente_limites_uso(cliente_id, competencia, recurso);
CREATE INDEX IF NOT EXISTS ix_cliente_saude_historico_classificacao ON plantaopro.cliente_saude_historico(classificacao, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_interacoes_cliente ON plantaopro.customer_success_interacoes(cliente_id, data_interacao DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_riscos_cliente_status ON plantaopro.customer_success_riscos(cliente_id, status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_tarefas_cliente_status ON plantaopro.customer_success_tarefas(cliente_id, status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_jornada_cliente_etapa_reg_date ON plantaopro.jornada_cliente(etapa, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_jornada_cliente_cliente_status ON plantaopro.jornada_cliente(cliente_id, reg_status);
CREATE INDEX IF NOT EXISTS ix_jornada_cliente_eventos_cliente_reg_date ON plantaopro.jornada_cliente_eventos(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_jornada_cliente_tarefas_cliente_status ON plantaopro.jornada_cliente_tarefas(cliente_id, status, vencimento);
CREATE INDEX IF NOT EXISTS ix_comercial_leads_status_reg_date ON plantaopro.comercial_leads(status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_comercial_oportunidades_etapa_reg_date ON plantaopro.comercial_oportunidades(etapa, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_comercial_propostas_status_validade ON plantaopro.comercial_propostas(status, validade);
CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_usuario_reg_date ON plantaopro.lgpd_consentimentos(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_lgpd_solicitacoes_cliente_status ON plantaopro.lgpd_solicitacoes_titular(cliente_id, status, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_lgpd_eventos_privacidade_usuario_reg_date ON plantaopro.lgpd_eventos_privacidade(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_ajuda_artigos_perfil_titulo ON plantaopro.ajuda_artigos(perfil, titulo);
CREATE INDEX IF NOT EXISTS ix_ajuda_feedbacks_artigo_reg_date ON plantaopro.ajuda_feedbacks(artigo_id, reg_date DESC);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='uq_jornada_cliente_cliente' AND conrelid='plantaopro.jornada_cliente'::regclass) THEN
        ALTER TABLE plantaopro.jornada_cliente ADD CONSTRAINT uq_jornada_cliente_cliente UNIQUE (cliente_id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_comercial_propostas_valores' AND conrelid='plantaopro.comercial_propostas'::regclass) THEN
        ALTER TABLE plantaopro.comercial_propostas ADD CONSTRAINT ck_comercial_propostas_valores CHECK (valor_total > 0 AND desconto_percentual >= 0);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_faturas_saas_valores_inteligente' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_valores_inteligente CHECK (valor >= 0 AND (valor_pago IS NULL OR valor_pago >= 0));
    END IF;
END $$;

INSERT INTO plantaopro.lgpd_bases_legais(nome, descricao)
SELECT x.nome, x.descricao FROM (VALUES
('Execução de contrato','Necessária para prestação do serviço PlantãoPro.'),
('Cumprimento de obrigação legal','Necessária para retenções e obrigações regulatórias.'),
('Legítimo interesse','Segurança, auditoria, antifraude e melhoria do serviço.'),
('Consentimento','Quando a manifestação livre do titular é aplicável.'),
('Exercício regular de direitos','Defesa em processos administrativos, judiciais ou arbitrais.')
) AS x(nome, descricao) WHERE NOT EXISTS (SELECT 1 FROM plantaopro.lgpd_bases_legais b WHERE b.nome=x.nome);

INSERT INTO plantaopro.lgpd_politicas(versao,titulo,conteudo,publicada,vigente_desde)
SELECT '2026.06','Política de Privacidade PlantãoPro','Finalidades: gestão de plantões, financeiro, comunicação operacional, auditoria e segurança, suporte, faturamento SaaS, Customer Success e cumprimento legal/regulatório.',true,now()
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.lgpd_politicas WHERE versao='2026.06');

INSERT INTO plantaopro.comercial_regras_desconto(perfil,limite_percentual,exige_aprovacao)
SELECT x.perfil,x.limite,x.aprovacao FROM (VALUES ('ADMINISTRADOR',15,true),('ADMINISTRADOR_GLOBAL',100,false)) AS x(perfil,limite,aprovacao)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.comercial_regras_desconto r WHERE r.perfil=x.perfil);
