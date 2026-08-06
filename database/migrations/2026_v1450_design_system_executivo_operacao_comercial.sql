-- PlantãoPro v1.45.0 - operação inteligente, comercial B2B e experiência premium
-- Estruturas são tenant-aware e preservam histórico auditável das decisões operacionais.

CREATE TABLE IF NOT EXISTS agenda_evento_participantes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, evento_id uuid NOT NULL,
 usuario_id uuid NULL, medico_id uuid NULL, papel varchar(30) NOT NULL DEFAULT 'PARTICIPANTE',
 status varchar(30) NOT NULL DEFAULT 'PENDENTE', respondido_em timestamptz NULL,
 criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_agenda_participante_identidade CHECK (usuario_id IS NOT NULL OR medico_id IS NOT NULL),
 CONSTRAINT uq_agenda_participante UNIQUE NULLS NOT DISTINCT (tenant_id,evento_id,usuario_id,medico_id)
);
CREATE INDEX IF NOT EXISTS idx_agenda_participantes_evento ON agenda_evento_participantes(tenant_id,evento_id,status);

CREATE TABLE IF NOT EXISTS agenda_evento_conflitos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, evento_id uuid NOT NULL,
 evento_conflitante_id uuid NULL, tipo varchar(40) NOT NULL, severidade varchar(20) NOT NULL DEFAULT 'ALTA',
 descricao varchar(1000) NOT NULL, status varchar(20) NOT NULL DEFAULT 'ABERTO',
 resolucao varchar(1000) NULL, resolvido_por uuid NULL, resolvido_em timestamptz NULL,
 criado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_agenda_conflito_status CHECK (status IN ('ABERTO','IGNORADO','RESOLVIDO'))
);
CREATE INDEX IF NOT EXISTS idx_agenda_conflitos_abertos ON agenda_evento_conflitos(tenant_id,evento_id,severidade) WHERE status = 'ABERTO';

CREATE TABLE IF NOT EXISTS medico_checkins (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, medico_id uuid NOT NULL, escala_id uuid NOT NULL,
 checkin_em timestamptz NOT NULL DEFAULT now(), checkout_em timestamptz NULL,
 checkin_latitude numeric(9,6) NULL, checkin_longitude numeric(9,6) NULL,
 checkout_latitude numeric(9,6) NULL, checkout_longitude numeric(9,6) NULL,
 origem varchar(20) NOT NULL DEFAULT 'MOBILE', dispositivo jsonb NOT NULL DEFAULT '{}'::jsonb,
 criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_medico_checkin_escala UNIQUE (tenant_id,escala_id),
 CONSTRAINT ck_medico_checkout_ordem CHECK (checkout_em IS NULL OR checkout_em >= checkin_em)
);
CREATE INDEX IF NOT EXISTS idx_medico_checkins_medico ON medico_checkins(tenant_id,medico_id,checkin_em DESC);

CREATE TABLE IF NOT EXISTS medico_disponibilidade_regras (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, medico_id uuid NOT NULL,
 tipo varchar(20) NOT NULL, dia_semana smallint NULL, inicio_hora time NULL, fim_hora time NULL,
 inicio_em timestamptz NULL, fim_em timestamptz NULL, especialidade_id uuid NULL, unidade_id uuid NULL,
 ativa boolean NOT NULL DEFAULT true, observacao varchar(500) NULL, criado_em timestamptz NOT NULL DEFAULT now(),
 atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_disponibilidade_tipo CHECK (tipo IN ('DISPONIVEL','INDISPONIVEL')),
 CONSTRAINT ck_disponibilidade_dia CHECK (dia_semana IS NULL OR dia_semana BETWEEN 0 AND 6),
 CONSTRAINT ck_disponibilidade_periodo CHECK (fim_em IS NULL OR inicio_em IS NULL OR fim_em > inicio_em)
);
CREATE INDEX IF NOT EXISTS idx_disponibilidade_regras_medico ON medico_disponibilidade_regras(tenant_id,medico_id,ativa);

CREATE TABLE IF NOT EXISTS onboarding_etapas_execucao (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, etapa varchar(60) NOT NULL,
 status varchar(20) NOT NULL DEFAULT 'PENDENTE', progresso smallint NOT NULL DEFAULT 0,
 dados_rascunho jsonb NOT NULL DEFAULT '{}'::jsonb, pendencias jsonb NOT NULL DEFAULT '[]'::jsonb,
 iniciado_por uuid NULL, concluido_por uuid NULL, iniciado_em timestamptz NULL, concluido_em timestamptz NULL,
 atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_onboarding_execucao_etapa UNIQUE (tenant_id,etapa),
 CONSTRAINT ck_onboarding_execucao_progresso CHECK (progresso BETWEEN 0 AND 100),
 CONSTRAINT ck_onboarding_execucao_status CHECK (status IN ('PENDENTE','EM_ANDAMENTO','CONCLUIDA','BLOQUEADA'))
);

CREATE TABLE IF NOT EXISTS relatorio_modelos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, codigo varchar(60) NOT NULL,
 nome varchar(140) NOT NULL, categoria varchar(40) NOT NULL, descricao varchar(500) NULL,
 definicao jsonb NOT NULL DEFAULT '{}'::jsonb, formatos varchar(10)[] NOT NULL DEFAULT ARRAY['CSV']::varchar[],
 ativo boolean NOT NULL DEFAULT true, criado_por uuid NULL, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_relatorio_modelo_codigo UNIQUE NULLS NOT DISTINCT (tenant_id,codigo)
);
CREATE TABLE IF NOT EXISTS relatorio_execucoes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, modelo_id uuid NOT NULL,
 solicitado_por uuid NOT NULL, formato varchar(10) NOT NULL, filtros jsonb NOT NULL DEFAULT '{}'::jsonb,
 status varchar(20) NOT NULL DEFAULT 'SOLICITADA', progresso smallint NOT NULL DEFAULT 0,
 arquivo_chave varchar(500) NULL, erro text NULL, solicitado_em timestamptz NOT NULL DEFAULT now(),
 iniciado_em timestamptz NULL, concluido_em timestamptz NULL, expira_em timestamptz NULL,
 CONSTRAINT ck_relatorio_execucao_formato CHECK (formato IN ('CSV','XLSX','PDF')),
 CONSTRAINT ck_relatorio_execucao_progresso CHECK (progresso BETWEEN 0 AND 100)
);
CREATE INDEX IF NOT EXISTS idx_relatorio_execucoes_historico ON relatorio_execucoes(tenant_id,solicitado_por,solicitado_em DESC);

CREATE TABLE IF NOT EXISTS superadmin_cliente_riscos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tipo varchar(40) NOT NULL,
 severidade varchar(20) NOT NULL, score numeric(5,2) NOT NULL, evidencias jsonb NOT NULL DEFAULT '[]'::jsonb,
 status varchar(20) NOT NULL DEFAULT 'ABERTO', responsavel_id uuid NULL, proxima_acao varchar(500) NULL,
 detectado_em timestamptz NOT NULL DEFAULT now(), resolvido_em timestamptz NULL, atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_cliente_risco_score CHECK (score BETWEEN 0 AND 100)
);
CREATE INDEX IF NOT EXISTS idx_cliente_riscos_abertos ON superadmin_cliente_riscos(tenant_id,severidade,score DESC) WHERE status = 'ABERTO';

CREATE TABLE IF NOT EXISTS white_label_temas (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, nome varchar(100) NOT NULL,
 logo_url varchar(500) NULL, cor_primaria varchar(9) NOT NULL, cor_secundaria varchar(9) NOT NULL,
 cor_fundo varchar(9) NOT NULL, cor_texto varchar(9) NOT NULL, contraste_minimo numeric(4,2) NOT NULL,
 contraste_aa boolean NOT NULL, tokens jsonb NOT NULL DEFAULT '{}'::jsonb, ativo boolean NOT NULL DEFAULT false,
 criado_por uuid NOT NULL, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_white_label_tema_contraste CHECK (contraste_minimo BETWEEN 1 AND 21),
 CONSTRAINT ck_white_label_tema_ativo_legivel CHECK (NOT ativo OR contraste_aa)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_white_label_tema_ativo ON white_label_temas(tenant_id) WHERE ativo;
CREATE TABLE IF NOT EXISTS white_label_historico (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tema_id uuid NOT NULL,
 acao varchar(30) NOT NULL, antes jsonb NULL, depois jsonb NOT NULL, alterado_por uuid NOT NULL,
 alterado_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_white_label_historico ON white_label_historico(tenant_id,alterado_em DESC);

CREATE TABLE IF NOT EXISTS ajuda_contextual_topicos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, rota varchar(180) NOT NULL,
 perfil varchar(50) NULL, titulo varchar(140) NOT NULL, resumo varchar(500) NOT NULL,
 acao_texto varchar(80) NULL, acao_url varchar(500) NULL, prioridade smallint NOT NULL DEFAULT 0,
 ativo boolean NOT NULL DEFAULT true, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_ajuda_contextual_rota ON ajuda_contextual_topicos(rota,perfil,ativo,prioridade DESC);

CREATE TABLE IF NOT EXISTS operacao_assistida_runbooks (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, codigo varchar(60) NOT NULL,
 titulo varchar(140) NOT NULL, modulo varchar(40) NOT NULL, gatilho jsonb NOT NULL DEFAULT '{}'::jsonb,
 passos jsonb NOT NULL DEFAULT '[]'::jsonb, versao integer NOT NULL DEFAULT 1, ativo boolean NOT NULL DEFAULT true,
 criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_runbook_codigo_versao UNIQUE NULLS NOT DISTINCT (tenant_id,codigo,versao)
);

CREATE TABLE IF NOT EXISTS notificacao_agrupamentos_v145 (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL,
 chave varchar(160) NOT NULL, categoria varchar(40) NOT NULL, severidade varchar(20) NOT NULL DEFAULT 'INFORMATIVA',
 titulo varchar(160) NOT NULL, quantidade integer NOT NULL DEFAULT 1, ultima_notificacao_em timestamptz NOT NULL DEFAULT now(),
 lido_em timestamptz NULL, expira_em timestamptz NULL, acao_url varchar(500) NULL,
 CONSTRAINT uq_notificacao_agrupamento UNIQUE (tenant_id,usuario_id,chave),
 CONSTRAINT ck_notificacao_quantidade CHECK (quantidade > 0)
);
CREATE INDEX IF NOT EXISTS idx_notificacao_agrupamentos_caixa ON notificacao_agrupamentos_v145(tenant_id,usuario_id,lido_em,ultima_notificacao_em DESC);

CREATE TABLE IF NOT EXISTS user_saved_dashboards (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL,
 nome varchar(120) NOT NULL, perfil varchar(50) NOT NULL, configuracao jsonb NOT NULL DEFAULT '{}'::jsonb,
 padrao boolean NOT NULL DEFAULT false, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_saved_dashboard_padrao ON user_saved_dashboards(tenant_id,usuario_id,perfil) WHERE padrao;
CREATE INDEX IF NOT EXISTS idx_saved_dashboards_usuario ON user_saved_dashboards(tenant_id,usuario_id,perfil);
