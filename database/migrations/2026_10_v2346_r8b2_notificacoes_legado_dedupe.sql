-- PlantãoPro v2.34.6 (R8-B2): idempotência do caminho legado de notificações em retries.
-- Achado E3 (r8a0-varredura-financas-pacientes-imports-falhas-notificacoes.md): o caminho legado
-- (NotificacaoService.CriarNotificacaoAsync -> plantaopro.notificacoes) inseria sem dedupe/unique;
-- cada chamada repetida do mesmo evento criava uma notificação duplicada para o usuário.
-- Adota a convenção já estabelecida pelo in-app operacional (Operation360 NotificationService:
-- origem_tipo/origem_id + on conflict do nothing + inserted==0 -> false).
-- Dedupe PARCIAL e intencional: só notificações COM origem declarada (origem_id não nulo) são
-- deduplicadas; chamadas sem origem mantêm o comportamento histórico (uma por chamada).
-- A chave inclui titulo para permitir a sequência natural de eventos distintos do MESMO registro
-- (ex.: escala confirmada -> cancelada); repetir o mesmo evento do mesmo registro para o mesmo
-- usuário vira no-op. O predicato reg_status='A' preserva o padrão de vínculo ativo da base.
-- Rerun idempotente: add column if not exists / create unique index if not exists.

alter table plantaopro.notificacoes
    add column if not exists origem_tipo text,
    add column if not exists origem_id uuid;

create unique index if not exists ux_notificacao_legado_origem
    on plantaopro.notificacoes(usuario_id, origem_tipo, origem_id, titulo)
    where reg_status='A' and origem_id is not null;
