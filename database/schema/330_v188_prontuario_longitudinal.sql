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
