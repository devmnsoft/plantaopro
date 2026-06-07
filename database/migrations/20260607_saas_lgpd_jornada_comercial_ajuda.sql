create schema if not exists plantaopro;
create extension if not exists pgcrypto;

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
    acao varchar(120) not null,
    detalhes text null,
    ip varchar(80) null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

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

create table if not exists plantaopro.jornada_cliente (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    etapa varchar(60) not null,
    responsavel varchar(160) null,
    proxima_acao text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

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
    concluida_em timestamp null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

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
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.comercial_oportunidades (
    id uuid primary key default gen_random_uuid(),
    lead_id uuid null,
    nome varchar(180) not null,
    etapa varchar(60) not null,
    valor_estimado numeric(12,2) not null default 0,
    plano_recomendado varchar(80) null,
    motivo_perda text null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create table if not exists plantaopro.comercial_propostas (
    id uuid primary key default gen_random_uuid(),
    oportunidade_id uuid not null,
    numero varchar(60) not null,
    valor_total numeric(12,2) not null,
    desconto_percentual numeric(5,2) not null default 0,
    validade date not null,
    status varchar(40) not null default 'RASCUNHO',
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
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_interacoes (
    id uuid primary key default gen_random_uuid(),
    oportunidade_id uuid null,
    lead_id uuid null,
    tipo varchar(80) not null,
    resumo text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_motivos_perda (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.comercial_regras_desconto (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null,
    limite_percentual numeric(5,2) not null,
    exige_aprovacao boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.ajuda_topicos (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null,
    titulo varchar(180) not null,
    descricao text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.ajuda_artigos (
    id uuid primary key default gen_random_uuid(),
    topico_id uuid not null,
    perfil varchar(80) not null,
    titulo varchar(180) not null,
    conteudo text not null,
    link_acao varchar(220) null,
    ordem int not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
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
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
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

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_jornada_cliente_cliente') THEN
        ALTER TABLE plantaopro.jornada_cliente ADD CONSTRAINT uq_jornada_cliente_cliente UNIQUE (cliente_id);
    END IF;
END $$;

insert into plantaopro.lgpd_bases_legais(nome, descricao)
select x.nome, x.descricao from (values
('Execução de contrato','Necessária para prestação do serviço PlantãoPro.'),
('Cumprimento de obrigação legal','Necessária para retenções e obrigações regulatórias.'),
('Legítimo interesse','Segurança, auditoria e melhoria do serviço.'),
('Consentimento','Quando a manifestação livre do titular é aplicável.'),
('Exercício regular de direitos','Defesa em processos administrativos, judiciais ou arbitrais.')
) as x(nome, descricao) where not exists (select 1 from plantaopro.lgpd_bases_legais b where b.nome=x.nome);

insert into plantaopro.lgpd_politicas(versao,titulo,conteudo,publicada,vigente_desde)
select '2026.06','Política de Privacidade PlantãoPro','Finalidades: gestão de plantões, financeiro, comunicação operacional, auditoria e segurança, suporte, faturamento SaaS, Customer Success e cumprimento legal/regulatório.',true,now()
where not exists (select 1 from plantaopro.lgpd_politicas where versao='2026.06');

insert into plantaopro.lgpd_retencao_dados(categoria,base_legal,prazo,regra)
select x.categoria,x.base_legal,x.prazo,x.regra from (values
('Auditoria','Legítimo interesse','Conforme necessidade de rastreabilidade','Não apagar indevidamente; usar minimização e mascaramento.'),
('Financeiro','Cumprimento de obrigação legal','Conforme legislação aplicável','Reter faturas, pagamentos e evidências.'),
('Suporte','Execução de contrato','Enquanto necessário ao atendimento','Revisar e anonimizar quando permitido.')
) as x(categoria,base_legal,prazo,regra) where not exists (select 1 from plantaopro.lgpd_retencao_dados r where r.categoria=x.categoria);

insert into plantaopro.comercial_regras_desconto(perfil,limite_percentual,exige_aprovacao)
select 'ADMINISTRADOR',15,true where not exists (select 1 from plantaopro.comercial_regras_desconto where perfil='ADMINISTRADOR');
insert into plantaopro.comercial_regras_desconto(perfil,limite_percentual,exige_aprovacao)
select 'ADMINISTRADOR_GLOBAL',100,false where not exists (select 1 from plantaopro.comercial_regras_desconto where perfil='ADMINISTRADOR_GLOBAL');

with topicos as (
    insert into plantaopro.ajuda_topicos(id,perfil,titulo,descricao)
    select gen_random_uuid(), x.perfil, x.titulo, x.descricao from (values
    ('ADMINISTRADOR_GLOBAL','Administração SaaS','Clientes, planos, assinaturas, faturas, risco e inteligência.'),
    ('COORDENACAO','Operação de plantões','Criação, publicação, convites, escalas e central.'),
    ('MEDICO','Área do médico','Plantões disponíveis, convites, agenda, pagamentos e disponibilidade.'),
    ('FINANCEIRO','Financeiro','Pagamentos, confirmações, contestações e relatórios.'),
    ('HOSPITAL','Hospital','Acompanhamento de plantões, escalas e comunicação.')
    ) as x(perfil,titulo,descricao)
    where not exists (select 1 from plantaopro.ajuda_topicos t where t.perfil=x.perfil and t.titulo=x.titulo)
    returning id, perfil
)
insert into plantaopro.ajuda_artigos(topico_id,perfil,titulo,conteudo,link_acao,ordem)
select t.id,t.perfil,x.titulo,x.conteudo,x.link_acao,x.ordem
from plantaopro.ajuda_topicos t
join (values
('ADMINISTRADOR_GLOBAL','Como cadastrar cliente','Acesse Clientes, revise LGPD, informe dados mínimos e acompanhe jornada.','/Clientes',1),
('ADMINISTRADOR_GLOBAL','Como usar inteligência SaaS','Acesse Inteligência SaaS para ver saúde, risco e upgrade.','/SaasDashboard',2),
('COORDENACAO','Como publicar plantão','Crie o plantão, valide limites e publique para convites.','/Plantoes',1),
('COORDENACAO','Como confirmar escala','Use a central de escala para confirmar, cancelar ou substituir com auditoria.','/CentralEscala',2),
('MEDICO','Como aceitar convite','Entre na Minha Agenda e responda convites disponíveis.','/MinhaAgenda',1),
('MEDICO','Como consultar pagamentos','Acesse Financeiro para ver pagamentos previstos e realizados.','/Financeiro',2),
('FINANCEIRO','Como confirmar pagamento','Abra Pagamentos, revise valor e confirme com rastreabilidade.','/Financeiro',1),
('HOSPITAL','Como acompanhar plantões','Consulte Plantões e Escalas vinculados à sua operação.','/Plantoes',1)
) as x(perfil,titulo,conteudo,link_acao,ordem) on x.perfil=t.perfil
where not exists (select 1 from plantaopro.ajuda_artigos a where a.perfil=x.perfil and a.titulo=x.titulo);
