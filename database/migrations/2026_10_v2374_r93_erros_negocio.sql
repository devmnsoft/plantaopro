-- R93 (B5 observabilidade): centralização de erros de negócio + digest diário.
-- Antes desta rodada, as exceções de negócio conhecidas eram convertidas em 400/403/404
-- amigáveis pelo Adm360BusinessExceptionFilter, mas nenhum destino guardava as violações:
-- apenas falhas técnicas (HTTP 500) caíam em plantaopro.api_error_logs. Esta tabela recebe
-- um registro por erro de negócio mapeado pelo filtro (gravação best-effort, com timeout
-- curto e sem alterar a resposta) para que o ADMINISTRADOR_GLOBAL consulte o digest diário
-- em GET /api/observabilidade/digest-diario e o contador TotalErrosNegocioHoje em
-- GET /api/observabilidade/resumo.
-- Idempotente: create table/index if not exists (padrão v2372/v2373).
create table if not exists plantaopro.erros_negocio (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    usuario_id uuid,
    modulo varchar(80) not null,
    categoria varchar(20) not null check (categoria in ('REGRA_NEGOCIO','ACESSO_NEGADO','NAO_ENCONTRADO')),
    mensagem text not null,
    endpoint text,
    reg_date timestamptz not null default now()
);
create index if not exists ix_erros_negocio_dia on plantaopro.erros_negocio(reg_date desc);
create index if not exists ix_erros_negocio_tenant_dia on plantaopro.erros_negocio(tenant_id, reg_date desc);
