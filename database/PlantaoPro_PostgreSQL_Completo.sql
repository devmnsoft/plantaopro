CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
SET search_path TO plantaopro;
CREATE TABLE IF NOT EXISTS perfis(id uuid primary key default uuid_generate_v4(),nome varchar(60) unique not null,descricao varchar(255),reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS usuarios(id uuid primary key default uuid_generate_v4(),nome varchar(120) not null,email varchar(120) unique not null,senha_hash varchar(255) not null,telefone varchar(20),reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS usuarios_perfis(id uuid primary key default uuid_generate_v4(),usuario_id uuid references usuarios(id),perfil_id uuid references perfis(id),reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS especialidades(id uuid primary key default uuid_generate_v4(),nome varchar(100) unique not null,descricao text,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS hospitais(id uuid primary key default uuid_generate_v4(),razao_social varchar(160),nome_fantasia varchar(160) not null,cnpj varchar(18) unique not null,telefone varchar(20),email varchar(120),endereco text,cidade varchar(80) not null,estado char(2) not null,responsavel varchar(120),reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS medicos(id uuid primary key default uuid_generate_v4(),usuario_id uuid references usuarios(id),especialidade_id uuid references especialidades(id),nome varchar(120),cpf varchar(14) unique,crm varchar(20),uf_crm char(2),telefone varchar(20),email varchar(120),cidade varchar(80),estado char(2),pix_chave varchar(120),dados_bancarios jsonb,observacoes text,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS plantoes(id uuid primary key default uuid_generate_v4(),hospital_id uuid references hospitais(id),especialidade_id uuid references especialidades(id),data_inicio timestamp not null,data_fim timestamp not null,valor numeric(12,2),vagas int,vagas_disponiveis int,tipo varchar(20),status varchar(20),observacoes text,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS escalas(id uuid primary key default uuid_generate_v4(),plantao_id uuid references plantoes(id),medico_id uuid references medicos(id),status varchar(20),justificativa text,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS pagamentos(id uuid primary key default uuid_generate_v4(),escala_id uuid references escalas(id),medico_id uuid references medicos(id),plantao_id uuid references plantoes(id),valor_previsto numeric(12,2),valor_pago numeric(12,2),status varchar(20),data_prevista date,data_pagamento date,forma_pagamento varchar(40),chave_pix varchar(120),observacoes text,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS notificacoes(id uuid primary key default uuid_generate_v4(),usuario_id uuid references usuarios(id),titulo varchar(160),mensagem text,tipo varchar(40),lida boolean default false,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS auditoria(id uuid primary key default uuid_generate_v4(),usuario_id uuid,acao varchar(60),entidade varchar(60),registro_id uuid,ip varchar(50),user_agent varchar(300),valor_anterior text,valor_novo text,descricao text,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS historico_plantao(id uuid primary key default uuid_generate_v4(),plantao_id uuid references plantoes(id),status_anterior varchar(20),status_novo varchar(20),justificativa text,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE TABLE IF NOT EXISTS historico_escala(id uuid primary key default uuid_generate_v4(),escala_id uuid references escalas(id),status_anterior varchar(20),status_novo varchar(20),justificativa text,reg_date timestamp default now(),reg_update timestamp,reg_status char(1) default 'A' check(reg_status in('A','I')),created_by uuid,updated_by uuid);
CREATE INDEX IF NOT EXISTS idx_plantoes_status ON plantoes(status);
CREATE INDEX IF NOT EXISTS idx_plantoes_data_inicio ON plantoes(data_inicio);
CREATE INDEX IF NOT EXISTS idx_plantoes_hospital ON plantoes(hospital_id);
CREATE INDEX IF NOT EXISTS idx_plantoes_especialidade ON plantoes(especialidade_id);
CREATE INDEX IF NOT EXISTS idx_escalas_medico ON escalas(medico_id);
CREATE INDEX IF NOT EXISTS idx_escalas_plantao ON escalas(plantao_id);
CREATE INDEX IF NOT EXISTS idx_pagamentos_status ON pagamentos(status);
CREATE INDEX IF NOT EXISTS idx_notificacoes_usuario_lida ON notificacoes(usuario_id,lida);

CREATE TABLE IF NOT EXISTS historico_pagamento(id uuid primary key default uuid_generate_v4(),pagamento_id uuid references pagamentos(id),status_anterior varchar(20),status_novo varchar(20),justificativa text,usuario_id uuid,reg_date timestamp default now());


CREATE INDEX IF NOT EXISTS idx_escalas_status ON escalas(status);
CREATE INDEX IF NOT EXISTS idx_pagamentos_medico ON pagamentos(medico_id);
CREATE INDEX IF NOT EXISTS idx_pagamentos_plantao ON pagamentos(plantao_id);
CREATE INDEX IF NOT EXISTS idx_historico_escala_escala ON historico_escala(escala_id);
CREATE INDEX IF NOT EXISTS idx_historico_pagamento_pagamento ON historico_pagamento(pagamento_id);

-- Evolução comercial 2026: inteligência operacional e financeira
ALTER TABLE plantaopro.escalas
    ADD COLUMN IF NOT EXISTS data_inicio timestamp,
    ADD COLUMN IF NOT EXISTS data_fim timestamp,
    ADD COLUMN IF NOT EXISTS horas_previstas numeric(6,2),
    ADD COLUMN IF NOT EXISTS score_prioridade numeric(8,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS conflito_detectado boolean NOT NULL DEFAULT false;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_escalas_horas_previstas_positivas'
          AND conrelid = 'plantaopro.escalas'::regclass
    ) THEN
        ALTER TABLE plantaopro.escalas
        ADD CONSTRAINT ck_escalas_horas_previstas_positivas CHECK (horas_previstas IS NULL OR (horas_previstas > 0 AND horas_previstas <= 24));
    END IF;
END $$;

ALTER TABLE plantaopro.pagamentos
    ADD COLUMN IF NOT EXISTS horas_referencia numeric(6,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS valor_hora numeric(10,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS processado_automaticamente boolean NOT NULL DEFAULT false;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_pagamentos_valor_hora_nao_negativo'
          AND conrelid = 'plantaopro.pagamentos'::regclass
    ) THEN
        ALTER TABLE plantaopro.pagamentos
        ADD CONSTRAINT ck_pagamentos_valor_hora_nao_negativo CHECK (valor_hora >= 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_pagamentos_horas_referencia_nao_negativa'
          AND conrelid = 'plantaopro.pagamentos'::regclass
    ) THEN
        ALTER TABLE plantaopro.pagamentos
        ADD CONSTRAINT ck_pagamentos_horas_referencia_nao_negativa CHECK (horas_referencia >= 0);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_escalas_medico_data_inicio_data_fim ON plantaopro.escalas (medico_id, data_inicio, data_fim);
CREATE INDEX IF NOT EXISTS ix_pagamentos_status_data_prevista ON plantaopro.pagamentos (status, data_prevista);

COMMENT ON COLUMN plantaopro.escalas.score_prioridade IS 'Score para priorização inteligente de médicos com menor carga recente.';
COMMENT ON COLUMN plantaopro.escalas.conflito_detectado IS 'Flag operacional para alertas visuais e auditoria de conflitos.';
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE TABLE IF NOT EXISTS plantaopro.fechamentos_operacionais(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,cliente_id uuid NOT NULL,plantao_id uuid NOT NULL REFERENCES plantaopro.plantoes(id),unidade_id uuid NULL,hospital_id uuid NULL,status varchar(40) NOT NULL,data_referencia timestamptz NOT NULL,valor_previsto numeric(14,2) NOT NULL DEFAULT 0,valor_apurado numeric(14,2) NOT NULL DEFAULT 0,horas_previstas numeric(10,2) NOT NULL DEFAULT 0,horas_realizadas numeric(10,2) NOT NULL DEFAULT 0,quantidade_escalas integer NOT NULL DEFAULT 0,conferido_por uuid NULL,conferido_em timestamptz NULL,aprovado_por uuid NULL,aprovado_em timestamptz NULL,devolvido_por uuid NULL,devolvido_em timestamptz NULL,motivo_devolucao varchar(500) NULL,financeiro_gerado_por uuid NULL,financeiro_gerado_em timestamptz NULL,created_by uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),updated_by uuid NULL,updated_at timestamptz NULL,CONSTRAINT uq_fechamento_tenant_plantao UNIQUE(tenant_id,plantao_id));
CREATE TABLE IF NOT EXISTS plantaopro.fechamento_operacional_itens(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),fechamento_id uuid NOT NULL REFERENCES plantaopro.fechamentos_operacionais(id) ON DELETE CASCADE,escala_id uuid NOT NULL REFERENCES plantaopro.escalas(id),medico_id uuid NOT NULL REFERENCES plantaopro.medicos(id),plantao_id uuid NOT NULL REFERENCES plantaopro.plantoes(id),status_escala varchar(40) NOT NULL,horas_previstas numeric(10,2) NOT NULL,horas_realizadas numeric(10,2) NOT NULL,valor_previsto numeric(14,2) NOT NULL,valor_apurado numeric(14,2) NOT NULL,observacao_operacional text NULL,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NULL,UNIQUE(fechamento_id,escala_id));
CREATE TABLE IF NOT EXISTS plantaopro.fechamento_divergencias(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),fechamento_id uuid NOT NULL REFERENCES plantaopro.fechamentos_operacionais(id) ON DELETE CASCADE,fechamento_item_id uuid NULL REFERENCES plantaopro.fechamento_operacional_itens(id),tipo varchar(30) NOT NULL,descricao text NOT NULL,valor_anterior numeric(14,2),valor_proposto numeric(14,2),status varchar(20) NOT NULL,motivo text,created_by uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),resolved_by uuid,resolved_at timestamptz,resolucao text,updated_at timestamptz);
CREATE TABLE IF NOT EXISTS plantaopro.fechamento_historico(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),fechamento_id uuid NOT NULL REFERENCES plantaopro.fechamentos_operacionais(id) ON DELETE CASCADE,status_anterior varchar(40),status_novo varchar(40) NOT NULL,acao varchar(60) NOT NULL,motivo text,created_by uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.fechamento_pagamentos(fechamento_id uuid NOT NULL REFERENCES plantaopro.fechamentos_operacionais(id),fechamento_item_id uuid NOT NULL REFERENCES plantaopro.fechamento_operacional_itens(id),pagamento_id uuid NOT NULL REFERENCES plantaopro.pagamentos(id),created_at timestamptz NOT NULL DEFAULT now(),PRIMARY KEY(fechamento_id,pagamento_id),UNIQUE(fechamento_item_id,pagamento_id));
CREATE TABLE IF NOT EXISTS plantaopro.pagamento_contestacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,cliente_id uuid NOT NULL,pagamento_id uuid NOT NULL REFERENCES plantaopro.pagamentos(id),motivo text NOT NULL,status varchar(20) NOT NULL,aberto_por uuid NOT NULL,aberto_em timestamptz NOT NULL DEFAULT now(),decisao varchar(40),justificativa_resolucao text,valor_anterior numeric(14,2) NOT NULL,valor_resolvido numeric(14,2),resolvido_por uuid,resolvido_em timestamptz,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS uq_pagamento_contestacao_aberta ON plantaopro.pagamento_contestacoes(pagamento_id) WHERE status='ABERTA';
CREATE INDEX IF NOT EXISTS ix_fechamento_tenant_status ON plantaopro.fechamentos_operacionais(tenant_id,status,data_referencia);
CREATE INDEX IF NOT EXISTS ix_fechamento_item_fechamento ON plantaopro.fechamento_operacional_itens(fechamento_id,escala_id);
CREATE INDEX IF NOT EXISTS ix_fechamento_divergencia ON plantaopro.fechamento_divergencias(fechamento_id,status,created_at);
CREATE INDEX IF NOT EXISTS ix_fechamento_historico ON plantaopro.fechamento_historico(fechamento_id,created_at);
CREATE INDEX IF NOT EXISTS ix_contestacao_tenant_pagamento ON plantaopro.pagamento_contestacoes(tenant_id,pagamento_id,created_at);
