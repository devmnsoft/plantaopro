set search_path to plantaopro, public;

create table if not exists plantaopro.paineis_publicos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null references plantaopro.clientes(id),
    unidade_id uuid not null,
    nome varchar(120) not null,
    logotipo_url varchar(500),
    cor_primaria varchar(9) not null default '#155EEF',
    ativo boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz
);

create table if not exists plantaopro.painel_publico_tokens (
    id uuid primary key default gen_random_uuid(),
    painel_id uuid not null references plantaopro.paineis_publicos(id) on delete cascade,
    cliente_id uuid not null references plantaopro.clientes(id),
    token_hash char(64) not null,
    expira_em timestamptz not null,
    revogado_em timestamptz,
    ultima_utilizacao_em timestamptz,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    constraint ck_painel_token_hash_sha256 check (token_hash ~ '^[0-9a-f]{64}$'),
    constraint uq_painel_token_hash unique (token_hash)
);

create index if not exists ix_paineis_publicos_escopo on plantaopro.paineis_publicos(cliente_id, unidade_id) where reg_status='A' and ativo;
create index if not exists ix_painel_tokens_validade on plantaopro.painel_publico_tokens(painel_id, expira_em) where reg_status='A' and revogado_em is null;
