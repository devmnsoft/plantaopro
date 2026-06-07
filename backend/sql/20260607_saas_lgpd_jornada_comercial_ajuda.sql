create schema if not exists plantaopro;

create table if not exists plantaopro.lgpd_politicas (
    id uuid primary key default gen_random_uuid(),
    versao varchar(30) not null,
    titulo varchar(200) not null,
    conteudo text not null,
    vigente_desde timestamptz not null default now(),
    ativo boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

create table if not exists plantaopro.lgpd_bases_legais (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    descricao text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.lgpd_consentimentos (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    politica_id uuid null,
    versao_politica varchar(30) not null,
    finalidade varchar(160) not null,
    base_legal varchar(120) not null,
    consentido boolean not null default true,
    ip varchar(80) null,
    user_agent varchar(500) null,
    data_consentimento timestamptz not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.lgpd_solicitacoes_titular (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    tipo varchar(60) not null,
    status varchar(40) not null default 'ABERTA',
    resumo text not null,
    resposta text null,
    respondido_por uuid null,
    respondido_em timestamptz null,
    ip varchar(80) null,
    user_agent varchar(500) null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

create table if not exists plantaopro.lgpd_eventos_privacidade (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    evento varchar(120) not null,
    entidade varchar(120) null,
    entidade_id uuid null,
    detalhes jsonb null,
    ip varchar(80) null,
    user_agent varchar(500) null,
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.lgpd_retencao_dados (
    id uuid primary key default gen_random_uuid(),
    categoria varchar(120) not null,
    base_legal varchar(120) not null,
    prazo_meses integer not null,
    observacoes text null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.lgpd_exportacoes_dados (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    solicitacao_id uuid null,
    formato varchar(30) not null default 'JSON',
    resumo jsonb not null,
    ip varchar(80) null,
    user_agent varchar(500) null,
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.lgpd_anonimizacoes (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid not null,
    cliente_id uuid null,
    permitido boolean not null,
    motivo text not null,
    campos_anonimizados text null,
    executado_por uuid null,
    ip varchar(80) null,
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.jornada_cliente (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    etapa varchar(80) not null,
    score integer not null default 0,
    responsavel varchar(160) null,
    proxima_acao text null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

create table if not exists plantaopro.jornada_cliente_eventos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    jornada_id uuid null,
    etapa_anterior varchar(80) null,
    etapa_nova varchar(80) not null,
    tipo varchar(60) not null,
    resumo text not null,
    usuario_id uuid null,
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.jornada_cliente_tarefas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    titulo varchar(200) not null,
    descricao text null,
    responsavel varchar(160) null,
    prazo timestamptz null,
    status varchar(40) not null default 'PENDENTE',
    concluida_em timestamptz null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

create table if not exists plantaopro.jornada_cliente_observacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    observacao text not null,
    usuario_id uuid null,
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.jornada_cliente_responsaveis (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    nome varchar(160) not null,
    papel varchar(120) not null,
    email varchar(180) null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.comercial_leads (
    id uuid primary key default gen_random_uuid(),
    nome varchar(180) not null,
    empresa varchar(180) not null,
    email varchar(180) not null,
    telefone varchar(60) null,
    origem varchar(80) null,
    status varchar(40) not null default 'NOVO',
    medicos_desejados integer not null default 0,
    hospitais integer not null default 0,
    plantoes_mes integer not null default 0,
    precisa_mobile boolean not null default false,
    precisa_bi boolean not null default false,
    suporte_prioritario boolean not null default false,
    operacao_assistida boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

create table if not exists plantaopro.comercial_oportunidades (
    id uuid primary key default gen_random_uuid(),
    lead_id uuid null,
    cliente_id uuid null,
    titulo varchar(200) not null,
    etapa varchar(80) not null default 'QUALIFICACAO',
    valor_estimado numeric(12,2) not null default 0,
    plano_recomendado varchar(120) null,
    probabilidade integer not null default 10,
    status varchar(40) not null default 'ABERTA',
    motivo_perda text null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

create table if not exists plantaopro.comercial_propostas (
    id uuid primary key default gen_random_uuid(),
    oportunidade_id uuid not null,
    numero varchar(40) not null,
    plano_nome varchar(120) not null,
    valor_mensal numeric(12,2) not null,
    desconto_percentual numeric(5,2) not null default 0,
    validade date not null,
    status varchar(40) not null default 'RASCUNHO',
    observacoes text null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

create table if not exists plantaopro.comercial_proposta_itens (
    id uuid primary key default gen_random_uuid(),
    proposta_id uuid not null,
    descricao varchar(200) not null,
    quantidade integer not null default 1,
    valor_unitario numeric(12,2) not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.comercial_interacoes (
    id uuid primary key default gen_random_uuid(),
    lead_id uuid null,
    oportunidade_id uuid null,
    tipo varchar(60) not null,
    resumo text not null,
    usuario_id uuid null,
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.comercial_motivos_perda (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.comercial_regras_desconto (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    limite_percentual numeric(5,2) not null,
    exige_admin_global boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.ajuda_topicos (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null,
    titulo varchar(180) not null,
    descricao text not null,
    ordem integer not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.ajuda_artigos (
    id uuid primary key default gen_random_uuid(),
    topico_id uuid null,
    perfil varchar(80) not null,
    titulo varchar(180) not null,
    resumo text not null,
    conteudo text not null,
    link_acao varchar(220) null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.ajuda_passos (
    id uuid primary key default gen_random_uuid(),
    artigo_id uuid not null,
    ordem integer not null,
    titulo varchar(180) not null,
    descricao text not null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.ajuda_checklists (
    id uuid primary key default gen_random_uuid(),
    perfil varchar(80) not null,
    titulo varchar(180) not null,
    descricao text not null,
    link_acao varchar(220) null,
    ordem integer not null default 0,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.ajuda_feedbacks (
    id uuid primary key default gen_random_uuid(),
    artigo_id uuid not null,
    usuario_id uuid null,
    util boolean not null,
    comentario text null,
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.eventos_sistema (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    perfil varchar(120) null,
    acao varchar(120) not null,
    entidade varchar(120) not null,
    entidade_id uuid null,
    ip varchar(80) null,
    user_agent varchar(500) null,
    sucesso boolean not null,
    mensagem text not null,
    dados_antes jsonb null,
    dados_depois jsonb null,
    correlation_id varchar(120) null,
    reg_date timestamptz not null default now()
);

create index if not exists ix_lgpd_solicitacoes_usuario on plantaopro.lgpd_solicitacoes_titular(usuario_id, reg_date desc);
create index if not exists ix_lgpd_consentimentos_usuario on plantaopro.lgpd_consentimentos(usuario_id, data_consentimento desc);
create index if not exists ix_jornada_cliente_cliente on plantaopro.jornada_cliente(cliente_id);
create index if not exists ix_comercial_leads_status on plantaopro.comercial_leads(status, reg_date desc);
create index if not exists ix_ajuda_artigos_perfil on plantaopro.ajuda_artigos(perfil);

insert into plantaopro.lgpd_politicas(versao,titulo,conteudo,ativo)
select '2026.06','Política de privacidade PlantãoPro','Tratamos dados para gestão de plantões, financeiro, comunicação operacional, auditoria, suporte, faturamento SaaS, Customer Success e cumprimento legal/regulatório.',true
where not exists (select 1 from plantaopro.lgpd_politicas where ativo=true and reg_status='A');

insert into plantaopro.lgpd_bases_legais(nome,descricao)
select v.nome, v.descricao
from (values
('Execução de contrato','Tratamento necessário para execução do contrato SaaS e operação dos plantões.'),
('Cumprimento de obrigação legal','Retenção de informações exigidas por lei, auditoria e obrigações regulatórias.'),
('Legítimo interesse','Segurança, prevenção a fraudes, melhoria operacional e Customer Success.'),
('Consentimento','Autorização do titular para finalidades específicas quando aplicável.'),
('Exercício regular de direitos','Tratamento necessário para defesa de direitos em processos administrativos ou judiciais.')
) as v(nome,descricao)
where not exists (select 1 from plantaopro.lgpd_bases_legais b where b.nome=v.nome);

insert into plantaopro.lgpd_retencao_dados(categoria,base_legal,prazo_meses,observacoes)
select v.categoria, v.base_legal, v.prazo_meses, v.observacoes
from (values
('Dados operacionais de plantões','Execução de contrato',60,'Mantidos para histórico operacional e comprovação de escala.'),
('Dados financeiros','Cumprimento de obrigação legal',120,'Não devem ser apagados em solicitações de anonimização enquanto houver obrigação legal.'),
('Auditoria e segurança','Legítimo interesse',120,'Mantidos para rastreabilidade e segurança da informação.'),
('Dados comerciais SaaS','Execução de contrato',36,'Mantidos durante relacionamento comercial e pós-venda.'),
('Suporte e Customer Success','Legítimo interesse',36,'Mantidos para continuidade do atendimento e melhoria do produto.')
) as v(categoria,base_legal,prazo_meses,observacoes)
where not exists (select 1 from plantaopro.lgpd_retencao_dados r where r.categoria=v.categoria);

insert into plantaopro.ajuda_topicos(perfil,titulo,descricao,ordem)
select v.perfil,v.titulo,v.descricao,v.ordem
from (values
('ADMINISTRADOR_GLOBAL','Administração SaaS','Clientes, planos, assinaturas, faturas, inteligência e relatórios.',1),
('COORDENACAO','Operação de escalas','Criação, publicação e confirmação de plantões e escalas.',2),
('MEDICO','Área do médico','Plantões disponíveis, convites, agenda e pagamentos.',3),
('FINANCEIRO','Financeiro operacional','Pagamentos médicos, faturas SaaS e contestações.',4),
('HOSPITAL','Visão hospitalar','Acompanhamento de plantões, escalas e comunicação.',5)
) as v(perfil,titulo,descricao,ordem)
where not exists (select 1 from plantaopro.ajuda_topicos t where t.perfil=v.perfil and t.titulo=v.titulo);

insert into plantaopro.ajuda_artigos(perfil,titulo,resumo,conteudo,link_acao)
select v.perfil,v.titulo,v.resumo,v.conteudo,v.link_acao
from (values
('ADMINISTRADOR_GLOBAL','Como cadastrar cliente','Cadastre dados comerciais, plano e jornada inicial.','Acesse Clientes, clique em Novo, informe dados mínimos, finalidade de tratamento e confirme. Depois acompanhe Jornada e Inteligência.','/Clientes'),
('ADMINISTRADOR_GLOBAL','Como usar inteligência SaaS','Veja saúde, riscos e oportunidades por regras determinísticas.','Acesse Inteligência, revise clientes em risco, use ações recomendadas e registre contato de Customer Success.','/Inteligencia'),
('COORDENACAO','Como publicar plantão','Crie plantão e publique após validar limites e conflitos.','Acesse Plantões, preencha hospital, especialidade, datas, vagas e valor. Publique usando confirmação segura.','/Plantoes'),
('MEDICO','Como aceitar convite','Consulte convites e confirme participação no plantão.','Acesse Minha Agenda ou Plantões Disponíveis, revise horários e aceite somente se estiver disponível.','/MinhaAgenda'),
('FINANCEIRO','Como confirmar pagamento','Localize pagamento previsto e registre dados de confirmação.','Acesse Financeiro, confira escala, valor e data de pagamento antes de confirmar.','/Financeiro'),
('HOSPITAL','Como acompanhar escalas','Consulte escalas publicadas e comunicação com a coordenação.','Acesse Escalas e Comunicação para acompanhar cobertura e resolver dúvidas operacionais.','/Escalas')
) as v(perfil,titulo,resumo,conteudo,link_acao)
where not exists (select 1 from plantaopro.ajuda_artigos a where a.perfil=v.perfil and a.titulo=v.titulo);

insert into plantaopro.ajuda_checklists(perfil,titulo,descricao,link_acao,ordem)
select v.perfil,v.titulo,v.descricao,v.link_acao,v.ordem
from (values
('ADMINISTRADOR_GLOBAL','Cadastrar cliente piloto','Crie cliente, plano, assinatura e operação assistida.','/Onboarding/NovoCliente',1),
('COORDENACAO','Publicar primeiro plantão','Crie hospital, médico, plantão e convide profissionais.','/Plantoes/Create',1),
('MEDICO','Atualizar disponibilidade','Revise agenda antes de aceitar convites.','/MinhaAgenda',1),
('FINANCEIRO','Validar pagamentos pendentes','Confira pagamentos previstos e faturas SaaS.','/Financeiro',1),
('HOSPITAL','Acompanhar cobertura','Veja escalas e converse com coordenação.','/Escalas',1)
) as v(perfil,titulo,descricao,link_acao,ordem)
where not exists (select 1 from plantaopro.ajuda_checklists c where c.perfil=v.perfil and c.titulo=v.titulo);
