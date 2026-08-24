-- Última estrutura legada suportada pelo caminho oficial de upgrade.
-- Esta fixture é intencionalmente mínima: o script completo é responsável
-- por evoluí-la até o catálogo canônico atual.
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.perfis (
    id uuid primary key default gen_random_uuid(), nome text, reg_status char(1) default 'A');
create table if not exists plantaopro.usuarios (
    id uuid primary key default gen_random_uuid(), nome text, email text, senha_hash text, reg_status char(1) default 'A');
create table if not exists plantaopro.usuarios_perfis (
    id uuid primary key default gen_random_uuid(), usuario_id uuid, perfil_id uuid, reg_status char(1) default 'A');
create table if not exists plantaopro.permissoes (
    id uuid primary key default gen_random_uuid(), nome text, modulo text, acao text, reg_status char(1) default 'A');
create table if not exists plantaopro.perfis_permissoes (
    id uuid primary key default gen_random_uuid(), perfil_id uuid, permissao_id uuid, reg_status char(1) default 'A');
create table if not exists plantaopro.planos (
    id uuid primary key default gen_random_uuid(), nome text);
create table if not exists plantaopro.clientes (
    id uuid primary key default gen_random_uuid(), nome_fantasia text);
create table if not exists plantaopro.assinaturas (
    id uuid primary key default gen_random_uuid(), cliente_id uuid, plano_id uuid);

-- Plantões pertence ao núcleo mínimo da última versão legada suportada. As
-- migrações posteriores dependem deste agregado operacional já existir.
create table if not exists plantaopro.plantoes (
    id uuid primary key default gen_random_uuid(), hospital_id uuid,
    especialidade_id uuid, data_inicio timestamp, data_fim timestamp,
    valor numeric(12,2) default 0, vagas integer default 1, tipo text,
    status text default 'ABERTO', reg_status char(1) default 'A');
