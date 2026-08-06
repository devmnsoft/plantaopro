-- PlantãoPro v1.44.0 - produto vendável, agenda e operação mobile
CREATE TABLE IF NOT EXISTS agenda_eventos_operacionais (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, unidade_id uuid NULL,
 tipo varchar(40) NOT NULL, titulo varchar(180) NOT NULL, descricao text NULL,
 inicio_em timestamptz NOT NULL, fim_em timestamptz NOT NULL, status varchar(30) NOT NULL DEFAULT 'AGENDADO',
 origem_tipo varchar(50) NULL, origem_id uuid NULL, responsavel_usuario_id uuid NULL,
 metadados jsonb NOT NULL DEFAULT '{}'::jsonb, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_agenda_evento_periodo CHECK (fim_em > inicio_em)
);
CREATE INDEX IF NOT EXISTS idx_agenda_eventos_tenant_periodo ON agenda_eventos_operacionais(tenant_id,inicio_em,fim_em);
CREATE INDEX IF NOT EXISTS idx_agenda_eventos_responsavel ON agenda_eventos_operacionais(responsavel_usuario_id,inicio_em) WHERE responsavel_usuario_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS medico_registros_jornada (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, medico_id uuid NOT NULL, escala_id uuid NOT NULL,
 tipo varchar(20) NOT NULL, registrado_em timestamptz NOT NULL DEFAULT now(), latitude numeric(9,6) NULL, longitude numeric(9,6) NULL,
 observacao varchar(1000) NULL, dispositivo jsonb NOT NULL DEFAULT '{}'::jsonb, criado_por uuid NOT NULL,
 CONSTRAINT ck_medico_jornada_tipo CHECK (tipo IN ('CHECKIN','CHECKOUT')),
 CONSTRAINT uq_medico_jornada_escala_tipo UNIQUE(escala_id,tipo)
);
CREATE INDEX IF NOT EXISTS idx_medico_jornada_medico_data ON medico_registros_jornada(medico_id,registrado_em DESC);

CREATE TABLE IF NOT EXISTS onboarding_progresso (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, etapa varchar(50) NOT NULL, status varchar(20) NOT NULL DEFAULT 'PENDENTE',
 dados jsonb NOT NULL DEFAULT '{}'::jsonb, concluido_por uuid NULL, concluido_em timestamptz NULL, atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_onboarding_tenant_etapa UNIQUE(tenant_id,etapa),
 CONSTRAINT ck_onboarding_status CHECK(status IN ('PENDENTE','EM_ANDAMENTO','CONCLUIDO','BLOQUEADO'))
);
CREATE TABLE IF NOT EXISTS relatorios_salvos_v144 (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL, nome varchar(140) NOT NULL,
 tipo varchar(40) NOT NULL, filtros jsonb NOT NULL DEFAULT '{}'::jsonb, formato_padrao varchar(10) NOT NULL DEFAULT 'CSV', criado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS exportacoes_gerenciais (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL, relatorio_id uuid NULL,
 tipo varchar(40) NOT NULL, formato varchar(10) NOT NULL, status varchar(20) NOT NULL DEFAULT 'SOLICITADA', arquivo_chave varchar(500) NULL,
 expira_em timestamptz NULL, erro text NULL, solicitado_em timestamptz NOT NULL DEFAULT now(), concluido_em timestamptz NULL
);
CREATE INDEX IF NOT EXISTS idx_exportacoes_tenant_usuario ON exportacoes_gerenciais(tenant_id,usuario_id,solicitado_em DESC);

CREATE TABLE IF NOT EXISTS notificacoes_mobile (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL, categoria varchar(30) NOT NULL,
 severidade varchar(20) NOT NULL DEFAULT 'INFORMATIVA', titulo varchar(160) NOT NULL, mensagem text NOT NULL, destino_seguro varchar(500) NULL,
 agrupamento_chave varchar(160) NULL, lida_em timestamptz NULL, expira_em timestamptz NULL, criada_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_notificacoes_mobile_caixa ON notificacoes_mobile(tenant_id,usuario_id,lida_em,criada_em DESC);

CREATE TABLE IF NOT EXISTS white_label_previews (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL,
 tema jsonb NOT NULL, contraste_minimo numeric(4,2) NOT NULL, contraste_valido boolean NOT NULL,
 criado_em timestamptz NOT NULL DEFAULT now(), aplicado_em timestamptz NULL,
 CONSTRAINT ck_white_label_contraste CHECK (contraste_minimo >= 1 AND contraste_minimo <= 21),
 CONSTRAINT ck_white_label_aplicacao_segura CHECK (aplicado_em IS NULL OR contraste_valido)
);
CREATE TABLE IF NOT EXISTS acoes_rapidas_auditoria (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, usuario_id uuid NOT NULL, acao varchar(80) NOT NULL,
 entidade varchar(80) NULL, entidade_id uuid NULL, resultado varchar(20) NOT NULL, contexto jsonb NOT NULL DEFAULT '{}'::jsonb,
 ip inet NULL, criada_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_acoes_rapidas_auditoria_tenant ON acoes_rapidas_auditoria(tenant_id,criada_em DESC);
