-- ============================================================================
-- Migration: 2026_09_v2201_reconciliar_colunas_modulos_sistema_comercial.sql
-- Objetivo: Reconciliar bases instaladas pela cadeia oficial que nao receberam
--           as colunas comerciais de plantaopro.modulos_sistema definidas em
--           2026_v2149_saas_core_modulos_perfis_menus.sql (migracao isolada,
--           sem secao nos manifests de instalacao). Idempotente: somente
--           adiciona as colunas ausentes, com os tipos exatos do DDL original.
-- Causa: GET /api/portal-cliente/modulos/catalogo e o upsert de modulos do
--        AdminSaas consultam m.funcionalidades, m.categoria, m.essencial e
--        m.limite_padrao; bases sem elas produzem Npgsql 42703
--        "coluna m.funcionalidades nao existe".
-- Regras: preco_base permanece sob responsabilidade do v2.16.3 (nullable);
--        nenhuma coluna existente e alterada nem removida; historico preservado;
--        tambem reconcile a tabela plantaopro.tenant_modulos_historico
--        (criada somente pelo v2149): ToggleTenantAsync do SaaS insere
--        auditoria de habilitar/desabilitar-tenant e falha com 42P01 sem ela.
-- ============================================================================

alter table plantaopro.modulos_sistema add column if not exists categoria text not null default 'OPERACAO';
alter table plantaopro.modulos_sistema add column if not exists essencial boolean not null default false;
alter table plantaopro.modulos_sistema add column if not exists funcionalidades jsonb not null default '[]'::jsonb;
alter table plantaopro.modulos_sistema add column if not exists limite_padrao integer null;

create table if not exists plantaopro.tenant_modulos_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_modulo_id uuid not null,
    tenant_id uuid not null,
    modulo_id uuid not null,
    acao text not null,
    antes jsonb null,
    depois jsonb null,
    usuario_id uuid null,
    ip_origem text null,
    reg_date timestamptz not null default now()
);

create index if not exists ix_tenant_modulos_historico_tenant
    on plantaopro.tenant_modulos_historico(tenant_id, reg_date desc);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'fk_tenant_modulos_historico_modulo') then
        alter table plantaopro.tenant_modulos_historico add constraint fk_tenant_modulos_historico_modulo foreign key (modulo_id) references plantaopro.modulos_sistema(id);
    end if;
end $$;
