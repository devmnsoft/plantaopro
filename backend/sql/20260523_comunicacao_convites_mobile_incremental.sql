create schema if not exists plantaopro;

create table if not exists plantaopro.conversas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    titulo varchar(180) not null,
    tipo varchar(40) not null,
    entidade varchar(40) null,
    entidade_id uuid null,
    status varchar(20) not null default 'ABERTA',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.conversa_participantes (
    id uuid primary key default gen_random_uuid(),
    conversa_id uuid not null,
    usuario_id uuid not null,
    papel varchar(30) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.mensagens (
    id uuid primary key default gen_random_uuid(),
    conversa_id uuid not null,
    remetente_usuario_id uuid not null,
    mensagem text not null,
    tipo varchar(30) not null default 'TEXTO',
    anexo_url text null,
    lida boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.mensagem_leituras (
    id uuid primary key default gen_random_uuid(),
    mensagem_id uuid not null,
    usuario_id uuid not null,
    lida_em timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.medico_disponibilidade (
    id uuid primary key default gen_random_uuid(),
    medico_id uuid not null,
    dia_semana smallint not null,
    hora_inicio time not null,
    hora_fim time not null,
    disponivel boolean not null default true,
    observacoes text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.medico_preferencias (
    id uuid primary key default gen_random_uuid(),
    medico_id uuid not null,
    valor_minimo numeric(12,2) null,
    turno_preferido varchar(20) null,
    aceita_noturno boolean not null default true,
    aceita_fim_semana boolean not null default true,
    raio_deslocamento_km integer null,
    observacoes text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.plantao_convites (
    id uuid primary key default gen_random_uuid(),
    plantao_id uuid not null,
    medico_id uuid not null,
    usuario_id uuid not null,
    status varchar(20) not null default 'ENVIADO',
    mensagem text null,
    data_envio timestamp not null default now(),
    data_resposta timestamp null,
    motivo_recusa text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create index if not exists idx_conversa_participantes_usuario on plantaopro.conversa_participantes(usuario_id,conversa_id);
create index if not exists idx_mensagens_conversa_data on plantaopro.mensagens(conversa_id,reg_date desc);
create index if not exists idx_convites_plantao on plantaopro.plantao_convites(plantao_id,status);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_conversa_participantes_conversa') THEN
        ALTER TABLE plantaopro.conversa_participantes
            ADD CONSTRAINT fk_conversa_participantes_conversa
            FOREIGN KEY (conversa_id) REFERENCES plantaopro.conversas(id);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_mensagens_conversa') THEN
        ALTER TABLE plantaopro.mensagens
            ADD CONSTRAINT fk_mensagens_conversa
            FOREIGN KEY (conversa_id) REFERENCES plantaopro.conversas(id);
    END IF;
END $$;
