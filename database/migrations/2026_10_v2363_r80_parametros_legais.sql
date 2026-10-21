-- PlantaoPro v2.36.3 (R80 - bloco B4, legislacao brasileira): parametros_legais como
-- fonte unica de parametros legais brasileiros (valor, fonte oficial, vigencia, ano).
--
-- Escopo da rodada (plano r66a0, R80): mapa legislativo x codigo + esta tabela como
-- fonte unica. As linhas de seed trazem apenas valores com origem citavel (texto de
-- lei/instrucao normativa oficial ou derivacao matematica explicita). Os vetores de
-- tributacao progressiva/regressiva 2026 (faixas INSS/IRRF, teto) entram na R81 com
-- contrato proprio lendo desta tabela -- nenhum numero legal fica hardcodado em
-- servico antes de existir aqui.
--
-- Convencao de valor: texto canonico ponto decimal para valores numericos ("1518.00")
-- e digito puro para dia do mes ("20"); o formato de exibicao (BRL, "dia 20") e
-- responsabilidade do consumidor. Parametros fixos (aliquota, dia de vencimento)
-- nao usam vigencia; anuais usam 01/01 a 31/12 do ano.
--
-- Idempotente: create ... if not exists + insert ... on conflict do nothing
-- (mesmo padrao de v2362_r72_anexos_arquivos). Tabela GLOBAL (sem tenant):
-- parametro legal e por pais/ano, nao por instituicao.

create table if not exists plantaopro.parametros_legais (
    id uuid primary key default gen_random_uuid(),
    codigo text not null,
    descricao text not null,
    valor text not null,
    fonte_oficial text not null,
    vigencia_inicio date null,
    vigencia_fim date null,
    ano smallint not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create unique index if not exists ux_parametros_legais_codigo_ano
    on plantaopro.parametros_legais (codigo, ano) where reg_status = 'A';

create index if not exists ix_parametros_legais_ano
    on plantaopro.parametros_legais (ano) where reg_status = 'A';

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('SALARIO_MINIMO', 'Salario minimo nacional para o ano', '1518.00',
 'MTE/MF - diario oficial (salario minimo vigente para 2026)', date '2026-01-01', date '2026-12-31', 2026),
('PISO_ISENCAO_IRRF_MENSAL', 'Piso mensal de rendimentos nao tributados pelo IRRF (3x salario minimo)', '4554.00',
 'RIR (Decreto-Lei 3.240/1941) art. 8 - derivacao: 3 x SALARIO_MINIMO 2026', date '2026-01-01', date '2026-12-31', 2026),
('FGTS_ALIQUOTA_MENSAL', 'Aliquota do deposito mensal do FGTS em % sobre a remuneracao', '8.00',
 'Lei 5.107/1966 (CLT) art. 15', null, null, 2026),
('FGTS_VENCIMENTO_DIA', 'Vencimento do deposito mensal do FGTS (dia do mes)', '20',
 'Lei 5.107/1966 (CLT) art. 15 - dia fixado no plano r66a0 (R84)', null, null, 2026),
('DAS_SIMPLES_VENCIMENTO_DIA', 'Vencimento da guia DAS do Simples Nacional (dia do mes)', '20',
 'LC 123/2006 art. 71 - competencia do mes anterior, vencimento dia 20', null, null, 2026),
('DCTFWEB_VENCIMENTO_DIA', 'Vencimento mensal da DCTFWeb (dia do mes)', '15',
 'IN RFB 2.110/2022 (DCTFWeb) - dia fixado no plano r66a0 (R84)', null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on table plantaopro.parametros_legais is
    'Fonte unica de parametros legais brasileiros por ano (R80 - bloco B4): valor canonico, fonte oficial citavel e vigencia; parametros fixos sem vigencia.';
comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ("20"); exibicao formata.';
comment on column plantaopro.parametros_legais.fonte_oficial is
    'Origem citavel do valor (lei/instrucao normativa ou derivacao explicita) - requisito do plano r66a0 R80.';
