-- PlantaoPro v2.37.2 (R89 - venda para fora do país): moeda base BRL + cotações
-- em USD/EUR/GBP na fonte única institucional (cotacoes_moedas), catálogo de
-- alíquotas padrão de IVA/VAT por país de destino (paises_vat, com fonte
-- citável por linha) e faturamento internacional simplificado como nota fiscal
-- não-tradicional (notas_internacionais) amarrado à fatura SaaS: converte o
-- valor da fatura em BRL para a moeda de destino pela cotação vigente e aplica
-- o VAT padrão do país de destino sobre a base convertida.
--
-- Escopo da rodada (plano r66a0, R89):
--   * "moeda base BRL + cotações (USD/EUR/GBP)": as faturas saem em real
--     brasileiro; o produto mantém uma tabela institucional de cotações
--     (BRL por unidade da moeda) com upsert por (moeda, data) e histórico;
--     a nota usa a cotação mais recente em data <= a data de emissao;
--   * "faturamento internacional simplificado (nota fiscal não-tradicional /
--     VAT por destino)": documento simplificado emitido por fatura aberta
--     (ABERTA/VENCIDA) para um país de destino catalogado, com snapshot da
--     cotação e das alíquotas aplicadas; exatamente uma nota EMITIDA por
--     fatura (anular libera para reemissão);
--   * sem integração externa (sem OSS/IOSS, sem sales tax estadual dos EUA,
--     sem atualização automática de câmbio) - tudo permanece em registro local.
--
-- Interpretação registrada (evidência r88a0-analoga desta rodada): o VAT do
-- destino é cobrado ACRESCIDO sobre a base convertida (subtotal + imposto),
-- exibido em duas linhas; no lado brasileiro a venda ao exterior enquadra-se
-- nas dispensas clássicas de exportação - ICMS (Lei 8.080/1990, art. 39, II)
-- e IPI (Lei 4.502/1964, art. 15, III, c/c Decreto 4.404/2002, arts. 19-23) -
-- citadas no parâmetro de forma de documento.
--
-- Fontes citáveis (travadas): cotações - taxa comercial do Banco Central do
-- Brasil (bcb.gov.br/en/currencyconversion), euro comercial (Valor Data) e
-- libra comercial (Investidor10), todas confirmadas em 2026-10-22; alíquotas
-- padrão de VAT - HMRC gov.uk/vat-rates (GB 20%), BOFiP DGFiP (FR 20%), AEAT
-- (ES 21%), Belastingdienst (NL 21%), Autoridade Tributária PT (23%), Agenzia
-- delle Entrate (IT 22%), BMF/Avalara (DE 19%), NTA Japão (10%).
--
-- Idempotente: create table if not exists + create unique index if not exists +
-- insert ... on conflict / where not exists (padrão v2332/v2363/v2367/v2368/v2369/v2370/v2371).

create table if not exists plantaopro.cotacoes_moedas (
    moeda char(3) not null constraint ck_cotacoes_moeda_iso check (moeda ~ '^[A-Z]{3}$'),
    data date not null,
    taxa numeric(18,6) not null constraint ck_cotacoes_taxa_positiva check (taxa > 0),
    fonte text not null constraint ck_cotacoes_fonte check (char_length(fonte) between 5 and 300),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null,
    primary key (moeda, data)
);

comment on table plantaopro.cotacoes_moedas is
    'R89: tabela institucional de cotacoes de cambio (BRL por unidade da moeda externa) usada pelo faturamento internacional; upsert por (moeda, data) preserva o historico.';
comment on column plantaopro.cotacoes_moedas.taxa is
    'R89: quantos reais compram 1 unidade da moeda externa (ex.: 5.027700 = 1 USD). Conversao da nota: valor_brl / taxa.';

insert into plantaopro.cotacoes_moedas (moeda, data, taxa, fonte) values
('USD', '2026-10-22', 5.027700,
 'Taxa comercial do dolar em BRL - Banco Central do Brasil (bcb.gov.br/en/currencyconversion), confirmado em 2026-10-22'),
('EUR', '2026-10-22', 5.635300,
 'Euro comercial em BRL - Valor Data, fechamento de 21/10/2026, confirmado em 2026-10-22'),
('GBP', '2026-10-22', 6.730000,
 'Libra esterlina comercial em BRL - Investidor10, cotação de 22/10/2026')
on conflict (moeda, data) do nothing;

create table if not exists plantaopro.paises_vat (
    pais char(2) primary key constraint ck_paises_vat_iso check (pais ~ '^[A-Z]{2}$'),
    nome varchar(120) not null constraint ck_paises_vat_nome check (char_length(nome) between 5 and 120),
    aliquota numeric(5,2) not null constraint ck_paises_vat_aliquota check (aliquota >= 0 and aliquota <= 100),
    fonte text not null constraint ck_paises_vat_fonte check (char_length(fonte) between 5 and 300),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

comment on table plantaopro.paises_vat is
    'R89: catalogo institucional de paises de destino com alíquota PADRAO de IVA/VAT aplicada pela nota fiscal nao-tradicional de faturamento internacional (escopo simplificado: somente alíquota padrao, sem regimes reduzidos/especiais).';

insert into plantaopro.paises_vat (pais, nome, aliquota, fonte) values
('DE', 'Alemanha', 19.00,
 'Alíquota padrão de MwSt 19% - BMF/Finanzverwaltung Alemanha (tabela europeia de VAT 2026, revisada em 2026)'),
('ES', 'Espanha', 21.00,
 'Alíquota padrão de IVA 21% - Agencia Tributaria Espanola (AEAT), tipos impositivos do IVA (vigente desde 01/09/2012)'),
('FR', 'França', 20.00,
 'Alíquota padrão de TVA 20% - BOFiP DGFiP (BOI-TVA-LIQ-10), vigente desde 01/01/2014'),
('GB', 'Reino Unido', 20.00,
 'Alíquota padrão de VAT 20% - HMRC, gov.uk/vat-rates (vigente desde 04/01/2011)'),
('IT', 'Itália', 22.00,
 'Alíquota padrão de IVA 22% - Agenzia delle Entrate (Itália)'),
('JP', 'Japão', 10.00,
 'Imposto de consumo padrão 10% - National Tax Agency do Japão (JCT)'),
('NL', 'Países Baixos', 21.00,
 'Alíquota padrão de btw 21% - Belastingdienst (Países Baixos)'),
('PT', 'Portugal', 23.00,
 'Alíquota padrão de IVA 23% - Autoridade Tributária (Portugal)'),
('US', 'Estados Unidos', 0.00,
 'Sem imposto federal sobre valor agregado nos EUA; sales tax estadual fora do escopo simplificado desta etapa')
on conflict (pais) do nothing;

create table if not exists plantaopro.notas_internacionais (
    id uuid primary key default gen_random_uuid(),
    fatura_id uuid not null,
    pais_destino char(2) not null constraint ck_notas_int_pais_iso check (pais_destino ~ '^[A-Z]{2}$'),
    moeda char(3) not null constraint ck_notas_int_moeda_iso check (moeda ~ '^[A-Z]{3}$'),
    data_emissao date not null default current_date,
    valor_brl numeric(14,2) not null constraint ck_notas_int_valor_brl check (valor_brl >= 0),
    data_cotacao date not null,
    taxa_aplicada numeric(18,6) not null constraint ck_notas_int_taxa check (taxa_aplicada > 0),
    valor_moeda numeric(14,2) not null constraint ck_notas_int_valor_moeda check (valor_moeda >= 0),
    aliquota_aplicada numeric(5,2) not null constraint ck_notas_int_aliquota check (aliquota_aplicada >= 0 and aliquota_aplicada <= 100),
    valor_vat numeric(14,2) not null constraint ck_notas_int_vat check (valor_vat >= 0),
    total_moeda numeric(14,2) not null constraint ck_notas_int_total check (total_moeda >= 0),
    forma_documento varchar(30) not null default 'NAO_TRADICIONAL' constraint ck_notas_int_forma check (forma_documento in ('NAO_TRADICIONAL')),
    status varchar(20) not null default 'EMITIDA' constraint ck_notas_int_status check (status in ('EMITIDA', 'ANULADA')),
    motivo_anulacao text null,
    criado_por uuid null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

comment on table plantaopro.notas_internacionais is
    'R89: nota fiscal nao-tradicional de faturamento internacional amarrada a uma fatura SaaS (faturas_saas.id): valor em BRL (moeda base) convertido pela cotação da data + VAT padrao do pais de destino, como snapshot imutavel; exatamente uma nota EMITIDA por fatura.';
comment on column plantaopro.notas_internacionais.taxa_aplicada is
    'R89: cotação BRL/unidade vigente na emissao (mais recente em data <= data_emissao); snapshot para reauditoria posterior independente do historico atual.';
comment on column plantaopro.notas_internacionais.valor_vat is
    'R89: VAT do destino calculado ACRESCIDO sobre a base convertida: round(valor_moeda * aliquota_aplicada / 100, 2); total_moeda = valor_moeda + valor_vat.';

create index if not exists ix_notas_internacionais_fatura
    on plantaopro.notas_internacionais (fatura_id);

create unique index if not exists ux_v2372_nota_emitida_por_fatura
    on plantaopro.notas_internacionais (fatura_id) where status = 'EMITIDA' and reg_status = 'A';

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('COTACAO_MOEDAS', 'Moedas externas aceitas no faturamento internacional (ISO 4217)', 'USD|EUR|GBP',
 'ISO 4217 - moedas aceitas na conversao de faturas para vendas internacionais nesta etapa',
 null, null, 2026),
('COTACAO_MOEDA_BASE', 'Moeda base do faturamento (faturas saem sempre nessa moeda)', 'BRL',
 'Faturamento SaaS emitido em real brasileiro (faturas_saas.valor); as cotacoes guardam BRL por unidade da moeda externa',
 null, null, 2026),
('NOTA_INTERNACIONAL_FORMA', 'Forma de documento do faturamento internacional simplificado', 'NAO_TRADICIONAL',
 'Nota fiscal nao-tradicional para venda ao exterior; base de dispensas domesticas de exportacao: ICMS (Lei 8.080/1990, art. 39, II) e IPI (Lei 4.502/1964, art. 15, III, c/c Decreto 4.404/2002, arts. 19-23)',
 null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ("20"), "periodicidade|offset_periodos|dia_vencimento|regime" para catalogo de obrigacoes fiscais (R84), "cUF|DOC,DOC,..." para configuracao SEFAZ por UF e versao ("4.0"/"3.00") para layouts de DF-e (R85), "AAAA-MM-DD" para dia base e documento ISO separado por "|" para trilhos de pagamento (R86), "CODIGO|CODIGO" para catalogos de escopos/canais/direitos e meses inteiros para retencoes minimas (R87), codigo "S-XXXX" separado por "|" para catalogo de eventos eSocial e dias/horas/pontos percentuais inteiros para jornada, divisor mensal e aviso previo (R88), e codigos ISO de moeda separados por "|" para cotações de câmbio, ISO puro para moeda base e identificador puro para forma de documento do faturamento internacional (R89); exibicao formata.';
