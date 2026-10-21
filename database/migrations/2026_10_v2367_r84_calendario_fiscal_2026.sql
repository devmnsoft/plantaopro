-- PlantaoPro v2.36.7 (R84 - bloco B4, legislacao brasileira): calendario fiscal e motor de
-- obrigacoes legais por tenant.
--
-- Escopo da rodada (plano r66a0, R84): calendario fiscal/obrigacoes legais (DAS dia 20, FGTS
-- dia 20, DCTFWeb dia 15, eSocial, EFD, livros contabeis) virando motor de obracoes/reminders
-- por tenant. O catalogo das obracoes e semeado na fonte unica (parametros_legais) com fonte
-- citavel; o formato canonico do valor e "periodicidade|offset_periodos|dia_vencimento|regime_aplicavel":
--   - MENSAL: o vencimento cai no dia da competencia + offset (offset 1 = mes seguinte);
--   - ANUAL: o vencimento cai em dezembro do ano referencia + offset (convencao do encerramento
--     anual dos livros contabeis; o item aparece no calendario somente na exibicao do mes 12).
-- Os dias fixos que variam por estado/orgao no mundo real (eSocial 7, DCTFWeb/EFD 15, livros 31)
-- sao fixados pelo plano r66a0 (R84) como simplificacao operacional de planejamento: o prazo
-- final segue sempre o calendario oficial de cada orgao. A janela de alerta antecipado (dias)
-- tambem e parametro operacional, nao valor legal.
--
-- O estado por tenant (obrigacao ativa/inativa por periodo de referencia e marca de
-- conclusao) vive na tabela adm_obrigacoes_fiscais, sem FK de referencia de tenant (o tenant_id
-- e o da sessao; nenhum registro de outro tenant e lido). Sem chave estrangeira para manter o
-- padrao enxuto das tabelas adm_* recentes e permitir qualquer organizacao da conta.
--
-- Idempotente: create ... if not exists + insert ... on conflict do nothing (padrao v2363).

create table if not exists plantaopro.adm_obrigacoes_fiscais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    codigo_obrigacao text not null,
    mes_referencia smallint not null,
    ano_referencia smallint not null,
    ativa boolean not null default true,
    concluida_em timestamptz null,
    concluida_por text null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create unique index if not exists ux_adm_obrigacoes_fiscais_tenant_codigo_referencia
    on plantaopro.adm_obrigacoes_fiscais (tenant_id, codigo_obrigacao, mes_referencia, ano_referencia) where reg_status = 'A';

create index if not exists ix_adm_obrigacoes_fiscais_tenant_referencia
    on plantaopro.adm_obrigacoes_fiscais (tenant_id, mes_referencia, ano_referencia) where reg_status = 'A';

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('OBRIGACAO_FISCAL_DAS_SIMPLES', 'DAS - Simples Nacional (PGDAS-D): recolhimento mensal dos tributos do regime', 'MENSAL|1|20|SIMPLES_NACIONAL',
 'LC 123/2006 art. 71 - competencia do mes anterior, vencimento dia 20', null, null, 2026),
('OBRIGACAO_FISCAL_FGTS_MENSAL', 'Deposito mensal do FGTS (8% da remuneracao)', 'MENSAL|1|20|CLT',
 'Lei 5.107/1966 (CLT) art. 15 - deposito ate o dia 20 do mes subsequente', null, null, 2026),
('OBRIGACAO_FISCAL_DCTFWEB_MENSAL', 'DCTFWeb: declaracao consolidada de arrecadacao mensal', 'MENSAL|1|15|GERAL',
 'IN RFB 2.110/2022 (DCTFWeb) - transmissao no mes subsequente a competencia; dia 15 fixado no plano r66a0 (R84)', null, null, 2026),
('OBRIGACAO_FISCAL_ESOCIAL_S1200', 'eSocial: fechamento mensal dos eventos de folha (S-1200)', 'MENSAL|1|7|CLT',
 'Manual de Orientacao do eSocial (evento S-1200 folha) - mes subsequente; dia 7 fixado no plano r66a0 (R84)', null, null, 2026),
('OBRIGACAO_FISCAL_EFD_ICMS_MENSAL', 'EFD ICms/IPI: transmissao mensal do livro digital (DEF)', 'MENSAL|1|15|GERAL',
 'Layout EFD ICms/IPI (IN Sefaz) - DEF mensal no mes subsequente; dia 15 fixado no plano r66a0 (R84)', null, null, 2026),
('OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS', 'Encerramento anual dos livros contabeis (exercicio fiscal)', 'ANUAL|0|31|GERAL',
 'LC 640/1994 art. 32 (normas de contabilidade aplicaveis as pequenas empresas) - encerramento do exercicio; dia 31 fixado no plano r66a0 (R84)', null, null, 2026),
('OBRIGACOES_FISCAIS_JANELA_ALERTA_DIAS', 'Janela de alerta antecipado do calendario fiscal (dias antes do vencimento)', '7',
 'Parametro operacional fixado no plano r66a0 (R84) - simplificacao de planejamento, nao valor legal', null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on table plantaopro.adm_obrigacoes_fiscais is
    'Estado por tenant do calendario fiscal (R84 - bloco B4): obrigacao ativa/inativa por periodo de referencia e marca de conclusao por usuario; catalogo e datas oficiais vivem em parametros_legais.';
comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ("20"), "periodicidade|offset_periodos|dia_vencimento|regime" para catalogo de obrigacoes fiscais (R84); exibicao formata.';
