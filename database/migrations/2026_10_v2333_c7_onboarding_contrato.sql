-- R5-C7: onboarding adaptado ao contrato (catalogo canonicos de etapas com jornada e criterio verificavel).
-- Contexto: o kernel B5 materializava 11 etapas genericas fixas (codigo ETAPA_00..11), iguais para
-- qualquer plano, sem objetivo/responsavel/pre-requisito e concluidas por clique (sucesso ficticio).
-- Esta migration e aditiva e idempotente: o catalogo vira a unica fonte das etapas; cada passo da
-- jornada do cliente passa a ter objetivo, responsavel, pre-requisito, destino e um CRITERIO que o
-- servidor verifica em dados persistidos (avaliador no OnboardingJornadaService). Etapas de modulo
-- so existem para o tenant quando o contrato do modulo esta efetivo (predicado de vigencia B4/B6).
create table if not exists plantaopro.onboarding_etapas_catalogo (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(60) not null constraint ux_onboarding_etapas_catalogo_codigo unique,
    titulo varchar(160) not null,
    objetivo text not null,
    descricao text null,
    responsavel varchar(20) not null default 'CLIENTE' constraint ck_onboarding_etapas_catalogo_resp check (responsavel in ('CLIENTE','MNSOFT')),
    pre_requisito_codigo varchar(60) null,
    modulo_codigo varchar(40) null,
    criterio_codigo varchar(60) null,
    link_acao text not null,
    ordem integer not null,
    obrigatorio boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);
comment on column plantaopro.onboarding_etapas_catalogo.modulo_codigo is 'nulo = nucleo geral do onboarding; senao codigo operacional em modulos_sistema (PLANTOES/SAUDE360/ADM360). A etapa so e materializada com contrato efetivo do modulo.';
comment on column plantaopro.onboarding_etapas_catalogo.criterio_codigo is 'nulo = conclusao humana explicita (origem MANUAL); senao o servidor deriva a conclusao verificando dados persistidos do tenant.';

-- Colunas que materializam a jornada no checklist do tenant (preserva linhas legadas ETAPA_xx).
alter table plantaopro.tenant_onboarding_checklist add column if not exists objetivo text null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists responsavel varchar(20) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists pre_requisito_codigo varchar(60) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists modulo_codigo varchar(40) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists criterio_codigo varchar(60) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists origem_conclusao varchar(20) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists evidencia text null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists avaliada_em timestamptz null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists pulada boolean not null default false;
alter table plantaopro.tenant_onboarding_checklist add column if not exists motivo_pular text null;

-- Arbitro anti-duplicacao de etapa por tenant (materializacao idempotente por codigo).
-- Deducacao previa defensiva: linhas antigas simultaneas nao bloqueiam o indice.
delete from plantaopro.tenant_onboarding_checklist a
 using plantaopro.tenant_onboarding_checklist b
 where a.tenant_id = b.tenant_id and a.codigo = b.codigo and a.reg_status = 'A' and b.reg_status = 'A'
   and a.ctid < b.ctid;
create unique index if not exists ux_tenant_onboarding_checklist_tenant_codigo
 on plantaopro.tenant_onboarding_checklist(tenant_id, codigo) where reg_status = 'A';

-- Catalogo canonicos (fonte unica das etapas; rerun atualiza a jornada sem tocar nos tenants).
insert into plantaopro.onboarding_etapas_catalogo
(codigo,titulo,objetivo,descricao,responsavel,pre_requisito_codigo,modulo_codigo,criterio_codigo,link_acao,ordem,obrigatorio)
values
('ONB_EMPRESA_DADOS','Completar os dados da empresa','Manter o cadastro do cliente integro para contato, faturamento e suporte.','Verificamos razao social, nome fantasia, CNPJ, e-mail, telefone, cidade e estado no cadastro persistido.','CLIENTE',null,null,'EMPRESA_DADOS_COMPLETOS','/ClientePortal/Index',10,true),
('ONB_EQUIPE_CONVIDAR','Convidar o primeiro integrante da equipe','Distribuir acesso nominal desde o inicio; nenhuma conta compartilhada.','O criterio le a tabela de convites de equipe (token com expiracao e uso unico, B5).','CLIENTE',null,null,'CONVITE_EQUIPE_ENVIADO','/ConvitesEquipe/Index',20,true),
('ONB_EQUIPE_ACESSO','Primeiro convite aceito pela equipe','Provar que uma pessoa real entrou no ambiente com login proprio.','O criterio le o aceite do convite (uso do token registrado no banco).','CLIENTE','ONB_EQUIPE_CONVIDAR',null,'MEMBRO_EQUIPE_ATIVO','/ConvitesEquipe/Index',30,true),
('ONB_IDENTIDADE','Personalizar a identidade visual','Entregar o produto com a marca do cliente (white label).','Etapa opcional: pode ser pulada sem travar o onboarding. O criterio le logo/nome customizados na configuracao salva.','CLIENTE',null,null,'WHITE_LABEL_PERSONALIZADO','/WhiteLabel',40,false),
('ONB_PL_CRIAR','Criar o primeiro plantao','Registrar uma vaga real na grade de plantoes.','Exige contrato efetivo do modulo Plantoes. O criterio le a tabela de plantoes do tenant.','CLIENTE',null,'PLANTOES','PRIMEIRO_PLANTAO_CRIADO','/Plantoes/Create',110,true),
('ONB_PL_PUBLICAR','Publicar o plantao na grade','Abrir a vaga publicada para que a operacao comecou de verdade.','Pre-requisito: plantao criado. O criterio le plantoes em status publicado (aberto/confirmado/realizado).','CLIENTE','ONB_PL_CRIAR','PLANTOES','PRIMEIRO_PLANTAO_PUBLICADO','/Plantoes/Index',120,true),
('ONB_PL_ESCALA','Fechar a primeira escala com medico','Completar o ciclo convite -> resposta -> escala confirmada.','Pre-requisito: plantao publicado. O criterio le escalas confirmadas/realizadas vinculadas aos plantoes do tenant.','CLIENTE','ONB_PL_PUBLICAR','PLANTOES','ESCALA_PREENCHIDA','/CentralEscala/Index',130,true),
('ONB_SD_UNIDADE','Cadastrar a unidade de atendimento','Estruturar o local fisico onde o Saude 360 vai operar.','Exige contrato efetivo do modulo Saude 360. O criterio le unidades de atendimento cadastradas.','CLIENTE',null,'SAUDE360','PRIMEIRA_UNIDADE_SAUDE','/ClinicaDashboard/Index',210,true),
('ONB_SD_PACIENTE','Cadastrar o primeiro paciente','Formar a base cadastral do atendimento em saude.','Pre-requisito: unidade cadastrada. O criterio le pacientes do tenant.','CLIENTE','ONB_SD_UNIDADE','SAUDE360','PRIMEIRO_PACIENTE_CADASTRADO','/Pacientes/Create',220,true),
('ONB_SD_TRIAGEM','Realizar a primeira triagem','Exercitar o fluxo assistido completo na operacao real.','Pre-requisito: paciente cadastrado. O criterio le triagens registradas do tenant.','CLIENTE','ONB_SD_PACIENTE','SAUDE360','PRIMEIRA_TRIAGEM_REALIZADA','/Triagem/Create',230,true),
('ONB_AD_OPERACAO','Registrar a primeira operacao administrativa','Movimentar o Administrativo 360 com um lancamento real.','Exige contrato efetivo do modulo Administrativo 360. O criterio le operacoes do tenant.','CLIENTE',null,'ADM360','PRIMEIRA_OPERACAO_ADM360','/Administrativo360/Index',310,true),
('ONB_REVISAO','Revisao final e liberacao','Conferir as pendencias restantes e declarar a operacao pronta.','Conclusao derivada: o servidor so marca esta etapa quando TODAS as obrigatorias contratadas estao atendidas em dados.','CLIENTE',null,null,'REVISAO_GERAL','/Onboarding/Index',900,true)
on conflict (codigo) do update set
    titulo = excluded.titulo,
    objetivo = excluded.objetivo,
    descricao = excluded.descricao,
    responsavel = excluded.responsavel,
    pre_requisito_codigo = excluded.pre_requisito_codigo,
    modulo_codigo = excluded.modulo_codigo,
    criterio_codigo = excluded.criterio_codigo,
    link_acao = excluded.link_acao,
    ordem = excluded.ordem,
    obrigatorio = excluded.obrigatorio,
    reg_update = now();
