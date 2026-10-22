-- PlantaoPro v2.36.9 (R86 - trilhos de pagamento): configuração global dos trilhos e
-- constantes institucionais na fonte unica (parametros_legais), amarradas ao motor de
-- cobranca SaaS (faturas_saas / cobranca_cobrancas).
--
-- Escopo da rodada (plano r66a0, R86): PIX EMV (QR Code copia-e-cola com CRC16-CCITT,
-- geracao e validacao), boleto (codigo de barras 44 + linha digitavel 47 + fator de
-- vencimento) e arquivo CNAB 400 de remessa, todos derivando das faturas abertas do
-- motor de cobranca. FEBEN (ISO 20022) entra como catalogo institucional nesta rodada;
-- a emissao XML/FEBEN e o CNAB 240 seguem no marco de homologacao externa (mesma regra
-- do produto: nenhuma integracao bancaria real ainda, sem credenciais de banco).
--
-- A tabela de configuracao e GLOBAL (plano SaaS, como cobranca_providers): a conta que
-- recebe as faturas e a chave PIX sao da operacao que emite as faturas, nao por cliente.
-- Restricoes fixadas nas especificacoes citaveis:
--   * conta_cedente com exatamente 7 digitos: campo livre do codigo de barras, posicoes
--     37-43 (BACEN Carta-Circular 2926, 07/2000); no CNAB 400 sai alinhado a esquerda em
--     campo de 11 digitos;
--   * beneficiario_nome com no maximo 25 e beneficiario_cidade com no maximo 15
--     caracteres: limites oficiais dos campos 59 e 60 do QR Code BR Code (BACEN/EMVCo);
--   * nosso_numero_base cabe no campo de 11 digitos do campo livre e no campo D17 do
--     registro do CNAB 400.
--
-- Idempotente: create table if not exists + insert ... on conflict do nothing
-- (padrao v2332/v2363/v2367/v2368).

create table if not exists plantaopro.cobranca_trilhos_config (
    id uuid primary key default gen_random_uuid(),
    banco_codigo char(3) not null constraint ck_cobranca_trilhos_banco check (banco_codigo ~ '^[0-9]{3}$'),
    banco_nome varchar(60) not null constraint ck_cobranca_trilhos_banco_nome check (char_length(banco_nome) between 1 and 60),
    agencia char(4) not null constraint ck_cobranca_trilhos_agencia check (agencia ~ '^[0-9]{4}$'),
    carteira char(2) not null constraint ck_cobranca_trilhos_carteira check (carteira ~ '^[0-9]{2}$'),
    conta_cedente char(7) not null constraint ck_cobranca_trilhos_conta check (conta_cedente ~ '^[0-9]{7}$'),
    nosso_numero_base bigint not null constraint ck_cobranca_trilhos_nosso check (nosso_numero_base between 1 and 99999999999),
    chave_pix text not null constraint ck_cobranca_trilhos_chave check (char_length(chave_pix) between 1 and 64),
    beneficiario_nome varchar(25) not null constraint ck_cobranca_trilhos_nome check (char_length(beneficiario_nome) between 1 and 25),
    beneficiario_cidade varchar(15) not null constraint ck_cobranca_trilhos_cidade check (char_length(beneficiario_cidade) between 1 and 15),
    ativo boolean not null default true,
    criado_em timestamp not null default now(),
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

comment on table plantaopro.cobranca_trilhos_config is
    'R86: configuracao global dos trilhos de pagamento (PIX EMV + boleto + CNAB 400) do plano SaaS, sem dimensao de tenant, no padrao de cobranca_providers.';
comment on column plantaopro.cobranca_trilhos_config.conta_cedente is
    'R86: exatamente 7 digitos - campo livre do codigo de barras do boleto, posicoes 37-43 (BACEN Carta-Circular 2926); no CNAB 400 sai alinhado a esquerda em campo de 11 digitos.';
comment on column plantaopro.cobranca_trilhos_config.beneficiario_nome is
    'R86: maximo 25 caracteres - limite oficial do campo 59 do QR Code BR Code (BACEN/EMVCo); usar letras sem acento.';
comment on column plantaopro.cobranca_trilhos_config.beneficiario_cidade is
    'R86: maximo 15 caracteres - limite oficial do campo 60 do QR Code BR Code (BACEN/EMVCo); usar letras sem acento.';
comment on column plantaopro.cobranca_trilhos_config.nosso_numero_base is
    'R86: base da sequencia de nosso numero; cada remessa sequencia base+1..base+N dentro do arquivo. Cabe nos campos de 11 e 17 digitos.';

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('TRILHO_PIX_GUI', 'Pix: GUI registrada no payload BR Code (campo 26>00)', 'br.gov.bcb.pix',
 'Banco Central do Brasil - GUI Pix br.gov.bcb.pix (Especificacao Pix, Manual do QR Code BR - gov.br/pix)', null, null, 2026),
('TRILHO_PIX_MCC_PADRAO', 'Pix: MCC padrao do payload BR Code (campo 52)', '0000',
 'Especificacao BR Code (BACEN/EMVCo) - MCC 0000 para payload Pix sem categoria de comercio', null, null, 2026),
('TRILHO_BOLETO_MOEDA', 'Boleto: codigo da moeda na posicao 4 do codigo de barras', '9',
 'BACEN Carta-Circular 2926 (25/07/2000) - codigo de moeda 9 (Real) na composicao do codigo de barras do bloqueto', null, null, 2026),
('TRILHO_BOLETO_BASE_FATOR_DIA', 'Boleto: data base do fator de vencimento (dias corridos ate o vencimento)', '1997-10-07',
 'BACEN Carta-Circular 2926 - marco zero 07/10/1997; a tabela oficial de correlacao se encerra em 21/02/2025 (fator 9999), apos o que vale rollover: fator exibido = dias totais mod 10000', null, null, 2026),
('TRILHO_CNAB_REMESSA_LAYOUT', 'Arquivo de remessa: layout canonico gerido pelo produto', '400',
 'Manual Febraban Boleto/CNAB - layout classico 400 para remessa de cobranca; o CNAB 240 e a homologacao externa entram no proximo marco', null, null, 2026),
('TRILHO_FEBEN_DOCUMENTOS', 'FEBEN: tipos de mensagem ISO 20022 de gestao de lote (remessa e retorno)', 'camt.239.00|camt.240.00',
 'Especificacao FEBEN (ISO 20022) - ManagementAdvice/Management Response Advice para gestao de lote de cobranca (bc.gov.br/estabilidadefinanceira/ISO20022)', null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ("20"), "periodicidade|offset_periodos|dia_vencimento|regime" para catalogo de obrigacoes fiscais (R84), "cUF|DOC,DOC,..." para configuracao SEFAZ por UF e versao ("4.0"/"3.00") para layouts de DF-e (R85), "AAAA-MM-DD" para dia base e documento ISO separado por "|" para trilhos de pagamento (R86); exibicao formata.';
