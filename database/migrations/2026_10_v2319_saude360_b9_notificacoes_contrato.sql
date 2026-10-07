-- ============================================================================
-- v2319 | B9 Saude 360 | notificacoes com contrato de instancia do codigo
-- ----------------------------------------------------------------------------
-- Problema: NotificacaoService.CriarNotificacaoAsync/ListarNotificacoesAsync/
-- MarcarLidaAsync e DashboardService (NotificacoesNaoLidas) leem/gravam em
-- plantaopro.notificacoes as colunas usuario_id, titulo, mensagem, tipo, lida,
-- created_by, updated_by, reg_date, reg_update e reg_status, mas a tabela
-- criada no schema base (030_operacao_plantoes.sql) conserva o layout generico
-- de catalogo (id/tenant_id/codigo/nome/status/dados/criado_em/atualizado_em).
-- Nenhuma migracao anterior adicionou essas colunas (v2069/v2070 repararam
-- apenas o agregado central novo plantaopro.notifications). Resultado: todo
-- fluxo que cria notificacao (escala aceita/confirmada/recusada/substituida/
-- realizada, pagamento gerado/confirmado/registrado/contestado/resolvido/
-- estornado/cancelado) falha com "column usuario_id does not exist" em bases
-- instaladas pelo object-catalog, e os contadores de dashboard tambem quebram.
-- Decisao: acrescentar as colunas faltantes de forma idempotente (mesmo
-- padrao de 345_v2069_reparar_base_notificacoes) e preservar as colunas
-- genericas por compatibilidade. Linhas de catalogo existentes (status
-- 'ATIVO', codigo/nome preenchidos) permanecem intactas: os caminhos de
-- notificacao filtram por usuario_id e reg_status='A' e usuario_id delas e
-- nulo, logo nao aparecem em listagens nem contagens.
-- ============================================================================
set search_path to plantaopro, public;

alter table plantaopro.notificacoes add column if not exists usuario_id uuid;
alter table plantaopro.notificacoes add column if not exists titulo varchar(160);
alter table plantaopro.notificacoes add column if not exists mensagem text;
alter table plantaopro.notificacoes add column if not exists tipo varchar(40);
alter table plantaopro.notificacoes add column if not exists lida boolean not null default false;
alter table plantaopro.notificacoes add column if not exists created_by uuid;
alter table plantaopro.notificacoes add column if not exists updated_by uuid;
alter table plantaopro.notificacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.notificacoes add column if not exists reg_update timestamptz;
alter table plantaopro.notificacoes add column if not exists reg_status char(1) not null default 'A' check (reg_status in ('A','I'));

create index if not exists idx_notificacoes_usuario_lida
    on plantaopro.notificacoes (usuario_id, lida);

comment on column plantaopro.notificacoes.usuario_id is
    'Destinatario da notificacao (NotificacaoService); linhas de catalogo permanecem com valor nulo.';
comment on column plantaopro.notificacoes.lida is
    'Estado de leitura da notificacao para o destinatario.';
