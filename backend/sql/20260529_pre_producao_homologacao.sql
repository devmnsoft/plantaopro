create schema if not exists plantaopro;

create table if not exists plantaopro.plantao_historico (
    id uuid primary key default gen_random_uuid(),
    plantao_id uuid not null,
    status_anterior varchar(50),
    status_novo varchar(50) not null,
    justificativa text,
    usuario_id uuid,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.medico_recomendacao_log (
    id uuid primary key default gen_random_uuid(),
    plantao_id uuid not null,
    medico_id uuid not null,
    score numeric(10,2) not null default 0,
    motivos text,
    alertas text,
    usuario_id uuid,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.alertas_operacionais (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid,
    tipo varchar(80) not null,
    titulo varchar(180) not null,
    descricao text,
    severidade varchar(30) not null default 'MEDIA',
    status varchar(30) not null default 'ABERTO',
    referencia_tipo varchar(80),
    referencia_id uuid,
    created_by uuid,
    updated_by uuid,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone,
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.alerta_operacional_historico (
    id uuid primary key default gen_random_uuid(),
    alerta_id uuid not null,
    status_anterior varchar(30),
    status_novo varchar(30) not null,
    observacao text,
    usuario_id uuid,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.pagamento_historico (
    id uuid primary key default gen_random_uuid(),
    pagamento_id uuid not null,
    status_anterior varchar(50),
    status_novo varchar(50) not null,
    observacao text,
    usuario_id uuid,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.pagamento_contestacoes (
    id uuid primary key default gen_random_uuid(),
    pagamento_id uuid not null,
    medico_id uuid not null,
    motivo text not null,
    resposta text,
    status varchar(30) not null default 'ABERTA',
    aberta_em timestamp without time zone not null default now(),
    resolvida_em timestamp without time zone,
    created_by uuid,
    updated_by uuid,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone,
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.notificacoes_envio_log (
    id uuid primary key default gen_random_uuid(),
    notificacao_id uuid,
    usuario_id uuid,
    canal varchar(40) not null default 'IN_APP',
    status varchar(40) not null default 'ENVIADO',
    detalhe text,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.comunicacao_contextos (
    id uuid primary key default gen_random_uuid(),
    tipo varchar(40) not null,
    referencia_id uuid not null,
    titulo varchar(180) not null,
    cliente_id uuid,
    hospital_id uuid,
    medico_id uuid,
    status varchar(30) not null default 'ABERTO',
    created_by uuid,
    updated_by uuid,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone,
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.auditoria_acoes_criticas (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid,
    acao varchar(120) not null,
    entidade varchar(120) not null,
    registro_id uuid,
    cliente_id uuid,
    ip varchar(80),
    user_agent text,
    descricao text,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.exportacao_logs (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid,
    cliente_id uuid,
    tipo varchar(80) not null,
    filtros text,
    status varchar(40) not null default 'GERADO',
    total_registros bigint not null default 0,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

create index if not exists idx_plantao_historico_plantao on plantaopro.plantao_historico(plantao_id, reg_date desc);
create index if not exists idx_medico_recomendacao_log_plantao on plantaopro.medico_recomendacao_log(plantao_id, score desc);
create index if not exists idx_alertas_operacionais_status on plantaopro.alertas_operacionais(status, severidade, reg_date desc);
create index if not exists idx_pagamento_contestacoes_pagamento on plantaopro.pagamento_contestacoes(pagamento_id, status);
create index if not exists idx_comunicacao_contextos_ref on plantaopro.comunicacao_contextos(tipo, referencia_id);
create index if not exists idx_exportacao_logs_usuario on plantaopro.exportacao_logs(usuario_id, reg_date desc);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_pagamento_contestacoes_status'
          AND conrelid = 'plantaopro.pagamento_contestacoes'::regclass
    ) THEN
        ALTER TABLE plantaopro.pagamento_contestacoes
        ADD CONSTRAINT ck_pagamento_contestacoes_status
        CHECK (status in ('ABERTA','EM_ANALISE','RESOLVIDA','CANCELADA'));
    END IF;
END $$;
