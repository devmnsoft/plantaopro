-- PlantaoPro v2.36.8 (R85 - bloco B4, legislacao brasileira): DF-e nacional - configuracao
-- SEFAZ por UF e layouts oficiais vigentes na fonte unica (parametros_legais).
--
-- Escopo da rodada (plano r66a0, R85): configuracao SEFAZ por UF, layouts (NF-e 4.0, NFC-e,
-- CF-e-SAT, CF-e-Servicos 3.0) e chave de acesso mod-11 com DV por UF. Esta migracao planta a
-- configuracao institucional: os 27 codigos oficiais cUF de UF com o conjunto de documentos
-- fiscais eletronicos apoiados em cada uma, e a versao vigente de cada layout gerido.
-- O algoritmo do dígito verificador (pesos 2..9 da direita para a esquerda, resto 0 ou 1 => DV 0)
-- e deterministico e vive no servico (DfeNacionalService), nao como parametro: ele nao muda por
-- ano/UF e o que varia por UF esta exatamente aqui (cUF + documentos apoiados).
--
-- Formato canonico do valor das linhas SEFAZ_UF_<UF>: "cUF|DOC,DOC,..." onde DOC pertence a
-- {NFE, NFC-E, CFE_SAT, CFE_SERVICOS}. Regra fixada na data da rodada (out/2026): NFE, NFC-E e
-- CFE_SERVICOS sao nacionais (CF-e Servicos e federal/DREX); os ambientes SAT apoiam NFE, CF-e
-- SAT e CF-e Servicos (o CF-e SAT substitui a NFC-e nesses estados). A lista de ambientes SAT
-- segue Portaria SVS/ME n 159/2011 e atos de extensao, verificada na consulta publica SEFAZ
-- Nacional (gov.br/solucoes); estados podem migrar de volta para a NFC-e nacional por ato proprio.
-- Os cUF sao estaveis desde o Ajuste SINIEF 07/05 (tabela de codigos de UF do DF-e).
--
-- Idempotente: insert ... on conflict do nothing (padrao v2363/v2367).

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('SEFAZ_UF_RO', 'SEFAZ Rondonia - DF-e nacional por UF (cUF oficial e documentos apoiados)', '11|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_AC', 'SEFAZ Acre - DF-e nacional por UF (cUF oficial e documentos apoiados)', '12|NFE,CFE_SAT,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); ambiente SAT por Portaria SVS/ME n 159/2011 e extensoes (out/2026)', null, null, 2026),
('SEFAZ_UF_AM', 'SEFAZ Amazonas - DF-e nacional por UF (cUF oficial e documentos apoiados)', '13|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_RR', 'SEFAZ Roraima - DF-e nacional por UF (cUF oficial e documentos apoiados)', '14|NFE,CFE_SAT,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); ambiente SAT por Portaria SVS/ME n 159/2011 e extensoes (out/2026)', null, null, 2026),
('SEFAZ_UF_PA', 'SEFAZ Para - DF-e nacional por UF (cUF oficial e documentos apoiados)', '15|NFE,CFE_SAT,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); ambiente SAT por Portaria SVS/ME n 159/2011 e extensoes (out/2026)', null, null, 2026),
('SEFAZ_UF_AP', 'SEFAZ Amapa - DF-e nacional por UF (cUF oficial e documentos apoiados)', '16|NFE,CFE_SAT,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); ambiente SAT por Portaria SVS/ME n 159/2011 e extensoes (out/2026)', null, null, 2026),
('SEFAZ_UF_TO', 'SEFAZ Tocantins - DF-e nacional por UF (cUF oficial e documentos apoiados)', '17|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_MA', 'SEFAZ Maranhao - DF-e nacional por UF (cUF oficial e documentos apoiados)', '21|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_PI', 'SEFAZ Piaui - DF-e nacional por UF (cUF oficial e documentos apoiados)', '22|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_CE', 'SEFAZ Ceara - DF-e nacional por UF (cUF oficial e documentos apoiados)', '23|NFE,CFE_SAT,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); ambiente SAT por Portaria SVS/ME n 159/2011 e extensoes (out/2026)', null, null, 2026),
('SEFAZ_UF_RN', 'SEFAZ Rio Grande do Norte - DF-e nacional por UF (cUF oficial e documentos apoiados)', '24|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_PB', 'SEFAZ Paraiba - DF-e nacional por UF (cUF oficial e documentos apoiados)', '25|NFE,CFE_SAT,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); ambiente SAT por Portaria SVS/ME n 159/2011 e extensoes (out/2026)', null, null, 2026),
('SEFAZ_UF_PE', 'SEFAZ Pernambuco - DF-e nacional por UF (cUF oficial e documentos apoiados)', '26|NFE,CFE_SAT,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); ambiente SAT por Portaria SVS/ME n 159/2011 e extensoes (out/2026)', null, null, 2026),
('SEFAZ_UF_AL', 'SEFAZ Alagoas - DF-e nacional por UF (cUF oficial e documentos apoiados)', '27|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_SE', 'SEFAZ Sergipe - DF-e nacional por UF (cUF oficial e documentos apoiados)', '28|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_BA', 'SEFAZ Bahia - DF-e nacional por UF (cUF oficial e documentos apoiados)', '29|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_MG', 'SEFAZ Minas Gerais - DF-e nacional por UF (cUF oficial e documentos apoiados)', '31|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_ES', 'SEFAZ Espirito Santo - DF-e nacional por UF (cUF oficial e documentos apoiados)', '32|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_RJ', 'SEFAZ Rio de Janeiro - DF-e nacional por UF (cUF oficial e documentos apoiados)', '33|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_SP', 'SEFAZ Sao Paulo - DF-e nacional por UF (cUF oficial e documentos apoiados)', '35|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_PR', 'SEFAZ Parana - DF-e nacional por UF (cUF oficial e documentos apoiados)', '41|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_SC', 'SEFAZ Santa Catarina - DF-e nacional por UF (cUF oficial e documentos apoiados)', '42|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_RS', 'SEFAZ Rio Grande do Sul - DF-e nacional por UF (cUF oficial e documentos apoiados)', '43|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_MS', 'SEFAZ Mato Grosso do Sul - DF-e nacional por UF (cUF oficial e documentos apoiados)', '50|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_MT', 'SEFAZ Mato Grosso - DF-e nacional por UF (cUF oficial e documentos apoiados)', '51|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_GO', 'SEFAZ Goias - DF-e nacional por UF (cUF oficial e documentos apoiados)', '52|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('SEFAZ_UF_DF', 'SEFAZ Distrito Federal - DF-e nacional por UF (cUF oficial e documentos apoiados)', '53|NFE,NFC-E,CFE_SERVICOS',
 'Tabela de codigos de UF do DF-e (Ajuste SINIEF 07/05 - gov.br/nfe); documentos por consulta publica SEFAZ Nacional (out/2026)', null, null, 2026),
('DFE_LAYOUT_NFE', 'NF-e: versao vigente do layout oficial', '4.0',
 'Manual de Orientacao do Contribuinte da NF-e - layout XML v4.00 (gov.br/nfe)', null, null, 2026),
('DFE_LAYOUT_NFCE', 'NFC-e: versao vigente do layout oficial', '4.0',
 'Manual de Orientacao do Contribuinte da NFC-e - layout XML v4.00 (gov.br/nfce)', null, null, 2026),
('DFE_LAYOUT_CFE_SAT', 'CF-e SAT: versao vigente do layout oficial (ambientes SAT)', '1.00',
 'Manual CF-e SAT / Portaria SVS ME n 159/2011 (gov.br/sat); versao consultada na SEFAZ do ambiente em out/2026', null, null, 2026),
('DFE_LAYOUT_CFE_SERVICOS', 'CF-e Servicos: versao vigente do layout oficial (federal DREX)', '3.00',
 'Manual CF-e Servicos 3.0 - DREX (gov.br/notasfiscais)', null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on table plantaopro.parametros_legais is
    'Fonte unica de parametros legais e institucionais (R80 - bloco B4): salario minimo, aliquotas, obracoes fiscais (R84), configuracao SEFAZ por UF e layouts de DF-e (R85); valor em texto canonico.';
comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ("20"), "periodicidade|offset_periodos|dia_vencimento|regime" para catalogo de obrigacoes fiscais (R84), "cUF|DOC,DOC,..." para configuracao SEFAZ por UF e versao ("4.0"/"3.00") para layouts de DF-e (R85); exibicao formata.';
