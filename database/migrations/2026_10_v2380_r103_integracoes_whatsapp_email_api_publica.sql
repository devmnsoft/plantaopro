-- PlantaoPro v2.37.0 (R103 - integracoes: WhatsApp opt-in, e-mail transacional e API publica com chaves):
-- a rodada fecha os tres pilares da linha 98 do plano num arquivo idempotente transacional.
--
-- Parte 1 (WhatsApp/e-mail): caixa de saida unica (mensagens_saida) que registra cada envio
-- nos dois canais com o resultado do opt-in do paciente (consentimento_lgpd + canal). O provedor
-- padrao e LOCAL (registro deterministico, sem segredos em config); outros provedores entram por
-- Integracoes:MensageriaProvider sem DDL.
create table if not exists plantaopro.mensagens_saida (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    canal varchar(20) not null,
    destino text not null default '',
    tipo_evento varchar(60) not null default '',
    assunto text null,
    mensagem text not null default '',
    status varchar(30) not null default 'ENVIADA',
    provider varchar(20) not null default 'LOCAL',
    detalhe text null,
    criado_por uuid null,
    criado_em timestamptz not null default now(),
    processado_em timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A',
    constraint ck_mensagens_saida_canal check (canal in ('WHATSAPP', 'EMAIL')),
    constraint ck_mensagens_saida_status check (status in ('ENVIADA', 'FALHOU', 'BLOQUEADA_SEM_CONSENTIMENTO', 'SEM_DESTINO'))
);
create index if not exists ix_mensagens_saida_tenant_criado on plantaopro.mensagens_saida(tenant_id, criado_em desc);
create index if not exists ix_mensagens_saida_paciente on plantaopro.mensagens_saida(paciente_id);
create index if not exists ix_mensagens_saida_canal_status on plantaopro.mensagens_saida(canal, status);

-- Parte 2 (API publica com chaves): trilha de uso por chave (base dos logs do portal do
-- desenvolvedor e das janelas de rate limit). Chave/limites ja existem desde o lancamento B2B
-- (api_chaves + api_rate_limits); a trilha era ausente - o metodo ListarApiKeyLogsAsync respondia
-- sempre lista vazia.
create table if not exists plantaopro.api_chaves_usos (
    id uuid primary key default gen_random_uuid(),
    chave_id uuid not null,
    tenant_id uuid null,
    cliente_id uuid null,
    metodo varchar(10) not null default 'GET',
    rota text not null default '',
    status_http integer not null default 0,
    ip_origem text null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_api_chaves_usos_chave on plantaopro.api_chaves_usos(chave_id, reg_date desc);
create index if not exists ix_api_chaves_usos_tenant on plantaopro.api_chaves_usos(tenant_id, reg_date desc);

-- Correcao de estado (achado R103): a criacao de chaves no servico B2B insere em
-- api_rate_limits as colunas api_chave_id/limite_minuto/limite_dia, que nunca existiram no banco
-- vivo (o insert falharia em runtime). As colunas entram aqui, com defaults dos limites de casa
-- (120/min e 10000/dia), guardadas por add column if not exists. O campo ultimo_uso_em, que a
-- autenticacao por chave atualiza em todo uso valido, tambem era ausente no vivo e entra junto.
alter table plantaopro.api_rate_limits add column if not exists api_chave_id uuid null;
alter table plantaopro.api_rate_limits add column if not exists limite_minuto integer not null default 120;
alter table plantaopro.api_rate_limits add column if not exists limite_dia integer not null default 10000;
alter table plantaopro.api_chaves add column if not exists ultimo_uso_em timestamptz null;
create index if not exists ix_api_rate_limits_chave on plantaopro.api_rate_limits(api_chave_id) where api_chave_id is not null;
