-- PlantaoPro v2.37.0 (R87 - LGPD + saúde): escopos de consentimento de dados de saúde
-- (PRONTUARIO/COAM), catálogo de retenção mínima legal e base do motor de direitos do
-- titular (listar/exportar/apagar com trilha), amarrados aos dados clínicos existentes
-- (pacientes / consultas / documentos_clinicos / lgpd_eventos_privacidade).
--
-- Escopo da rodada (plano r66a0, R87):
--   * consentimento (COAM/prontuário): dados de saúde são sensíveis (LGPD art. 11, I) e o
--     produto registra consentimento específico e destacado por escopo controlado —
--     PRONTUARIO (uso secundário do prontuário eletrônico além da assistência em si, que
--     dispensa prévio consentimento via art. 7, II + art. 11, II, "a") e COAM (Comissão
--     de Avaliação Médica, compartilhamento com terceira comissão interna);
--   * direitos do titular (LGPD art. 18): LISTAR (art. 18, I), EXPORTAR (art. 18, V) e
--     APAGAR (art. 18, IV/VII, executado como anonimização + nulação de identificadores);
--     cada exercício deixa trilha em lgpd_eventos_privacidade;
--   * retenção mínima legal: eliminação fica bloqueada enquanto o prontuário estiver na
--     janela de 20 anos contados do último registro (Lei 13.787/2018, art. 6; Resolução
--     CFM nº 1.821/2007, arts. 7-8) — o "último registro" é a maior data entre consultas
--     e documentos clínicos do paciente.
--
-- Idempotente: create table if not exists + add column if not exists + insert ... on
-- conflict / where not exists (padrão v2332/v2363/v2367/v2368/v2369).

create table if not exists plantaopro.lgpd_escopos_consentimento (
    codigo text primary key,
    nome varchar(60) not null constraint ck_lgpd_escopos_nome check (char_length(nome) between 1 and 60),
    descricao text not null,
    exige_consentimento_explicito boolean not null default true,
    fonte_oficial text not null constraint ck_lgpd_escopos_fonte check (char_length(fonte_oficial) between 5 and 400),
    ativo boolean not null default true,
    criado_em timestamp not null default now(),
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

comment on table plantaopro.lgpd_escopos_consentimento is
    'R87: catalogo institucional dos escopos de consentimento de dados de saude (LGPD art. 11, I). PRONTUARIO = uso secundario do prontuario eletrico; COAM = Comissao de Avaliacao Medica.';
comment on column plantaopro.lgpd_escopos_consentimento.exige_consentimento_explicito is
    'R87: true quando o escopo trata dado pessoal sensivel alem da assistencia em si (LGPD art. 11, I: consentimento especifico e destacado).';

insert into plantaopro.lgpd_escopos_consentimento (codigo, nome, descricao, exige_consentimento_explicito, fonte_oficial) values
('PRONTUARIO', 'Prontuário eletrônico',
 'Uso secundário do prontuário eletrônico além da assistência em si (pesquisa, ensino, gestão). A assistência exclusiva usa cumprimento de obrigação legal sem prévio consentimento.',
 true, 'Lei 13.709/2018 (LGPD), arts. 7, I e 11, I; Lei 13.787/2018'),
('COAM', 'Comissão de Avaliação Médica',
 'Compartilhamento dos dados de saúde do paciente com a comissão interna de avaliação médica (revisão de casos, segunda opinião, controle de qualidade).',
 true, 'Lei 13.709/2018 (LGPD), art. 11, I')
on conflict (codigo) do nothing;

alter table plantaopro.paciente_consentimentos add column if not exists versao_termo varchar(40);
alter table plantaopro.paciente_consentimentos add column if not exists base_legal varchar(40) not null default 'CONSENTIMENTO_EXPLICITO';
comment on column plantaopro.paciente_consentimentos.finalidade is
    'R87: escopo controlado (PRONTUARIO|COAM), validado contra lgpd_escopos_consentimento na camada de serviço.';
comment on column plantaopro.paciente_consentimentos.versao_termo is
    'R87: versão do termo apresentado ao titular; obrigatória quando concedido=true.';
comment on column plantaopro.paciente_consentimentos.base_legal is
    'R87: base do tratamento; dados de saúde sensíveis exigem CONSENTIMENTO_EXPLICITO (LGPD art. 11, I).';

alter table plantaopro.lgpd_retencao_dados add column if not exists prazo_meses integer check (prazo_meses is null or prazo_meses > 0);
alter table plantaopro.lgpd_retencao_dados add column if not exists fonte_oficial text;
comment on column plantaopro.lgpd_retencao_dados.prazo_meses is
    'R87: prazo mínimo legal em meses a partir da referência da categoria (nulo = flexível, conforme necessidade).';

insert into plantaopro.lgpd_retencao_dados (categoria, base_legal, prazo, prazo_meses, fonte_oficial, regra)
select 'Prontuario', 'Cumprimento de obrigação legal', 'Mínimo de 20 anos a partir do último registro', 240,
       'Lei Federal 13.787/2018, art. 6; Resolução CFM n. 1.821/2007, arts. 7 e 8',
       'Eliminar somente após 240 meses do último registro clínico; antes disso, anonimizar.'
where not exists (select 1 from plantaopro.lgpd_retencao_dados r where r.categoria = 'Prontuario' and r.reg_status = 'A');

update plantaopro.lgpd_retencao_dados
set prazo_meses = 60,
    fonte_oficial = 'CTN (Lei 5.172/1966), art. 194 - prescrição quinquenal do crédito tributário'
where categoria = 'Financeiro' and reg_status = 'A' and prazo_meses is null;

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('LGPD_SA_BASE_SENSIVEL', 'Dados de saúde: base do tratamento para finalidades além da assistência', 'CONSENTIMENTO_EXPLICITO',
 'Lei 13.709/2018 (LGPD), art. 11, I - dados sensíveis exigem consentimento específico e destacado', null, null, 2026),
('LGPD_SA_ESCOPOS_CONSENTIMENTO', 'Escopos de consentimento de dados de saúde gerenciados pelo produto', 'PRONTUARIO|COAM',
 'Lei 13.709/2018 (LGPD), arts. 7, I e 11, I - uso secundário e compartilhamento com comissão interna', null, null, 2026),
('LGPD_SA_CANAIS', 'Canais de captura de consentimento com trilha auditável', 'RECEPCAO|TELEFONE|DIGITAL|PRONTUARIO',
 'Produto PlantaoPro - politica institucional de captura de consentimento com trilha (operador + canal + IP)', null, null, 2026),
('LGPD_SA_RETENCAO_PRONTUARIO_MESES', 'Retenção mínima do prontuário médico (papel e digitalizado)', '240',
 'Lei Federal 13.787/2018, art. 6 - decorrido o prazo mínimo de 20 anos a partir do último registro; Resolução CFM n. 1.821/2007, arts. 7 e 8', null, null, 2026),
('LGPD_SA_RETENCAO_FISCAL_MESES', 'Documentos financeiros/fiscais: prescrição quinquenal', '60',
 'CTN (Lei 5.172/1966), art. 194 - decadência/prescrição do crédito tributário em 5 anos', null, null, 2026),
('LGPD_SA_DIREITOS', 'Direitos do titular exercíveis no produto', 'LISTAR|EXPORTAR|APAGAR',
 'Lei 13.709/2018 (LGPD), art. 18, I (confirmação/acesso), V (portabilidade) e IV (anonimização/eliminação)', null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ("20"), "periodicidade|offset_periodos|dia_vencimento|regime" para catalogo de obrigacoes fiscais (R84), "cUF|DOC,DOC,..." para configuracao SEFAZ por UF e versao ("4.0"/"3.00") para layouts de DF-e (R85), "AAAA-MM-DD" para dia base e documento ISO separado por "|" para trilhos de pagamento (R86), "CODIGO|CODIGO" para catalogos de escopos/canais/direitos e meses inteiros para retencoes minimas (R87); exibicao formata.';
