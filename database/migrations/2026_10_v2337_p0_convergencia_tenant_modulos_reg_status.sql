-- R5-P0: Convergencia de shape do tenant_modulos para linhagens legadas de instalacao.
-- Linhagens criadas antes do catalogo SaaS de modulos possuem plantaopro.tenant_modulos com o
-- shape antigo (codigo/dados/criado_em) sem as colunas canonicas; como as migrations subsequentes
-- usam create table if not exists, o indice unico parcial de
-- 2026_plantao_pro_self_service_white_label (tenant_id, lower(codigo_modulo) WHERE reg_status='A')
-- falha com 42703 ao referenciar colunas ausentes em bancos legados (medido no ensaio P0 sobre
-- clone do banco principal). Esta guarda replica exatamente o bloco de colunas da reconciliacao
-- canonica 2026_09_v2197 (mesmas colunas, mesmas nulabilidades), permitindo que a ordem canonica
-- seja aplicada por PlantaoPro.Tools.Database upgrade em qualquer linhagem sem reordenar o
-- manifesto nem remendo ad-hoc fora das migrations; a propria v2197 continua sendo a reconciliacao
-- de dados e constraints ao chegar sua vez no manifesto.
-- Aditivo e idempotente: add column if not exists e no-op onde a coluna ja existe.
alter table if exists plantaopro.tenant_modulos add column if not exists modulo_id uuid null;
alter table if exists plantaopro.tenant_modulos add column if not exists codigo text null;
alter table if exists plantaopro.tenant_modulos add column if not exists codigo_modulo text null;
alter table if exists plantaopro.tenant_modulos add column if not exists habilitado boolean null;
alter table if exists plantaopro.tenant_modulos add column if not exists origem text null;
alter table if exists plantaopro.tenant_modulos add column if not exists status text null;
alter table if exists plantaopro.tenant_modulos add column if not exists reg_status char(1) null;
alter table if exists plantaopro.tenant_modulos add column if not exists reg_date timestamptz null;
alter table if exists plantaopro.tenant_modulos add column if not exists reg_update timestamptz null;
alter table if exists plantaopro.tenant_modulos add column if not exists ativado_em timestamptz null;
alter table if exists plantaopro.tenant_modulos add column if not exists desativado_em timestamptz null;
alter table if exists plantaopro.tenant_modulos add column if not exists limite_contratado integer null;
alter table if exists plantaopro.tenant_modulos add column if not exists preco_contratado numeric(14,2) null;
alter table if exists plantaopro.tenant_modulos add column if not exists created_by uuid null;
alter table if exists plantaopro.tenant_modulos add column if not exists updated_by uuid null;
