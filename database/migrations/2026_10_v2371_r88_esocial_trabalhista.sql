-- PlantaoPro v2.37.1 (R88 - trabalhista / eSocial): catálogo oficial dos eventos
-- eSocial da faixa S-1000 a S-1299 (leiaute S-1.3): tabelas do empregador
-- (S-1000/S-1005/S-1010/S-1020/S-1070) e eventos periódicos mensais
-- (S-1200/S-1202/S-1207/S-1210/S-1260/S-1270/S-1280/S-1298/S-1299); registro em
-- prévia de eventos por colaborador (forma_envio fixa 'PREVIEW' - sem transmissão
-- ao governo nesta etapa); declaração semanal de horas comparada à jornada
-- contratual do colaborador (adm_contratos_trabalho.carga_horaria_semanal) e ao
-- limite legal de 44h semanais; e parâmetros legais de jornada/hora extra/aviso
-- prévio na fonte única parametros_legais.
--
-- Escopo da rodada (plano r66a0, R88):
--   * "eventos eSocial por colaborador (S-1000...S-1299) em preview": o produto
--     registra localmente, com trilha de autoria, quais eventos da faixa a folha
--     do colaborador/mês passa por, sem transmitir ao governo (integração futura);
--   * "horas/jornada": semanas declaradas (segunda-feira de referência) contra a
--     carga horária semanal do contrato vigente e o teto legal — CF/88 art. 7,
--     XIII (jornada semanal máxima de 44h); a estimativa de hora extra usa o
--     mínimo de acréscimo de 50% (CLT art. 59, §1º; CF/88 art. 7, XVI) sobre o
--     valor-hora (salário mensal / 220h — CLT art. 74, §2º);
--   * "aviso prévio": base de 30 dias (CLT art. 487, II; Lei 12.506/2011, art. 1)
--     + 3 dias por ano de serviço na mesma empresa, limitado o total a 90 dias
--     (Lei 12.506/2011, parágrafo único). Interpretação adotada e documentada na
--     evidência: o acréscimo incide a partir do tempo superior a 1 ano na mesma
--     empresa (Nota Técnica MTE nº 184/2012); tempo de até 1 ano completo mantém
--     os 30 dias da base. A redução de 2h diárias (ou 7 dias corridos) durante o
--     aviso é exibida como referência (CLT art. 488).
--
-- Fontes citáveis (travadas): leiaute eSocial S-1.3 (Nota Técnica 06/2026, rev.
-- 09/04/2026) e MOS S-1.3 no portal gov.br/esocial; Lei 12.506/2011 (Planalto);
-- CLT (Decreto-Lei 5.452/1943), arts. 59 §1º, 74 §2º, 487 e 488; CF/88 art. 7,
-- XIII/XVI; Nota Técnica MTE nº 184/2012.
--
-- Idempotente: create table if not exists + create unique index if not exists +
-- insert ... on conflict / where not exists (padrão v2332/v2363/v2367/v2368/v2369/v2370).

create table if not exists plantaopro.esocial_eventos_catalogo (
    codigo varchar(10) primary key,
    nome varchar(120) not null constraint ck_esocial_eventos_nome check (char_length(nome) between 5 and 120),
    categoria varchar(30) not null constraint ck_esocial_eventos_categoria check (categoria in ('TABELA_EMPREGADOR', 'PERIODICO')),
    descricao text not null,
    vencimento text not null,
    fonte_oficial text not null constraint ck_esocial_eventos_fonte check (char_length(fonte_oficial) between 5 and 300),
    ativo boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

comment on table plantaopro.esocial_eventos_catalogo is
    'R88: catalogo oficial dos eventos eSocial da faixa S-1000 a S-1299 (leiaute S-1.3): tabelas do empregador (configuracao) e eventos periodicos mensais (ciclo da folha).';
comment on column plantaopro.esocial_eventos_catalogo.categoria is
    'R88: TABELA_EMPREGADOR (S-1000 a S-1070, configuracao do contribuinte) ou PERIODICO (S-1200 a S-1299, ciclo mensal da folha).';

insert into plantaopro.esocial_eventos_catalogo (codigo, nome, categoria, descricao, vencimento, fonte_oficial) values
('S-1000', 'Informações do Empregador/Contribuinte/Órgão Público', 'TABELA_EMPREGADOR',
 'Dados básicos do empregador (classificações tributárias, regimes, eleições) que contextualizam todos os demais eventos.',
 'Primeiro evento transmitido antes de qualquer outro; atualizações a qualquer momento.',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1005', 'Tabela de Estabelecimentos, Obras ou Unidades de Órgãos Públicos', 'TABELA_EMPREGADOR',
 'Cada estabelecimento (CNPJ/CNO) com o código de atividade principal (CNAE) e os fatores RAT/FAP que definem a contribuição acidentária (GILRAT).',
 'Antes de qualquer evento que refira o estabelecimento (S-2200, S-1200 etc.).',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1010', 'Tabela de Rubricas', 'TABELA_EMPREGADOR',
 'Catálogo de todas as rubricas de pagamento com as flags de incidência de INSS, IRRF e FGTS de cada item.',
 'Antes de eventos de remuneração que utilizem a rubrica.',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1020', 'Tabela de Lotações Tributárias', 'TABELA_EMPREGADOR',
 'Define as lotações (FPAS, códigos de terceiros, obras, tomadores de serviço) às quais os trabalhadores se vinculam para fins de contribuição.',
 'Antes dos eventos que utilizem a lotação (S-1200, S-2200 etc.).',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1070', 'Tabela de Processos Administrativos/Judiciais', 'TABELA_EMPREGADOR',
 'Registra processos judiciais ou administrativos que suspendem ou alteram a responsabilidade tributária/FGTS, para que decisões sejam aplicadas nos cálculos.',
 'Até o dia 15 do mês seguinte à decisão, ou antes do evento que a utilize.',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1200', 'Remuneração de Trabalhador vinculado ao RGPS', 'PERIODICO',
 'Folha mensal trabalhador a trabalhador e rubrica a rubrica, no regime de competência.',
 'Dia 15 do mês seguinte ao de competência (folha de 13º: 20/12).',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1202', 'Remuneração de Servidor vinculado ao Regime Próprio (RPPS)', 'PERIODICO',
 'Folha mensal de servidores do regime próprio de previdência social (equivalente público do S-1200).',
 'Dia 15 do mês seguinte ao de competência (folha de 13º: 20/12).',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1207', 'Benefícios - Entes Públicos', 'PERIODICO',
 'Valores mensais de aposentadorias/benefícios pagos por entes públicos aos beneficiários cadastrados.',
 'Dia 15 do mês seguinte ao de competência (benefício de 13º: 20/12).',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1210', 'Pagamentos de Rendimentos do Trabalho', 'PERIODICO',
 'Relata os valores efetivamente pagos no mês (regime de caixa); base da retenção do imposto de renda na fonte (IRRF) e complementa os eventos de remuneração por competência.',
 'Dia 15 do mês seguinte ao de pagamento.',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1260', 'Comercialização da Produção Rural Pessoa Física', 'PERIODICO',
 'Relata a comercialização da produção rural de produtores pessoa física, base da contribuição previdenciária sobre a receita rural.',
 'Dia 15 do mês seguinte ao de competência.',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1270', 'Contratação de Trabalhadores Avulsos Não Portuários', 'PERIODICO',
 'Apresentado pelo tomador de serviços para relatar a contratação de trabalhadores avulsos por meio do sindicato.',
 'Dia 15 do mês seguinte ao de competência.',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1280', 'Informações Complementares aos Eventos Periódicos', 'PERIODICO',
 'Ajustes gerais da folha, como remissão de tributos patronais (CPRB) e percentuais de Simples Nacional com tributação mista.',
 'Dia 15 do mês seguinte, com ou antes do fechamento (S-1299).',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1298', 'Reabertura dos Eventos Periódicos', 'PERIODICO',
 'Reabre um mês já fechado para que eventos periódicos possam ser corrigidos ou incluídos.',
 'Sempre que for necessária correção em mês fechado.',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial'),
('S-1299', 'Fechamento dos Eventos Periódicos', 'PERIODICO',
 'Declara o fechamento do mês da folha, dispara os totalizadores (S-5011/S-5012/S-5013) e alimenta o DCTFWeb e o FGTS Digital.',
 'Dia 15 do mês seguinte ao de competência (folha de 13º: 20/12).',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial')
on conflict (codigo) do nothing;

create table if not exists plantaopro.colaborador_eventos_esocial (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    colaborador_id uuid null,
    codigo_evento varchar(10) not null constraint ck_esocial_ev_codigo check (char_length(codigo_evento) between 4 and 10),
    periodo_apuracao char(7) null constraint ck_esocial_ev_periodo check (periodo_apuracao is null or periodo_apuracao ~ '^[0-9]{4}-(0[1-9]|1[0-2])$'),
    data_fato date not null,
    descricao varchar(400) null,
    forma_envio varchar(20) not null default 'PREVIEW' constraint ck_esocial_ev_forma check (forma_envio in ('PREVIEW')),
    criado_por uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

comment on table plantaopro.colaborador_eventos_esocial is
    'R88: registro em previa (sem transmissao ao governo) dos eventos eSocial da faixa S-1000 a S-1299 pelos quais a folha do colaborador/mes passa. colaborador_id nulo = evento institucional de tabela do empregador (S-1000 a S-1070) ou do ciclo fechado sem colaborador especifico.';
comment on column plantaopro.colaborador_eventos_esocial.periodo_apuracao is
    'R88: competencia "AAAA-MM" do evento periodico; obrigatoria para eventos PERIODICO (validada na camada de servico contra esocial_eventos_catalogo).';
comment on column plantaopro.colaborador_eventos_esocial.forma_envio is
    'R88: forma de envio registrada. Nesta etapa apenas PREVIEW (registro local para a futura integracao com o ambiente eSocial).';

create index if not exists ix_colaborador_eventos_esocial_tenant_colab_data
    on plantaopro.colaborador_eventos_esocial (tenant_id, colaborador_id, data_fato desc);

create table if not exists plantaopro.colaborador_jornadas_semanais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    colaborador_id uuid not null,
    semana_inicio date not null constraint ck_jornada_semana_segunda check (date_part('dow', semana_inicio) = 1),
    horas_normais numeric(6,2) not null constraint ck_jornada_normais check (horas_normais >= 0 and horas_normais <= 120),
    horas_extras numeric(6,2) not null default 0 constraint ck_jornada_extras check (horas_extras >= 0 and horas_extras <= 120),
    observacao varchar(400) null,
    criado_por uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

comment on table plantaopro.colaborador_jornadas_semanais is
    'R88: declaracao semanal de horas trabalhadas por colaborador (semana_inicio sempre segunda-feira). A analise compara a jornada contratual (adm_contratos_trabalho.carga_horaria_semanal do contrato vigente) e o teto legal de 44h semanais (CF/88 art. 7, XIII).';

create unique index if not exists ux_v2371_jornada_tenant_colab_semana
    on plantaopro.colaborador_jornadas_semanais (tenant_id, colaborador_id, semana_inicio);

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('ESOCIAL_EVENTOS_CATALOGO', 'Eventos eSocial gerenciados pelo produto em prévia (faixa S-1000 a S-1299)',
 'S-1000|S-1005|S-1010|S-1020|S-1070|S-1200|S-1202|S-1207|S-1210|S-1260|S-1270|S-1280|S-1298|S-1299',
 'eSocial leiaute S-1.3 (NT 06/2026, rev. 09/04/2026) e MOS S-1.3 - gov.br/esocial: tabelas do empregador (S-1000 a S-1070) e eventos periodicos mensais (S-1200 a S-1299)',
 null, null, 2026),
('ESOCIAL_VENCIMENTO_PERIODICO_DIA', 'Dia de vencimento padrao dos eventos periodicos eSocial', '15',
 'eSocial MOS S-1.3 - gov.br/esocial: eventos periodicos vencem no dia 15 do mes seguinte ao de competencia (adiado para o proximo dia util quando nao util)',
 null, null, 2026),
('AVISO_PREVIO_BASE_DIAS', 'Aviso previo base para tempo de servico de ate 1 ano na mesma empresa', '30',
 'CLT (Decreto-Lei 5.452/1943), art. 487, II; Lei 12.506/2011, art. 1',
 null, null, 2026),
('AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS', 'Acrescimo ao aviso previo por ano de servico na mesma empresa', '3',
 'Lei 12.506/2011, paragrafo unico - acrescidos 3 dias por ano de servico, limitado o acrescimo a 60 dias',
 null, null, 2026),
('AVISO_PREVIO_TETO_TOTAL_DIAS', 'Teto total do aviso previo proporcional', '90',
 'Lei 12.506/2011, paragrafo unico - limitado o total a 90 dias',
 null, null, 2026),
('JORNADA_SEMANAL_MAX_HORAS', 'Jornada semanal maxima legal (referencia quando inexistente contrato vigente)', '44',
 'CF/88, art. 7, XIII - duracao da jornada de trabalho de, no maximo, 44 horas semanais (recebida no art. 7, XIII da CLT)',
 null, null, 2026),
('HORA_EXTRA_ACRESIMO_MIN_PCT', 'Acrescimo minimo legal sobre a hora para horas extraordinarias', '50',
 'CLT (Decreto-Lei 5.452/1943), art. 59, paragrafo unico - remuneradas com acrescimo de, no minimo, cinquenta por cento; CF/88, art. 7, XVI',
 null, null, 2026),
('SALARIO_DIVISOR_MENSAL_HORAS', 'Divisor do salario mensal para conversao em valor-hora', '220',
 'CLT (Decreto-Lei 5.452/1943), art. 74, paragrafo unico - o salario mensal corresponde a, no maximo, duzentas e vinte horas de trabalho mensalmente',
 null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ("20"), "periodicidade|offset_periodos|dia_vencimento|regime" para catalogo de obrigacoes fiscais (R84), "cUF|DOC,DOC,..." para configuracao SEFAZ por UF e versao ("4.0"/"3.00") para layouts de DF-e (R85), "AAAA-MM-DD" para dia base e documento ISO separado por "|" para trilhos de pagamento (R86), "CODIGO|CODIGO" para catalogos de escopos/canais/direitos e meses inteiros para retencoes minimas (R87), e codigo "S-XXXX" separado por "|" para catalogo de eventos eSocial e dias/horas/pontos percentuais inteiros para jornada, divisor mensal e aviso previo (R88); exibicao formata.';
