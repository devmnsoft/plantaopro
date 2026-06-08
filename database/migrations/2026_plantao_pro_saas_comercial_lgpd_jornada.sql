create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.clientes (
    id uuid primary key default gen_random_uuid(),
    razao_social varchar(200) not null,
    nome_fantasia varchar(200) null,
    cnpj varchar(20) null,
    status varchar(40) not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.clientes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.clientes add column if not exists reg_status char(1) not null default 'A';
alter table plantaopro.clientes add column if not exists reg_date timestamp not null default now();
alter table plantaopro.clientes add column if not exists reg_update timestamp null;

create table if not exists plantaopro.planos (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    descricao text null,
    valor_mensal numeric(12,2) not null default 0,
    limite_medicos int not null default 0,
    limite_hospitais int not null default 0,
    limite_plantoes_mes int not null default 0,
    limite_usuarios int not null default 0,
    limite_convites_mes int not null default 0,
    permite_mobile boolean not null default false,
    permite_bi boolean not null default false,
    permite_relatorios_avancados boolean not null default false,
    permite_integracoes boolean not null default false,
    permite_operacao_assistida boolean not null default false,
    permite_suporte_prioritario boolean not null default false,
    status varchar(40) not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.planos add column if not exists descricao text null;
alter table plantaopro.planos add column if not exists valor_mensal numeric(12,2) not null default 0;
alter table plantaopro.planos add column if not exists limite_medicos int not null default 0;
alter table plantaopro.planos add column if not exists limite_hospitais int not null default 0;
alter table plantaopro.planos add column if not exists limite_plantoes_mes int not null default 0;
alter table plantaopro.planos add column if not exists limite_usuarios int not null default 0;
alter table plantaopro.planos add column if not exists limite_convites_mes int not null default 0;
alter table plantaopro.planos add column if not exists permite_mobile boolean not null default false;
alter table plantaopro.planos add column if not exists permite_bi boolean not null default false;
alter table plantaopro.planos add column if not exists permite_relatorios_avancados boolean not null default false;
alter table plantaopro.planos add column if not exists permite_integracoes boolean not null default false;
alter table plantaopro.planos add column if not exists permite_operacao_assistida boolean not null default false;
alter table plantaopro.planos add column if not exists permite_suporte_prioritario boolean not null default false;
alter table plantaopro.planos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.planos add column if not exists reg_status char(1) not null default 'A';
alter table plantaopro.planos add column if not exists reg_date timestamp not null default now();
alter table plantaopro.planos add column if not exists reg_update timestamp null;

create table if not exists plantaopro.plano_recursos (
    id uuid primary key default gen_random_uuid(),
    plano_id uuid not null,
    recurso varchar(80) not null,
    habilitado boolean not null default true,
    limite int null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.assinaturas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    plano_id uuid not null,
    status varchar(40) not null default 'ATIVA',
    data_inicio date not null default current_date,
    data_fim date null,
    data_trial_fim date null,
    valor_contratado numeric(12,2) not null default 0,
    dia_vencimento int not null default 10,
    periodicidade varchar(30) not null default 'MENSAL',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.assinaturas add column if not exists data_trial_fim date null;
alter table plantaopro.assinaturas add column if not exists valor_contratado numeric(12,2) not null default 0;
alter table plantaopro.assinaturas add column if not exists dia_vencimento int not null default 10;
alter table plantaopro.assinaturas add column if not exists periodicidade varchar(30) not null default 'MENSAL';
alter table plantaopro.assinaturas add column if not exists reg_update timestamp null;

create table if not exists plantaopro.assinatura_historico (
    id uuid primary key default gen_random_uuid(),
    assinatura_id uuid null,
    cliente_id uuid not null,
    plano_id uuid null,
    status_anterior varchar(40) null,
    status_novo varchar(40) not null,
    motivo text null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.assinatura_uso (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    assinatura_id uuid null,
    recurso varchar(80) not null,
    quantidade int not null default 0,
    competencia date not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.faturas_saas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    assinatura_id uuid null,
    competencia date not null,
    vencimento date not null,
    valor_total numeric(12,2) not null default 0,
    status varchar(40) not null default 'ABERTA',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.fatura_itens (
    id uuid primary key default gen_random_uuid(),
    fatura_id uuid not null,
    descricao varchar(220) not null,
    quantidade int not null default 1,
    valor_unitario numeric(12,2) not null default 0,
    valor_total numeric(12,2) not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.pagamentos_saas (
    id uuid primary key default gen_random_uuid(),
    fatura_id uuid not null,
    cliente_id uuid not null,
    valor_pago numeric(12,2) not null default 0,
    data_pagamento timestamp null,
    metodo varchar(60) null,
    status varchar(40) not null default 'PENDENTE',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.cobranca_eventos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    fatura_id uuid null,
    tipo varchar(80) not null,
    mensagem text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.cliente_bloqueios (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    motivo text not null,
    origem varchar(80) null,
    resolvido boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.cliente_bloqueios add column if not exists resolvido boolean not null default false;

create table if not exists plantaopro.cliente_alertas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    severidade varchar(20) not null default 'MEDIA',
    titulo varchar(160) not null,
    mensagem text not null,
    resolvido boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.cliente_limites_uso (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    recurso varchar(80) not null,
    limite int not null default 0,
    utilizado int not null default 0,
    competencia date not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.cliente_saude_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    score int not null,
    classificacao varchar(30) not null,
    riscos text null,
    oportunidades text null,
    acoes text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.jornada_cliente (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    etapa varchar(60) not null default 'LEAD_CADASTRADO',
    responsavel varchar(160) null,
    proxima_acao text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.jornada_cliente add column if not exists proxima_acao text null;
alter table plantaopro.jornada_cliente add column if not exists reg_update timestamp null;

create table if not exists plantaopro.jornada_cliente_eventos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    resumo text not null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.jornada_cliente_tarefas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    titulo varchar(220) not null,
    responsavel varchar(160) null,
    status varchar(40) not null default 'PENDENTE',
    vencimento timestamp null,
    tipo varchar(80) null,
    concluida_em timestamp null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.jornada_cliente_tarefas add column if not exists tipo varchar(80) null;
alter table plantaopro.jornada_cliente_tarefas add column if not exists concluida_em timestamp null;
alter table plantaopro.jornada_cliente_tarefas add column if not exists reg_update timestamp null;

create table if not exists plantaopro.jornada_cliente_observacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    observacao text not null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.jornada_cliente_responsaveis (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    usuario_id uuid null,
    nome varchar(160) not null,
    papel varchar(80) not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_leads (
    id uuid primary key default gen_random_uuid(),
    nome varchar(180) not null,
    email varchar(180) not null,
    telefone varchar(60) null,
    empresa varchar(180) null,
    status varchar(40) not null default 'NOVO',
    plano_recomendado varchar(80) null,
    medicos_desejados int not null default 0,
    hospitais_desejados int not null default 0,
    plantoes_mes int not null default 0,
    precisa_mobile boolean not null default false,
    precisa_bi boolean not null default false,
    suporte_prioritario boolean not null default false,
    operacao_assistida boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.comercial_leads add column if not exists medicos_desejados int not null default 0;
alter table plantaopro.comercial_leads add column if not exists hospitais_desejados int not null default 0;
alter table plantaopro.comercial_leads add column if not exists plantoes_mes int not null default 0;
alter table plantaopro.comercial_leads add column if not exists precisa_mobile boolean not null default false;
alter table plantaopro.comercial_leads add column if not exists precisa_bi boolean not null default false;
alter table plantaopro.comercial_leads add column if not exists suporte_prioritario boolean not null default false;
alter table plantaopro.comercial_leads add column if not exists operacao_assistida boolean not null default false;

create table if not exists plantaopro.comercial_oportunidades (
    id uuid primary key default gen_random_uuid(),
    lead_id uuid null,
    cliente_id uuid null,
    nome varchar(180) not null,
    etapa varchar(60) not null default 'ABERTA',
    valor_estimado numeric(12,2) not null default 0,
    plano_recomendado varchar(80) null,
    motivo_perda text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.comercial_oportunidades add column if not exists cliente_id uuid null;

create table if not exists plantaopro.comercial_propostas (
    id uuid primary key default gen_random_uuid(),
    oportunidade_id uuid not null,
    numero varchar(60) not null,
    valor_total numeric(12,2) not null default 0,
    desconto_percentual numeric(5,2) not null default 0,
    validade date not null,
    status varchar(40) not null default 'RASCUNHO',
    enviada_em timestamp null,
    aprovada_em timestamp null,
    recusada_em timestamp null,
    motivo_recusa text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.comercial_proposta_itens (
    id uuid primary key default gen_random_uuid(),
    proposta_id uuid not null,
    descricao varchar(220) not null,
    quantidade int not null default 1,
    valor_unitario numeric(12,2) not null default 0,
    valor_total numeric(12,2) not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_interacoes (
    id uuid primary key default gen_random_uuid(),
    lead_id uuid null,
    oportunidade_id uuid null,
    tipo varchar(80) not null,
    resumo text not null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_motivos_perda (
    id uuid primary key default gen_random_uuid(),
    motivo varchar(160) not null,
    ativo boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_regras_desconto (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null,
    percentual_maximo numeric(5,2) not null,
    exige_aprovacao boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.customer_success_interacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    resumo text not null,
    usuario_id uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.customer_success_planos_acao (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    titulo varchar(180) not null,
    objetivo text not null,
    status varchar(40) not null default 'ABERTO',
    responsavel varchar(160) null,
    vencimento date null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.customer_success_riscos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    tipo varchar(80) not null,
    severidade varchar(20) not null,
    descricao text not null,
    status varchar(40) not null default 'ABERTO',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.customer_success_tarefas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    titulo varchar(180) not null,
    status varchar(40) not null default 'PENDENTE',
    responsavel varchar(160) null,
    vencimento date null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.lgpd_politicas (
    id uuid primary key default gen_random_uuid(),
    versao varchar(30) not null,
    titulo varchar(200) not null,
    conteudo text not null,
    publicada boolean not null default false,
    vigente_desde timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.lgpd_bases_legais (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    descricao text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.lgpd_consentimentos (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    finalidade varchar(160) not null,
    base_legal varchar(120) not null,
    versao_politica varchar(30) not null,
    consentido boolean not null default true,
    ip varchar(80) null,
    user_agent text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

alter table plantaopro.lgpd_consentimentos add column if not exists cliente_id uuid null;

create table if not exists plantaopro.lgpd_solicitacoes_titular (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    tipo varchar(40) not null,
    status varchar(40) not null default 'ABERTA',
    descricao text not null,
    resposta text null,
    respondida_por uuid null,
    respondida_em timestamp null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.lgpd_eventos_privacidade (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    acao varchar(120) not null,
    detalhes text null,
    ip varchar(80) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

alter table plantaopro.lgpd_eventos_privacidade add column if not exists cliente_id uuid null;

create table if not exists plantaopro.lgpd_retencao_dados (
    id uuid primary key default gen_random_uuid(),
    categoria varchar(120) not null,
    base_legal varchar(120) not null,
    prazo varchar(120) not null,
    regra text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.lgpd_exportacoes_dados (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    status varchar(40) not null,
    ip varchar(80) null,
    arquivo_url text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.lgpd_anonimizacoes (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid not null,
    motivo text not null,
    status varchar(40) not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.ajuda_topicos (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null default 'TODOS',
    titulo varchar(180) not null,
    descricao text null,
    ordem int not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.ajuda_artigos (
    id uuid primary key default gen_random_uuid(),
    topico_id uuid null,
    perfil varchar(80) not null default 'TODOS',
    titulo varchar(220) not null,
    conteudo text not null,
    link_acao text null,
    ordem int not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.ajuda_passos (
    id uuid primary key default gen_random_uuid(),
    artigo_id uuid not null,
    ordem int not null,
    titulo varchar(180) not null,
    descricao text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.ajuda_checklists (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null,
    titulo varchar(180) not null,
    concluido boolean not null default false,
    ordem int not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.ajuda_feedbacks (
    id uuid primary key default gen_random_uuid(),
    artigo_id uuid not null,
    usuario_id uuid null,
    util boolean not null,
    comentario text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.eventos_sistema (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    tipo varchar(120) not null,
    entidade varchar(120) null,
    entidade_id uuid null,
    mensagem text null,
    correlation_id varchar(120) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.logs_operacionais (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    perfil varchar(80) null,
    acao varchar(120) not null,
    entidade varchar(120) null,
    entidade_id uuid null,
    ip varchar(80) null,
    user_agent text null,
    sucesso boolean not null default true,
    mensagem text null,
    dados_antes text null,
    dados_depois text null,
    correlation_id varchar(120) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.auditoria_eventos (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    perfil varchar(80) null,
    acao varchar(120) not null,
    entidade varchar(120) not null,
    entidade_id uuid null,
    ip varchar(80) null,
    user_agent text null,
    sucesso boolean not null default true,
    mensagem text null,
    dados_antes text null,
    dados_depois text null,
    correlation_id varchar(120) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.auditoria_lgpd_eventos (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    acao varchar(120) not null,
    finalidade varchar(160) null,
    base_legal varchar(120) null,
    ip varchar(80) null,
    sucesso boolean not null default true,
    mensagem text null,
    correlation_id varchar(120) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create index if not exists ix_clientes_status on plantaopro.clientes(status);
create index if not exists ix_clientes_reg_date on plantaopro.clientes(reg_date);
create index if not exists ix_planos_status on plantaopro.planos(status);
create index if not exists ix_assinaturas_cliente_status on plantaopro.assinaturas(cliente_id, status);
create index if not exists ix_assinaturas_reg_date on plantaopro.assinaturas(reg_date);
create index if not exists ix_assinatura_uso_cliente_competencia on plantaopro.assinatura_uso(cliente_id, competencia);
create index if not exists ix_faturas_saas_cliente_status on plantaopro.faturas_saas(cliente_id, status);
create index if not exists ix_faturas_saas_vencimento on plantaopro.faturas_saas(vencimento);
create index if not exists ix_pagamentos_saas_cliente_status on plantaopro.pagamentos_saas(cliente_id, status);
create index if not exists ix_cobranca_eventos_cliente_tipo on plantaopro.cobranca_eventos(cliente_id, tipo);
create index if not exists ix_cliente_bloqueios_cliente_tipo on plantaopro.cliente_bloqueios(cliente_id, tipo);
create index if not exists ix_cliente_alertas_cliente_tipo on plantaopro.cliente_alertas(cliente_id, tipo);
create index if not exists ix_cliente_limites_uso_cliente on plantaopro.cliente_limites_uso(cliente_id, recurso, competencia);
create index if not exists ix_cliente_saude_historico_cliente on plantaopro.cliente_saude_historico(cliente_id, reg_date);
create unique index if not exists ux_jornada_cliente_cliente on plantaopro.jornada_cliente(cliente_id);
create index if not exists ix_jornada_cliente_etapa on plantaopro.jornada_cliente(etapa);
create index if not exists ix_jornada_cliente_eventos_cliente_tipo on plantaopro.jornada_cliente_eventos(cliente_id, tipo);
create index if not exists ix_jornada_cliente_tarefas_cliente_status on plantaopro.jornada_cliente_tarefas(cliente_id, status);
create index if not exists ix_jornada_cliente_tarefas_vencimento on plantaopro.jornada_cliente_tarefas(vencimento);
create index if not exists ix_comercial_leads_status on plantaopro.comercial_leads(status);
create index if not exists ix_comercial_leads_reg_date on plantaopro.comercial_leads(reg_date);
create index if not exists ix_comercial_oportunidades_etapa on plantaopro.comercial_oportunidades(etapa);
create index if not exists ix_comercial_oportunidades_reg_date on plantaopro.comercial_oportunidades(reg_date);
create index if not exists ix_comercial_propostas_status on plantaopro.comercial_propostas(status);
create index if not exists ix_comercial_propostas_validade on plantaopro.comercial_propostas(validade);
create index if not exists ix_customer_success_riscos_cliente_status on plantaopro.customer_success_riscos(cliente_id, status);
create index if not exists ix_customer_success_tarefas_cliente_status on plantaopro.customer_success_tarefas(cliente_id, status);
create index if not exists ix_lgpd_consentimentos_usuario on plantaopro.lgpd_consentimentos(usuario_id, finalidade);
create index if not exists ix_lgpd_solicitacoes_usuario_status on plantaopro.lgpd_solicitacoes_titular(usuario_id, status);
create index if not exists ix_lgpd_eventos_usuario on plantaopro.lgpd_eventos_privacidade(usuario_id, reg_date);
create index if not exists ix_ajuda_artigos_perfil on plantaopro.ajuda_artigos(perfil);
create index if not exists ix_eventos_sistema_cliente_tipo on plantaopro.eventos_sistema(cliente_id, tipo);
create index if not exists ix_logs_operacionais_cliente_acao on plantaopro.logs_operacionais(cliente_id, acao);
create index if not exists ix_auditoria_eventos_cliente_acao on plantaopro.auditoria_eventos(cliente_id, acao);
create index if not exists ix_auditoria_lgpd_eventos_cliente_acao on plantaopro.auditoria_lgpd_eventos(cliente_id, acao);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_planos_valor_mensal_nao_negativo') THEN
        ALTER TABLE plantaopro.planos ADD CONSTRAINT ck_planos_valor_mensal_nao_negativo CHECK (valor_mensal >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_comercial_propostas_desconto_percentual') THEN
        ALTER TABLE plantaopro.comercial_propostas ADD CONSTRAINT ck_comercial_propostas_desconto_percentual CHECK (desconto_percentual >= 0 AND desconto_percentual <= 100);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_faturas_saas_valor_total_nao_negativo') THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_valor_total_nao_negativo CHECK (valor_total >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_pagamentos_saas_valor_pago_nao_negativo') THEN
        ALTER TABLE plantaopro.pagamentos_saas ADD CONSTRAINT ck_pagamentos_saas_valor_pago_nao_negativo CHECK (valor_pago >= 0);
    END IF;
END $$;

insert into plantaopro.lgpd_bases_legais(nome, descricao, reg_status, reg_date)
select base.nome, base.descricao, 'A', now()
from (values
    ('Execução de contrato','Tratamento necessário para execução dos serviços contratados.'),
    ('Cumprimento de obrigação legal','Tratamento necessário para obrigações legais e regulatórias.'),
    ('Legítimo interesse','Tratamento necessário para segurança, auditoria e melhoria operacional.'),
    ('Consentimento','Tratamento autorizado pelo titular para finalidade específica.'),
    ('Exercício regular de direitos','Tratamento necessário para exercício regular de direitos em processos administrativos ou judiciais.')
) as base(nome, descricao)
where not exists (select 1 from plantaopro.lgpd_bases_legais b where b.nome = base.nome);
