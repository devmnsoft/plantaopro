-- PlantãoPro - script completo oficial de instalação limpa
-- Versão do schema: v1.18.4
-- PostgreSQL suportado: 16
-- Data de geração: 2026-07-21
-- Execução oficial:
--   psql \
--     -v ON_ERROR_STOP=1 \
--     -h localhost \
--     -p 5432 \
--     -U postgres \
--     -d plantaopro \
--     -f database/scrpt_completo.sql
-- O banco de dados de destino deve existir antes da execução.
-- Este arquivo não contém credenciais reais, senhas administrativas, tokens ou connection strings.
-- Não use scripts de demonstração em produção.

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE SCHEMA IF NOT EXISTS plantaopro;
SET search_path TO plantaopro, public;

-- ============================================================
-- Seção 04 — Tabelas fundamentais e SaaS mínimo antes de ALTERs
-- ============================================================

CREATE TABLE IF NOT EXISTS plantaopro.planos (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, slug text, descricao text, valor_mensal numeric(14,2) NOT NULL DEFAULT 0, valor_anual numeric(14,2) NOT NULL DEFAULT 0, limite_medicos int NOT NULL DEFAULT 0, limite_hospitais int NOT NULL DEFAULT 0, limite_plantoes_mes int NOT NULL DEFAULT 0, limite_usuarios int NOT NULL DEFAULT 0, limite_convites_mes int, permite_mobile boolean NOT NULL DEFAULT false, permite_bi boolean NOT NULL DEFAULT false, permite_api boolean NOT NULL DEFAULT false, permite_integracoes boolean NOT NULL DEFAULT false, permite_relatorios boolean NOT NULL DEFAULT true, permite_relatorios_avancados boolean NOT NULL DEFAULT true, permite_white_label boolean NOT NULL DEFAULT false, permite_trial boolean NOT NULL DEFAULT true, dias_trial int NOT NULL DEFAULT 14, permite_perfis_customizados boolean NOT NULL DEFAULT true, permite_suporte_prioritario boolean NOT NULL DEFAULT false, permite_operacao_assistida boolean NOT NULL DEFAULT false, destaque boolean NOT NULL DEFAULT false, publico boolean NOT NULL DEFAULT true, ordem int NOT NULL DEFAULT 0, ordem_exibicao int NOT NULL DEFAULT 0, status varchar(40) NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);

CREATE TABLE IF NOT EXISTS plantaopro.clientes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), razao_social varchar(200), nome_fantasia varchar(200), cnpj varchar(30), email varchar(180), telefone varchar(40), cidade varchar(100), estado char(2), plano_id uuid, status varchar(40) NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);

CREATE TABLE IF NOT EXISTS plantaopro.tenants (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid, plano_id uuid, nome varchar(180) NOT NULL, slug text NOT NULL, subdominio text, status varchar(40) NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);

CREATE TABLE IF NOT EXISTS plantaopro.assinaturas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid NOT NULL, plano_id uuid NOT NULL, status varchar(40) NOT NULL DEFAULT 'ATIVA', data_inicio date NOT NULL DEFAULT current_date, data_fim date, data_trial_fim date, valor_contratado numeric(14,2) NOT NULL DEFAULT 0, valor_mensal numeric(14,2) NOT NULL DEFAULT 0, dia_vencimento int NOT NULL DEFAULT 5, periodicidade varchar(20) NOT NULL DEFAULT 'MENSAL', observacoes text, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz);

-- ============================================================
-- Seção 05 — Schema histórico canônico ordenado por dependência
-- ============================================================

-- Origem histórica: database/PlantaoPro_PostgreSQL_Completo.sql
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

-- Origem histórica: database/20260525_evolucao_saas_premium.sql
-- Evolução SaaS Premium - PlantãoPro
create schema if not exists plantaopro;

create table if not exists plantaopro.medico_score (
    id uuid primary key default gen_random_uuid(),
    medico_id uuid not null,
    cliente_id uuid null,
    score_geral numeric(5,2) not null default 0,
    taxa_aceite numeric(5,2) not null default 0,
    taxa_cancelamento numeric(5,2) not null default 0,
    pontualidade numeric(5,2) not null default 0,
    total_horas_mes numeric(10,2) not null default 0,
    total_plantoes_mes int not null default 0,
    total_realizados int not null default 0,
    total_recusados int not null default 0,
    total_cancelados int not null default 0,
    media_avaliacao numeric(3,2) not null default 0,
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);
create index if not exists idx_medico_score_medico on plantaopro.medico_score(medico_id);

create table if not exists plantaopro.plantao_avaliacoes (
    id uuid primary key default gen_random_uuid(),
    escala_id uuid not null,
    plantao_id uuid not null,
    medico_id uuid not null,
    hospital_id uuid null,
    cliente_id uuid null,
    avaliador_usuario_id uuid not null,
    nota smallint not null,
    pontualidade smallint not null,
    conduta smallint not null,
    comunicacao smallint not null,
    observacoes text null,
    status varchar(30) not null default 'ATIVA',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.cliente_saude (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    score_saude numeric(5,2) not null default 0,
    classificacao varchar(20) not null default 'ATENCAO',
    dias_sem_acesso int not null default 0,
    uso_plano_percentual numeric(5,2) not null default 0,
    faturas_vencidas int not null default 0,
    chamados_criticos int not null default 0,
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.documentos (
    id uuid primary key default gen_random_uuid(),
    nome varchar(255) not null,
    tipo varchar(60) not null,
    content_type varchar(120) not null,
    tamanho_bytes bigint not null,
    storage_path text not null,
    cliente_id uuid null,
    medico_id uuid null,
    hospital_id uuid null,
    criado_por_usuario_id uuid not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'uk_plantao_avaliacoes_escala') then
        alter table plantaopro.plantao_avaliacoes add constraint uk_plantao_avaliacoes_escala unique (escala_id);
    end if;
end $$;

-- Origem histórica: backend/sql/20260522_saas_multiempresa.sql
create schema if not exists plantaopro;

create table if not exists plantaopro.planos (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    descricao text null,
    valor_mensal numeric(12,2) not null default 0,
    limite_medicos int not null default 0,
    limite_hospitais int not null default 0,
    limite_plantoes_mes int not null default 0,
    permite_relatorios boolean not null default true,
    permite_api boolean not null default false,
    permite_notificacao_email boolean not null default true,
    status varchar(20) not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.clientes (
    id uuid primary key default gen_random_uuid(),
    razao_social varchar(180) not null,
    nome_fantasia varchar(180) not null,
    cnpj varchar(18) not null,
    email varchar(180) null,
    telefone varchar(20) null,
    cidade varchar(120) null,
    estado varchar(2) null,
    plano_id uuid null,
    status varchar(20) not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

do $$ begin
if not exists (select 1 from pg_constraint where conname='fk_clientes_planos') then
alter table plantaopro.clientes add constraint fk_clientes_planos foreign key (plano_id) references plantaopro.planos(id);
end if;
end $$;

alter table plantaopro.usuarios add column if not exists cliente_id uuid;
alter table plantaopro.hospitais add column if not exists cliente_id uuid;
alter table plantaopro.medicos add column if not exists cliente_id uuid;
alter table plantaopro.plantoes add column if not exists cliente_id uuid;
alter table plantaopro.escalas add column if not exists cliente_id uuid;
alter table plantaopro.pagamentos add column if not exists cliente_id uuid;
alter table plantaopro.notificacoes add column if not exists cliente_id uuid;
alter table plantaopro.auditoria add column if not exists cliente_id uuid;

create index if not exists ix_clientes_status on plantaopro.clientes(status, reg_status);
create index if not exists ix_hospitais_cliente_id on plantaopro.hospitais(cliente_id);

insert into plantaopro.planos(nome,descricao,valor_mensal,limite_medicos,limite_hospitais,limite_plantoes_mes,permite_api,status)
select 'Starter','Plano inicial para pequenas operações.',299,20,5,150,false,'ATIVO'
where not exists(select 1 from plantaopro.planos where nome='Starter');

insert into plantaopro.clientes(razao_social,nome_fantasia,cnpj,email,telefone,cidade,estado,plano_id,status)
select 'Cliente Demonstração PlantãoPro LTDA','Cliente Demonstração PlantãoPro','12.345.678/0001-90','demo@plantaopro.com','11999999999','São Paulo','SP',(select id from plantaopro.planos where nome='Starter' limit 1),'ATIVO'
where not exists(select 1 from plantaopro.clientes where nome_fantasia='Cliente Demonstração PlantãoPro');

update plantaopro.usuarios set cliente_id=(select id from plantaopro.clientes where nome_fantasia='Cliente Demonstração PlantãoPro' limit 1) where cliente_id is null;

-- Origem histórica: database/migrations/2026_plantao_pro_self_service_white_label.sql
-- PlantãoPro SaaS multiempresa, white label e self-service
-- Idempotente: usa schema plantaopro, CREATE/ALTER IF NOT EXISTS e constraints via pg_constraint.
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.tenants(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NULL,
    nome text NOT NULL,
    slug text NOT NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    plano_id uuid NULL,
    criado_por uuid NULL,
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS plano_id uuid NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS subdominio text NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS dominio_customizado text NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS motivo_suspensao text NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenants_slug ON plantaopro.tenants(lower(slug)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenants_cliente_status_regdate ON plantaopro.tenants(cliente_id,status,reg_date);

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_tenants_status' AND conrelid='plantaopro.tenants'::regclass) THEN
        ALTER TABLE plantaopro.tenants ADD CONSTRAINT ck_tenants_status CHECK (status IN ('ATIVO','TRIAL','SUSPENSO','CANCELADO','PENDENTE'));
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS plantaopro.modulos_sistema(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_modulos_sistema_codigo ON plantaopro.modulos_sistema(lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_modulos_sistema_status_regdate ON plantaopro.modulos_sistema(status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.acoes_sistema(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_acoes_sistema_codigo ON plantaopro.acoes_sistema(lower(codigo)) WHERE reg_status='A';
CREATE TABLE IF NOT EXISTS plantaopro.permissoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), modulo_id uuid NOT NULL, acao_id uuid NOT NULL, codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', sensivel boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_permissoes_codigo ON plantaopro.permissoes(lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_permissoes_modulo_status_regdate ON plantaopro.permissoes(modulo_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.tenant_configuracoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', categoria text NOT NULL DEFAULT 'GERAL', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_configuracoes_chave ON plantaopro.tenant_configuracoes(tenant_id, lower(categoria), lower(chave)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_configuracoes_tenant_status_regdate ON plantaopro.tenant_configuracoes(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, modulo_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'PLANO', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_modulos_codigo ON plantaopro.tenant_modulos(tenant_id, lower(codigo_modulo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_modulos_tenant_status_regdate ON plantaopro.tenant_modulos(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_parametros(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, categoria text NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', tipo text NOT NULL DEFAULT 'texto', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_parametros_chave ON plantaopro.tenant_parametros(tenant_id, lower(categoria), lower(chave)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_parametros_tenant_status_regdate ON plantaopro.tenant_parametros(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_dominios(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, dominio text NOT NULL, tipo text NOT NULL DEFAULT 'SUBDOMINIO', verificado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_dominios_dominio ON plantaopro.tenant_dominios(lower(dominio)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_dominios_tenant_status_regdate ON plantaopro.tenant_dominios(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_white_label(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, nome_plataforma text NOT NULL DEFAULT 'PlantãoPro', cliente_nome text NOT NULL DEFAULT '', slogan text NOT NULL DEFAULT '', logo_url text NOT NULL DEFAULT '', logo_reduzida_url text NOT NULL DEFAULT '', favicon_url text NOT NULL DEFAULT '', cor_primaria text NOT NULL DEFAULT '#0d6efd', cor_secundaria text NOT NULL DEFAULT '#20c997', cor_fundo text NOT NULL DEFAULT '#f8fafc', cor_menu text NOT NULL DEFAULT '#0f172a', tema text NOT NULL DEFAULT 'claro', email_remetente text NOT NULL DEFAULT '', texto_boas_vindas text NOT NULL DEFAULT '', texto_rodape text NOT NULL DEFAULT '', login_banner_url text NOT NULL DEFAULT '', mobile_json jsonb NOT NULL DEFAULT '{}'::jsonb, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_white_label_tenant ON plantaopro.tenant_white_label(tenant_id) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_white_label_tenant_status_regdate ON plantaopro.tenant_white_label(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_onboarding(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, cliente_id uuid NULL, status text NOT NULL DEFAULT 'EM_ANDAMENTO', progresso int NOT NULL DEFAULT 0, proxima_acao text NOT NULL DEFAULT '', iniciado_em timestamp NOT NULL DEFAULT now(), finalizado_em timestamp NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_tenant_onboarding_tenant_status_regdate ON plantaopro.tenant_onboarding(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_onboarding_checklist(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), onboarding_id uuid NOT NULL, tenant_id uuid NOT NULL, cliente_id uuid NULL, codigo text NOT NULL, titulo text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, obrigatorio boolean NOT NULL DEFAULT true, concluido boolean NOT NULL DEFAULT false, concluido_em timestamp NULL, link_acao text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'PENDENTE', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_onboarding_checklist_codigo ON plantaopro.tenant_onboarding_checklist(onboarding_id, lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_onboarding_checklist_tenant_status_regdate ON plantaopro.tenant_onboarding_checklist(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_auditoria_configuracoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, auditar_configuracoes boolean NOT NULL DEFAULT true, auditar_permissoes boolean NOT NULL DEFAULT true, auditar_lgpd boolean NOT NULL DEFAULT true, retencao_dias int NOT NULL DEFAULT 1825, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_tenant_auditoria_configuracoes_tenant_status_regdate ON plantaopro.tenant_auditoria_configuracoes(tenant_id,status,reg_date);

ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS slug text NULL;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS destaque boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS publico boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_trial boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS dias_trial int NOT NULL DEFAULT 14;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_white_label boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_perfis_customizados boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS ordem int NOT NULL DEFAULT 0;
CREATE INDEX IF NOT EXISTS ix_planos_status_regdate ON plantaopro.planos(status,reg_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_planos_slug ON plantaopro.planos(lower(slug)) WHERE reg_status='A' AND slug IS NOT NULL;

CREATE TABLE IF NOT EXISTS plantaopro.plano_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, modulo_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_modulos_plano_status_regdate ON plantaopro.plano_modulos(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_precos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, periodicidade text NOT NULL DEFAULT 'MENSAL', valor numeric(12,2) NOT NULL DEFAULT 0, moeda text NOT NULL DEFAULT 'BRL', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_precos_plano_status_regdate ON plantaopro.plano_precos(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_limites(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, codigo text NOT NULL, limite int NULL, ilimitado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_limites_plano_status_regdate ON plantaopro.plano_limites(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_comparativo(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, grupo text NOT NULL, recurso text NOT NULL, valor text NOT NULL DEFAULT '', incluido boolean NOT NULL DEFAULT false, ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_comparativo_plano_status_regdate ON plantaopro.plano_comparativo(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_faq(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NULL, pergunta text NOT NULL, resposta text NOT NULL, ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_faq_plano_status_regdate ON plantaopro.plano_faq(plano_id,status,reg_date);
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS periodicidade text NOT NULL DEFAULT 'MENSAL';
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS renovacao_automatica boolean NOT NULL DEFAULT true;
CREATE INDEX IF NOT EXISTS ix_assinaturas_tenant_cliente_status_regdate ON plantaopro.assinaturas(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, status_anterior text NOT NULL DEFAULT '', status_novo text NOT NULL, motivo text NOT NULL DEFAULT '', usuario_id uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_historico_tenant_cliente_status_regdate ON plantaopro.assinatura_historico(tenant_id,cliente_id,status_novo,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, competencia date NOT NULL, medicos_usados int NOT NULL DEFAULT 0, hospitais_usados int NOT NULL DEFAULT 0, usuarios_usados int NOT NULL DEFAULT 0, plantoes_usados int NOT NULL DEFAULT 0, convites_usados int NOT NULL DEFAULT 0, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_uso_tenant_cliente_status_regdate ON plantaopro.assinatura_uso(tenant_id,cliente_id,competencia,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'PLANO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_modulos_tenant_cliente_status_regdate ON plantaopro.assinatura_modulos(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_bloqueios(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NULL, tenant_id uuid NULL, cliente_id uuid NULL, tipo text NOT NULL, mensagem text NOT NULL, resolvido boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_bloqueios_tenant_cliente_status_regdate ON plantaopro.assinatura_bloqueios(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.upgrade_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, assinatura_id uuid NULL, plano_atual_id uuid NULL, plano_destino_id uuid NOT NULL, motivo text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'SOLICITADO', solicitado_por uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_upgrade_solicitacoes_tenant_cliente_status_regdate ON plantaopro.upgrade_solicitacoes(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.downgrade_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, assinatura_id uuid NULL, plano_atual_id uuid NULL, plano_destino_id uuid NOT NULL, motivo text NOT NULL DEFAULT '', impacto_validado boolean NOT NULL DEFAULT false, bloqueado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'SOLICITADO', solicitado_por uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_downgrade_solicitacoes_tenant_cliente_status_regdate ON plantaopro.downgrade_solicitacoes(tenant_id,cliente_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, plano_id uuid NULL, nome_fantasia text NOT NULL DEFAULT '', razao_social text NOT NULL DEFAULT '', cnpj text NOT NULL DEFAULT '', segmento text NOT NULL DEFAULT '', qtd_medicos int NOT NULL DEFAULT 0, qtd_hospitais int NOT NULL DEFAULT 0, volume_plantoes_mes int NOT NULL DEFAULT 0, cidade text NOT NULL DEFAULT '', uf text NOT NULL DEFAULT '', telefone text NOT NULL DEFAULT '', email_corporativo text NOT NULL DEFAULT '', responsavel_nome text NOT NULL DEFAULT '', responsavel_email text NOT NULL DEFAULT '', responsavel_telefone text NOT NULL DEFAULT '', responsavel_cargo text NOT NULL DEFAULT '', periodicidade text NOT NULL DEFAULT 'MENSAL', aceite_termos boolean NOT NULL DEFAULT false, aceite_privacidade boolean NOT NULL DEFAULT false, consentimento_lgpd boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'INICIADO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_solicitacoes_tenant_cliente_status_regdate ON plantaopro.cadastro_cliente_solicitacoes(tenant_id,cliente_id,status,reg_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_cadastro_cliente_solicitacoes_cnpj_ativo ON plantaopro.cadastro_cliente_solicitacoes(regexp_replace(cnpj,'\D','','g')) WHERE reg_status='A' AND status <> 'CANCELADO';
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_etapas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, etapa text NOT NULL, concluida boolean NOT NULL DEFAULT false, dados jsonb NOT NULL DEFAULT '{}'::jsonb, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_etapas_status_regdate ON plantaopro.cadastro_cliente_etapas(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_validacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, campo text NOT NULL, mensagem text NOT NULL, valido boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_validacoes_status_regdate ON plantaopro.cadastro_cliente_validacoes(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_convites(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, email text NOT NULL, perfil text NOT NULL DEFAULT 'ADMINISTRADOR_CLIENTE', token_hash text NOT NULL DEFAULT '', aceito boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_convites_status_regdate ON plantaopro.cadastro_cliente_convites(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_pagamentos_iniciais(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, cliente_id uuid NULL, assinatura_id uuid NULL, valor numeric(12,2) NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ABERTO', vencimento date NOT NULL DEFAULT (current_date + 7), reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_pagamentos_status_regdate ON plantaopro.cadastro_cliente_pagamentos_iniciais(cliente_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.perfis(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', base_sistema boolean NOT NULL DEFAULT false, customizado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_perfis_tenant_codigo ON plantaopro.perfis(coalesce(tenant_id,'00000000-0000-0000-0000-000000000000'::uuid), lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_perfis_tenant_cliente_status_regdate ON plantaopro.perfis(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.perfil_permissoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, bloqueado_por_plano boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_perfil_permissoes_status_regdate ON plantaopro.perfil_permissoes(perfil_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.perfil_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, modulo_id uuid NOT NULL, habilitado boolean NOT NULL DEFAULT true, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_perfil_modulos_status_regdate ON plantaopro.perfil_modulos(perfil_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.usuario_perfis(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, perfil_id uuid NOT NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_usuario_perfis_tenant_cliente_status_regdate ON plantaopro.usuario_perfis(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.usuario_permissoes_especiais(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, justificativa text NOT NULL DEFAULT '', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_usuario_permissoes_especiais_tenant_cliente_status_regdate ON plantaopro.usuario_permissoes_especiais(tenant_id,cliente_id,reg_status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.white_label_temas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, nome text NOT NULL, tema_json jsonb NOT NULL DEFAULT '{}'::jsonb, padrao boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_temas_tenant_status_regdate ON plantaopro.white_label_temas(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_assets(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tipo text NOT NULL, url text NOT NULL, content_type text NOT NULL DEFAULT '', tamanho_bytes bigint NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_assets_tenant_status_regdate ON plantaopro.white_label_assets(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_textos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_textos_tenant_status_regdate ON plantaopro.white_label_textos(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_emails(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tipo text NOT NULL, assunto text NOT NULL DEFAULT '', corpo_html text NOT NULL DEFAULT '', remetente text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_emails_tenant_status_regdate ON plantaopro.white_label_emails(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_parametros_mobile(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_parametros_mobile_tenant_status_regdate ON plantaopro.white_label_parametros_mobile(tenant_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.lgpd_consentimentos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, titular_email text NOT NULL DEFAULT '', politica_id uuid NULL, finalidade text NOT NULL, versao_politica text NOT NULL DEFAULT '1.0', aceito boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'SELF_SERVICE', ip_origem text NOT NULL DEFAULT '', user_agent text NOT NULL DEFAULT '', reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_tenant_cliente_status_regdate ON plantaopro.lgpd_consentimentos(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_politicas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, versao text NOT NULL, titulo text NOT NULL, conteudo text NOT NULL, vigente boolean NOT NULL DEFAULT false, publicada_em timestamp NULL, status text NOT NULL DEFAULT 'RASCUNHO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_politicas_tenant_status_regdate ON plantaopro.lgpd_politicas(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_solicitacoes_titular(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, tipo text NOT NULL, descricao text NOT NULL, status text NOT NULL DEFAULT 'ABERTA', prazo_resposta date NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_solicitacoes_titular_tenant_cliente_status_regdate ON plantaopro.lgpd_solicitacoes_titular(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_eventos_privacidade(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, tipo text NOT NULL, descricao text NOT NULL, severidade text NOT NULL DEFAULT 'INFO', reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_eventos_privacidade_tenant_cliente_status_regdate ON plantaopro.lgpd_eventos_privacidade(tenant_id,cliente_id,reg_status,reg_date);

INSERT INTO plantaopro.modulos_sistema(codigo,nome,descricao,ordem)
SELECT x.codigo,x.nome,x.descricao,x.ordem FROM (VALUES
('DASHBOARD','Dashboard','Indicadores e visão geral',10),('MEDICOS','Médicos','Cadastro e gestão de médicos',20),('HOSPITAIS','Hospitais','Unidades e hospitais',30),('ESPECIALIDADES','Especialidades','Especialidades médicas',40),('PLANTOES','Plantões','Criação e publicação de plantões',50),('ESCALAS','Escalas','Escalas e substituições',60),('CONVITES','Convites','Convites aos médicos',70),('AGENDA','Agenda','Agenda operacional',80),('FINANCEIRO','Financeiro','Pagamentos e regras financeiras',90),('PAGAMENTOS','Pagamentos','Pagamentos médicos',100),('NOTIFICACOES','Notificações','Notificações internas e e-mail',110),('COMUNICACAO','Comunicação','Mensagens e comunicados',120),('RELATORIOS','Relatórios','Relatórios operacionais',130),('BI','BI','Inteligência de negócios',140),('OPERACAO_ASSISTIDA','Operação Assistida','Acompanhamento de implantação',150),('SAAS','SaaS','Gestão SaaS',160),('CLIENTES','Clientes','Clientes e tenants',170),('PLANOS','Planos','Planos comerciais',180),('ASSINATURAS','Assinaturas','Assinaturas e uso',190),('FATURAMENTO_SAAS','Faturamento SaaS','Faturas SaaS',200),('CUSTOMER_SUCCESS','Customer Success','Saúde e sucesso do cliente',210),('COMERCIAL','Comercial','Leads e oportunidades',220),('JORNADA_CLIENTE','Jornada Cliente','Jornada e tarefas',230),('LGPD','LGPD','Privacidade e direitos do titular',240),('AJUDA','Ajuda','Central de ajuda',250),('AUDITORIA','Auditoria','Logs e trilhas',260),('OBSERVABILIDADE','Observabilidade','Métricas e logs',270),('CONFIGURACOES','Configurações','Parametrizações',280),('WHITE_LABEL','White Label','Identidade visual por tenant',290)
) AS x(codigo,nome,descricao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.modulos_sistema m WHERE lower(m.codigo)=lower(x.codigo) AND m.reg_status='A');

INSERT INTO plantaopro.acoes_sistema(codigo,nome,descricao,ordem)
SELECT x.codigo,x.nome,x.descricao,x.ordem FROM (VALUES
('VISUALIZAR','Visualizar','Visualizar dados',10),('CRIAR','Criar','Criar registros',20),('EDITAR','Editar','Editar registros',30),('INATIVAR','Inativar','Inativar registros',40),('CANCELAR','Cancelar','Cancelar operações',50),('REATIVAR','Reativar','Reativar registros',60),('APROVAR','Aprovar','Aprovar solicitações',70),('RECUSAR','Recusar','Recusar solicitações',80),('EXPORTAR','Exportar','Exportar dados',90),('CONFIGURAR','Configurar','Configurar recursos',100),('ADMINISTRAR','Administrar','Administração total',110),('VER_DADOS_SENSIVEIS','VerDadosSensiveis','Visualizar dados sensíveis',120)
) AS x(codigo,nome,descricao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.acoes_sistema a WHERE lower(a.codigo)=lower(x.codigo) AND a.reg_status='A');

INSERT INTO plantaopro.permissoes(modulo_id,acao_id,codigo,nome,descricao,sensivel)
SELECT m.id,a.id,m.codigo || '.' || a.codigo,m.nome || ' - ' || a.nome,'Permissão ' || a.nome || ' no módulo ' || m.nome,(a.codigo='VER_DADOS_SENSIVEIS')
FROM plantaopro.modulos_sistema m CROSS JOIN plantaopro.acoes_sistema a
WHERE m.reg_status='A' AND a.reg_status='A'
AND NOT EXISTS (SELECT 1 FROM plantaopro.permissoes p WHERE p.codigo=m.codigo || '.' || a.codigo AND p.reg_status='A');

INSERT INTO plantaopro.perfis(codigo,nome,descricao,base_sistema,customizado)
SELECT x.codigo,x.nome,x.descricao,true,false FROM (VALUES
('ADMINISTRADOR_GLOBAL','Administrador global','Acesso global à plataforma'),('ADMINISTRADOR_CLIENTE','Administrador cliente','Administração do tenant'),('DIRETOR','Diretor','Visão executiva'),('COORDENADOR','Coordenador','Coordenação operacional'),('OPERADOR','Operador','Operação diária'),('FINANCEIRO','Financeiro','Gestão financeira'),('MEDICO','Médico','Área médica'),('HOSPITAL','Hospital','Usuário hospital/unidade'),('SUPORTE','Suporte','Suporte e atendimento'),('AUDITOR','Auditor','Auditoria e observabilidade'),('COMERCIAL','Comercial','Vendas e upgrades'),('CUSTOMER_SUCCESS','Customer Success','Sucesso do cliente')
) AS x(codigo,nome,descricao)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfis p WHERE p.tenant_id IS NULL AND lower(p.codigo)=lower(x.codigo) AND p.reg_status='A');

-- Seeds comerciais sugeridos, preservando planos existentes.
INSERT INTO plantaopro.planos(id,nome,descricao,valor_mensal,limite_medicos,limite_hospitais,limite_plantoes_mes,limite_usuarios,limite_convites_mes,permite_mobile,permite_bi,permite_relatorios_avancados,permite_integracoes,permite_operacao_assistida,permite_suporte_prioritario,permite_white_label,permite_perfis_customizados,publico,destaque,status,reg_status,reg_date,slug,ordem)
SELECT gen_random_uuid(),x.nome,x.descricao,x.valor,x.medicos,x.hospitais,x.plantoes,x.usuarios,x.convites,x.mobile,x.bi,x.rel_avancado,x.integracoes,x.operacao,x.suporte,x.white_label,true,true,x.destaque,'ATIVO','A',now(),x.slug,x.ordem
FROM (VALUES
('Essencial','Para equipes iniciando a gestão digital de plantões',399.00,20,2,100,5,300,false,false,false,false,false,false,false,false,'essencial',10),
('Profissional','Para operações em crescimento com relatórios avançados e mobile',899.00,100,10,500,20,1500,true,false,true,false,true,true,false,true,'profissional',20),
('Enterprise','Para redes com white label, BI, integrações e SLA customizado',1999.00,0,0,0,0,0,true,true,true,true,true,true,true,false,'enterprise',30),
('Custom','Projeto sob medida com contrato personalizado',0.00,0,0,0,0,0,true,true,true,true,true,true,true,false,'custom',40)
) AS x(nome,descricao,valor,medicos,hospitais,plantoes,usuarios,convites,mobile,bi,rel_avancado,integracoes,operacao,suporte,white_label,destaque,slug,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.planos p WHERE lower(coalesce(p.slug,p.nome))=lower(x.slug) AND p.reg_status='A');

-- Complemento da rodada PLANTÃOPRO — MULTI-TENANT SELF-SERVICE WHITE LABEL COM PLANOS COMERCIAIS
ALTER TABLE plantaopro.cadastro_cliente_pagamentos_iniciais ADD COLUMN IF NOT EXISTS descricao text NOT NULL DEFAULT 'Fatura SaaS inicial';
ALTER TABLE plantaopro.assinatura_uso ADD COLUMN IF NOT EXISTS recurso text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.assinatura_uso ADD COLUMN IF NOT EXISTS quantidade int NOT NULL DEFAULT 0;
CREATE INDEX IF NOT EXISTS ix_assinatura_uso_recurso_competencia ON plantaopro.assinatura_uso(cliente_id, recurso, competencia) WHERE reg_status='A';

INSERT INTO plantaopro.plano_faq(id, pergunta, resposta, ordem, status, reg_date, reg_status)
SELECT gen_random_uuid(), v.pergunta, v.resposta, v.ordem, 'ATIVO', now(), 'A'
FROM (VALUES
    ('Posso começar sem implantação manual?', 'Sim. O cadastro self-service provisiona tenant, cliente, assinatura, administrador, LGPD, white label padrão e onboarding.', 10),
    ('White label está disponível em todos os planos?', 'White label é liberado conforme regra comercial do plano e possui fallback visual PlantãoPro.', 20),
    ('Como funcionam upgrade e downgrade?', 'Upgrade registra solicitação comercial; downgrade valida limites atuais para evitar impacto operacional.', 30)
) AS v(pergunta,resposta,ordem)
WHERE NOT EXISTS (
    SELECT 1 FROM plantaopro.plano_faq f WHERE lower(f.pergunta)=lower(v.pergunta) AND f.reg_status='A'
);

-- Origem histórica: database/migrations/2026_plantao_pro_white_label_self_service.sql
-- PlantãoPro SaaS multiempresa, white label e self-service
-- Idempotente: usa schema plantaopro, CREATE/ALTER IF NOT EXISTS e constraints via pg_constraint.
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.tenants(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NULL,
    nome text NOT NULL,
    slug text NOT NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    plano_id uuid NULL,
    criado_por uuid NULL,
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS plano_id uuid NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS subdominio text NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS dominio_customizado text NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS motivo_suspensao text NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenants_slug ON plantaopro.tenants(lower(slug)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenants_cliente_status_regdate ON plantaopro.tenants(cliente_id,status,reg_date);

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_tenants_status' AND conrelid='plantaopro.tenants'::regclass) THEN
        ALTER TABLE plantaopro.tenants ADD CONSTRAINT ck_tenants_status CHECK (status IN ('ATIVO','TRIAL','SUSPENSO','CANCELADO','PENDENTE'));
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS plantaopro.modulos_sistema(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_modulos_sistema_codigo ON plantaopro.modulos_sistema(lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_modulos_sistema_status_regdate ON plantaopro.modulos_sistema(status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.acoes_sistema(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_acoes_sistema_codigo ON plantaopro.acoes_sistema(lower(codigo)) WHERE reg_status='A';
CREATE TABLE IF NOT EXISTS plantaopro.permissoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), modulo_id uuid NOT NULL, acao_id uuid NOT NULL, codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', sensivel boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_permissoes_codigo ON plantaopro.permissoes(lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_permissoes_modulo_status_regdate ON plantaopro.permissoes(modulo_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.tenant_configuracoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', categoria text NOT NULL DEFAULT 'GERAL', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_configuracoes_chave ON plantaopro.tenant_configuracoes(tenant_id, lower(categoria), lower(chave)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_configuracoes_tenant_status_regdate ON plantaopro.tenant_configuracoes(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, modulo_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'PLANO', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_modulos_codigo ON plantaopro.tenant_modulos(tenant_id, lower(codigo_modulo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_modulos_tenant_status_regdate ON plantaopro.tenant_modulos(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_parametros(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, categoria text NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', tipo text NOT NULL DEFAULT 'texto', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_parametros_chave ON plantaopro.tenant_parametros(tenant_id, lower(categoria), lower(chave)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_parametros_tenant_status_regdate ON plantaopro.tenant_parametros(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_dominios(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, dominio text NOT NULL, tipo text NOT NULL DEFAULT 'SUBDOMINIO', verificado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_dominios_dominio ON plantaopro.tenant_dominios(lower(dominio)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_dominios_tenant_status_regdate ON plantaopro.tenant_dominios(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_white_label(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, nome_plataforma text NOT NULL DEFAULT 'PlantãoPro', cliente_nome text NOT NULL DEFAULT '', slogan text NOT NULL DEFAULT '', logo_url text NOT NULL DEFAULT '', logo_reduzida_url text NOT NULL DEFAULT '', favicon_url text NOT NULL DEFAULT '', cor_primaria text NOT NULL DEFAULT '#0d6efd', cor_secundaria text NOT NULL DEFAULT '#20c997', cor_fundo text NOT NULL DEFAULT '#f8fafc', cor_menu text NOT NULL DEFAULT '#0f172a', tema text NOT NULL DEFAULT 'claro', email_remetente text NOT NULL DEFAULT '', texto_boas_vindas text NOT NULL DEFAULT '', texto_rodape text NOT NULL DEFAULT '', login_banner_url text NOT NULL DEFAULT '', mobile_json jsonb NOT NULL DEFAULT '{}'::jsonb, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_white_label_tenant ON plantaopro.tenant_white_label(tenant_id) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_white_label_tenant_status_regdate ON plantaopro.tenant_white_label(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_onboarding(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, cliente_id uuid NULL, status text NOT NULL DEFAULT 'EM_ANDAMENTO', progresso int NOT NULL DEFAULT 0, proxima_acao text NOT NULL DEFAULT '', iniciado_em timestamp NOT NULL DEFAULT now(), finalizado_em timestamp NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_tenant_onboarding_tenant_status_regdate ON plantaopro.tenant_onboarding(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_onboarding_checklist(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), onboarding_id uuid NOT NULL, tenant_id uuid NOT NULL, cliente_id uuid NULL, codigo text NOT NULL, titulo text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, obrigatorio boolean NOT NULL DEFAULT true, concluido boolean NOT NULL DEFAULT false, concluido_em timestamp NULL, link_acao text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'PENDENTE', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_onboarding_checklist_codigo ON plantaopro.tenant_onboarding_checklist(onboarding_id, lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_onboarding_checklist_tenant_status_regdate ON plantaopro.tenant_onboarding_checklist(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_auditoria_configuracoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, auditar_configuracoes boolean NOT NULL DEFAULT true, auditar_permissoes boolean NOT NULL DEFAULT true, auditar_lgpd boolean NOT NULL DEFAULT true, retencao_dias int NOT NULL DEFAULT 1825, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_tenant_auditoria_configuracoes_tenant_status_regdate ON plantaopro.tenant_auditoria_configuracoes(tenant_id,status,reg_date);

ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS slug text NULL;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS destaque boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS publico boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_trial boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS dias_trial int NOT NULL DEFAULT 14;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_white_label boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_perfis_customizados boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS ordem int NOT NULL DEFAULT 0;
CREATE INDEX IF NOT EXISTS ix_planos_status_regdate ON plantaopro.planos(status,reg_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_planos_slug ON plantaopro.planos(lower(slug)) WHERE reg_status='A' AND slug IS NOT NULL;

CREATE TABLE IF NOT EXISTS plantaopro.plano_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, modulo_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_modulos_plano_status_regdate ON plantaopro.plano_modulos(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_precos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, periodicidade text NOT NULL DEFAULT 'MENSAL', valor numeric(12,2) NOT NULL DEFAULT 0, moeda text NOT NULL DEFAULT 'BRL', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_precos_plano_status_regdate ON plantaopro.plano_precos(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_limites(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, codigo text NOT NULL, limite int NULL, ilimitado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_limites_plano_status_regdate ON plantaopro.plano_limites(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_comparativo(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, grupo text NOT NULL, recurso text NOT NULL, valor text NOT NULL DEFAULT '', incluido boolean NOT NULL DEFAULT false, ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_comparativo_plano_status_regdate ON plantaopro.plano_comparativo(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_faq(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NULL, pergunta text NOT NULL, resposta text NOT NULL, ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_faq_plano_status_regdate ON plantaopro.plano_faq(plano_id,status,reg_date);
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS periodicidade text NOT NULL DEFAULT 'MENSAL';
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS renovacao_automatica boolean NOT NULL DEFAULT true;
CREATE INDEX IF NOT EXISTS ix_assinaturas_tenant_cliente_status_regdate ON plantaopro.assinaturas(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, status_anterior text NOT NULL DEFAULT '', status_novo text NOT NULL, motivo text NOT NULL DEFAULT '', usuario_id uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_historico_tenant_cliente_status_regdate ON plantaopro.assinatura_historico(tenant_id,cliente_id,status_novo,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, competencia date NOT NULL, medicos_usados int NOT NULL DEFAULT 0, hospitais_usados int NOT NULL DEFAULT 0, usuarios_usados int NOT NULL DEFAULT 0, plantoes_usados int NOT NULL DEFAULT 0, convites_usados int NOT NULL DEFAULT 0, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_uso_tenant_cliente_status_regdate ON plantaopro.assinatura_uso(tenant_id,cliente_id,competencia,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'PLANO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_modulos_tenant_cliente_status_regdate ON plantaopro.assinatura_modulos(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_bloqueios(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NULL, tenant_id uuid NULL, cliente_id uuid NULL, tipo text NOT NULL, mensagem text NOT NULL, resolvido boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_bloqueios_tenant_cliente_status_regdate ON plantaopro.assinatura_bloqueios(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.upgrade_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, assinatura_id uuid NULL, plano_atual_id uuid NULL, plano_destino_id uuid NOT NULL, motivo text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'SOLICITADO', solicitado_por uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_upgrade_solicitacoes_tenant_cliente_status_regdate ON plantaopro.upgrade_solicitacoes(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.downgrade_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, assinatura_id uuid NULL, plano_atual_id uuid NULL, plano_destino_id uuid NOT NULL, motivo text NOT NULL DEFAULT '', impacto_validado boolean NOT NULL DEFAULT false, bloqueado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'SOLICITADO', solicitado_por uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_downgrade_solicitacoes_tenant_cliente_status_regdate ON plantaopro.downgrade_solicitacoes(tenant_id,cliente_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, plano_id uuid NULL, nome_fantasia text NOT NULL DEFAULT '', razao_social text NOT NULL DEFAULT '', cnpj text NOT NULL DEFAULT '', segmento text NOT NULL DEFAULT '', qtd_medicos int NOT NULL DEFAULT 0, qtd_hospitais int NOT NULL DEFAULT 0, volume_plantoes_mes int NOT NULL DEFAULT 0, cidade text NOT NULL DEFAULT '', uf text NOT NULL DEFAULT '', telefone text NOT NULL DEFAULT '', email_corporativo text NOT NULL DEFAULT '', responsavel_nome text NOT NULL DEFAULT '', responsavel_email text NOT NULL DEFAULT '', responsavel_telefone text NOT NULL DEFAULT '', responsavel_cargo text NOT NULL DEFAULT '', periodicidade text NOT NULL DEFAULT 'MENSAL', aceite_termos boolean NOT NULL DEFAULT false, aceite_privacidade boolean NOT NULL DEFAULT false, consentimento_lgpd boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'INICIADO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_solicitacoes_tenant_cliente_status_regdate ON plantaopro.cadastro_cliente_solicitacoes(tenant_id,cliente_id,status,reg_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_cadastro_cliente_solicitacoes_cnpj_ativo ON plantaopro.cadastro_cliente_solicitacoes(regexp_replace(cnpj,'\D','','g')) WHERE reg_status='A' AND status <> 'CANCELADO';
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_etapas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, etapa text NOT NULL, concluida boolean NOT NULL DEFAULT false, dados jsonb NOT NULL DEFAULT '{}'::jsonb, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_etapas_status_regdate ON plantaopro.cadastro_cliente_etapas(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_validacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, campo text NOT NULL, mensagem text NOT NULL, valido boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_validacoes_status_regdate ON plantaopro.cadastro_cliente_validacoes(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_convites(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, email text NOT NULL, perfil text NOT NULL DEFAULT 'ADMINISTRADOR_CLIENTE', token_hash text NOT NULL DEFAULT '', aceito boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_convites_status_regdate ON plantaopro.cadastro_cliente_convites(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_pagamentos_iniciais(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, cliente_id uuid NULL, assinatura_id uuid NULL, valor numeric(12,2) NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ABERTO', vencimento date NOT NULL DEFAULT (current_date + 7), reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_pagamentos_status_regdate ON plantaopro.cadastro_cliente_pagamentos_iniciais(cliente_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.perfis(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', base_sistema boolean NOT NULL DEFAULT false, customizado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_perfis_tenant_codigo ON plantaopro.perfis(coalesce(tenant_id,'00000000-0000-0000-0000-000000000000'::uuid), lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_perfis_tenant_cliente_status_regdate ON plantaopro.perfis(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.perfil_permissoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, bloqueado_por_plano boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_perfil_permissoes_status_regdate ON plantaopro.perfil_permissoes(perfil_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.perfil_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, modulo_id uuid NOT NULL, habilitado boolean NOT NULL DEFAULT true, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_perfil_modulos_status_regdate ON plantaopro.perfil_modulos(perfil_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.usuario_perfis(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, perfil_id uuid NOT NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_usuario_perfis_tenant_cliente_status_regdate ON plantaopro.usuario_perfis(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.usuario_permissoes_especiais(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, justificativa text NOT NULL DEFAULT '', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_usuario_permissoes_especiais_tenant_cliente_status_regdate ON plantaopro.usuario_permissoes_especiais(tenant_id,cliente_id,reg_status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.white_label_temas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, nome text NOT NULL, tema_json jsonb NOT NULL DEFAULT '{}'::jsonb, padrao boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_temas_tenant_status_regdate ON plantaopro.white_label_temas(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_assets(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tipo text NOT NULL, url text NOT NULL, content_type text NOT NULL DEFAULT '', tamanho_bytes bigint NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_assets_tenant_status_regdate ON plantaopro.white_label_assets(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_textos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_textos_tenant_status_regdate ON plantaopro.white_label_textos(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_emails(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tipo text NOT NULL, assunto text NOT NULL DEFAULT '', corpo_html text NOT NULL DEFAULT '', remetente text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_emails_tenant_status_regdate ON plantaopro.white_label_emails(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_parametros_mobile(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_parametros_mobile_tenant_status_regdate ON plantaopro.white_label_parametros_mobile(tenant_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.lgpd_consentimentos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, titular_email text NOT NULL DEFAULT '', politica_id uuid NULL, finalidade text NOT NULL, versao_politica text NOT NULL DEFAULT '1.0', aceito boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'SELF_SERVICE', ip_origem text NOT NULL DEFAULT '', user_agent text NOT NULL DEFAULT '', reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_tenant_cliente_status_regdate ON plantaopro.lgpd_consentimentos(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_politicas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, versao text NOT NULL, titulo text NOT NULL, conteudo text NOT NULL, vigente boolean NOT NULL DEFAULT false, publicada_em timestamp NULL, status text NOT NULL DEFAULT 'RASCUNHO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_politicas_tenant_status_regdate ON plantaopro.lgpd_politicas(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_solicitacoes_titular(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, tipo text NOT NULL, descricao text NOT NULL, status text NOT NULL DEFAULT 'ABERTA', prazo_resposta date NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_solicitacoes_titular_tenant_cliente_status_regdate ON plantaopro.lgpd_solicitacoes_titular(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_eventos_privacidade(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, tipo text NOT NULL, descricao text NOT NULL, severidade text NOT NULL DEFAULT 'INFO', reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_eventos_privacidade_tenant_cliente_status_regdate ON plantaopro.lgpd_eventos_privacidade(tenant_id,cliente_id,reg_status,reg_date);

INSERT INTO plantaopro.modulos_sistema(codigo,nome,descricao,ordem)
SELECT x.codigo,x.nome,x.descricao,x.ordem FROM (VALUES
('DASHBOARD','Dashboard','Indicadores e visão geral',10),('MEDICOS','Médicos','Cadastro e gestão de médicos',20),('HOSPITAIS','Hospitais','Unidades e hospitais',30),('ESPECIALIDADES','Especialidades','Especialidades médicas',40),('PLANTOES','Plantões','Criação e publicação de plantões',50),('ESCALAS','Escalas','Escalas e substituições',60),('CONVITES','Convites','Convites aos médicos',70),('AGENDA','Agenda','Agenda operacional',80),('FINANCEIRO','Financeiro','Pagamentos e regras financeiras',90),('PAGAMENTOS','Pagamentos','Pagamentos médicos',100),('NOTIFICACOES','Notificações','Notificações internas e e-mail',110),('COMUNICACAO','Comunicação','Mensagens e comunicados',120),('RELATORIOS','Relatórios','Relatórios operacionais',130),('BI','BI','Inteligência de negócios',140),('OPERACAO_ASSISTIDA','Operação Assistida','Acompanhamento de implantação',150),('SAAS','SaaS','Gestão SaaS',160),('CLIENTES','Clientes','Clientes e tenants',170),('PLANOS','Planos','Planos comerciais',180),('ASSINATURAS','Assinaturas','Assinaturas e uso',190),('FATURAMENTO_SAAS','Faturamento SaaS','Faturas SaaS',200),('CUSTOMER_SUCCESS','Customer Success','Saúde e sucesso do cliente',210),('COMERCIAL','Comercial','Leads e oportunidades',220),('JORNADA_CLIENTE','Jornada Cliente','Jornada e tarefas',230),('LGPD','LGPD','Privacidade e direitos do titular',240),('AJUDA','Ajuda','Central de ajuda',250),('AUDITORIA','Auditoria','Logs e trilhas',260),('OBSERVABILIDADE','Observabilidade','Métricas e logs',270),('CONFIGURACOES','Configurações','Parametrizações',280),('WHITE_LABEL','White Label','Identidade visual por tenant',290)
) AS x(codigo,nome,descricao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.modulos_sistema m WHERE lower(m.codigo)=lower(x.codigo) AND m.reg_status='A');

INSERT INTO plantaopro.acoes_sistema(codigo,nome,descricao,ordem)
SELECT x.codigo,x.nome,x.descricao,x.ordem FROM (VALUES
('VISUALIZAR','Visualizar','Visualizar dados',10),('CRIAR','Criar','Criar registros',20),('EDITAR','Editar','Editar registros',30),('INATIVAR','Inativar','Inativar registros',40),('CANCELAR','Cancelar','Cancelar operações',50),('REATIVAR','Reativar','Reativar registros',60),('APROVAR','Aprovar','Aprovar solicitações',70),('RECUSAR','Recusar','Recusar solicitações',80),('EXPORTAR','Exportar','Exportar dados',90),('CONFIGURAR','Configurar','Configurar recursos',100),('ADMINISTRAR','Administrar','Administração total',110),('VER_DADOS_SENSIVEIS','VerDadosSensiveis','Visualizar dados sensíveis',120)
) AS x(codigo,nome,descricao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.acoes_sistema a WHERE lower(a.codigo)=lower(x.codigo) AND a.reg_status='A');

INSERT INTO plantaopro.permissoes(modulo_id,acao_id,codigo,nome,descricao,sensivel)
SELECT m.id,a.id,m.codigo || '.' || a.codigo,m.nome || ' - ' || a.nome,'Permissão ' || a.nome || ' no módulo ' || m.nome,(a.codigo='VER_DADOS_SENSIVEIS')
FROM plantaopro.modulos_sistema m CROSS JOIN plantaopro.acoes_sistema a
WHERE m.reg_status='A' AND a.reg_status='A'
AND NOT EXISTS (SELECT 1 FROM plantaopro.permissoes p WHERE p.codigo=m.codigo || '.' || a.codigo AND p.reg_status='A');

INSERT INTO plantaopro.perfis(codigo,nome,descricao,base_sistema,customizado)
SELECT x.codigo,x.nome,x.descricao,true,false FROM (VALUES
('ADMINISTRADOR_GLOBAL','Administrador global','Acesso global à plataforma'),('ADMINISTRADOR_CLIENTE','Administrador cliente','Administração do tenant'),('DIRETOR','Diretor','Visão executiva'),('COORDENADOR','Coordenador','Coordenação operacional'),('OPERADOR','Operador','Operação diária'),('FINANCEIRO','Financeiro','Gestão financeira'),('MEDICO','Médico','Área médica'),('HOSPITAL','Hospital','Usuário hospital/unidade'),('SUPORTE','Suporte','Suporte e atendimento'),('AUDITOR','Auditor','Auditoria e observabilidade'),('COMERCIAL','Comercial','Vendas e upgrades'),('CUSTOMER_SUCCESS','Customer Success','Sucesso do cliente')
) AS x(codigo,nome,descricao)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfis p WHERE p.tenant_id IS NULL AND lower(p.codigo)=lower(x.codigo) AND p.reg_status='A');

-- Seeds comerciais sugeridos, preservando planos existentes.
INSERT INTO plantaopro.planos(id,nome,descricao,valor_mensal,limite_medicos,limite_hospitais,limite_plantoes_mes,limite_usuarios,limite_convites_mes,permite_mobile,permite_bi,permite_relatorios_avancados,permite_integracoes,permite_operacao_assistida,permite_suporte_prioritario,permite_white_label,permite_perfis_customizados,publico,destaque,status,reg_status,reg_date,slug,ordem)
SELECT gen_random_uuid(),x.nome,x.descricao,x.valor,x.medicos,x.hospitais,x.plantoes,x.usuarios,x.convites,x.mobile,x.bi,x.rel_avancado,x.integracoes,x.operacao,x.suporte,x.white_label,true,true,x.destaque,'ATIVO','A',now(),x.slug,x.ordem
FROM (VALUES
('Essencial','Para equipes iniciando a gestão digital de plantões',399.00,20,2,100,5,300,false,false,false,false,false,false,false,false,'essencial',10),
('Profissional','Para operações em crescimento com relatórios avançados e mobile',899.00,100,10,500,20,1500,true,false,true,false,true,true,false,true,'profissional',20),
('Enterprise','Para redes com white label, BI, integrações e SLA customizado',1999.00,0,0,0,0,0,true,true,true,true,true,true,true,false,'enterprise',30),
('Custom','Projeto sob medida com contrato personalizado',0.00,0,0,0,0,0,true,true,true,true,true,true,true,false,'custom',40)
) AS x(nome,descricao,valor,medicos,hospitais,plantoes,usuarios,convites,mobile,bi,rel_avancado,integracoes,operacao,suporte,white_label,destaque,slug,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.planos p WHERE lower(coalesce(p.slug,p.nome))=lower(x.slug) AND p.reg_status='A');

-- Origem histórica: database/migrations/2026_saude360_base_clinica_minima.sql
set search_path to plantaopro, public;

create schema if not exists plantaopro;
create extension if not exists pgcrypto;
create sequence if not exists plantaopro.seq_painel_senhas start 1;

create table if not exists plantaopro.pacientes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    nome_completo text not null default '',
    nome text not null default '',
    nome_social text not null default '',
    data_nascimento date null,
    sexo text not null default '',
    sexo_genero text not null default '',
    cpf text not null default '',
    cns text not null default '',
    documento text not null default '',
    documento_alternativo text not null default '',
    telefone text not null default '',
    email text not null default '',
    endereco text not null default '',
    responsavel_nome text not null default '',
    observacoes text not null default '',
    consentimento_lgpd boolean not null default false,
    consentimento_lgpd_em timestamp without time zone null,
    consentimento_lgpd_canal text not null default '',
    finalidade_tratamento text not null default 'ASSISTENCIA_SAUDE',
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.pacientes
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists nome_completo text not null default '',
    add column if not exists nome text not null default '',
    add column if not exists nome_social text not null default '',
    add column if not exists data_nascimento date null,
    add column if not exists sexo text not null default '',
    add column if not exists sexo_genero text not null default '',
    add column if not exists cpf text not null default '',
    add column if not exists cns text not null default '',
    add column if not exists documento text not null default '',
    add column if not exists documento_alternativo text not null default '',
    add column if not exists telefone text not null default '',
    add column if not exists email text not null default '',
    add column if not exists endereco text not null default '',
    add column if not exists responsavel_nome text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists consentimento_lgpd boolean not null default false,
    add column if not exists consentimento_lgpd_em timestamp without time zone null,
    add column if not exists consentimento_lgpd_canal text not null default '',
    add column if not exists finalidade_tratamento text not null default 'ASSISTENCIA_SAUDE',
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

update plantaopro.pacientes set nome_completo = nome where nome_completo = '' and coalesce(nome, '') <> '';
update plantaopro.pacientes set nome = nome_completo where nome = '' and coalesce(nome_completo, '') <> '';

create table if not exists plantaopro.agendamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid not null,
    medico_id uuid null,
    hospital_id uuid null,
    unidade_id uuid null,
    especialidade_id uuid null,
    especialidade text not null default '',
    sala_id uuid null,
    tipo text not null default 'CONSULTA',
    data_inicio timestamp without time zone not null,
    data_fim timestamp without time zone not null,
    status text not null default 'AGENDADO',
    observacoes text not null default '',
    motivo_cancelamento text not null default '',
    valor numeric(12,2) not null default 0,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.agendamentos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists hospital_id uuid null,
    add column if not exists unidade_id uuid null,
    add column if not exists especialidade_id uuid null,
    add column if not exists especialidade text not null default '',
    add column if not exists sala_id uuid null,
    add column if not exists tipo text not null default 'CONSULTA',
    add column if not exists data_inicio timestamp without time zone null,
    add column if not exists data_fim timestamp without time zone null,
    add column if not exists status text not null default 'AGENDADO',
    add column if not exists observacoes text not null default '',
    add column if not exists motivo_cancelamento text not null default '',
    add column if not exists valor numeric(12,2) not null default 0,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.painel_chamada_fila (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    painel_id uuid null,
    agendamento_id uuid null,
    paciente_id uuid null,
    triagem_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    senha text not null default '',
    paciente_nome text not null default '',
    setor text not null default '',
    sala text not null default '',
    guiche text not null default '',
    status text not null default 'AGUARDANDO',
    prioridade integer not null default 0,
    chamado_em timestamp without time zone null,
    finalizado_em timestamp without time zone null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.painel_chamada_fila
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists painel_id uuid null,
    add column if not exists agendamento_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists triagem_id uuid null,
    add column if not exists setor_id uuid null,
    add column if not exists sala_id uuid null,
    add column if not exists guiche_id uuid null,
    add column if not exists senha text not null default '',
    add column if not exists paciente_nome text not null default '',
    add column if not exists setor text not null default '',
    add column if not exists sala text not null default '',
    add column if not exists guiche text not null default '',
    add column if not exists status text not null default 'AGUARDANDO',
    add column if not exists prioridade integer not null default 0,
    add column if not exists chamado_em timestamp without time zone null,
    add column if not exists finalizado_em timestamp without time zone null,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.painel_chamada_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    fila_id uuid null,
    agendamento_id uuid null,
    paciente_id uuid null,
    acao text not null default '',
    detalhe text not null default '',
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.painel_chamada_historico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists fila_id uuid null,
    add column if not exists agendamento_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists acao text not null default '',
    add column if not exists detalhe text not null default '',
    add column if not exists detalhes jsonb not null default '{}'::jsonb,
    add column if not exists usuario_id uuid null,
    add column if not exists status text not null default 'REGISTRADO',
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.triagens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid not null,
    agendamento_id uuid null,
    medico_id uuid null,
    profissional_id uuid null,
    enfermeiro_id uuid null,
    queixa_principal text not null default '',
    classificacao_risco text not null default 'NAO_URGENTE',
    alergias_relatadas text not null default '',
    medicamentos_uso text not null default '',
    status text not null default 'AGUARDANDO',
    observacoes text not null default '',
    motivo_cancelamento text not null default '',
    iniciada_em timestamp without time zone null,
    finalizada_em timestamp without time zone null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.triagens
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists agendamento_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists profissional_id uuid null,
    add column if not exists enfermeiro_id uuid null,
    add column if not exists queixa_principal text not null default '',
    add column if not exists classificacao_risco text not null default 'NAO_URGENTE',
    add column if not exists alergias_relatadas text not null default '',
    add column if not exists medicamentos_uso text not null default '',
    add column if not exists status text not null default 'AGUARDANDO',
    add column if not exists observacoes text not null default '',
    add column if not exists motivo_cancelamento text not null default '',
    add column if not exists iniciada_em timestamp without time zone null,
    add column if not exists finalizada_em timestamp without time zone null,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.triagem_sinais_vitais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    triagem_id uuid not null,
    paciente_id uuid not null,
    pressao_arterial text not null default '',
    frequencia_cardiaca numeric(10,2) null,
    frequencia_respiratoria numeric(10,2) null,
    temperatura numeric(10,2) null,
    saturacao numeric(10,2) null,
    peso numeric(10,2) null,
    altura numeric(10,2) null,
    imc numeric(10,2) null,
    glicemia numeric(10,2) null,
    alergias text not null default '',
    medicamentos_uso text not null default '',
    observacoes text not null default '',
    status text not null default 'ATIVO',
    created_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.triagem_sinais_vitais
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists triagem_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists pressao_arterial text not null default '',
    add column if not exists frequencia_cardiaca numeric(10,2) null,
    add column if not exists frequencia_respiratoria numeric(10,2) null,
    add column if not exists temperatura numeric(10,2) null,
    add column if not exists saturacao numeric(10,2) null,
    add column if not exists peso numeric(10,2) null,
    add column if not exists altura numeric(10,2) null,
    add column if not exists imc numeric(10,2) null,
    add column if not exists glicemia numeric(10,2) null,
    add column if not exists alergias text not null default '',
    add column if not exists medicamentos_uso text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.triagem_classificacoes_risco (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo text not null,
    nome text not null,
    cor text not null default '',
    prioridade integer not null default 0,
    tempo_alvo_minutos integer null,
    status text not null default 'ATIVO',
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.triagem_classificacoes_risco
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists codigo text not null default '',
    add column if not exists nome text not null default '',
    add column if not exists cor text not null default '',
    add column if not exists prioridade integer not null default 0,
    add column if not exists tempo_alvo_minutos integer null,
    add column if not exists status text not null default 'ATIVO',
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.triagem_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    triagem_id uuid not null,
    paciente_id uuid null,
    acao text not null default '',
    detalhe text not null default '',
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.triagem_historico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists triagem_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists acao text not null default '',
    add column if not exists detalhe text not null default '',
    add column if not exists detalhes jsonb not null default '{}'::jsonb,
    add column if not exists usuario_id uuid null,
    add column if not exists status text not null default 'REGISTRADO',
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_pacientes_tenant_id on plantaopro.pacientes(tenant_id);
create index if not exists ix_pacientes_cliente_id on plantaopro.pacientes(cliente_id);
create index if not exists ix_pacientes_cpf on plantaopro.pacientes(cpf);
create index if not exists ix_pacientes_nome_completo on plantaopro.pacientes(nome_completo);
create index if not exists ix_pacientes_nome on plantaopro.pacientes(nome);
create index if not exists ix_pacientes_status on plantaopro.pacientes(status);
create index if not exists ix_pacientes_reg_date on plantaopro.pacientes(reg_date desc);
create unique index if not exists ux_pacientes_cliente_cpf on plantaopro.pacientes(cliente_id, cpf) where cpf <> '' and reg_status = 'A';

create index if not exists ix_agendamentos_cliente_id on plantaopro.agendamentos(cliente_id);
create index if not exists ix_agendamentos_paciente_id on plantaopro.agendamentos(paciente_id);
create index if not exists ix_agendamentos_medico_id on plantaopro.agendamentos(medico_id);
create index if not exists ix_agendamentos_hospital_id on plantaopro.agendamentos(hospital_id);
create index if not exists ix_agendamentos_especialidade_id on plantaopro.agendamentos(especialidade_id);
create index if not exists ix_agendamentos_data_inicio on plantaopro.agendamentos(data_inicio);
create index if not exists ix_agendamentos_status on plantaopro.agendamentos(status);
create index if not exists ix_agendamentos_reg_date on plantaopro.agendamentos(reg_date desc);

create index if not exists ix_painel_chamada_fila_cliente_id on plantaopro.painel_chamada_fila(cliente_id);
create index if not exists ix_painel_chamada_fila_agendamento_id on plantaopro.painel_chamada_fila(agendamento_id);
create index if not exists ix_painel_chamada_fila_paciente_id on plantaopro.painel_chamada_fila(paciente_id);
create index if not exists ix_painel_chamada_fila_status on plantaopro.painel_chamada_fila(status);
create index if not exists ix_painel_chamada_fila_prioridade on plantaopro.painel_chamada_fila(prioridade);
create index if not exists ix_painel_chamada_fila_reg_date on plantaopro.painel_chamada_fila(reg_date desc);

create index if not exists ix_painel_chamada_historico_cliente_id on plantaopro.painel_chamada_historico(cliente_id);
create index if not exists ix_painel_chamada_historico_fila_id on plantaopro.painel_chamada_historico(fila_id);
create index if not exists ix_painel_chamada_historico_paciente_id on plantaopro.painel_chamada_historico(paciente_id);
create index if not exists ix_painel_chamada_historico_acao on plantaopro.painel_chamada_historico(acao);
create index if not exists ix_painel_chamada_historico_reg_date on plantaopro.painel_chamada_historico(reg_date desc);

create index if not exists ix_triagens_cliente_id on plantaopro.triagens(cliente_id);
create index if not exists ix_triagens_paciente_id on plantaopro.triagens(paciente_id);
create index if not exists ix_triagens_agendamento_id on plantaopro.triagens(agendamento_id);
create index if not exists ix_triagens_classificacao_risco on plantaopro.triagens(classificacao_risco);
create index if not exists ix_triagens_status on plantaopro.triagens(status);
create index if not exists ix_triagens_reg_date on plantaopro.triagens(reg_date desc);

create index if not exists ix_triagem_sinais_vitais_cliente_id on plantaopro.triagem_sinais_vitais(cliente_id);
create index if not exists ix_triagem_sinais_vitais_triagem_id on plantaopro.triagem_sinais_vitais(triagem_id);
create index if not exists ix_triagem_sinais_vitais_paciente_id on plantaopro.triagem_sinais_vitais(paciente_id);
create index if not exists ix_triagem_sinais_vitais_reg_date on plantaopro.triagem_sinais_vitais(reg_date desc);

create index if not exists ix_triagem_classificacoes_risco_codigo on plantaopro.triagem_classificacoes_risco(codigo);
create index if not exists ix_triagem_classificacoes_risco_prioridade on plantaopro.triagem_classificacoes_risco(prioridade);

create index if not exists ix_triagem_historico_cliente_id on plantaopro.triagem_historico(cliente_id);
create index if not exists ix_triagem_historico_triagem_id on plantaopro.triagem_historico(triagem_id);
create index if not exists ix_triagem_historico_paciente_id on plantaopro.triagem_historico(paciente_id);
create index if not exists ix_triagem_historico_acao on plantaopro.triagem_historico(acao);
create index if not exists ix_triagem_historico_reg_date on plantaopro.triagem_historico(reg_date desc);

do $$
begin
    if not exists (
        select 1 from pg_constraint
        where conname = 'ck_agendamentos_data_fim_maior_inicio'
          and conrelid = 'plantaopro.agendamentos'::regclass
    ) then
        alter table plantaopro.agendamentos
        add constraint ck_agendamentos_data_fim_maior_inicio
        check (data_fim is null or data_inicio is null or data_fim > data_inicio);
    end if;
end $$;

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'EMERGENCIA', 'Emergência', 'VERMELHO', 1, 0
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'EMERGENCIA' and cliente_id is null);

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'MUITO_URGENTE', 'Muito urgente', 'LARANJA', 2, 10
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'MUITO_URGENTE' and cliente_id is null);

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'URGENTE', 'Urgente', 'AMARELO', 3, 60
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'URGENTE' and cliente_id is null);

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'POUCO_URGENTE', 'Pouco urgente', 'VERDE', 4, 120
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'POUCO_URGENTE' and cliente_id is null);

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'NAO_URGENTE', 'Não urgente', 'AZUL', 5, 240
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'NAO_URGENTE' and cliente_id is null);

-- Origem histórica: database/migrations/2026_plantao_pro_saude360_base_clinica.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;
create sequence if not exists plantaopro.seq_painel_senhas start 1;

-- PlantãoPro Saúde 360 — Fase 5.1: base clínica idempotente.
-- Todas as tabelas operacionais usam cliente_id como escopo tenant, reg_status e reg_date.

create table if not exists plantaopro.pacientes (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    nome text not null, nome_social text null, data_nascimento date null, sexo_genero text null,
    cpf text null, cns text null, documento_alternativo text null, telefone text null, email text null,
    endereco text null, responsavel_nome text null, observacoes text null,
    finalidade_tratamento text not null default 'ASSISTENCIA_SAUDE', ver_dados_sensiveis boolean not null default false,
    status text not null default 'ATIVO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);
alter table plantaopro.pacientes add column if not exists nome_social text null;
alter table plantaopro.pacientes add column if not exists sexo_genero text null;
alter table plantaopro.pacientes add column if not exists cns text null;
alter table plantaopro.pacientes add column if not exists documento_alternativo text null;
alter table plantaopro.pacientes add column if not exists endereco text null;
alter table plantaopro.pacientes add column if not exists responsavel_nome text null;
alter table plantaopro.pacientes add column if not exists observacoes text null;
alter table plantaopro.pacientes add column if not exists finalidade_tratamento text not null default 'ASSISTENCIA_SAUDE';
alter table plantaopro.pacientes add column if not exists ver_dados_sensiveis boolean not null default false;
create unique index if not exists ux_pacientes_cliente_cpf_informado on plantaopro.pacientes (cliente_id, regexp_replace(coalesce(cpf,''), '[^0-9]', '', 'g')) where cpf is not null and btrim(cpf) <> '' and reg_status='A';
create index if not exists ix_pacientes_cliente_id on plantaopro.pacientes (cliente_id);
create index if not exists ix_pacientes_tenant_id on plantaopro.pacientes (tenant_id);
create index if not exists ix_pacientes_status on plantaopro.pacientes (status);
create index if not exists ix_pacientes_reg_date on plantaopro.pacientes (reg_date);

create table if not exists plantaopro.paciente_contatos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, nome text null, tipo text not null default 'PRINCIPAL', telefone text null, email text null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.paciente_enderecos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, logradouro text null, numero text null, bairro text null, cidade text null, estado text null, cep text null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.paciente_documentos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, tipo text not null, numero text not null, emissor text null, validade date null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.paciente_historico (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, acao text not null, detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null, status text not null default 'REGISTRADO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.paciente_observacoes (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, tipo text not null default 'ADMINISTRATIVA', observacao text not null, sensivel boolean not null default false, status text not null default 'ATIVA', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.paciente_consentimentos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, finalidade text not null, concedido boolean not null default true, canal text null, ip text null, usuario_id uuid null, status text not null default 'VIGENTE', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');

create table if not exists plantaopro.paineis_chamada (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, unidade_id uuid null, nome text not null, token_publico text not null default encode(gen_random_bytes(24),'hex'), permite_tv_publica boolean not null default false, exibir_nome_completo boolean not null default false, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.painel_chamada_configuracoes (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, painel_id uuid not null, tema text not null default 'WHITE_LABEL', exibir_nome_completo boolean not null default false, exibir_somente_senha boolean not null default true, tempo_exibicao_segundos int not null default 20, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.painel_chamada_setores (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, painel_id uuid null, nome text not null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.painel_chamada_salas (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, setor_id uuid null, nome text not null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.painel_chamada_guiches (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, setor_id uuid null, nome text not null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.painel_chamada_fila (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, painel_id uuid null, paciente_id uuid null, agendamento_id uuid null, triagem_id uuid null, atendimento_id uuid null, setor_id uuid null, sala_id uuid null, guiche_id uuid null, senha text null, paciente_nome text null, status text not null default 'AGUARDANDO', prioridade int not null default 0, chamada_em timestamptz null, created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.painel_chamada_historico (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, fila_id uuid null, painel_id uuid null, paciente_id uuid null, agendamento_id uuid null, acao text not null, detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null, status text not null default 'REGISTRADO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');

create table if not exists plantaopro.agendamentos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, medico_id uuid not null, unidade_id uuid null, sala_id uuid null, especialidade text null, tipo text not null default 'CONSULTA', data_inicio timestamptz not null, data_fim timestamptz not null, convenio_id uuid null, plano_saude_id uuid null, observacoes text null, valor numeric(14,2) not null default 0, status text not null default 'AGENDADO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
alter table plantaopro.agendamentos add column if not exists sala_id uuid null;
alter table plantaopro.agendamentos add column if not exists especialidade text null;
alter table plantaopro.agendamentos add column if not exists convenio_id uuid null;
alter table plantaopro.agendamentos add column if not exists plano_saude_id uuid null;
create table if not exists plantaopro.agendamento_historico (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, agendamento_id uuid not null, acao text not null, detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null, status text not null default 'REGISTRADO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.agendamento_bloqueios (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, medico_id uuid null, unidade_id uuid null, data_inicio timestamptz not null, data_fim timestamptz not null, motivo text not null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.agendamento_cancelamentos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, agendamento_id uuid not null, motivo text not null, usuario_id uuid null, status text not null default 'CANCELADO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.agendamento_checkins (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, agendamento_id uuid not null, paciente_id uuid not null, usuario_id uuid null, observacoes text null, status text not null default 'REALIZADO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.agendamento_tipos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, nome text not null, codigo text not null, duracao_minutos int not null default 30, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.agendamento_status_historico (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, agendamento_id uuid not null, status_anterior text null, status_novo text not null, motivo text null, usuario_id uuid null, created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');

create table if not exists plantaopro.triagens (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, agendamento_id uuid null, enfermeiro_id uuid null, queixa_principal text null, pressao_sistolica numeric(6,2) null, pressao_diastolica numeric(6,2) null, frequencia_cardiaca numeric(6,2) null, frequencia_respiratoria numeric(6,2) null, temperatura numeric(6,2) null, saturacao numeric(6,2) null, peso numeric(8,2) null, altura numeric(5,2) null, imc numeric(8,2) null, glicemia numeric(8,2) null, alergias_relatadas text null, medicamentos_uso text null, classificacao_risco text null, observacoes text null, status text not null default 'AGUARDANDO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
alter table plantaopro.triagens add column if not exists pressao_sistolica numeric(6,2) null;
alter table plantaopro.triagens add column if not exists pressao_diastolica numeric(6,2) null;
alter table plantaopro.triagens add column if not exists medicamentos_uso text null;
create table if not exists plantaopro.triagem_sinais_vitais (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, triagem_id uuid not null, pressao_sistolica numeric(6,2) null, pressao_diastolica numeric(6,2) null, frequencia_cardiaca numeric(6,2) null, frequencia_respiratoria numeric(6,2) null, temperatura numeric(6,2) null, saturacao numeric(6,2) null, peso numeric(8,2) null, altura numeric(5,2) null, imc numeric(8,2) null, glicemia numeric(8,2) null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.triagem_classificacoes_risco (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, codigo text not null, nome text not null, cor_hex text not null, prioridade int not null, tempo_alvo_minutos int null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.triagem_historico (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, triagem_id uuid not null, acao text not null, detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null, status text not null default 'REGISTRADO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.triagem_fila (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null, agendamento_id uuid null, triagem_id uuid null, classificacao_risco text null, prioridade int not null default 0, status text not null default 'AGUARDANDO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.triagem_encaminhamentos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, triagem_id uuid not null, paciente_id uuid not null, agendamento_id uuid null, destino text not null default 'CONSULTA', status text not null default 'ENCAMINHADA', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');

create table if not exists plantaopro.clinica_parametros (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, chave text not null, valor text null, modulo text not null default 'CLINICA', status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_unidades_atendimento (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, nome text not null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_salas_atendimento (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, unidade_id uuid null, nome text not null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_setores_atendimento (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, unidade_id uuid null, nome text not null, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_permissoes_modulos (id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, perfil text not null, modulo text not null, permissao text not null, plano_minimo text not null default 'ESSENCIAL', habilitado boolean not null default true, status text not null default 'ATIVO', created_by uuid null, updated_by uuid null, reg_update timestamptz null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');


-- Compatibilidade com serviços existentes que atualizam updated_at.
do $$
declare t text;
begin
  foreach t in array array['pacientes','paciente_contatos','paciente_enderecos','paciente_documentos','paciente_historico','paciente_observacoes','paciente_consentimentos','paineis_chamada','painel_chamada_configuracoes','painel_chamada_setores','painel_chamada_salas','painel_chamada_guiches','painel_chamada_fila','painel_chamada_historico','agendamentos','agendamento_historico','agendamento_bloqueios','agendamento_cancelamentos','agendamento_checkins','agendamento_tipos','agendamento_status_historico','triagens','triagem_sinais_vitais','triagem_classificacoes_risco','triagem_historico','triagem_fila','triagem_encaminhamentos','clinica_parametros','clinica_unidades_atendimento','clinica_salas_atendimento','clinica_setores_atendimento','clinica_permissoes_modulos'] loop
    execute format('alter table plantaopro.%I add column if not exists created_at timestamptz not null default now()', t);
    execute format('alter table plantaopro.%I add column if not exists updated_at timestamptz null', t);
  end loop;
end $$;

-- Índices padronizados por tenant, vínculos, status e datas.
do $$
declare t text;
begin
  foreach t in array array['paciente_contatos','paciente_enderecos','paciente_documentos','paciente_historico','paciente_observacoes','paciente_consentimentos','paineis_chamada','painel_chamada_configuracoes','painel_chamada_setores','painel_chamada_salas','painel_chamada_guiches','painel_chamada_fila','painel_chamada_historico','agendamentos','agendamento_historico','agendamento_bloqueios','agendamento_cancelamentos','agendamento_checkins','agendamento_tipos','agendamento_status_historico','triagens','triagem_sinais_vitais','triagem_classificacoes_risco','triagem_historico','triagem_fila','triagem_encaminhamentos','clinica_parametros','clinica_unidades_atendimento','clinica_salas_atendimento','clinica_setores_atendimento','clinica_permissoes_modulos'] loop
    execute format('create index if not exists ix_%s_cliente_id on plantaopro.%I (cliente_id)', t, t);
    execute format('create index if not exists ix_%s_status on plantaopro.%I (status)', t, t);
    execute format('create index if not exists ix_%s_reg_date on plantaopro.%I (reg_date)', t, t);
  end loop;
end $$;

create index if not exists ix_painel_fila_paciente_id on plantaopro.painel_chamada_fila (paciente_id);
create index if not exists ix_painel_fila_agendamento_id on plantaopro.painel_chamada_fila (agendamento_id);
create index if not exists ix_agendamentos_paciente_id on plantaopro.agendamentos (paciente_id);
create index if not exists ix_agendamentos_medico_id on plantaopro.agendamentos (medico_id);
create index if not exists ix_agendamentos_periodo on plantaopro.agendamentos (cliente_id, medico_id, data_inicio, data_fim);
create index if not exists ix_triagens_paciente_id on plantaopro.triagens (paciente_id);
create index if not exists ix_triagens_agendamento_id on plantaopro.triagens (agendamento_id);
create index if not exists ix_triagem_fila_agendamento_id on plantaopro.triagem_fila (agendamento_id);

insert into plantaopro.clinica_permissoes_modulos (cliente_id, perfil, modulo, permissao, plano_minimo)
select null, perfil, modulo, permissao, plano from (values
('RECEPCAO','PACIENTES','Visualizar','ESSENCIAL'),('RECEPCAO','PACIENTES','Criar','ESSENCIAL'),('RECEPCAO','PACIENTES','Editar','ESSENCIAL'),('RECEPCAO','AGENDAMENTO','CheckIn','ESSENCIAL'),('RECEPCAO','PAINEL_CHAMADA','Chamar','ESSENCIAL'),
('TRIAGEM','TRIAGEM','Criar','PROFISSIONAL'),('TRIAGEM','TRIAGEM','Iniciar','PROFISSIONAL'),('TRIAGEM','TRIAGEM','Finalizar','PROFISSIONAL'),('ENFERMAGEM','TRIAGEM','VerHistorico','PROFISSIONAL'),
('MEDICO','AGENDAMENTO','Visualizar','ESSENCIAL'),('MEDICO','TRIAGEM','Visualizar','PROFISSIONAL'),('COORDENADOR_CLINICO','TRIAGEM','Visualizar','PROFISSIONAL'),('ADMINISTRADOR_CLINICA','PAINEL_CHAMADA','Configurar','PROFISSIONAL'),('AUDITOR_CLINICO','TRIAGEM','VerHistorico','ENTERPRISE')
) as v(perfil,modulo,permissao,plano)
where not exists (select 1 from plantaopro.clinica_permissoes_modulos p where p.cliente_id is null and p.perfil=v.perfil and p.modulo=v.modulo and p.permissao=v.permissao);

-- Origem histórica: database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.pacientes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    data_nascimento date null,
    cpf text null,
    email text null,
    telefone text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_pacientes_cliente_id on plantaopro.pacientes (cliente_id);
create index if not exists ix_pacientes_status on plantaopro.pacientes (status);
create index if not exists ix_pacientes_reg_date on plantaopro.pacientes (reg_date);
create index if not exists ix_pacientes_reg_status on plantaopro.pacientes (reg_status);

create table if not exists plantaopro.paciente_contatos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    nome text not null,
    tipo text not null default 'CONTATO',
    telefone text null,
    email text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_contatos_cliente_id on plantaopro.paciente_contatos (cliente_id);
create index if not exists ix_paciente_contatos_paciente_id on plantaopro.paciente_contatos (paciente_id);
create index if not exists ix_paciente_contatos_status on plantaopro.paciente_contatos (status);
create index if not exists ix_paciente_contatos_reg_date on plantaopro.paciente_contatos (reg_date);
create index if not exists ix_paciente_contatos_reg_status on plantaopro.paciente_contatos (reg_status);

create table if not exists plantaopro.paciente_enderecos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    logradouro text null,
    numero text null,
    bairro text null,
    cidade text null,
    estado text null,
    cep text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_enderecos_cliente_id on plantaopro.paciente_enderecos (cliente_id);
create index if not exists ix_paciente_enderecos_paciente_id on plantaopro.paciente_enderecos (paciente_id);
create index if not exists ix_paciente_enderecos_status on plantaopro.paciente_enderecos (status);
create index if not exists ix_paciente_enderecos_reg_date on plantaopro.paciente_enderecos (reg_date);
create index if not exists ix_paciente_enderecos_reg_status on plantaopro.paciente_enderecos (reg_status);

create table if not exists plantaopro.paciente_documentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    tipo text not null,
    numero text not null,
    emissor text null,
    validade date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_documentos_cliente_id on plantaopro.paciente_documentos (cliente_id);
create index if not exists ix_paciente_documentos_paciente_id on plantaopro.paciente_documentos (paciente_id);
create index if not exists ix_paciente_documentos_status on plantaopro.paciente_documentos (status);
create index if not exists ix_paciente_documentos_reg_date on plantaopro.paciente_documentos (reg_date);
create index if not exists ix_paciente_documentos_reg_status on plantaopro.paciente_documentos (reg_status);

create table if not exists plantaopro.paciente_convenios (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    convenio_id uuid null,
    plano_saude_id uuid null,
    numero_carteirinha text null,
    principal boolean not null default false,
    validade date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_convenios_cliente_id on plantaopro.paciente_convenios (cliente_id);
create index if not exists ix_paciente_convenios_paciente_id on plantaopro.paciente_convenios (paciente_id);
create index if not exists ix_paciente_convenios_status on plantaopro.paciente_convenios (status);
create index if not exists ix_paciente_convenios_reg_date on plantaopro.paciente_convenios (reg_date);
create index if not exists ix_paciente_convenios_reg_status on plantaopro.paciente_convenios (reg_status);

create table if not exists plantaopro.paciente_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_historico_cliente_id on plantaopro.paciente_historico (cliente_id);
create index if not exists ix_paciente_historico_paciente_id on plantaopro.paciente_historico (paciente_id);
create index if not exists ix_paciente_historico_status on plantaopro.paciente_historico (status);
create index if not exists ix_paciente_historico_reg_date on plantaopro.paciente_historico (reg_date);
create index if not exists ix_paciente_historico_reg_status on plantaopro.paciente_historico (reg_status);

create table if not exists plantaopro.paineis_chamada (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    unidade_id uuid null,
    token_publico text null,
    permite_tv_publica boolean not null default false,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paineis_chamada_cliente_id on plantaopro.paineis_chamada (cliente_id);
create index if not exists ix_paineis_chamada_status on plantaopro.paineis_chamada (status);
create index if not exists ix_paineis_chamada_reg_date on plantaopro.paineis_chamada (reg_date);
create index if not exists ix_paineis_chamada_reg_status on plantaopro.paineis_chamada (reg_status);

create table if not exists plantaopro.painel_chamada_configuracoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    painel_id uuid not null,
    tema text not null default 'WHITE_LABEL',
    exibir_nome_completo boolean not null default false,
    tempo_exibicao_segundos int not null default 20,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_configuracoes_cliente_id on plantaopro.painel_chamada_configuracoes (cliente_id);
create index if not exists ix_painel_chamada_configuracoes_status on plantaopro.painel_chamada_configuracoes (status);
create index if not exists ix_painel_chamada_configuracoes_reg_date on plantaopro.painel_chamada_configuracoes (reg_date);
create index if not exists ix_painel_chamada_configuracoes_reg_status on plantaopro.painel_chamada_configuracoes (reg_status);

create table if not exists plantaopro.painel_chamada_setores (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    painel_id uuid null,
    nome text not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_setores_cliente_id on plantaopro.painel_chamada_setores (cliente_id);
create index if not exists ix_painel_chamada_setores_status on plantaopro.painel_chamada_setores (status);
create index if not exists ix_painel_chamada_setores_reg_date on plantaopro.painel_chamada_setores (reg_date);
create index if not exists ix_painel_chamada_setores_reg_status on plantaopro.painel_chamada_setores (reg_status);

create table if not exists plantaopro.painel_chamada_salas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    setor_id uuid null,
    nome text not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_salas_cliente_id on plantaopro.painel_chamada_salas (cliente_id);
create index if not exists ix_painel_chamada_salas_status on plantaopro.painel_chamada_salas (status);
create index if not exists ix_painel_chamada_salas_reg_date on plantaopro.painel_chamada_salas (reg_date);
create index if not exists ix_painel_chamada_salas_reg_status on plantaopro.painel_chamada_salas (reg_status);

create table if not exists plantaopro.painel_chamada_guiches (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    setor_id uuid null,
    nome text not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_guiches_cliente_id on plantaopro.painel_chamada_guiches (cliente_id);
create index if not exists ix_painel_chamada_guiches_status on plantaopro.painel_chamada_guiches (status);
create index if not exists ix_painel_chamada_guiches_reg_date on plantaopro.painel_chamada_guiches (reg_date);
create index if not exists ix_painel_chamada_guiches_reg_status on plantaopro.painel_chamada_guiches (reg_status);

create table if not exists plantaopro.painel_chamada_fila (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    painel_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    senha text null,
    paciente_nome text null,
    prioridade int not null default 0,
    status text not null default 'AGUARDANDO',
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_fila_cliente_id on plantaopro.painel_chamada_fila (cliente_id);
create index if not exists ix_painel_chamada_fila_paciente_id on plantaopro.painel_chamada_fila (paciente_id);
create index if not exists ix_painel_chamada_fila_agendamento_id on plantaopro.painel_chamada_fila (agendamento_id);
create index if not exists ix_painel_chamada_fila_status on plantaopro.painel_chamada_fila (status);
create index if not exists ix_painel_chamada_fila_reg_date on plantaopro.painel_chamada_fila (reg_date);
create index if not exists ix_painel_chamada_fila_reg_status on plantaopro.painel_chamada_fila (reg_status);

create table if not exists plantaopro.painel_chamada_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    painel_id uuid null,
    fila_id uuid null,
    paciente_id uuid null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_historico_cliente_id on plantaopro.painel_chamada_historico (cliente_id);
create index if not exists ix_painel_chamada_historico_paciente_id on plantaopro.painel_chamada_historico (paciente_id);
create index if not exists ix_painel_chamada_historico_status on plantaopro.painel_chamada_historico (status);
create index if not exists ix_painel_chamada_historico_reg_date on plantaopro.painel_chamada_historico (reg_date);
create index if not exists ix_painel_chamada_historico_reg_status on plantaopro.painel_chamada_historico (reg_status);

create table if not exists plantaopro.agendamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    medico_id uuid null,
    unidade_id uuid null,
    especialidade_id uuid null,
    data_inicio timestamptz not null,
    data_fim timestamptz not null,
    tipo text not null default 'CONSULTA',
    status text not null default 'AGENDADO',
    observacoes text null,
    valor numeric(12,2) not null default 0,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamentos_cliente_id on plantaopro.agendamentos (cliente_id);
create index if not exists ix_agendamentos_paciente_id on plantaopro.agendamentos (paciente_id);
create index if not exists ix_agendamentos_medico_id on plantaopro.agendamentos (medico_id);
create index if not exists ix_agendamentos_status on plantaopro.agendamentos (status);
create index if not exists ix_agendamentos_reg_date on plantaopro.agendamentos (reg_date);
create index if not exists ix_agendamentos_reg_status on plantaopro.agendamentos (reg_status);

create table if not exists plantaopro.agendamento_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    agendamento_id uuid not null,
    acao text not null,
    status_anterior text null,
    status_novo text null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_historico_cliente_id on plantaopro.agendamento_historico (cliente_id);
create index if not exists ix_agendamento_historico_agendamento_id on plantaopro.agendamento_historico (agendamento_id);
create index if not exists ix_agendamento_historico_status on plantaopro.agendamento_historico (status);
create index if not exists ix_agendamento_historico_reg_date on plantaopro.agendamento_historico (reg_date);
create index if not exists ix_agendamento_historico_reg_status on plantaopro.agendamento_historico (reg_status);

create table if not exists plantaopro.agendamento_bloqueios (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    medico_id uuid null,
    unidade_id uuid null,
    data_inicio timestamptz not null,
    data_fim timestamptz not null,
    motivo text not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_bloqueios_cliente_id on plantaopro.agendamento_bloqueios (cliente_id);
create index if not exists ix_agendamento_bloqueios_medico_id on plantaopro.agendamento_bloqueios (medico_id);
create index if not exists ix_agendamento_bloqueios_status on plantaopro.agendamento_bloqueios (status);
create index if not exists ix_agendamento_bloqueios_reg_date on plantaopro.agendamento_bloqueios (reg_date);
create index if not exists ix_agendamento_bloqueios_reg_status on plantaopro.agendamento_bloqueios (reg_status);

create table if not exists plantaopro.agendamento_cancelamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    agendamento_id uuid not null,
    motivo text not null,
    usuario_id uuid null,
    status text not null default 'CANCELADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_cancelamentos_cliente_id on plantaopro.agendamento_cancelamentos (cliente_id);
create index if not exists ix_agendamento_cancelamentos_agendamento_id on plantaopro.agendamento_cancelamentos (agendamento_id);
create index if not exists ix_agendamento_cancelamentos_status on plantaopro.agendamento_cancelamentos (status);
create index if not exists ix_agendamento_cancelamentos_reg_date on plantaopro.agendamento_cancelamentos (reg_date);
create index if not exists ix_agendamento_cancelamentos_reg_status on plantaopro.agendamento_cancelamentos (reg_status);

create table if not exists plantaopro.agendamento_checkins (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    agendamento_id uuid not null,
    paciente_id uuid null,
    realizado_em timestamptz not null default now(),
    origem text not null default 'RECEPCAO',
    status text not null default 'REALIZADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_checkins_cliente_id on plantaopro.agendamento_checkins (cliente_id);
create index if not exists ix_agendamento_checkins_paciente_id on plantaopro.agendamento_checkins (paciente_id);
create index if not exists ix_agendamento_checkins_agendamento_id on plantaopro.agendamento_checkins (agendamento_id);
create index if not exists ix_agendamento_checkins_status on plantaopro.agendamento_checkins (status);
create index if not exists ix_agendamento_checkins_reg_date on plantaopro.agendamento_checkins (reg_date);
create index if not exists ix_agendamento_checkins_reg_status on plantaopro.agendamento_checkins (reg_status);

create table if not exists plantaopro.triagens (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    agendamento_id uuid null,
    enfermeiro_id uuid null,
    classificacao_risco text null,
    queixa_principal text null,
    status text not null default 'ABERTA',
    finalizada_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagens_cliente_id on plantaopro.triagens (cliente_id);
create index if not exists ix_triagens_paciente_id on plantaopro.triagens (paciente_id);
create index if not exists ix_triagens_agendamento_id on plantaopro.triagens (agendamento_id);
create index if not exists ix_triagens_status on plantaopro.triagens (status);
create index if not exists ix_triagens_reg_date on plantaopro.triagens (reg_date);
create index if not exists ix_triagens_reg_status on plantaopro.triagens (reg_status);

create table if not exists plantaopro.triagem_sinais_vitais (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    triagem_id uuid not null,
    pressao_arterial text null,
    frequencia_cardiaca int null,
    frequencia_respiratoria int null,
    temperatura numeric(4,1) null,
    saturacao int null,
    glicemia int null,
    dor_escala int null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_sinais_vitais_cliente_id on plantaopro.triagem_sinais_vitais (cliente_id);
create index if not exists ix_triagem_sinais_vitais_status on plantaopro.triagem_sinais_vitais (status);
create index if not exists ix_triagem_sinais_vitais_reg_date on plantaopro.triagem_sinais_vitais (reg_date);
create index if not exists ix_triagem_sinais_vitais_reg_status on plantaopro.triagem_sinais_vitais (reg_status);

create table if not exists plantaopro.triagem_classificacoes_risco (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    cor text not null,
    prioridade int not null,
    tempo_alvo_minutos int not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_classificacoes_risco_cliente_id on plantaopro.triagem_classificacoes_risco (cliente_id);
create index if not exists ix_triagem_classificacoes_risco_status on plantaopro.triagem_classificacoes_risco (status);
create index if not exists ix_triagem_classificacoes_risco_reg_date on plantaopro.triagem_classificacoes_risco (reg_date);
create index if not exists ix_triagem_classificacoes_risco_reg_status on plantaopro.triagem_classificacoes_risco (reg_status);

create table if not exists plantaopro.triagem_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    triagem_id uuid not null,
    paciente_id uuid null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_historico_cliente_id on plantaopro.triagem_historico (cliente_id);
create index if not exists ix_triagem_historico_paciente_id on plantaopro.triagem_historico (paciente_id);
create index if not exists ix_triagem_historico_status on plantaopro.triagem_historico (status);
create index if not exists ix_triagem_historico_reg_date on plantaopro.triagem_historico (reg_date);
create index if not exists ix_triagem_historico_reg_status on plantaopro.triagem_historico (reg_status);

create table if not exists plantaopro.consultas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    agendamento_id uuid null,
    triagem_id uuid null,
    medico_id uuid not null,
    data_inicio timestamptz null,
    data_fim timestamptz null,
    status text not null default 'ABERTA',
    motivo_cancelamento text null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consultas_cliente_id on plantaopro.consultas (cliente_id);
create index if not exists ix_consultas_paciente_id on plantaopro.consultas (paciente_id);
create index if not exists ix_consultas_medico_id on plantaopro.consultas (medico_id);
create index if not exists ix_consultas_agendamento_id on plantaopro.consultas (agendamento_id);
create index if not exists ix_consultas_status on plantaopro.consultas (status);
create index if not exists ix_consultas_reg_date on plantaopro.consultas (reg_date);
create index if not exists ix_consultas_reg_status on plantaopro.consultas (reg_status);

create table if not exists plantaopro.consulta_anamnese (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    queixa_principal text null,
    historia_doenca_atual text null,
    antecedentes text null,
    alergias text null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_anamnese_cliente_id on plantaopro.consulta_anamnese (cliente_id);
create index if not exists ix_consulta_anamnese_consulta_id on plantaopro.consulta_anamnese (consulta_id);
create index if not exists ix_consulta_anamnese_status on plantaopro.consulta_anamnese (status);
create index if not exists ix_consulta_anamnese_reg_date on plantaopro.consulta_anamnese (reg_date);
create index if not exists ix_consulta_anamnese_reg_status on plantaopro.consulta_anamnese (reg_status);

create table if not exists plantaopro.consulta_exame_fisico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    descricao text null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_exame_fisico_cliente_id on plantaopro.consulta_exame_fisico (cliente_id);
create index if not exists ix_consulta_exame_fisico_consulta_id on plantaopro.consulta_exame_fisico (consulta_id);
create index if not exists ix_consulta_exame_fisico_status on plantaopro.consulta_exame_fisico (status);
create index if not exists ix_consulta_exame_fisico_reg_date on plantaopro.consulta_exame_fisico (reg_date);
create index if not exists ix_consulta_exame_fisico_reg_status on plantaopro.consulta_exame_fisico (reg_status);

create table if not exists plantaopro.consulta_diagnosticos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    cid_id uuid null,
    codigo_cid text null,
    descricao text null,
    principal boolean not null default false,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_diagnosticos_cliente_id on plantaopro.consulta_diagnosticos (cliente_id);
create index if not exists ix_consulta_diagnosticos_consulta_id on plantaopro.consulta_diagnosticos (consulta_id);
create index if not exists ix_consulta_diagnosticos_status on plantaopro.consulta_diagnosticos (status);
create index if not exists ix_consulta_diagnosticos_reg_date on plantaopro.consulta_diagnosticos (reg_date);
create index if not exists ix_consulta_diagnosticos_reg_status on plantaopro.consulta_diagnosticos (reg_status);

create table if not exists plantaopro.consulta_condutas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    descricao text not null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_condutas_cliente_id on plantaopro.consulta_condutas (cliente_id);
create index if not exists ix_consulta_condutas_consulta_id on plantaopro.consulta_condutas (consulta_id);
create index if not exists ix_consulta_condutas_status on plantaopro.consulta_condutas (status);
create index if not exists ix_consulta_condutas_reg_date on plantaopro.consulta_condutas (reg_date);
create index if not exists ix_consulta_condutas_reg_status on plantaopro.consulta_condutas (reg_status);

create table if not exists plantaopro.consulta_encaminhamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    destino text not null,
    motivo text null,
    status text not null default 'ABERTO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_encaminhamentos_cliente_id on plantaopro.consulta_encaminhamentos (cliente_id);
create index if not exists ix_consulta_encaminhamentos_consulta_id on plantaopro.consulta_encaminhamentos (consulta_id);
create index if not exists ix_consulta_encaminhamentos_status on plantaopro.consulta_encaminhamentos (status);
create index if not exists ix_consulta_encaminhamentos_reg_date on plantaopro.consulta_encaminhamentos (reg_date);
create index if not exists ix_consulta_encaminhamentos_reg_status on plantaopro.consulta_encaminhamentos (reg_status);

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_historico_cliente_id on plantaopro.consulta_historico (cliente_id);
create index if not exists ix_consulta_historico_paciente_id on plantaopro.consulta_historico (paciente_id);
create index if not exists ix_consulta_historico_consulta_id on plantaopro.consulta_historico (consulta_id);
create index if not exists ix_consulta_historico_status on plantaopro.consulta_historico (status);
create index if not exists ix_consulta_historico_reg_date on plantaopro.consulta_historico (reg_date);
create index if not exists ix_consulta_historico_reg_status on plantaopro.consulta_historico (reg_status);

create table if not exists plantaopro.cid_tabela (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    codigo text not null,
    descricao text not null,
    grupo text null,
    ativo boolean not null default true,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_tabela_cliente_id on plantaopro.cid_tabela (cliente_id);
create index if not exists ix_cid_tabela_status on plantaopro.cid_tabela (status);
create index if not exists ix_cid_tabela_reg_date on plantaopro.cid_tabela (reg_date);
create index if not exists ix_cid_tabela_reg_status on plantaopro.cid_tabela (reg_status);

create table if not exists plantaopro.cid_favoritos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    cid_id uuid not null,
    medico_id uuid null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_favoritos_cliente_id on plantaopro.cid_favoritos (cliente_id);
create index if not exists ix_cid_favoritos_medico_id on plantaopro.cid_favoritos (medico_id);
create index if not exists ix_cid_favoritos_status on plantaopro.cid_favoritos (status);
create index if not exists ix_cid_favoritos_reg_date on plantaopro.cid_favoritos (reg_date);
create index if not exists ix_cid_favoritos_reg_status on plantaopro.cid_favoritos (reg_status);

create table if not exists plantaopro.cid_uso_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    cid_id uuid null,
    consulta_id uuid null,
    medico_id uuid null,
    paciente_id uuid null,
    acao text not null default 'USO',
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_uso_historico_cliente_id on plantaopro.cid_uso_historico (cliente_id);
create index if not exists ix_cid_uso_historico_paciente_id on plantaopro.cid_uso_historico (paciente_id);
create index if not exists ix_cid_uso_historico_medico_id on plantaopro.cid_uso_historico (medico_id);
create index if not exists ix_cid_uso_historico_consulta_id on plantaopro.cid_uso_historico (consulta_id);
create index if not exists ix_cid_uso_historico_status on plantaopro.cid_uso_historico (status);
create index if not exists ix_cid_uso_historico_reg_date on plantaopro.cid_uso_historico (reg_date);
create index if not exists ix_cid_uso_historico_reg_status on plantaopro.cid_uso_historico (reg_status);

create table if not exists plantaopro.prescricoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    consulta_id uuid null,
    medico_id uuid not null,
    modelo_id uuid null,
    status text not null default 'RASCUNHO',
    finalizada_em timestamptz null,
    cancelada_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricoes_cliente_id on plantaopro.prescricoes (cliente_id);
create index if not exists ix_prescricoes_paciente_id on plantaopro.prescricoes (paciente_id);
create index if not exists ix_prescricoes_medico_id on plantaopro.prescricoes (medico_id);
create index if not exists ix_prescricoes_consulta_id on plantaopro.prescricoes (consulta_id);
create index if not exists ix_prescricoes_status on plantaopro.prescricoes (status);
create index if not exists ix_prescricoes_reg_date on plantaopro.prescricoes (reg_date);
create index if not exists ix_prescricoes_reg_status on plantaopro.prescricoes (reg_status);

create table if not exists plantaopro.prescricao_itens (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    prescricao_id uuid not null,
    medicamento text not null,
    posologia text not null,
    quantidade text null,
    duracao text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_itens_cliente_id on plantaopro.prescricao_itens (cliente_id);
create index if not exists ix_prescricao_itens_status on plantaopro.prescricao_itens (status);
create index if not exists ix_prescricao_itens_reg_date on plantaopro.prescricao_itens (reg_date);
create index if not exists ix_prescricao_itens_reg_status on plantaopro.prescricao_itens (reg_status);

create table if not exists plantaopro.prescricao_modelos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    medico_id uuid null,
    descricao text null,
    itens jsonb not null default '[]'::jsonb,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_modelos_cliente_id on plantaopro.prescricao_modelos (cliente_id);
create index if not exists ix_prescricao_modelos_medico_id on plantaopro.prescricao_modelos (medico_id);
create index if not exists ix_prescricao_modelos_status on plantaopro.prescricao_modelos (status);
create index if not exists ix_prescricao_modelos_reg_date on plantaopro.prescricao_modelos (reg_date);
create index if not exists ix_prescricao_modelos_reg_status on plantaopro.prescricao_modelos (reg_status);

create table if not exists plantaopro.prescricao_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    prescricao_id uuid not null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_historico_cliente_id on plantaopro.prescricao_historico (cliente_id);
create index if not exists ix_prescricao_historico_status on plantaopro.prescricao_historico (status);
create index if not exists ix_prescricao_historico_reg_date on plantaopro.prescricao_historico (reg_date);
create index if not exists ix_prescricao_historico_reg_status on plantaopro.prescricao_historico (reg_status);

create table if not exists plantaopro.prescricao_cancelamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    prescricao_id uuid not null,
    justificativa text not null,
    usuario_id uuid null,
    status text not null default 'CANCELADA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_cancelamentos_cliente_id on plantaopro.prescricao_cancelamentos (cliente_id);
create index if not exists ix_prescricao_cancelamentos_status on plantaopro.prescricao_cancelamentos (status);
create index if not exists ix_prescricao_cancelamentos_reg_date on plantaopro.prescricao_cancelamentos (reg_date);
create index if not exists ix_prescricao_cancelamentos_reg_status on plantaopro.prescricao_cancelamentos (reg_status);

create table if not exists plantaopro.clinica_contas_receber (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    descricao text not null,
    valor numeric(12,2) not null,
    vencimento date null,
    status text not null default 'ABERTA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_contas_receber_cliente_id on plantaopro.clinica_contas_receber (cliente_id);
create index if not exists ix_clinica_contas_receber_paciente_id on plantaopro.clinica_contas_receber (paciente_id);
create index if not exists ix_clinica_contas_receber_agendamento_id on plantaopro.clinica_contas_receber (agendamento_id);
create index if not exists ix_clinica_contas_receber_consulta_id on plantaopro.clinica_contas_receber (consulta_id);
create index if not exists ix_clinica_contas_receber_status on plantaopro.clinica_contas_receber (status);
create index if not exists ix_clinica_contas_receber_reg_date on plantaopro.clinica_contas_receber (reg_date);
create index if not exists ix_clinica_contas_receber_reg_status on plantaopro.clinica_contas_receber (reg_status);

create table if not exists plantaopro.clinica_recebimentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    conta_receber_id uuid null,
    valor numeric(12,2) not null,
    forma_pagamento text not null,
    recebido_em timestamptz not null default now(),
    status text not null default 'RECEBIDO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_recebimentos_cliente_id on plantaopro.clinica_recebimentos (cliente_id);
create index if not exists ix_clinica_recebimentos_status on plantaopro.clinica_recebimentos (status);
create index if not exists ix_clinica_recebimentos_reg_date on plantaopro.clinica_recebimentos (reg_date);
create index if not exists ix_clinica_recebimentos_reg_status on plantaopro.clinica_recebimentos (reg_status);

create table if not exists plantaopro.clinica_caixa (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    data_abertura timestamptz not null default now(),
    data_fechamento timestamptz null,
    saldo_inicial numeric(12,2) not null default 0,
    saldo_final numeric(12,2) null,
    status text not null default 'ABERTO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_caixa_cliente_id on plantaopro.clinica_caixa (cliente_id);
create index if not exists ix_clinica_caixa_status on plantaopro.clinica_caixa (status);
create index if not exists ix_clinica_caixa_reg_date on plantaopro.clinica_caixa (reg_date);
create index if not exists ix_clinica_caixa_reg_status on plantaopro.clinica_caixa (reg_status);

create table if not exists plantaopro.clinica_fechamentos_caixa (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    caixa_id uuid not null,
    valor_informado numeric(12,2) not null,
    divergencia numeric(12,2) not null default 0,
    observacoes text null,
    status text not null default 'FECHADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_fechamentos_caixa_cliente_id on plantaopro.clinica_fechamentos_caixa (cliente_id);
create index if not exists ix_clinica_fechamentos_caixa_status on plantaopro.clinica_fechamentos_caixa (status);
create index if not exists ix_clinica_fechamentos_caixa_reg_date on plantaopro.clinica_fechamentos_caixa (reg_date);
create index if not exists ix_clinica_fechamentos_caixa_reg_status on plantaopro.clinica_fechamentos_caixa (reg_status);

create table if not exists plantaopro.clinica_repasses (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    medico_id uuid null,
    valor numeric(12,2) not null,
    competencia text null,
    status text not null default 'PENDENTE',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_repasses_cliente_id on plantaopro.clinica_repasses (cliente_id);
create index if not exists ix_clinica_repasses_medico_id on plantaopro.clinica_repasses (medico_id);
create index if not exists ix_clinica_repasses_status on plantaopro.clinica_repasses (status);
create index if not exists ix_clinica_repasses_reg_date on plantaopro.clinica_repasses (reg_date);
create index if not exists ix_clinica_repasses_reg_status on plantaopro.clinica_repasses (reg_status);

create table if not exists plantaopro.clinica_lancamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    caixa_id uuid null,
    tipo text not null,
    descricao text not null,
    valor numeric(12,2) not null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_lancamentos_cliente_id on plantaopro.clinica_lancamentos (cliente_id);
create index if not exists ix_clinica_lancamentos_status on plantaopro.clinica_lancamentos (status);
create index if not exists ix_clinica_lancamentos_reg_date on plantaopro.clinica_lancamentos (reg_date);
create index if not exists ix_clinica_lancamentos_reg_status on plantaopro.clinica_lancamentos (reg_status);

create table if not exists plantaopro.clinica_glosas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid null,
    faturamento_id uuid null,
    valor numeric(12,2) not null,
    motivo text not null,
    status text not null default 'ABERTA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_glosas_cliente_id on plantaopro.clinica_glosas (cliente_id);
create index if not exists ix_clinica_glosas_status on plantaopro.clinica_glosas (status);
create index if not exists ix_clinica_glosas_reg_date on plantaopro.clinica_glosas (reg_date);
create index if not exists ix_clinica_glosas_reg_status on plantaopro.clinica_glosas (reg_status);

create table if not exists plantaopro.convenios (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    codigo_ans text null,
    cnpj text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenios_cliente_id on plantaopro.convenios (cliente_id);
create index if not exists ix_convenios_status on plantaopro.convenios (status);
create index if not exists ix_convenios_reg_date on plantaopro.convenios (reg_date);
create index if not exists ix_convenios_reg_status on plantaopro.convenios (reg_status);

create table if not exists plantaopro.convenio_contratos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    numero text null,
    vigencia_inicio date null,
    vigencia_fim date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_contratos_cliente_id on plantaopro.convenio_contratos (cliente_id);
create index if not exists ix_convenio_contratos_status on plantaopro.convenio_contratos (status);
create index if not exists ix_convenio_contratos_reg_date on plantaopro.convenio_contratos (reg_date);
create index if not exists ix_convenio_contratos_reg_status on plantaopro.convenio_contratos (reg_status);

create table if not exists plantaopro.convenio_planos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    nome text not null,
    registro_ans text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_planos_cliente_id on plantaopro.convenio_planos (cliente_id);
create index if not exists ix_convenio_planos_status on plantaopro.convenio_planos (status);
create index if not exists ix_convenio_planos_reg_date on plantaopro.convenio_planos (reg_date);
create index if not exists ix_convenio_planos_reg_status on plantaopro.convenio_planos (reg_status);

create table if not exists plantaopro.convenio_tabelas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    nome text not null,
    vigencia_inicio date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_tabelas_cliente_id on plantaopro.convenio_tabelas (cliente_id);
create index if not exists ix_convenio_tabelas_status on plantaopro.convenio_tabelas (status);
create index if not exists ix_convenio_tabelas_reg_date on plantaopro.convenio_tabelas (reg_date);
create index if not exists ix_convenio_tabelas_reg_status on plantaopro.convenio_tabelas (reg_status);

create table if not exists plantaopro.convenio_procedimentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    tabela_id uuid null,
    codigo text not null,
    descricao text not null,
    valor numeric(12,2) not null default 0,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_procedimentos_cliente_id on plantaopro.convenio_procedimentos (cliente_id);
create index if not exists ix_convenio_procedimentos_status on plantaopro.convenio_procedimentos (status);
create index if not exists ix_convenio_procedimentos_reg_date on plantaopro.convenio_procedimentos (reg_date);
create index if not exists ix_convenio_procedimentos_reg_status on plantaopro.convenio_procedimentos (reg_status);

create table if not exists plantaopro.convenio_autorizacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    procedimento_id uuid null,
    numero_guia text null,
    motivo text null,
    status text not null default 'SOLICITADA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_autorizacoes_cliente_id on plantaopro.convenio_autorizacoes (cliente_id);
create index if not exists ix_convenio_autorizacoes_paciente_id on plantaopro.convenio_autorizacoes (paciente_id);
create index if not exists ix_convenio_autorizacoes_agendamento_id on plantaopro.convenio_autorizacoes (agendamento_id);
create index if not exists ix_convenio_autorizacoes_consulta_id on plantaopro.convenio_autorizacoes (consulta_id);
create index if not exists ix_convenio_autorizacoes_status on plantaopro.convenio_autorizacoes (status);
create index if not exists ix_convenio_autorizacoes_reg_date on plantaopro.convenio_autorizacoes (reg_date);
create index if not exists ix_convenio_autorizacoes_reg_status on plantaopro.convenio_autorizacoes (reg_status);

create table if not exists plantaopro.convenio_glosas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid null,
    autorizacao_id uuid null,
    valor numeric(12,2) not null default 0,
    motivo text not null,
    status text not null default 'ABERTA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_glosas_cliente_id on plantaopro.convenio_glosas (cliente_id);
create index if not exists ix_convenio_glosas_status on plantaopro.convenio_glosas (status);
create index if not exists ix_convenio_glosas_reg_date on plantaopro.convenio_glosas (reg_date);
create index if not exists ix_convenio_glosas_reg_status on plantaopro.convenio_glosas (reg_status);

create table if not exists plantaopro.convenio_faturamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    competencia text not null,
    valor_total numeric(12,2) not null default 0,
    status text not null default 'ABERTO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_faturamentos_cliente_id on plantaopro.convenio_faturamentos (cliente_id);
create index if not exists ix_convenio_faturamentos_status on plantaopro.convenio_faturamentos (status);
create index if not exists ix_convenio_faturamentos_reg_date on plantaopro.convenio_faturamentos (reg_date);
create index if not exists ix_convenio_faturamentos_reg_status on plantaopro.convenio_faturamentos (reg_status);

create table if not exists plantaopro.planos_saude (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    operadora text null,
    registro_ans text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_planos_saude_cliente_id on plantaopro.planos_saude (cliente_id);
create index if not exists ix_planos_saude_status on plantaopro.planos_saude (status);
create index if not exists ix_planos_saude_reg_date on plantaopro.planos_saude (reg_date);
create index if not exists ix_planos_saude_reg_status on plantaopro.planos_saude (reg_status);

create table if not exists plantaopro.plano_saude_coberturas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    plano_saude_id uuid not null,
    descricao text not null,
    limite text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_coberturas_cliente_id on plantaopro.plano_saude_coberturas (cliente_id);
create index if not exists ix_plano_saude_coberturas_status on plantaopro.plano_saude_coberturas (status);
create index if not exists ix_plano_saude_coberturas_reg_date on plantaopro.plano_saude_coberturas (reg_date);
create index if not exists ix_plano_saude_coberturas_reg_status on plantaopro.plano_saude_coberturas (reg_status);

create table if not exists plantaopro.plano_saude_pacientes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    plano_saude_id uuid not null,
    paciente_id uuid not null,
    numero_carteirinha text null,
    principal boolean not null default false,
    validade date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_pacientes_cliente_id on plantaopro.plano_saude_pacientes (cliente_id);
create index if not exists ix_plano_saude_pacientes_paciente_id on plantaopro.plano_saude_pacientes (paciente_id);
create index if not exists ix_plano_saude_pacientes_status on plantaopro.plano_saude_pacientes (status);
create index if not exists ix_plano_saude_pacientes_reg_date on plantaopro.plano_saude_pacientes (reg_date);
create index if not exists ix_plano_saude_pacientes_reg_status on plantaopro.plano_saude_pacientes (reg_status);

create table if not exists plantaopro.plano_saude_autorizacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    plano_saude_id uuid null,
    paciente_id uuid null,
    procedimento text null,
    motivo text null,
    status text not null default 'SOLICITADA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_autorizacoes_cliente_id on plantaopro.plano_saude_autorizacoes (cliente_id);
create index if not exists ix_plano_saude_autorizacoes_paciente_id on plantaopro.plano_saude_autorizacoes (paciente_id);
create index if not exists ix_plano_saude_autorizacoes_status on plantaopro.plano_saude_autorizacoes (status);
create index if not exists ix_plano_saude_autorizacoes_reg_date on plantaopro.plano_saude_autorizacoes (reg_date);
create index if not exists ix_plano_saude_autorizacoes_reg_status on plantaopro.plano_saude_autorizacoes (reg_status);

create table if not exists plantaopro.plano_saude_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    plano_saude_id uuid null,
    paciente_id uuid null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_historico_cliente_id on plantaopro.plano_saude_historico (cliente_id);
create index if not exists ix_plano_saude_historico_paciente_id on plantaopro.plano_saude_historico (paciente_id);
create index if not exists ix_plano_saude_historico_status on plantaopro.plano_saude_historico (status);
create index if not exists ix_plano_saude_historico_reg_date on plantaopro.plano_saude_historico (reg_date);
create index if not exists ix_plano_saude_historico_reg_status on plantaopro.plano_saude_historico (reg_status);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ux_cid_tabela_codigo') then
        alter table plantaopro.cid_tabela add constraint ux_cid_tabela_codigo unique (codigo);
    end if;
end $$;

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_agendamentos_periodo') then
        alter table plantaopro.agendamentos add constraint ck_agendamentos_periodo check (data_fim > data_inicio);
    end if;
end $$;

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_plano_saude_pacientes_validade') then
        alter table plantaopro.plano_saude_pacientes add constraint ck_plano_saude_pacientes_validade check (validade is null or validade >= date '1900-01-01');
    end if;
end $$;

-- Origem histórica: database/migrations/2026_saude360_consultas_base.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.consultas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid not null,
    agendamento_id uuid null,
    triagem_id uuid null,
    medico_id uuid null,
    profissional_id uuid null,
    hospital_id uuid null,
    unidade_id uuid null,
    especialidade_id uuid null,
    data_inicio timestamp without time zone null,
    data_fim timestamp without time zone null,
    status text not null default 'AGUARDANDO',
    tipo text not null default 'CONSULTA',
    motivo_atendimento text not null default '',
    resumo text not null default '',
    observacoes text not null default '',
    motivo_cancelamento text not null default '',
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consultas
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists agendamento_id uuid null,
    add column if not exists triagem_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists profissional_id uuid null,
    add column if not exists hospital_id uuid null,
    add column if not exists unidade_id uuid null,
    add column if not exists especialidade_id uuid null,
    add column if not exists data_inicio timestamp without time zone null,
    add column if not exists data_fim timestamp without time zone null,
    add column if not exists status text not null default 'AGUARDANDO',
    add column if not exists tipo text not null default 'CONSULTA',
    add column if not exists motivo_atendimento text not null default '',
    add column if not exists resumo text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists motivo_cancelamento text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.consultas
    alter column status set default 'AGUARDANDO',
    alter column tipo set default 'CONSULTA',
    alter column motivo_atendimento set default '',
    alter column resumo set default '',
    alter column observacoes set default '',
    alter column motivo_cancelamento set default '',
    alter column reg_date set default now(),
    alter column reg_status set default 'A';

create index if not exists ix_consultas_cliente_id on plantaopro.consultas (cliente_id);
create index if not exists ix_consultas_paciente_id on plantaopro.consultas (paciente_id);
create index if not exists ix_consultas_agendamento_id on plantaopro.consultas (agendamento_id);
create index if not exists ix_consultas_triagem_id on plantaopro.consultas (triagem_id);
create index if not exists ix_consultas_medico_id on plantaopro.consultas (medico_id);
create index if not exists ix_consultas_status on plantaopro.consultas (status);
create index if not exists ix_consultas_data_inicio on plantaopro.consultas (data_inicio);
create index if not exists ix_consultas_reg_date on plantaopro.consultas (reg_date);

create table if not exists plantaopro.consulta_anamnese (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    queixa_principal text not null default '',
    historia_doenca_atual text not null default '',
    antecedentes_pessoais text not null default '',
    antecedentes_familiares text not null default '',
    alergias text not null default '',
    medicamentos_uso text not null default '',
    habitos_vida text not null default '',
    observacoes text not null default '',
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_anamnese
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists queixa_principal text not null default '',
    add column if not exists historia_doenca_atual text not null default '',
    add column if not exists antecedentes_pessoais text not null default '',
    add column if not exists antecedentes_familiares text not null default '',
    add column if not exists alergias text not null default '',
    add column if not exists medicamentos_uso text not null default '',
    add column if not exists habitos_vida text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_anamnese_consulta_id on plantaopro.consulta_anamnese (consulta_id);
create index if not exists ix_consulta_anamnese_paciente_id on plantaopro.consulta_anamnese (paciente_id);
create index if not exists ix_consulta_anamnese_cliente_id on plantaopro.consulta_anamnese (cliente_id);

create table if not exists plantaopro.consulta_exame_fisico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    descricao_geral text not null default '',
    aparelho_cardiovascular text not null default '',
    aparelho_respiratorio text not null default '',
    abdomen text not null default '',
    neurologico text not null default '',
    pele text not null default '',
    observacoes text not null default '',
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_exame_fisico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists descricao_geral text not null default '',
    add column if not exists aparelho_cardiovascular text not null default '',
    add column if not exists aparelho_respiratorio text not null default '',
    add column if not exists abdomen text not null default '',
    add column if not exists neurologico text not null default '',
    add column if not exists pele text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_exame_fisico_consulta_id on plantaopro.consulta_exame_fisico (consulta_id);
create index if not exists ix_consulta_exame_fisico_paciente_id on plantaopro.consulta_exame_fisico (paciente_id);
create index if not exists ix_consulta_exame_fisico_cliente_id on plantaopro.consulta_exame_fisico (cliente_id);

create table if not exists plantaopro.consulta_diagnosticos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    cid_id uuid null,
    cid_codigo text not null default '',
    cid_descricao text not null default '',
    tipo text not null default 'PRINCIPAL',
    hipotese boolean not null default false,
    observacoes text not null default '',
    created_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_diagnosticos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists cid_id uuid null,
    add column if not exists cid_codigo text not null default '',
    add column if not exists cid_descricao text not null default '',
    add column if not exists tipo text not null default 'PRINCIPAL',
    add column if not exists hipotese boolean not null default false,
    add column if not exists observacoes text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_diagnosticos_consulta_id on plantaopro.consulta_diagnosticos (consulta_id);
create index if not exists ix_consulta_diagnosticos_paciente_id on plantaopro.consulta_diagnosticos (paciente_id);
create index if not exists ix_consulta_diagnosticos_cid_codigo on plantaopro.consulta_diagnosticos (cid_codigo);
create index if not exists ix_consulta_diagnosticos_tipo on plantaopro.consulta_diagnosticos (tipo);
create index if not exists ix_consulta_diagnosticos_cliente_id on plantaopro.consulta_diagnosticos (cliente_id);

create table if not exists plantaopro.consulta_condutas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    conduta text not null default '',
    solicitacao_exames text not null default '',
    orientacoes text not null default '',
    retorno_recomendado boolean not null default false,
    retorno_em_dias integer null,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_condutas
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists conduta text not null default '',
    add column if not exists solicitacao_exames text not null default '',
    add column if not exists orientacoes text not null default '',
    add column if not exists retorno_recomendado boolean not null default false,
    add column if not exists retorno_em_dias integer null,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_condutas_consulta_id on plantaopro.consulta_condutas (consulta_id);
create index if not exists ix_consulta_condutas_paciente_id on plantaopro.consulta_condutas (paciente_id);
create index if not exists ix_consulta_condutas_cliente_id on plantaopro.consulta_condutas (cliente_id);

create table if not exists plantaopro.consulta_encaminhamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    especialidade_destino_id uuid null,
    descricao text not null default '',
    prioridade text not null default '',
    observacoes text not null default '',
    created_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_encaminhamentos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists especialidade_destino_id uuid null,
    add column if not exists descricao text not null default '',
    add column if not exists prioridade text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_encaminhamentos_consulta_id on plantaopro.consulta_encaminhamentos (consulta_id);
create index if not exists ix_consulta_encaminhamentos_paciente_id on plantaopro.consulta_encaminhamentos (paciente_id);
create index if not exists ix_consulta_encaminhamentos_cliente_id on plantaopro.consulta_encaminhamentos (cliente_id);

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    acao text not null default '',
    detalhe text not null default '',
    usuario_id uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_historico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists acao text not null default '',
    add column if not exists detalhe text not null default '',
    add column if not exists usuario_id uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_historico_consulta_id on plantaopro.consulta_historico (consulta_id);
create index if not exists ix_consulta_historico_paciente_id on plantaopro.consulta_historico (paciente_id);
create index if not exists ix_consulta_historico_acao on plantaopro.consulta_historico (acao);
create index if not exists ix_consulta_historico_cliente_id on plantaopro.consulta_historico (cliente_id);
create index if not exists ix_consulta_historico_reg_date on plantaopro.consulta_historico (reg_date);

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'ck_consultas_data_fim_maior_inicio'
          and conrelid = 'plantaopro.consultas'::regclass
    ) then
        alter table plantaopro.consultas
            add constraint ck_consultas_data_fim_maior_inicio
            check (data_inicio is null or data_fim is null or data_fim > data_inicio) not valid;
    end if;
end $$;

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'ck_consultas_status_valido'
          and conrelid = 'plantaopro.consultas'::regclass
    ) then
        alter table plantaopro.consultas
            add constraint ck_consultas_status_valido
            check (status in ('AGUARDANDO', 'EM_ATENDIMENTO', 'EM_PREENCHIMENTO', 'FINALIZADA', 'CANCELADA', 'RETORNO_SOLICITADO')) not valid;
    end if;
end $$;

-- Origem histórica: database/migrations/2026_saude360_cid_prescricao.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.cid_tabela (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo text not null,
    descricao text not null,
    categoria text not null default '',
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.cid_tabela
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists codigo text not null default '',
    add column if not exists descricao text not null default '',
    add column if not exists categoria text not null default '',
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists updated_at timestamptz null,
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_update timestamptz null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.cid_favoritos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    cid_id uuid not null,
    medico_id uuid null,
    usuario_id uuid null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.cid_favoritos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists cid_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists usuario_id uuid null,
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists updated_at timestamptz null,
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_update timestamptz null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.cid_uso_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    cid_id uuid not null,
    consulta_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    created_at timestamptz not null default now(),
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.cid_uso_historico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists cid_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists usuario_id uuid null,
    add column if not exists status text not null default 'REGISTRADO',
    add column if not exists created_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.consultas
    add column if not exists anamnese text not null default '',
    add column if not exists exame_fisico text not null default '',
    add column if not exists diagnostico text not null default '',
    add column if not exists cid_id uuid null,
    add column if not exists codigo_cid text not null default '',
    add column if not exists conduta text not null default '',
    add column if not exists orientacoes text not null default '',
    add column if not exists finalizada_em timestamptz null,
    add column if not exists cancelada_em timestamptz null;

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid null,
    acao text not null,
    detalhe text not null default '',
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    created_at timestamptz not null default now(),
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.prescricoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid not null,
    consulta_id uuid not null,
    medico_id uuid not null,
    modelo_id uuid null,
    orientacoes text not null default '',
    status text not null default 'RASCUNHO',
    finalizada_em timestamptz null,
    cancelada_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.prescricoes
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists modelo_id uuid null,
    add column if not exists orientacoes text not null default '',
    add column if not exists status text not null default 'RASCUNHO',
    add column if not exists finalizada_em timestamptz null,
    add column if not exists cancelada_em timestamptz null,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists updated_at timestamptz null,
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_update timestamptz null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.prescricao_itens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    prescricao_id uuid not null,
    medicamento text not null default '',
    posologia text not null default '',
    frequencia text not null default '',
    duracao text not null default '',
    orientacoes text not null default '',
    ordem integer not null default 1,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.prescricao_modelos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    nome text not null,
    medico_id uuid null,
    descricao text not null default '',
    conteudo jsonb not null default '{}'::jsonb,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.prescricao_modelos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists nome text not null default '',
    add column if not exists medico_id uuid null,
    add column if not exists descricao text not null default '',
    add column if not exists conteudo jsonb not null default '{}'::jsonb,
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists updated_at timestamptz null,
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_update timestamptz null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.prescricao_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    prescricao_id uuid not null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    created_at timestamptz not null default now(),
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.prescricao_cancelamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    prescricao_id uuid not null,
    justificativa text not null,
    usuario_id uuid null,
    status text not null default 'CANCELADO',
    created_by uuid null,
    created_at timestamptz not null default now(),
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create index if not exists ix_cid_tabela_codigo on plantaopro.cid_tabela (codigo);
create index if not exists ix_cid_tabela_busca on plantaopro.cid_tabela (codigo, descricao);
create index if not exists ix_cid_favoritos_medico on plantaopro.cid_favoritos (cliente_id, medico_id, cid_id);
create index if not exists ix_cid_uso_historico_cid on plantaopro.cid_uso_historico (cliente_id, cid_id);
create index if not exists ix_consultas_cid_id on plantaopro.consultas (cid_id);
create index if not exists ix_consulta_historico_consulta_id on plantaopro.consulta_historico (consulta_id);
create index if not exists ix_prescricoes_consulta_id on plantaopro.prescricoes (consulta_id);
create index if not exists ix_prescricoes_paciente_id on plantaopro.prescricoes (paciente_id);
create index if not exists ix_prescricoes_medico_id on plantaopro.prescricoes (medico_id);
create index if not exists ix_prescricao_itens_prescricao_id on plantaopro.prescricao_itens (prescricao_id);
create index if not exists ix_prescricao_modelos_medico on plantaopro.prescricao_modelos (cliente_id, medico_id);
create index if not exists ix_prescricao_historico_prescricao_id on plantaopro.prescricao_historico (prescricao_id);
create index if not exists ix_prescricao_cancelamentos_prescricao_id on plantaopro.prescricao_cancelamentos (prescricao_id);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ux_cid_tabela_codigo') then
        alter table plantaopro.cid_tabela add constraint ux_cid_tabela_codigo unique (codigo);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ux_cid_favoritos_medico_cid') then
        alter table plantaopro.cid_favoritos add constraint ux_cid_favoritos_medico_cid unique (cliente_id, medico_id, cid_id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_prescricao_itens_prescricao') then
        alter table plantaopro.prescricao_itens add constraint fk_prescricao_itens_prescricao foreign key (prescricao_id) references plantaopro.prescricoes(id);
    end if;
end $$;

-- Origem histórica: database/migrations/2026_saude360_convenios_planos_saude.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.convenios (
    id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null,
    nome text not null default '', codigo text not null default '', codigo_ans text not null default '',
    cnpj text not null default '', telefone text not null default '', email text not null default '', responsavel text not null default '',
    status text not null default 'ATIVO', observacoes text not null default '', created_by uuid null, updated_by uuid null,
    created_at timestamp without time zone not null default now(), updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A'
);
alter table plantaopro.convenios add column if not exists codigo text not null default '';
alter table plantaopro.convenios add column if not exists codigo_ans text not null default '';
update plantaopro.convenios set codigo = coalesce(nullif(codigo,''), codigo_ans, '') where codigo = '';
create index if not exists ix_convenios_cliente_id on plantaopro.convenios(cliente_id);
create index if not exists ix_convenios_nome on plantaopro.convenios(nome);
create index if not exists ix_convenios_codigo on plantaopro.convenios(codigo);
create index if not exists ix_convenios_cnpj on plantaopro.convenios(cnpj);
create index if not exists ix_convenios_status on plantaopro.convenios(status);
create index if not exists ix_convenios_reg_date on plantaopro.convenios(reg_date);
create unique index if not exists ux_convenios_cliente_codigo_ativo on plantaopro.convenios(cliente_id, upper(codigo)) where reg_status='A' and codigo <> '';

create table if not exists plantaopro.convenio_contratos (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, convenio_id uuid null, numero_contrato text not null default '', vigencia_inicio date null, vigencia_fim date null, status text not null default 'ATIVO', observacoes text not null default '', created_by uuid null, updated_by uuid null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A');
create table if not exists plantaopro.convenio_planos (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, convenio_id uuid null, nome text not null default '', codigo text not null default '', registro_ans text not null default '', tipo_acomodacao text not null default '', coparticipacao_percentual numeric(7,2) not null default 0, status text not null default 'ATIVO', observacoes text not null default '', created_by uuid null, updated_by uuid null, created_at timestamp without time zone not null default now(), updated_at timestamp without time zone null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A');
alter table plantaopro.convenio_planos add column if not exists codigo text not null default '';
alter table plantaopro.convenio_planos add column if not exists registro_ans text not null default '';
update plantaopro.convenio_planos set codigo=coalesce(nullif(codigo,''), registro_ans, '') where codigo='';
create table if not exists plantaopro.convenio_tabelas (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, convenio_id uuid null, nome text not null default '', codigo text not null default '', status text not null default 'ATIVO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.convenio_procedimentos (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, tabela_id uuid null, convenio_id uuid null, codigo text not null default '', descricao text not null default '', valor numeric(14,2) not null default 0, status text not null default 'ATIVO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.convenio_autorizacoes (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, paciente_id uuid null, convenio_id uuid null, plano_saude_id uuid null, agendamento_id uuid null, consulta_id uuid null, numero_guia text not null default '', senha_autorizacao text not null default '', procedimento text not null default '', procedimento_id uuid null, motivo text not null default '', valor_autorizado numeric(14,2) not null default 0, status text not null default 'PENDENTE', motivo_negativa text not null default '', validade date null, created_by uuid null, updated_by uuid null, created_at timestamp without time zone not null default now(), updated_at timestamp without time zone null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A');
create table if not exists plantaopro.convenio_glosas (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, convenio_id uuid null, faturamento_id uuid null, motivo text not null default '', valor numeric(14,2) not null default 0, status text not null default 'ABERTA', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.convenio_glosa_recursos (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, glosa_id uuid null, justificativa text not null default '', status text not null default 'ENVIADO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.convenio_faturamentos (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, convenio_id uuid null, competencia text not null default '', valor_total numeric(14,2) not null default 0, status text not null default 'ABERTO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.convenio_faturamento_itens (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, faturamento_id uuid null, autorizacao_id uuid null, descricao text not null default '', valor numeric(14,2) not null default 0, status text not null default 'ABERTO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.convenio_historico (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, convenio_id uuid null, acao text not null default '', detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null, reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.planos_saude (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, convenio_id uuid null, nome text not null default '', codigo text not null default '', operadora text not null default '', registro_ans text not null default '', tipo text not null default '', status text not null default 'ATIVO', observacoes text not null default '', created_by uuid null, updated_by uuid null, created_at timestamp without time zone not null default now(), updated_at timestamp without time zone null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A');
alter table plantaopro.planos_saude add column if not exists codigo text not null default '';
alter table plantaopro.planos_saude add column if not exists operadora text not null default '';
alter table plantaopro.planos_saude add column if not exists registro_ans text not null default '';
update plantaopro.planos_saude set codigo=coalesce(nullif(codigo,''), registro_ans, '') where codigo='';
create table if not exists plantaopro.plano_saude_coberturas (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, plano_saude_id uuid null, descricao text not null default '', status text not null default 'ATIVO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.plano_saude_pacientes (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, paciente_id uuid null, plano_saude_id uuid null, convenio_id uuid null, numero_carteirinha text not null default '', validade date null, titular_nome text not null default '', titular_documento text not null default '', dependente boolean not null default false, principal boolean not null default false, status text not null default 'ATIVO', observacoes text not null default '', created_by uuid null, updated_by uuid null, created_at timestamp without time zone not null default now(), updated_at timestamp without time zone null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A');
create table if not exists plantaopro.plano_saude_autorizacoes (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, plano_saude_id uuid null, paciente_id uuid null, procedimento text not null default '', status text not null default 'PENDENTE', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.plano_saude_historico (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, plano_saude_id uuid null, acao text not null default '', detalhes jsonb not null default '{}'::jsonb, reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');

create index if not exists ix_convenio_planos_cliente_id on plantaopro.convenio_planos(cliente_id); create index if not exists ix_convenio_planos_convenio_id on plantaopro.convenio_planos(convenio_id); create index if not exists ix_convenio_planos_status on plantaopro.convenio_planos(status); create index if not exists ix_convenio_planos_reg_date on plantaopro.convenio_planos(reg_date);
create index if not exists ix_convenio_autorizacoes_cliente_id on plantaopro.convenio_autorizacoes(cliente_id); create index if not exists ix_convenio_autorizacoes_paciente_id on plantaopro.convenio_autorizacoes(paciente_id); create index if not exists ix_convenio_autorizacoes_agendamento_id on plantaopro.convenio_autorizacoes(agendamento_id); create index if not exists ix_convenio_autorizacoes_consulta_id on plantaopro.convenio_autorizacoes(consulta_id); create index if not exists ix_convenio_autorizacoes_convenio_id on plantaopro.convenio_autorizacoes(convenio_id); create index if not exists ix_convenio_autorizacoes_status on plantaopro.convenio_autorizacoes(status); create index if not exists ix_convenio_autorizacoes_reg_date on plantaopro.convenio_autorizacoes(reg_date);
create index if not exists ix_planos_saude_cliente_id on plantaopro.planos_saude(cliente_id); create index if not exists ix_planos_saude_convenio_id on plantaopro.planos_saude(convenio_id); create index if not exists ix_planos_saude_codigo on plantaopro.planos_saude(codigo); create index if not exists ix_planos_saude_status on plantaopro.planos_saude(status); create index if not exists ix_planos_saude_reg_date on plantaopro.planos_saude(reg_date);
create index if not exists ix_plano_saude_pacientes_cliente_id on plantaopro.plano_saude_pacientes(cliente_id); create index if not exists ix_plano_saude_pacientes_paciente_id on plantaopro.plano_saude_pacientes(paciente_id); create index if not exists ix_plano_saude_pacientes_plano on plantaopro.plano_saude_pacientes(plano_saude_id); create index if not exists ix_plano_saude_pacientes_status on plantaopro.plano_saude_pacientes(status); create index if not exists ix_plano_saude_pacientes_reg_date on plantaopro.plano_saude_pacientes(reg_date);

-- Origem histórica: database/migrations/2026_saude360_financeiro_clinica.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.clinica_contas_receber (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, paciente_id uuid null, agendamento_id uuid null, consulta_id uuid null, convenio_id uuid null, plano_saude_id uuid null, medico_id uuid null, descricao text not null default '', origem text not null default 'MANUAL', tipo_recebimento text not null default 'PARTICULAR', valor_total numeric(14,2) not null default 0, valor_pago numeric(14,2) not null default 0, valor_desconto numeric(14,2) not null default 0, valor_pendente numeric(14,2) not null default 0, vencimento date null, status text not null default 'ABERTO', observacoes text not null default '', created_by uuid null, updated_by uuid null, created_at timestamp without time zone not null default now(), updated_at timestamp without time zone null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_recebimentos (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, conta_receber_id uuid null, paciente_id uuid null, valor numeric(14,2) not null default 0, forma_pagamento text not null default '', data_recebimento timestamp without time zone not null default now(), comprovante text not null default '', observacoes text not null default '', status text not null default 'CONFIRMADO', created_by uuid null, updated_by uuid null, created_at timestamp without time zone not null default now(), updated_at timestamp without time zone null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_caixa (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, unidade_id uuid null, usuario_abertura_id uuid null, usuario_fechamento_id uuid null, data_abertura timestamp without time zone not null default now(), data_fechamento timestamp without time zone null, saldo_inicial numeric(14,2) not null default 0, total_entradas numeric(14,2) not null default 0, total_saidas numeric(14,2) not null default 0, saldo_final numeric(14,2) not null default 0, status text not null default 'ABERTO', observacoes text not null default '', created_by uuid null, updated_by uuid null, created_at timestamp without time zone not null default now(), updated_at timestamp without time zone null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_fechamentos_caixa (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, caixa_id uuid null, saldo_informado numeric(14,2) not null default 0, divergencia numeric(14,2) not null default 0, status text not null default 'FECHADO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_repasses (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, medico_id uuid null, valor numeric(14,2) not null default 0, status text not null default 'ABERTO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_lancamentos (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, caixa_id uuid null, tipo text not null default 'ENTRADA', descricao text not null default '', valor numeric(14,2) not null default 0, status text not null default 'CONFIRMADO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_estornos (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, recebimento_id uuid null, motivo text not null default '', valor numeric(14,2) not null default 0, status text not null default 'ESTORNADO', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_glosas (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, convenio_id uuid null, conta_receber_id uuid null, motivo text not null default '', valor numeric(14,2) not null default 0, status text not null default 'ABERTA', reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_financeiro_historico (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, entidade text not null default '', entidade_id uuid null, acao text not null default '', detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null, reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');

create index if not exists ix_clinica_contas_receber_cliente_id on plantaopro.clinica_contas_receber(cliente_id); create index if not exists ix_clinica_contas_receber_paciente_id on plantaopro.clinica_contas_receber(paciente_id); create index if not exists ix_clinica_contas_receber_consulta_id on plantaopro.clinica_contas_receber(consulta_id); create index if not exists ix_clinica_contas_receber_agendamento_id on plantaopro.clinica_contas_receber(agendamento_id); create index if not exists ix_clinica_contas_receber_convenio_id on plantaopro.clinica_contas_receber(convenio_id); create index if not exists ix_clinica_contas_receber_status on plantaopro.clinica_contas_receber(status); create index if not exists ix_clinica_contas_receber_reg_date on plantaopro.clinica_contas_receber(reg_date);
create index if not exists ix_clinica_recebimentos_cliente_id on plantaopro.clinica_recebimentos(cliente_id); create index if not exists ix_clinica_recebimentos_paciente_id on plantaopro.clinica_recebimentos(paciente_id); create index if not exists ix_clinica_recebimentos_status on plantaopro.clinica_recebimentos(status); create index if not exists ix_clinica_recebimentos_reg_date on plantaopro.clinica_recebimentos(reg_date);
create index if not exists ix_clinica_caixa_cliente_id on plantaopro.clinica_caixa(cliente_id); create index if not exists ix_clinica_caixa_status on plantaopro.clinica_caixa(status); create index if not exists ix_clinica_caixa_reg_date on plantaopro.clinica_caixa(reg_date);

-- Origem histórica: database/migrations/2026_plantao_pro_saas_inteligente.sql
-- ============================================================================
-- PlantãoPro SaaS Inteligente - regras comerciais, limites, faturamento e CS
-- Script incremental idempotente para PostgreSQL
-- ============================================================================

CREATE SCHEMA IF NOT EXISTS plantaopro;

-- Clientes SaaS: complementos comerciais sem quebrar seeds existentes.
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

-- Planos e recursos.
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

CREATE TABLE IF NOT EXISTS plantaopro.plano_recursos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plano_id uuid NOT NULL,
    codigo varchar(80) NOT NULL,
    nome varchar(140) NOT NULL,
    descricao text,
    habilitado boolean NOT NULL DEFAULT true,
    limite int,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz
);

ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS data_trial_fim timestamptz;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS periodicidade varchar(20) NOT NULL DEFAULT 'MENSAL';
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS motivo_suspensao text;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS data_suspensao timestamptz;

CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    assinatura_id uuid NOT NULL,
    cliente_id uuid NOT NULL,
    plano_id_anterior uuid,
    plano_id_novo uuid,
    status_anterior varchar(30),
    status_novo varchar(30),
    acao varchar(60) NOT NULL,
    justificativa text,
    usuario_id uuid,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    assinatura_id uuid,
    cliente_id uuid NOT NULL,
    recurso varchar(80) NOT NULL,
    quantidade int NOT NULL DEFAULT 0,
    competencia date NOT NULL DEFAULT date_trunc('month', now())::date,
    origem varchar(80),
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

-- Faturamento SaaS.
CREATE TABLE IF NOT EXISTS plantaopro.faturas_saas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    assinatura_id uuid NOT NULL,
    competencia date NOT NULL,
    valor numeric(14,2) NOT NULL DEFAULT 0,
    vencimento date NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'ABERTA',
    valor_pago numeric(14,2),
    data_pagamento date,
    forma_pagamento varchar(60),
    motivo_cancelamento text,
    motivo_contestacao text,
    resposta_contestacao text,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.fatura_itens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    fatura_id uuid NOT NULL,
    descricao varchar(180) NOT NULL,
    quantidade numeric(12,2) NOT NULL DEFAULT 1,
    valor_unitario numeric(14,2) NOT NULL DEFAULT 0,
    valor_total numeric(14,2) NOT NULL DEFAULT 0,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.pagamentos_saas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    fatura_id uuid NOT NULL,
    cliente_id uuid,
    valor_pago numeric(14,2) NOT NULL,
    data_pagamento date NOT NULL,
    forma_pagamento varchar(60) NOT NULL,
    observacoes text,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cobranca_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    fatura_id uuid NOT NULL,
    cliente_id uuid NOT NULL,
    tipo varchar(60) NOT NULL,
    canal varchar(40),
    mensagem text,
    sucesso boolean NOT NULL DEFAULT true,
    usuario_id uuid,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

-- Customer Success e saúde do cliente.
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_interacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    usuario_id uuid,
    tipo varchar(40) NOT NULL DEFAULT 'CONTATO',
    risco varchar(30),
    resumo varchar(220) NOT NULL,
    descricao text,
    proxima_acao text,
    data_interacao timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.customer_success_planos_acao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    titulo varchar(180) NOT NULL,
    descricao text,
    responsavel varchar(120),
    status varchar(30) NOT NULL DEFAULT 'ABERTO',
    prioridade varchar(20) NOT NULL DEFAULT 'MEDIA',
    prazo date,
    concluido_em timestamptz,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cliente_saude_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    score int NOT NULL DEFAULT 0,
    classificacao varchar(20) NOT NULL,
    riscos text,
    oportunidades text,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cliente_alertas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tipo varchar(60) NOT NULL,
    severidade varchar(20) NOT NULL DEFAULT 'MEDIA',
    titulo varchar(180) NOT NULL,
    mensagem text NOT NULL,
    resolvido boolean NOT NULL DEFAULT false,
    resolvido_em timestamptz,
    usuario_resolucao_id uuid,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cliente_bloqueios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tipo varchar(80) NOT NULL,
    motivo text NOT NULL,
    origem varchar(80) NOT NULL DEFAULT 'SISTEMA',
    usuario_id uuid,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cliente_limites_uso (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    assinatura_id uuid,
    recurso varchar(80) NOT NULL,
    usado int NOT NULL DEFAULT 0,
    limite int,
    percentual numeric(8,4) NOT NULL DEFAULT 0,
    competencia date NOT NULL DEFAULT date_trunc('month', now())::date,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz
);

-- Índices.
CREATE UNIQUE INDEX IF NOT EXISTS ux_clientes_cnpj_ativo ON plantaopro.clientes(cnpj) WHERE reg_status='A' AND cnpj IS NOT NULL AND cnpj <> '';
CREATE INDEX IF NOT EXISTS ix_clientes_status_reg_date ON plantaopro.clientes(status, reg_date);
CREATE INDEX IF NOT EXISTS ix_clientes_saude_status ON plantaopro.clientes(saude_status, reg_status);
CREATE INDEX IF NOT EXISTS ix_planos_status_ordem ON plantaopro.planos(status, ordem_exibicao, reg_status);
CREATE INDEX IF NOT EXISTS ix_plano_recursos_plano_codigo ON plantaopro.plano_recursos(plano_id, codigo, reg_status);
CREATE INDEX IF NOT EXISTS ix_assinaturas_cliente_status_reg_date ON plantaopro.assinaturas(cliente_id, status, reg_date);
CREATE INDEX IF NOT EXISTS ix_assinaturas_plano_status ON plantaopro.assinaturas(plano_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_assinatura_historico_cliente_reg_date ON plantaopro.assinatura_historico(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_assinatura_uso_cliente_competencia ON plantaopro.assinatura_uso(cliente_id, competencia, recurso);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_cliente_status ON plantaopro.faturas_saas(cliente_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_competencia ON plantaopro.faturas_saas(competencia, reg_status);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_vencimento_status ON plantaopro.faturas_saas(vencimento, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_fatura_itens_fatura ON plantaopro.fatura_itens(fatura_id, reg_status);
CREATE INDEX IF NOT EXISTS ix_pagamentos_saas_fatura ON plantaopro.pagamentos_saas(fatura_id, reg_status);
CREATE INDEX IF NOT EXISTS ix_cobranca_eventos_cliente_reg_date ON plantaopro.cobranca_eventos(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_interacoes_cliente ON plantaopro.customer_success_interacoes(cliente_id, data_interacao DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_planos_cliente_status ON plantaopro.customer_success_planos_acao(cliente_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_cliente_saude_historico_cliente_reg_date ON plantaopro.cliente_saude_historico(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_saude_historico_classificacao ON plantaopro.cliente_saude_historico(classificacao, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_alertas_cliente_status ON plantaopro.cliente_alertas(cliente_id, resolvido, reg_status);
CREATE INDEX IF NOT EXISTS ix_cliente_alertas_tipo_reg_date ON plantaopro.cliente_alertas(tipo, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_bloqueios_cliente_reg_date ON plantaopro.cliente_bloqueios(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_limites_uso_cliente_competencia ON plantaopro.cliente_limites_uso(cliente_id, competencia, recurso);

-- Constraints idempotentes.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_clientes_status_saas' AND conrelid='plantaopro.clientes'::regclass) THEN
        ALTER TABLE plantaopro.clientes ADD CONSTRAINT ck_clientes_status_saas CHECK (status IN ('TESTE','ATIVO','SUSPENSO','CANCELADO','INATIVO'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_planos_status_saas' AND conrelid='plantaopro.planos'::regclass) THEN
        ALTER TABLE plantaopro.planos ADD CONSTRAINT ck_planos_status_saas CHECK (status IN ('ATIVO','INATIVO','ARQUIVADO'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_planos_valores_limites_saas' AND conrelid='plantaopro.planos'::regclass) THEN
        ALTER TABLE plantaopro.planos ADD CONSTRAINT ck_planos_valores_limites_saas CHECK (
            valor_mensal >= 0 AND valor_anual >= 0
            AND (limite_medicos IS NULL OR limite_medicos >= 0)
            AND (limite_hospitais IS NULL OR limite_hospitais >= 0)
            AND (limite_usuarios IS NULL OR limite_usuarios >= 0)
            AND (limite_plantoes_mes IS NULL OR limite_plantoes_mes >= 0)
            AND (limite_convites_mes IS NULL OR limite_convites_mes >= 0)
        );
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_plano_recursos_plano' AND conrelid='plantaopro.plano_recursos'::regclass) THEN
        ALTER TABLE plantaopro.plano_recursos ADD CONSTRAINT fk_plano_recursos_plano FOREIGN KEY (plano_id) REFERENCES plantaopro.planos(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='uq_plano_recursos_codigo' AND conrelid='plantaopro.plano_recursos'::regclass) THEN
        ALTER TABLE plantaopro.plano_recursos ADD CONSTRAINT uq_plano_recursos_codigo UNIQUE (plano_id, codigo);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_assinaturas_status_saas' AND conrelid='plantaopro.assinaturas'::regclass) THEN
        ALTER TABLE plantaopro.assinaturas ADD CONSTRAINT ck_assinaturas_status_saas CHECK (status IN ('TRIAL','ATIVA','SUSPENSA','CANCELADA','VENCIDA'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_assinaturas_periodicidade_saas' AND conrelid='plantaopro.assinaturas'::regclass) THEN
        ALTER TABLE plantaopro.assinaturas ADD CONSTRAINT ck_assinaturas_periodicidade_saas CHECK (periodicidade IN ('MENSAL','ANUAL'));
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_assinaturas_cliente_ativa_trial ON plantaopro.assinaturas(cliente_id) WHERE reg_status='A' AND status IN ('ATIVA','TRIAL');

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_assinatura_historico_assinatura' AND conrelid='plantaopro.assinatura_historico'::regclass) THEN
        ALTER TABLE plantaopro.assinatura_historico ADD CONSTRAINT fk_assinatura_historico_assinatura FOREIGN KEY (assinatura_id) REFERENCES plantaopro.assinaturas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_assinatura_uso_cliente' AND conrelid='plantaopro.assinatura_uso'::regclass) THEN
        ALTER TABLE plantaopro.assinatura_uso ADD CONSTRAINT fk_assinatura_uso_cliente FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_faturas_saas_cliente' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT fk_faturas_saas_cliente FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_faturas_saas_assinatura' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT fk_faturas_saas_assinatura FOREIGN KEY (assinatura_id) REFERENCES plantaopro.assinaturas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='uq_faturas_saas_assinatura_competencia' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT uq_faturas_saas_assinatura_competencia UNIQUE (assinatura_id, competencia);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_faturas_saas_status' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_status CHECK (status IN ('ABERTA','PAGA','VENCIDA','CANCELADA','EM_CONTESTACAO'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_faturas_saas_valores' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_valores CHECK (valor >= 0 AND (valor_pago IS NULL OR valor_pago >= 0));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_fatura_itens_fatura' AND conrelid='plantaopro.fatura_itens'::regclass) THEN
        ALTER TABLE plantaopro.fatura_itens ADD CONSTRAINT fk_fatura_itens_fatura FOREIGN KEY (fatura_id) REFERENCES plantaopro.faturas_saas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_pagamentos_saas_fatura' AND conrelid='plantaopro.pagamentos_saas'::regclass) THEN
        ALTER TABLE plantaopro.pagamentos_saas ADD CONSTRAINT fk_pagamentos_saas_fatura FOREIGN KEY (fatura_id) REFERENCES plantaopro.faturas_saas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_cliente_alertas_cliente' AND conrelid='plantaopro.cliente_alertas'::regclass) THEN
        ALTER TABLE plantaopro.cliente_alertas ADD CONSTRAINT fk_cliente_alertas_cliente FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_cliente_bloqueios_cliente' AND conrelid='plantaopro.cliente_bloqueios'::regclass) THEN
        ALTER TABLE plantaopro.cliente_bloqueios ADD CONSTRAINT fk_cliente_bloqueios_cliente FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;

-- Origem histórica: database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql
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

-- Origem histórica: database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.clientes (
    id uuid primary key default gen_random_uuid(),
    razao_social varchar(200) not null,
    nome_fantasia varchar(200) null,
    cnpj varchar(20) null,
    status varchar(40) not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.clientes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.clientes add column if not exists reg_status char(1) not null default 'A';
alter table plantaopro.clientes add column if not exists reg_date timestamp not null default now();
alter table plantaopro.clientes add column if not exists reg_update timestamp null;

create table if not exists plantaopro.planos (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    descricao text null,
    valor_mensal numeric(12,2) not null default 0,
    limite_medicos int not null default 0,
    limite_hospitais int not null default 0,
    limite_plantoes_mes int not null default 0,
    limite_usuarios int not null default 0,
    limite_convites_mes int not null default 0,
    permite_mobile boolean not null default false,
    permite_bi boolean not null default false,
    permite_relatorios_avancados boolean not null default false,
    permite_integracoes boolean not null default false,
    permite_operacao_assistida boolean not null default false,
    permite_suporte_prioritario boolean not null default false,
    status varchar(40) not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.planos add column if not exists descricao text null;
alter table plantaopro.planos add column if not exists valor_mensal numeric(12,2) not null default 0;
alter table plantaopro.planos add column if not exists limite_medicos int not null default 0;
alter table plantaopro.planos add column if not exists limite_hospitais int not null default 0;
alter table plantaopro.planos add column if not exists limite_plantoes_mes int not null default 0;
alter table plantaopro.planos add column if not exists limite_usuarios int not null default 0;
alter table plantaopro.planos add column if not exists limite_convites_mes int not null default 0;
alter table plantaopro.planos add column if not exists permite_mobile boolean not null default false;
alter table plantaopro.planos add column if not exists permite_bi boolean not null default false;
alter table plantaopro.planos add column if not exists permite_relatorios_avancados boolean not null default false;
alter table plantaopro.planos add column if not exists permite_integracoes boolean not null default false;
alter table plantaopro.planos add column if not exists permite_operacao_assistida boolean not null default false;
alter table plantaopro.planos add column if not exists permite_suporte_prioritario boolean not null default false;
alter table plantaopro.planos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.planos add column if not exists reg_status char(1) not null default 'A';
alter table plantaopro.planos add column if not exists reg_date timestamp not null default now();
alter table plantaopro.planos add column if not exists reg_update timestamp null;

create table if not exists plantaopro.plano_recursos (
    id uuid primary key default gen_random_uuid(),
    plano_id uuid not null,
    recurso varchar(80) not null,
    habilitado boolean not null default true,
    limite int null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.assinaturas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    plano_id uuid not null,
    status varchar(40) not null default 'ATIVA',
    data_inicio date not null default current_date,
    data_fim date null,
    data_trial_fim date null,
    valor_contratado numeric(12,2) not null default 0,
    dia_vencimento int not null default 10,
    periodicidade varchar(30) not null default 'MENSAL',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.assinaturas add column if not exists data_trial_fim date null;
alter table plantaopro.assinaturas add column if not exists valor_contratado numeric(12,2) not null default 0;
alter table plantaopro.assinaturas add column if not exists dia_vencimento int not null default 10;
alter table plantaopro.assinaturas add column if not exists periodicidade varchar(30) not null default 'MENSAL';
alter table plantaopro.assinaturas add column if not exists reg_update timestamp null;

create table if not exists plantaopro.assinatura_historico (
    id uuid primary key default gen_random_uuid(),
    assinatura_id uuid null,
    cliente_id uuid not null,
    plano_id uuid null,
    status_anterior varchar(40) null,
    status_novo varchar(40) not null,
    motivo text null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.assinatura_uso (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    assinatura_id uuid null,
    recurso varchar(80) not null,
    quantidade int not null default 0,
    competencia date not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.faturas_saas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    assinatura_id uuid null,
    competencia date not null,
    vencimento date not null,
    valor_total numeric(12,2) not null default 0,
    status varchar(40) not null default 'ABERTA',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.fatura_itens (
    id uuid primary key default gen_random_uuid(),
    fatura_id uuid not null,
    descricao varchar(220) not null,
    quantidade int not null default 1,
    valor_unitario numeric(12,2) not null default 0,
    valor_total numeric(12,2) not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.pagamentos_saas (
    id uuid primary key default gen_random_uuid(),
    fatura_id uuid not null,
    cliente_id uuid not null,
    valor_pago numeric(12,2) not null default 0,
    data_pagamento timestamp null,
    metodo varchar(60) null,
    status varchar(40) not null default 'PENDENTE',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.cobranca_eventos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    fatura_id uuid null,
    tipo varchar(80) not null,
    mensagem text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.cliente_bloqueios (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    motivo text not null,
    origem varchar(80) null,
    resolvido boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.cliente_bloqueios add column if not exists resolvido boolean not null default false;

create table if not exists plantaopro.cliente_alertas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    severidade varchar(20) not null default 'MEDIA',
    titulo varchar(160) not null,
    mensagem text not null,
    resolvido boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.cliente_limites_uso (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    recurso varchar(80) not null,
    limite int not null default 0,
    utilizado int not null default 0,
    competencia date not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.cliente_saude_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    score int not null,
    classificacao varchar(30) not null,
    riscos text null,
    oportunidades text null,
    acoes text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.jornada_cliente (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    etapa varchar(60) not null default 'LEAD_CADASTRADO',
    responsavel varchar(160) null,
    proxima_acao text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.jornada_cliente add column if not exists proxima_acao text null;
alter table plantaopro.jornada_cliente add column if not exists reg_update timestamp null;

create table if not exists plantaopro.jornada_cliente_eventos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    resumo text not null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.jornada_cliente_tarefas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    titulo varchar(220) not null,
    responsavel varchar(160) null,
    status varchar(40) not null default 'PENDENTE',
    vencimento timestamp null,
    tipo varchar(80) null,
    concluida_em timestamp null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.jornada_cliente_tarefas add column if not exists tipo varchar(80) null;
alter table plantaopro.jornada_cliente_tarefas add column if not exists concluida_em timestamp null;
alter table plantaopro.jornada_cliente_tarefas add column if not exists reg_update timestamp null;

create table if not exists plantaopro.jornada_cliente_observacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    observacao text not null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.jornada_cliente_responsaveis (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    usuario_id uuid null,
    nome varchar(160) not null,
    papel varchar(80) not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_leads (
    id uuid primary key default gen_random_uuid(),
    nome varchar(180) not null,
    email varchar(180) not null,
    telefone varchar(60) null,
    empresa varchar(180) null,
    status varchar(40) not null default 'NOVO',
    plano_recomendado varchar(80) null,
    medicos_desejados int not null default 0,
    hospitais_desejados int not null default 0,
    plantoes_mes int not null default 0,
    precisa_mobile boolean not null default false,
    precisa_bi boolean not null default false,
    suporte_prioritario boolean not null default false,
    operacao_assistida boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.comercial_leads add column if not exists medicos_desejados int not null default 0;
alter table plantaopro.comercial_leads add column if not exists hospitais_desejados int not null default 0;
alter table plantaopro.comercial_leads add column if not exists plantoes_mes int not null default 0;
alter table plantaopro.comercial_leads add column if not exists precisa_mobile boolean not null default false;
alter table plantaopro.comercial_leads add column if not exists precisa_bi boolean not null default false;
alter table plantaopro.comercial_leads add column if not exists suporte_prioritario boolean not null default false;
alter table plantaopro.comercial_leads add column if not exists operacao_assistida boolean not null default false;

create table if not exists plantaopro.comercial_oportunidades (
    id uuid primary key default gen_random_uuid(),
    lead_id uuid null,
    cliente_id uuid null,
    nome varchar(180) not null,
    etapa varchar(60) not null default 'ABERTA',
    valor_estimado numeric(12,2) not null default 0,
    plano_recomendado varchar(80) null,
    motivo_perda text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.comercial_oportunidades add column if not exists cliente_id uuid null;

create table if not exists plantaopro.comercial_propostas (
    id uuid primary key default gen_random_uuid(),
    oportunidade_id uuid not null,
    numero varchar(60) not null,
    valor_total numeric(12,2) not null default 0,
    desconto_percentual numeric(5,2) not null default 0,
    validade date not null,
    status varchar(40) not null default 'RASCUNHO',
    enviada_em timestamp null,
    aprovada_em timestamp null,
    recusada_em timestamp null,
    motivo_recusa text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.comercial_proposta_itens (
    id uuid primary key default gen_random_uuid(),
    proposta_id uuid not null,
    descricao varchar(220) not null,
    quantidade int not null default 1,
    valor_unitario numeric(12,2) not null default 0,
    valor_total numeric(12,2) not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_interacoes (
    id uuid primary key default gen_random_uuid(),
    lead_id uuid null,
    oportunidade_id uuid null,
    tipo varchar(80) not null,
    resumo text not null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_motivos_perda (
    id uuid primary key default gen_random_uuid(),
    motivo varchar(160) not null,
    ativo boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_regras_desconto (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null,
    percentual_maximo numeric(5,2) not null,
    exige_aprovacao boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.customer_success_interacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    resumo text not null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.customer_success_planos_acao (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    titulo varchar(180) not null,
    objetivo text not null,
    status varchar(40) not null default 'ABERTO',
    responsavel varchar(160) null,
    vencimento date null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.customer_success_riscos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    severidade varchar(20) not null,
    descricao text not null,
    status varchar(40) not null default 'ABERTO',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.customer_success_tarefas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    titulo varchar(180) not null,
    status varchar(40) not null default 'PENDENTE',
    responsavel varchar(160) null,
    vencimento date null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.lgpd_politicas (
    id uuid primary key default gen_random_uuid(),
    versao varchar(30) not null,
    titulo varchar(200) not null,
    conteudo text not null,
    publicada boolean not null default false,
    vigente_desde timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.lgpd_bases_legais (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    descricao text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.lgpd_consentimentos (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    finalidade varchar(160) not null,
    base_legal varchar(120) not null,
    versao_politica varchar(30) not null,
    consentido boolean not null default true,
    ip varchar(80) null,
    user_agent text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.lgpd_consentimentos add column if not exists cliente_id uuid null;

create table if not exists plantaopro.lgpd_solicitacoes_titular (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    tipo varchar(40) not null,
    status varchar(40) not null default 'ABERTA',
    descricao text not null,
    resposta text null,
    respondida_por uuid null,
    respondida_em timestamp null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.lgpd_eventos_privacidade (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    acao varchar(120) not null,
    detalhes text null,
    ip varchar(80) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

alter table plantaopro.lgpd_eventos_privacidade add column if not exists cliente_id uuid null;

create table if not exists plantaopro.lgpd_retencao_dados (
    id uuid primary key default gen_random_uuid(),
    categoria varchar(120) not null,
    base_legal varchar(120) not null,
    prazo varchar(120) not null,
    regra text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.lgpd_exportacoes_dados (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    status varchar(40) not null,
    ip varchar(80) null,
    arquivo_url text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.lgpd_anonimizacoes (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid not null,
    motivo text not null,
    status varchar(40) not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.ajuda_topicos (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null default 'TODOS',
    titulo varchar(180) not null,
    descricao text null,
    ordem int not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.ajuda_artigos (
    id uuid primary key default gen_random_uuid(),
    topico_id uuid null,
    perfil varchar(80) not null default 'TODOS',
    titulo varchar(220) not null,
    conteudo text not null,
    link_acao text null,
    ordem int not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.ajuda_passos (
    id uuid primary key default gen_random_uuid(),
    artigo_id uuid not null,
    ordem int not null,
    titulo varchar(180) not null,
    descricao text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.ajuda_checklists (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null,
    titulo varchar(180) not null,
    concluido boolean not null default false,
    ordem int not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.ajuda_feedbacks (
    id uuid primary key default gen_random_uuid(),
    artigo_id uuid not null,
    usuario_id uuid null,
    util boolean not null,
    comentario text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.eventos_sistema (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    tipo varchar(120) not null,
    entidade varchar(120) null,
    entidade_id uuid null,
    mensagem text null,
    correlation_id varchar(120) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.logs_operacionais (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    perfil varchar(80) null,
    acao varchar(120) not null,
    entidade varchar(120) null,
    entidade_id uuid null,
    ip varchar(80) null,
    user_agent text null,
    sucesso boolean not null default true,
    mensagem text null,
    dados_antes text null,
    dados_depois text null,
    correlation_id varchar(120) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.auditoria_eventos (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    perfil varchar(80) null,
    acao varchar(120) not null,
    entidade varchar(120) not null,
    entidade_id uuid null,
    ip varchar(80) null,
    user_agent text null,
    sucesso boolean not null default true,
    mensagem text null,
    dados_antes text null,
    dados_depois text null,
    correlation_id varchar(120) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.auditoria_lgpd_eventos (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    acao varchar(120) not null,
    finalidade varchar(160) null,
    base_legal varchar(120) null,
    ip varchar(80) null,
    sucesso boolean not null default true,
    mensagem text null,
    correlation_id varchar(120) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create index if not exists ix_clientes_status on plantaopro.clientes(status);
create index if not exists ix_clientes_reg_date on plantaopro.clientes(reg_date);
create index if not exists ix_planos_status on plantaopro.planos(status);
create index if not exists ix_assinaturas_cliente_status on plantaopro.assinaturas(cliente_id, status);
create index if not exists ix_assinaturas_reg_date on plantaopro.assinaturas(reg_date);
create index if not exists ix_assinatura_uso_cliente_competencia on plantaopro.assinatura_uso(cliente_id, competencia);
create index if not exists ix_faturas_saas_cliente_status on plantaopro.faturas_saas(cliente_id, status);
create index if not exists ix_faturas_saas_vencimento on plantaopro.faturas_saas(vencimento);
create index if not exists ix_pagamentos_saas_cliente_status on plantaopro.pagamentos_saas(cliente_id, status);
create index if not exists ix_cobranca_eventos_cliente_tipo on plantaopro.cobranca_eventos(cliente_id, tipo);
create index if not exists ix_cliente_bloqueios_cliente_tipo on plantaopro.cliente_bloqueios(cliente_id, tipo);
create index if not exists ix_cliente_alertas_cliente_tipo on plantaopro.cliente_alertas(cliente_id, tipo);
create index if not exists ix_cliente_limites_uso_cliente on plantaopro.cliente_limites_uso(cliente_id, recurso, competencia);
create index if not exists ix_cliente_saude_historico_cliente on plantaopro.cliente_saude_historico(cliente_id, reg_date);
create unique index if not exists ux_jornada_cliente_cliente on plantaopro.jornada_cliente(cliente_id);
create index if not exists ix_jornada_cliente_etapa on plantaopro.jornada_cliente(etapa);
create index if not exists ix_jornada_cliente_eventos_cliente_tipo on plantaopro.jornada_cliente_eventos(cliente_id, tipo);
create index if not exists ix_jornada_cliente_tarefas_cliente_status on plantaopro.jornada_cliente_tarefas(cliente_id, status);
create index if not exists ix_jornada_cliente_tarefas_vencimento on plantaopro.jornada_cliente_tarefas(vencimento);
create index if not exists ix_comercial_leads_status on plantaopro.comercial_leads(status);
create index if not exists ix_comercial_leads_reg_date on plantaopro.comercial_leads(reg_date);
create index if not exists ix_comercial_oportunidades_etapa on plantaopro.comercial_oportunidades(etapa);
create index if not exists ix_comercial_oportunidades_reg_date on plantaopro.comercial_oportunidades(reg_date);
create index if not exists ix_comercial_propostas_status on plantaopro.comercial_propostas(status);
create index if not exists ix_comercial_propostas_validade on plantaopro.comercial_propostas(validade);
create index if not exists ix_customer_success_riscos_cliente_status on plantaopro.customer_success_riscos(cliente_id, status);
create index if not exists ix_customer_success_tarefas_cliente_status on plantaopro.customer_success_tarefas(cliente_id, status);
create index if not exists ix_lgpd_consentimentos_usuario on plantaopro.lgpd_consentimentos(usuario_id, finalidade);
create index if not exists ix_lgpd_solicitacoes_usuario_status on plantaopro.lgpd_solicitacoes_titular(usuario_id, status);
create index if not exists ix_lgpd_eventos_usuario on plantaopro.lgpd_eventos_privacidade(usuario_id, reg_date);
create index if not exists ix_ajuda_artigos_perfil on plantaopro.ajuda_artigos(perfil);
create index if not exists ix_eventos_sistema_cliente_tipo on plantaopro.eventos_sistema(cliente_id, tipo);
create index if not exists ix_logs_operacionais_cliente_acao on plantaopro.logs_operacionais(cliente_id, acao);
create index if not exists ix_auditoria_eventos_cliente_acao on plantaopro.auditoria_eventos(cliente_id, acao);
create index if not exists ix_auditoria_lgpd_eventos_cliente_acao on plantaopro.auditoria_lgpd_eventos(cliente_id, acao);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_planos_valor_mensal_nao_negativo') THEN
        ALTER TABLE plantaopro.planos ADD CONSTRAINT ck_planos_valor_mensal_nao_negativo CHECK (valor_mensal >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_comercial_propostas_desconto_percentual') THEN
        ALTER TABLE plantaopro.comercial_propostas ADD CONSTRAINT ck_comercial_propostas_desconto_percentual CHECK (desconto_percentual >= 0 AND desconto_percentual <= 100);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_faturas_saas_valor_total_nao_negativo') THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_valor_total_nao_negativo CHECK (valor_total >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_pagamentos_saas_valor_pago_nao_negativo') THEN
        ALTER TABLE plantaopro.pagamentos_saas ADD CONSTRAINT ck_pagamentos_saas_valor_pago_nao_negativo CHECK (valor_pago >= 0);
    END IF;
END $$;

insert into plantaopro.lgpd_bases_legais(nome, descricao, reg_status, reg_date)
select base.nome, base.descricao, 'A', now()
from (values
    ('Execução de contrato','Tratamento necessário para execução dos serviços contratados.'),
    ('Cumprimento de obrigação legal','Tratamento necessário para obrigações legais e regulatórias.'),
    ('Legítimo interesse','Tratamento necessário para segurança, auditoria e melhoria operacional.'),
    ('Consentimento','Tratamento autorizado pelo titular para finalidade específica.'),
    ('Exercício regular de direitos','Tratamento necessário para exercício regular de direitos em processos administrativos ou judiciais.')
) as base(nome, descricao)
where not exists (select 1 from plantaopro.lgpd_bases_legais b where b.nome = base.nome);

-- Origem histórica: database/migrations/2026_saas_comercial_core.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.saas_clientes (id uuid primary key default gen_random_uuid(), nome_fantasia text not null, razao_social text not null, cnpj text not null, email text not null, telefone text, responsavel text, segmento text, cidade text, estado text, status text not null default 'ATIVO', reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_planos (id uuid primary key default gen_random_uuid(), nome text not null unique, descricao text, valor_mensal numeric(12,2) not null default 0, status text not null default 'ATIVO', reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_tenants (id uuid primary key default gen_random_uuid(), cliente_id uuid not null references plantaopro.saas_clientes(id), plano_id uuid references plantaopro.saas_planos(id), nome text not null, subdominio text not null unique, status text not null default 'ATIVO', data_ativacao timestamptz, observacoes text, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_modulos (id uuid primary key default gen_random_uuid(), codigo text not null unique, nome text not null, descricao text, categoria text, status text not null default 'ATIVO', reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_plano_modulos (id uuid primary key default gen_random_uuid(), plano_id uuid not null references plantaopro.saas_planos(id), modulo_id uuid not null references plantaopro.saas_modulos(id), habilitado boolean not null default true, reg_status char(1) not null default 'A', reg_date timestamptz not null default now(), unique(plano_id, modulo_id));
create table if not exists plantaopro.saas_assinaturas (id uuid primary key default gen_random_uuid(), cliente_id uuid not null references plantaopro.saas_clientes(id), tenant_id uuid references plantaopro.saas_tenants(id), plano_id uuid references plantaopro.saas_planos(id), valor_mensal numeric(12,2) not null default 0, dia_vencimento int not null default 10, status text not null default 'ATIVA', data_inicio date not null default current_date, data_fim date, renovacao_automatica boolean not null default true, observacoes text, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_assinatura_historico (id uuid primary key default gen_random_uuid(), assinatura_id uuid not null references plantaopro.saas_assinaturas(id), cliente_id uuid, tenant_id uuid, acao text not null, plano_anterior_id uuid, plano_novo_id uuid, observacoes text, usuario_id uuid, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_limites (id uuid primary key default gen_random_uuid(), plano_id uuid not null references plantaopro.saas_planos(id), codigo text not null, nome text not null, limite int, bloqueante boolean not null default true, reg_status char(1) not null default 'A', reg_date timestamptz not null default now(), unique(plano_id,codigo));
create table if not exists plantaopro.saas_uso_mensal (id uuid primary key default gen_random_uuid(), cliente_id uuid not null, tenant_id uuid not null, competencia date not null, codigo_limite text not null, quantidade int not null default 0, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_bloqueios (id uuid primary key default gen_random_uuid(), cliente_id uuid, tenant_id uuid, codigo text not null, motivo text not null, status text not null default 'ATIVO', liberado_por uuid, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_onboarding (id uuid primary key default gen_random_uuid(), cliente_id uuid not null, tenant_id uuid, percentual int not null default 0, status text not null default 'EM_ANDAMENTO', reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_onboarding_etapas (id uuid primary key default gen_random_uuid(), onboarding_id uuid not null references plantaopro.saas_onboarding(id), tenant_id uuid, ordem int not null, nome text not null, descricao text, responsavel text, status text not null default 'PENDENTE', link_tela text, dica_contextual text, documento_apoio text, data_conclusao timestamptz, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_white_label (id uuid primary key default gen_random_uuid(), cliente_id uuid not null, tenant_id uuid, logo_url text, nome_comercial text, slogan text, cor_primaria text, cor_secundaria text, cor_destaque text, favicon_url text, email_remetente text, url_publica text, texto_rodape text, modo_tema text, tema_institucional text, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_marketplace_modulos (id uuid primary key default gen_random_uuid(), modulo_id uuid references plantaopro.saas_modulos(id), nome text not null, descricao text, beneficios text, plano_minimo text, status text not null default 'DISPONIVEL', reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_marketplace_contratacoes (id uuid primary key default gen_random_uuid(), cliente_id uuid not null, tenant_id uuid, marketplace_modulo_id uuid not null references plantaopro.saas_marketplace_modulos(id), status text not null default 'SOLICITADO', solicitado_por uuid, aprovado_por uuid, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_billing_faturas (id uuid primary key default gen_random_uuid(), assinatura_id uuid not null references plantaopro.saas_assinaturas(id), cliente_id uuid, tenant_id uuid, competencia date not null, vencimento date not null, valor numeric(12,2) not null, status text not null default 'ABERTA', data_pagamento date, metodo_informativo text, observacoes text, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_billing_pagamentos (id uuid primary key default gen_random_uuid(), fatura_id uuid not null references plantaopro.saas_billing_faturas(id), valor numeric(12,2) not null, data_pagamento date not null, metodo_informativo text, observacoes text, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.saas_billing_eventos (id uuid primary key default gen_random_uuid(), assinatura_id uuid, fatura_id uuid, cliente_id uuid, tenant_id uuid, tipo text not null, descricao text, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.suporte_chamados (id uuid primary key default gen_random_uuid(), tenant_id uuid, cliente_id uuid, solicitante_id uuid, titulo text not null, descricao text not null, categoria text not null, prioridade text not null, status text not null default 'ABERTO', sla_horas int not null default 24, data_abertura timestamptz not null default now(), data_primeira_resposta timestamptz, data_fechamento timestamptz, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.suporte_chamado_mensagens (id uuid primary key default gen_random_uuid(), chamado_id uuid not null references plantaopro.suporte_chamados(id), autor_id uuid, mensagem text not null, interno boolean not null default false, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.suporte_chamado_anexos (id uuid primary key default gen_random_uuid(), chamado_id uuid not null references plantaopro.suporte_chamados(id), nome_arquivo text not null, content_type text not null, tamanho_bytes bigint not null, url text not null, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.suporte_sla_politicas (id uuid primary key default gen_random_uuid(), prioridade text not null unique, primeira_resposta_horas int not null, resolucao_horas int not null, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create table if not exists plantaopro.suporte_base_conhecimento (id uuid primary key default gen_random_uuid(), titulo text not null, categoria text not null, conteudo text not null, status text not null default 'PUBLICADO', reg_status char(1) not null default 'A', reg_date timestamptz not null default now());

DO $$ begin if not exists (select 1 from pg_constraint where conname='ck_saas_clientes_status') then alter table plantaopro.saas_clientes add constraint ck_saas_clientes_status check (status in ('ATIVO','SUSPENSO','INATIVO')); end if; end $$;

create index if not exists ix_saas_clientes_status_date on plantaopro.saas_clientes(status, reg_date);
create index if not exists ix_saas_tenants_cliente_status on plantaopro.saas_tenants(cliente_id,status,reg_date);
create index if not exists ix_saas_assinaturas_cliente_tenant on plantaopro.saas_assinaturas(cliente_id,tenant_id,status,reg_date);
create index if not exists ix_saas_uso_tenant_competencia on plantaopro.saas_uso_mensal(tenant_id,competencia,reg_date);
create index if not exists ix_suporte_chamados_tenant_status on plantaopro.suporte_chamados(tenant_id,status,reg_date);
insert into plantaopro.saas_planos(nome, descricao, valor_mensal) values
('Essencial','Até 5 usuários, 2 médicos, atendimento e plantões básicos.',299),
('Profissional','Operação clínica completa com financeiro, CID, prescrição e relatórios.',799),
('Enterprise','Limites configuráveis, convênios, API pública, webhooks e suporte premium.',1990),
('White Label','Enterprise com marca, domínio, e-mail e marketplace próprios.',2990)
on conflict (nome) do update set descricao=excluded.descricao, valor_mensal=excluded.valor_mensal;

insert into plantaopro.saas_modulos(codigo,nome,descricao,categoria) values
('ATENDIMENTO','Atendimento','Fluxo de atendimento clínico.','Clinico'),('PLANTOES','Plantões','Gestão de plantões.','Operacao'),('ESCALAS','Escalas','Escalas médicas.','Operacao'),('FINANCEIRO','Financeiro','Rotinas financeiras.','Financeiro'),('CONVENIOS','Convênios','Convênios e autorizações.','Clinico'),('CID','CID','Consulta e importação CID.','Clinico'),('PRESCRICOES','Prescrições','Prescrições médicas.','Clinico'),('RELATORIOS','Relatórios','Relatórios operacionais.','Gestao'),('DASHBOARD_GESTOR','Dashboard Gestor','Painéis executivos.','Gestao'),('API_PUBLICA','API Pública','Integrações públicas.','Integracao'),('WEBHOOKS','Webhooks','Eventos externos.','Integracao'),('WHITE_LABEL','White Label','Marca própria.','SaaS'),('MARKETPLACE','Marketplace','Loja de módulos.','SaaS'),('SUPORTE_PREMIUM','Suporte Premium','SLA premium.','Suporte')
on conflict (codigo) do update set nome=excluded.nome, descricao=excluded.descricao;
create table if not exists plantaopro.saas_billing_assinaturas (id uuid primary key default gen_random_uuid(), cliente_id uuid not null, tenant_id uuid, plano_id uuid, valor_mensal numeric(12,2) not null default 0, dia_vencimento int not null default 10, status text not null default 'ATIVA', data_inicio date not null default current_date, data_fim date, renovacao_automatica boolean not null default true, observacoes text, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
create index if not exists ix_saas_billing_assinaturas_cliente_tenant on plantaopro.saas_billing_assinaturas(cliente_id,tenant_id,status,reg_date);

-- Origem histórica: database/migrations/2026_v113_operacional_real.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;
create table if not exists plantaopro.v113_clientes (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, nome varchar(160), documento varchar(40), email varchar(160), status varchar(40));
alter table if exists plantaopro.v113_clientes add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_clientes add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_clientes add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_clientes add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_clientes add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_clientes add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_clientes add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_clientes add column if not exists nome varchar(160);
alter table if exists plantaopro.v113_clientes add column if not exists documento varchar(40);
alter table if exists plantaopro.v113_clientes add column if not exists email varchar(160);
alter table if exists plantaopro.v113_clientes add column if not exists status varchar(40);
create index if not exists ix_v113_clientes_cliente_id on plantaopro.v113_clientes (cliente_id);
create index if not exists ix_v113_clientes_status on plantaopro.v113_clientes (status);
create index if not exists ix_v113_clientes_created_at on plantaopro.v113_clientes (created_at);
create index if not exists ix_v113_clientes_tenant_id on plantaopro.v113_clientes (tenant_id);
create table if not exists plantaopro.v113_produtos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, codigo varchar(60), nome varchar(160), preco numeric(14,2) default 0, estoque_minimo numeric(14,2) default 0, status varchar(40));
alter table if exists plantaopro.v113_produtos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_produtos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_produtos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_produtos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_produtos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_produtos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_produtos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_produtos add column if not exists codigo varchar(60);
alter table if exists plantaopro.v113_produtos add column if not exists nome varchar(160);
alter table if exists plantaopro.v113_produtos add column if not exists preco numeric(14,2) default 0;
alter table if exists plantaopro.v113_produtos add column if not exists estoque_minimo numeric(14,2) default 0;
alter table if exists plantaopro.v113_produtos add column if not exists status varchar(40);
create index if not exists ix_v113_produtos_cliente_id on plantaopro.v113_produtos (cliente_id);
create index if not exists ix_v113_produtos_status on plantaopro.v113_produtos (status);
create index if not exists ix_v113_produtos_created_at on plantaopro.v113_produtos (created_at);
create index if not exists ix_v113_produtos_tenant_id on plantaopro.v113_produtos (tenant_id);
create table if not exists plantaopro.v113_estoque_movimentos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, produto_id uuid, pedido_id uuid, quantidade numeric(14,2) default 0, tipo varchar(60), observacao text, status varchar(40));
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists produto_id uuid;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists pedido_id uuid;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists quantidade numeric(14,2) default 0;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists tipo varchar(60);
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists observacao text;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists status varchar(40);
create index if not exists ix_v113_estoque_movimentos_cliente_id on plantaopro.v113_estoque_movimentos (cliente_id);
create index if not exists ix_v113_estoque_movimentos_status on plantaopro.v113_estoque_movimentos (status);
create index if not exists ix_v113_estoque_movimentos_created_at on plantaopro.v113_estoque_movimentos (created_at);
create index if not exists ix_v113_estoque_movimentos_tenant_id on plantaopro.v113_estoque_movimentos (tenant_id);
create table if not exists plantaopro.v113_pedidos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, cliente_operacional_id uuid, status varchar(40), total numeric(14,2) default 0);
alter table if exists plantaopro.v113_pedidos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_pedidos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_pedidos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_pedidos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_pedidos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_pedidos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_pedidos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_pedidos add column if not exists cliente_operacional_id uuid;
alter table if exists plantaopro.v113_pedidos add column if not exists status varchar(40);
alter table if exists plantaopro.v113_pedidos add column if not exists total numeric(14,2) default 0;
create index if not exists ix_v113_pedidos_cliente_id on plantaopro.v113_pedidos (cliente_id);
create index if not exists ix_v113_pedidos_status on plantaopro.v113_pedidos (status);
create index if not exists ix_v113_pedidos_created_at on plantaopro.v113_pedidos (created_at);
create index if not exists ix_v113_pedidos_tenant_id on plantaopro.v113_pedidos (tenant_id);
create table if not exists plantaopro.v113_pedido_itens (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, pedido_id uuid, produto_id uuid, quantidade numeric(14,2) default 0, valor_unitario numeric(14,2) default 0, status varchar(40));
alter table if exists plantaopro.v113_pedido_itens add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_pedido_itens add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_pedido_itens add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists pedido_id uuid;
alter table if exists plantaopro.v113_pedido_itens add column if not exists produto_id uuid;
alter table if exists plantaopro.v113_pedido_itens add column if not exists quantidade numeric(14,2) default 0;
alter table if exists plantaopro.v113_pedido_itens add column if not exists valor_unitario numeric(14,2) default 0;
alter table if exists plantaopro.v113_pedido_itens add column if not exists status varchar(40);
create index if not exists ix_v113_pedido_itens_cliente_id on plantaopro.v113_pedido_itens (cliente_id);
create index if not exists ix_v113_pedido_itens_status on plantaopro.v113_pedido_itens (status);
create index if not exists ix_v113_pedido_itens_created_at on plantaopro.v113_pedido_itens (created_at);
create index if not exists ix_v113_pedido_itens_tenant_id on plantaopro.v113_pedido_itens (tenant_id);
create table if not exists plantaopro.v113_tarefas (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, pedido_id uuid, titulo varchar(180), status varchar(40), responsavel varchar(120), comentarios jsonb default '[]'::jsonb);
alter table if exists plantaopro.v113_tarefas add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_tarefas add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_tarefas add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_tarefas add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_tarefas add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_tarefas add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_tarefas add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_tarefas add column if not exists pedido_id uuid;
alter table if exists plantaopro.v113_tarefas add column if not exists titulo varchar(180);
alter table if exists plantaopro.v113_tarefas add column if not exists status varchar(40);
alter table if exists plantaopro.v113_tarefas add column if not exists responsavel varchar(120);
alter table if exists plantaopro.v113_tarefas add column if not exists comentarios jsonb default '[]'::jsonb;
create index if not exists ix_v113_tarefas_cliente_id on plantaopro.v113_tarefas (cliente_id);
create index if not exists ix_v113_tarefas_status on plantaopro.v113_tarefas (status);
create index if not exists ix_v113_tarefas_created_at on plantaopro.v113_tarefas (created_at);
create index if not exists ix_v113_tarefas_tenant_id on plantaopro.v113_tarefas (tenant_id);
create table if not exists plantaopro.v113_faturas (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, pedido_id uuid, valor numeric(14,2) default 0, status varchar(40));
alter table if exists plantaopro.v113_faturas add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_faturas add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_faturas add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_faturas add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_faturas add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_faturas add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_faturas add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_faturas add column if not exists pedido_id uuid;
alter table if exists plantaopro.v113_faturas add column if not exists valor numeric(14,2) default 0;
alter table if exists plantaopro.v113_faturas add column if not exists status varchar(40);
create index if not exists ix_v113_faturas_cliente_id on plantaopro.v113_faturas (cliente_id);
create index if not exists ix_v113_faturas_status on plantaopro.v113_faturas (status);
create index if not exists ix_v113_faturas_created_at on plantaopro.v113_faturas (created_at);
create index if not exists ix_v113_faturas_tenant_id on plantaopro.v113_faturas (tenant_id);
create table if not exists plantaopro.v113_titulos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, fatura_id uuid, valor numeric(14,2) default 0, status varchar(40), demo_boleto boolean default false, vencimento timestamptz);
alter table if exists plantaopro.v113_titulos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_titulos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_titulos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_titulos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_titulos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_titulos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_titulos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_titulos add column if not exists fatura_id uuid;
alter table if exists plantaopro.v113_titulos add column if not exists valor numeric(14,2) default 0;
alter table if exists plantaopro.v113_titulos add column if not exists status varchar(40);
alter table if exists plantaopro.v113_titulos add column if not exists demo_boleto boolean default false;
alter table if exists plantaopro.v113_titulos add column if not exists vencimento timestamptz;
create index if not exists ix_v113_titulos_cliente_id on plantaopro.v113_titulos (cliente_id);
create index if not exists ix_v113_titulos_status on plantaopro.v113_titulos (status);
create index if not exists ix_v113_titulos_created_at on plantaopro.v113_titulos (created_at);
create index if not exists ix_v113_titulos_tenant_id on plantaopro.v113_titulos (tenant_id);
create table if not exists plantaopro.v113_outbox_eventos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, tipo varchar(80), payload_ref varchar(120), payload jsonb default '{}'::jsonb, status varchar(40), erro text);
alter table if exists plantaopro.v113_outbox_eventos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_outbox_eventos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_outbox_eventos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists tipo varchar(80);
alter table if exists plantaopro.v113_outbox_eventos add column if not exists payload_ref varchar(120);
alter table if exists plantaopro.v113_outbox_eventos add column if not exists payload jsonb default '{}'::jsonb;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists status varchar(40);
alter table if exists plantaopro.v113_outbox_eventos add column if not exists erro text;
create index if not exists ix_v113_outbox_eventos_cliente_id on plantaopro.v113_outbox_eventos (cliente_id);
create index if not exists ix_v113_outbox_eventos_status on plantaopro.v113_outbox_eventos (status);
create index if not exists ix_v113_outbox_eventos_created_at on plantaopro.v113_outbox_eventos (created_at);
create index if not exists ix_v113_outbox_eventos_tenant_id on plantaopro.v113_outbox_eventos (tenant_id);
create table if not exists plantaopro.v113_outbox_logs (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, outbox_evento_id uuid, status varchar(40), detalhe text);
alter table if exists plantaopro.v113_outbox_logs add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_outbox_logs add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_outbox_logs add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists outbox_evento_id uuid;
alter table if exists plantaopro.v113_outbox_logs add column if not exists status varchar(40);
alter table if exists plantaopro.v113_outbox_logs add column if not exists detalhe text;
create index if not exists ix_v113_outbox_logs_cliente_id on plantaopro.v113_outbox_logs (cliente_id);
create index if not exists ix_v113_outbox_logs_status on plantaopro.v113_outbox_logs (status);
create index if not exists ix_v113_outbox_logs_created_at on plantaopro.v113_outbox_logs (created_at);
create index if not exists ix_v113_outbox_logs_tenant_id on plantaopro.v113_outbox_logs (tenant_id);
create table if not exists plantaopro.v113_templates (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, codigo varchar(80) unique, nome varchar(160), descricao text, status varchar(40));
alter table if exists plantaopro.v113_templates add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_templates add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_templates add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_templates add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_templates add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_templates add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_templates add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_templates add column if not exists codigo varchar(80) unique;
alter table if exists plantaopro.v113_templates add column if not exists nome varchar(160);
alter table if exists plantaopro.v113_templates add column if not exists descricao text;
alter table if exists plantaopro.v113_templates add column if not exists status varchar(40);
create index if not exists ix_v113_templates_cliente_id on plantaopro.v113_templates (cliente_id);
create index if not exists ix_v113_templates_status on plantaopro.v113_templates (status);
create index if not exists ix_v113_templates_created_at on plantaopro.v113_templates (created_at);
create index if not exists ix_v113_templates_tenant_id on plantaopro.v113_templates (tenant_id);
create table if not exists plantaopro.v113_template_instalacoes (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, template_id uuid, status varchar(40));
alter table if exists plantaopro.v113_template_instalacoes add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_template_instalacoes add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_template_instalacoes add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists template_id uuid;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists status varchar(40);
create index if not exists ix_v113_template_instalacoes_cliente_id on plantaopro.v113_template_instalacoes (cliente_id);
create index if not exists ix_v113_template_instalacoes_status on plantaopro.v113_template_instalacoes (status);
create index if not exists ix_v113_template_instalacoes_created_at on plantaopro.v113_template_instalacoes (created_at);
create index if not exists ix_v113_template_instalacoes_tenant_id on plantaopro.v113_template_instalacoes (tenant_id);
create table if not exists plantaopro.v113_jornada_acoes (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, codigo varchar(80), status varchar(40), detalhe text, erro text, open_url varchar(180), ordem int default 0);
alter table if exists plantaopro.v113_jornada_acoes add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_jornada_acoes add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_jornada_acoes add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists codigo varchar(80);
alter table if exists plantaopro.v113_jornada_acoes add column if not exists status varchar(40);
alter table if exists plantaopro.v113_jornada_acoes add column if not exists detalhe text;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists erro text;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists open_url varchar(180);
alter table if exists plantaopro.v113_jornada_acoes add column if not exists ordem int default 0;
create index if not exists ix_v113_jornada_acoes_cliente_id on plantaopro.v113_jornada_acoes (cliente_id);
create index if not exists ix_v113_jornada_acoes_status on plantaopro.v113_jornada_acoes (status);
create index if not exists ix_v113_jornada_acoes_created_at on plantaopro.v113_jornada_acoes (created_at);
create index if not exists ix_v113_jornada_acoes_tenant_id on plantaopro.v113_jornada_acoes (tenant_id);
create table if not exists plantaopro.v113_atividades (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, tipo varchar(80), descricao text, status varchar(40));
alter table if exists plantaopro.v113_atividades add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_atividades add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_atividades add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_atividades add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_atividades add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_atividades add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_atividades add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_atividades add column if not exists tipo varchar(80);
alter table if exists plantaopro.v113_atividades add column if not exists descricao text;
alter table if exists plantaopro.v113_atividades add column if not exists status varchar(40);
create index if not exists ix_v113_atividades_cliente_id on plantaopro.v113_atividades (cliente_id);
create index if not exists ix_v113_atividades_status on plantaopro.v113_atividades (status);
create index if not exists ix_v113_atividades_created_at on plantaopro.v113_atividades (created_at);
create index if not exists ix_v113_atividades_tenant_id on plantaopro.v113_atividades (tenant_id);
create table if not exists plantaopro.v113_auditoria (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, usuario_id uuid, entidade varchar(120), entidade_id uuid, acao varchar(120), detalhes jsonb default '{}'::jsonb, sucesso boolean default true, ip_origem varchar(80), perfil varchar(120), status varchar(40));
alter table if exists plantaopro.v113_auditoria add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_auditoria add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_auditoria add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_auditoria add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_auditoria add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_auditoria add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_auditoria add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_auditoria add column if not exists usuario_id uuid;
alter table if exists plantaopro.v113_auditoria add column if not exists entidade varchar(120);
alter table if exists plantaopro.v113_auditoria add column if not exists entidade_id uuid;
alter table if exists plantaopro.v113_auditoria add column if not exists acao varchar(120);
alter table if exists plantaopro.v113_auditoria add column if not exists detalhes jsonb default '{}'::jsonb;
alter table if exists plantaopro.v113_auditoria add column if not exists sucesso boolean default true;
alter table if exists plantaopro.v113_auditoria add column if not exists ip_origem varchar(80);
alter table if exists plantaopro.v113_auditoria add column if not exists perfil varchar(120);
alter table if exists plantaopro.v113_auditoria add column if not exists status varchar(40);
create index if not exists ix_v113_auditoria_cliente_id on plantaopro.v113_auditoria (cliente_id);
create index if not exists ix_v113_auditoria_status on plantaopro.v113_auditoria (status);
create index if not exists ix_v113_auditoria_created_at on plantaopro.v113_auditoria (created_at);
create index if not exists ix_v113_auditoria_tenant_id on plantaopro.v113_auditoria (tenant_id);

-- Origem histórica: database/migrations/2026_v114_consolidacao_produto.sql
-- v1.14 consolida o domínio PlantãoPro sobre as tabelas persistidas da v1.13.
-- Não cria módulo paralelo; adiciona estruturas pequenas para favoritos, filtros, atalhos e timelines.
create schema if not exists plantaopro;
create table if not exists plantaopro.v114_favoritos_usuario(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, usuario_id uuid null, titulo text not null, rota text not null, modulo text not null, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null);
create table if not exists plantaopro.v114_filtros_salvos(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, usuario_id uuid null, nome text not null, rota text not null, filtros jsonb not null default '{}'::jsonb, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null);
create table if not exists plantaopro.v114_timelines(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, entidade text not null, entidade_id uuid null, evento text not null, resumo text not null, perfil text null, dados_minimos jsonb not null default '{}'::jsonb, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null);
create table if not exists plantaopro.v114_checklist_implantacao(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, titulo text not null, perfil_responsavel text not null, status text not null default 'PENDENTE', ordem int not null default 0, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null);

-- Origem histórica: database/migrations/2026_v115_regras_faturamento_repasses.sql
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.v115_regras_faturamento(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, codigo text not null, nome text not null, tipo_faturamento text not null, item_faturavel_id uuid null, convenio_id uuid null, valor_base numeric(12,2) not null default 0, percentual_desconto numeric(6,2) not null default 0, percentual_acrescimo numeric(6,2) not null default 0, status text not null default 'ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_regras_repasse(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, referencia_id uuid null, medico_id uuid null, convenio_id uuid null, tipo_regra text not null default 'PERCENTUAL', percentual numeric(6,2) not null default 0, valor_fixo numeric(12,2) not null default 0, valor_base numeric(12,2) not null default 0, valor_repasse numeric(12,2) not null default 0, contestacao text null, status text not null default 'REGRA_ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_regras_glosa(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, conta_receber_id uuid null, titulo_id uuid null, convenio_id uuid null, motivo text not null default 'REGRA_CONVENIO', valor_glosado numeric(12,2) not null default 0, percentual_glosa numeric(6,2) not null default 0, prazo_recurso timestamptz null, resolucao text null, status text not null default 'ABERTA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_convenio_regras(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, convenio_id uuid null, nome text not null, prazo_recebimento_dias int not null default 30, exige_autorizacao boolean not null default false, status text not null default 'ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_faturamento_eventos(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, tipo text not null, entidade_id uuid null, payload jsonb not null default '{}'::jsonb, status text not null default 'PENDENTE', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_recebimentos(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, conta_receber_id uuid null, valor_recebido numeric(12,2) not null default 0, forma text not null default 'MANUAL_AUDITADO', status text not null default 'RECEBIDO', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_configuracoes_financeiras(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, chave text not null, valor text not null, escopo text not null default 'TENANT', status text not null default 'ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_jornada_perfil_progresso(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, perfil text not null, passo text not null, rota text not null, cta text not null, pendencia_relacionada text null, status text not null default 'PENDENTE', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_alertas_operacionais(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, tipo text not null, prioridade text not null, perfil_responsavel text not null, modulo text not null, entidade_id uuid null, cta text not null, rota text not null, prazo timestamptz null, status text not null default 'ABERTA', origem_regra text not null, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_configuracoes_mobile(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, perfil text not null, chave text not null, valor jsonb not null default '{}'::jsonb, status text not null default 'ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);

alter table if exists plantaopro.v115_regras_faturamento add column if not exists updated_by uuid null;
alter table if exists plantaopro.v115_regras_repasse add column if not exists contestacao text null;
alter table if exists plantaopro.v115_regras_glosa add column if not exists resolucao text null;
alter table if exists plantaopro.v115_alertas_operacionais add column if not exists origem_regra text not null default 'v115';

create index if not exists ix_v115_regras_faturamento_tenant_status_data on plantaopro.v115_regras_faturamento(tenant_id,reg_status,created_at);
create index if not exists ix_v115_regras_repasse_tenant_status_data on plantaopro.v115_regras_repasse(tenant_id,reg_status,created_at);
create index if not exists ix_v115_regras_glosa_tenant_status_data on plantaopro.v115_regras_glosa(tenant_id,reg_status,created_at);
create index if not exists ix_v115_convenio_regras_tenant_status_data on plantaopro.v115_convenio_regras(tenant_id,reg_status,created_at);
create index if not exists ix_v115_faturamento_eventos_tenant_status_data on plantaopro.v115_faturamento_eventos(tenant_id,reg_status,created_at);
create index if not exists ix_v115_recebimentos_tenant_status_data on plantaopro.v115_recebimentos(tenant_id,reg_status,created_at);
create index if not exists ix_v115_config_fin_tenant_status_data on plantaopro.v115_configuracoes_financeiras(tenant_id,reg_status,created_at);
create index if not exists ix_v115_jornada_tenant_status_data on plantaopro.v115_jornada_perfil_progresso(tenant_id,reg_status,created_at);
create index if not exists ix_v115_alertas_tenant_status_data on plantaopro.v115_alertas_operacionais(tenant_id,reg_status,created_at);
create index if not exists ix_v115_mobile_tenant_status_data on plantaopro.v115_configuracoes_mobile(tenant_id,reg_status,created_at);

-- Origem histórica: database/migrations/2026_v116_consolidacao_operacional_final.sql
CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.v116_convenio_autorizacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_convenio_autorizacoes_tenant_status_data ON plantaopro.v116_convenio_autorizacoes(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_convenio_guias (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_convenio_guias_tenant_status_data ON plantaopro.v116_convenio_guias(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_faturamento_lotes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_faturamento_lotes_tenant_status_data ON plantaopro.v116_faturamento_lotes(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_faturamento_lote_itens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_faturamento_lote_itens_tenant_status_data ON plantaopro.v116_faturamento_lote_itens(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_caixas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_caixas_tenant_status_data ON plantaopro.v116_caixas(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_caixa_movimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_caixa_movimentos_tenant_status_data ON plantaopro.v116_caixa_movimentos(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_recebimentos_parciais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_recebimentos_parciais_tenant_status_data ON plantaopro.v116_recebimentos_parciais(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_estornos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_estornos_tenant_status_data ON plantaopro.v116_estornos(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_timelines (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_timelines_tenant_status_data ON plantaopro.v116_timelines(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_notificacoes_operacionais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_notificacoes_operacionais_tenant_status_data ON plantaopro.v116_notificacoes_operacionais(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_relatorios_execucoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_relatorios_execucoes_tenant_status_data ON plantaopro.v116_relatorios_execucoes(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_auditoria_consultas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_auditoria_consultas_tenant_status_data ON plantaopro.v116_auditoria_consultas(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_integracao_provedores (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_integracao_provedores_tenant_status_data ON plantaopro.v116_integracao_provedores(tenant_id, reg_status, created_at);

-- Origem histórica: database/migrations/2026_v117_hardening_v116_runtime.sql
CREATE SCHEMA IF NOT EXISTS plantaopro;
ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS entidade varchar(120) NULL;
ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS entidade_id uuid NULL;
ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS prioridade varchar(30) NOT NULL DEFAULT 'MEDIA';
ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS perfil_responsavel varchar(80) NOT NULL DEFAULT 'OPERACAO';
CREATE INDEX IF NOT EXISTS ix_v117_v116_timelines_entidade ON plantaopro.v116_timelines(tenant_id, entidade, entidade_id, created_at);
CREATE INDEX IF NOT EXISTS ix_v117_v116_notificacoes_prioridade ON plantaopro.v116_notificacoes_operacionais(tenant_id, prioridade, status_operacional, created_at);

-- ============================================================
-- Seção 09 — Auditoria de relatórios v1.18.4
-- ============================================================

CREATE TABLE IF NOT EXISTS plantaopro.relatorio_exportacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid, tenant_id uuid NOT NULL, usuario_id uuid, codigo_relatorio varchar(80) NOT NULL, formato varchar(20) NOT NULL, filtros_sanitizados jsonb NOT NULL DEFAULT '{}'::jsonb, quantidade_registros int NOT NULL DEFAULT 0, contem_dados_sensiveis boolean NOT NULL DEFAULT false, status varchar(30) NOT NULL DEFAULT 'SUCESSO', duracao_ms bigint NOT NULL DEFAULT 0, nome_arquivo varchar(260), ip_origem varchar(80), reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS idx_relatorio_exportacoes_tenant_data ON plantaopro.relatorio_exportacoes(tenant_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS idx_relatorio_exportacoes_cliente_status_data ON plantaopro.relatorio_exportacoes(cliente_id, status, reg_date DESC);

-- ============================================================
-- Seção 14 — Dados referenciais mínimos
-- ============================================================

INSERT INTO plantaopro.planos(nome, slug, descricao, valor_mensal, limite_medicos, limite_hospitais, limite_plantoes_mes, limite_usuarios, permite_relatorios, permite_relatorios_avancados, publico, status) SELECT 'Essencial', 'essencial', 'Plano inicial PlantãoPro', 0, 10, 3, 100, 5, true, true, true, 'ATIVO' WHERE NOT EXISTS (SELECT 1 FROM plantaopro.planos WHERE slug='essencial');
