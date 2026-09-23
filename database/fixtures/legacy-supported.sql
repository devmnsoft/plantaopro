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

-- Dados reais do dominio usados para provar preservacao no upgrade (nao apenas
-- uma tabela auxiliar de marcadores).
insert into plantaopro.perfis(id, nome, reg_status) values
('10000000-0000-0000-0000-000000000001', 'Perfil legado preservado', 'A')
on conflict (id) do nothing;
insert into plantaopro.usuarios(id, nome, email, senha_hash, reg_status) values
('20000000-0000-0000-0000-000000000001', 'Usuario legado preservado', 'legado.preservado@example.invalid', 'HASH_LEGADO_NAO_AUTENTICAVEL', 'A')
on conflict (id) do nothing;
insert into plantaopro.usuarios_perfis(id, usuario_id, perfil_id, reg_status) values
('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'A')
on conflict (id) do nothing;
insert into plantaopro.plantoes(id, data_inicio, data_fim, valor, vagas, tipo, status, reg_status) values
('40000000-0000-0000-0000-000000000001', timestamp '2026-01-10 08:00:00', timestamp '2026-01-10 20:00:00', 1575.50, 2, 'LEGADO', 'ABERTO', 'A')
on conflict (id) do nothing;
