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
    ADD COLUMN IF NOT EXISTS horas_previstas numeric(6,2) GENERATED ALWAYS AS (EXTRACT(EPOCH FROM (fim_utc - inicio_utc))/3600.0) STORED,
    ADD COLUMN IF NOT EXISTS score_prioridade numeric(8,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS conflito_horario boolean NOT NULL DEFAULT false;

ALTER TABLE plantaopro.escalas
    ADD CONSTRAINT IF NOT EXISTS ck_escalas_horas_previstas_positivas CHECK (horas_previstas > 0 AND horas_previstas <= 24);

ALTER TABLE plantaopro.pagamentos
    ADD COLUMN IF NOT EXISTS horas_referencia numeric(6,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS valor_hora numeric(10,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS processado_automaticamente boolean NOT NULL DEFAULT false;

ALTER TABLE plantaopro.pagamentos
    ADD CONSTRAINT IF NOT EXISTS ck_pagamentos_valor_hora_nao_negativo CHECK (valor_hora >= 0),
    ADD CONSTRAINT IF NOT EXISTS ck_pagamentos_horas_referencia_nao_negativa CHECK (horas_referencia >= 0);

CREATE INDEX IF NOT EXISTS ix_escalas_medico_inicio_fim ON plantaopro.escalas (medico_id, inicio_utc, fim_utc);
CREATE INDEX IF NOT EXISTS ix_escalas_hospital_especialidade_inicio ON plantaopro.escalas (hospital_id, especialidade_id, inicio_utc DESC);
CREATE INDEX IF NOT EXISTS ix_pagamentos_status_vencimento ON plantaopro.pagamentos (status, vencimento_utc);

COMMENT ON COLUMN plantaopro.escalas.score_prioridade IS 'Score para priorização inteligente de médicos com menor carga recente.';
COMMENT ON COLUMN plantaopro.escalas.conflito_horario IS 'Flag operacional para alertas visuais e auditoria de conflitos.';
