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

\ir schema/320_v187_fechamento_operacional_financeiro.sql

-- BEGIN v1.88.0 prontuario longitudinal
-- PlantaoPro v1.88.0 - camada clinica longitudinal, tenant-safe e auditavel.
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.paciente_problemas(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, cliente_id uuid NOT NULL, paciente_id uuid NOT NULL, cid_id uuid,
 descricao varchar(500) NOT NULL, status varchar(20) NOT NULL DEFAULT 'ATIVO', data_inicio date NOT NULL DEFAULT current_date, data_resolucao date,
 observacao text, origem_consulta_id uuid, versao integer NOT NULL DEFAULT 1, criado_por uuid, criado_em timestamptz NOT NULL DEFAULT now(),
 atualizado_por uuid, atualizado_em timestamptz, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_paciente_problemas_status CHECK(status IN ('ATIVO','RESOLVIDO','INATIVO')));
CREATE INDEX IF NOT EXISTS ix_paciente_problemas_tenant_paciente ON plantaopro.paciente_problemas(tenant_id,paciente_id,status) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_paciente_problemas_consulta ON plantaopro.paciente_problemas(tenant_id,origem_consulta_id) WHERE origem_consulta_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS plantaopro.paciente_alergias(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,tipo varchar(20) NOT NULL,substancia varchar(250) NOT NULL,
 descricao text,gravidade varchar(20) NOT NULL DEFAULT 'NAO_INFORMADA',reacao text,status varchar(20) NOT NULL DEFAULT 'ATIVA',confirmada boolean NOT NULL DEFAULT false,
 origem_consulta_id uuid,registrado_por uuid,registrado_em timestamptz NOT NULL DEFAULT now(),atualizado_por uuid,atualizado_em timestamptz,
 versao integer NOT NULL DEFAULT 1,reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_paciente_alergias_tipo CHECK(tipo IN ('MEDICAMENTO','ALIMENTO','SUBSTANCIA','OUTRA')),
 CONSTRAINT ck_paciente_alergias_gravidade CHECK(gravidade IN ('LEVE','MODERADA','GRAVE','NAO_INFORMADA')),
 CONSTRAINT ck_paciente_alergias_status CHECK(status IN ('ATIVA','INATIVA')));
CREATE INDEX IF NOT EXISTS ix_paciente_alergias_tenant_paciente ON plantaopro.paciente_alergias(tenant_id,paciente_id,status) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.paciente_medicamentos_uso(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,medicamento_id uuid,medicamento_descricao varchar(300) NOT NULL,
 dose varchar(100),frequencia varchar(150),via varchar(80),inicio_em date,fim_em date,status varchar(20) NOT NULL DEFAULT 'EM_USO',origem varchar(50) NOT NULL,
 consulta_id uuid,prescricao_id uuid,observacao text,versao integer NOT NULL DEFAULT 1,created_by uuid,created_at timestamptz NOT NULL DEFAULT now(),
 updated_by uuid,updated_at timestamptz,reg_status char(1) NOT NULL DEFAULT 'A',CONSTRAINT ck_medicamentos_uso_status CHECK(status IN ('EM_USO','SUSPENSO','FINALIZADO')));
CREATE INDEX IF NOT EXISTS ix_medicamentos_uso_tenant_paciente ON plantaopro.paciente_medicamentos_uso(tenant_id,paciente_id,status) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.solicitacoes_exames(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,consulta_id uuid,medico_id uuid,unidade_id uuid,
 status varchar(30) NOT NULL DEFAULT 'SOLICITADO',prioridade varchar(20) NOT NULL DEFAULT 'ROTINA',indicacao_clinica text NOT NULL,observacoes text,
 solicitado_em timestamptz NOT NULL DEFAULT now(),realizado_em timestamptz,cancelado_em timestamptz,created_by uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_by uuid,updated_at timestamptz,
 CONSTRAINT ck_solicitacoes_exames_status CHECK(status IN ('SOLICITADO','AUTORIZACAO_PENDENTE','AUTORIZADO','AGENDADO','REALIZADO','RESULTADO_DISPONIVEL','CANCELADO')));
CREATE INDEX IF NOT EXISTS ix_solicitacoes_exames_tenant_paciente ON plantaopro.solicitacoes_exames(tenant_id,paciente_id,status,solicitado_em DESC);
CREATE INDEX IF NOT EXISTS ix_solicitacoes_exames_consulta ON plantaopro.solicitacoes_exames(tenant_id,consulta_id) WHERE consulta_id IS NOT NULL;
CREATE TABLE IF NOT EXISTS plantaopro.solicitacao_exame_itens(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),solicitacao_id uuid NOT NULL,codigo varchar(80),nome varchar(250) NOT NULL,tipo varchar(80) NOT NULL,observacao text,status varchar(30) NOT NULL DEFAULT 'SOLICITADO',created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_solicitacao_exame_itens_solicitacao ON plantaopro.solicitacao_exame_itens(solicitacao_id,status);

CREATE TABLE IF NOT EXISTS plantaopro.resultados_exames(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,solicitacao_id uuid NOT NULL,item_id uuid,paciente_id uuid NOT NULL,tipo varchar(80) NOT NULL,
 resumo varchar(500) NOT NULL,resultado_textual text NOT NULL,realizado_em timestamptz NOT NULL,liberado_em timestamptz,profissional_responsavel varchar(250),
 documento_id uuid,created_by uuid,created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_resultados_exames_tenant_paciente ON plantaopro.resultados_exames(tenant_id,paciente_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_resultados_exames_solicitacao ON plantaopro.resultados_exames(tenant_id,solicitacao_id);

CREATE TABLE IF NOT EXISTS plantaopro.encaminhamentos_clinicos(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,consulta_id uuid NOT NULL,medico_origem_id uuid,
 especialidade_destino_id uuid,profissional_destino_id uuid,unidade_destino_id uuid,motivo text NOT NULL,resumo_clinico text NOT NULL,prioridade varchar(20) NOT NULL DEFAULT 'ROTINA',
 status varchar(30) NOT NULL DEFAULT 'CRIADO',criado_em timestamptz NOT NULL DEFAULT now(),agendado_em timestamptz,concluido_em timestamptz,created_by uuid,updated_by uuid,updated_at timestamptz,
 CONSTRAINT ck_encaminhamentos_status CHECK(status IN ('CRIADO','AGUARDANDO_AGENDAMENTO','AGENDADO','CONCLUIDO','CANCELADO')));
CREATE INDEX IF NOT EXISTS ix_encaminhamentos_tenant_paciente ON plantaopro.encaminhamentos_clinicos(tenant_id,paciente_id,status,criado_em DESC);
CREATE INDEX IF NOT EXISTS ix_encaminhamentos_consulta ON plantaopro.encaminhamentos_clinicos(tenant_id,consulta_id);

CREATE TABLE IF NOT EXISTS plantaopro.documentos_clinicos(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,consulta_id uuid,tipo varchar(40) NOT NULL,titulo varchar(250) NOT NULL,
 conteudo text NOT NULL,status varchar(20) NOT NULL DEFAULT 'RASCUNHO',versao integer NOT NULL DEFAULT 1,emitido_por uuid,emitido_em timestamptz,
 cancelado_por uuid,cancelado_em timestamptz,motivo_cancelamento text,hash_documento varchar(64),assinatura_status varchar(20) NOT NULL DEFAULT 'NAO_ASSINADO',
 cid_exibido boolean NOT NULL DEFAULT false,quantidade_dias integer,inicio_afastamento date,created_by uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,
 CONSTRAINT ck_documentos_tipo CHECK(tipo IN ('ATESTADO','DECLARACAO','ENCAMINHAMENTO','RESUMO_ATENDIMENTO','RELATORIO_CLINICO')),
 CONSTRAINT ck_documentos_assinatura CHECK(assinatura_status IN ('NAO_ASSINADO','PENDENTE','ASSINADO','FALHOU','CANCELADO')));
CREATE INDEX IF NOT EXISTS ix_documentos_clinicos_tenant_paciente ON plantaopro.documentos_clinicos(tenant_id,paciente_id,status,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documentos_clinicos_consulta ON plantaopro.documentos_clinicos(tenant_id,consulta_id) WHERE consulta_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS plantaopro.anexos_clinicos(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,entidade_tipo varchar(30) NOT NULL,entidade_id uuid NOT NULL,
 nome_original varchar(255) NOT NULL,nome_armazenado varchar(100) NOT NULL,mime_type varchar(100) NOT NULL,tamanho bigint NOT NULL,hash varchar(64) NOT NULL,
 storage_provider varchar(30) NOT NULL,storage_key varchar(500) NOT NULL,created_by uuid,created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_anexos_clinicos_entidade ON plantaopro.anexos_clinicos(tenant_id,entidade_tipo,entidade_id);
CREATE INDEX IF NOT EXISTS ix_anexos_clinicos_paciente ON plantaopro.anexos_clinicos(tenant_id,paciente_id,created_at DESC);
-- END v1.88.0 prontuario longitudinal

\ir schema/340_v189_clinical_operational_hardening.sql
