-- PlantaoPro v2.36.6 (R83 - bloco B4, Simples Nacional): parametros legais que a previa
-- do DAS consome. Mesma filosofia da fonte unica (v2363 a v2365): nenhum numero legal
-- vive no codigo; cada linha tem fonte citavel. As tabelas sao valores fixos em reais
-- (nao indexadas ao salario minimo), por isso vigencia nula (regra fixa), padrao v2363.
-- Tabela oficial: anexos I a V da LC 123/2006 na redacao da LC 155/2016 (vigencia
-- 01/01/2018), texto consolidado em gov.br/planalto (lcp123.htm). Enquadramento da
-- atividade: art. 18 paragrafos 4o e 5o-B a 5o-I. Formula de apuracao: art. 18
-- capitulo e paragrafos 1o, 1o-A e 3o (aliquota efetiva = (RBT12 x aliquota - parcela)
-- / RBT12, aplicada sobre a receita bruta do mes). Faixas usam o formato canonico
-- "limite|aliquota_pct|parcela_a_deduzir" (v2364), em que o limite e a receita bruta
-- acumulada nos 12 meses anteriores ao periodo de apuracao.
-- Idempotente (on conflict do nothing, padrao v2362 a v2365).

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('SIMPLES_ANEXO_1_FAIXA_1', 'Anexo I (comercio por revenda) - faixa 1: receita acumulada em 12 meses ate 180.000,00', '180000.00|4.00|0.00',
 'LC 123/2006 anexo I - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_1_FAIXA_2', 'Anexo I (comercio por revenda) - faixa 2: de 180.000,01 a 360.000,00', '360000.00|7.30|5940.00',
 'LC 123/2006 anexo I - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_1_FAIXA_3', 'Anexo I (comercio por revenda) - faixa 3: de 360.000,01 a 720.000,00', '720000.00|9.50|13860.00',
 'LC 123/2006 anexo I - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_1_FAIXA_4', 'Anexo I (comercio por revenda) - faixa 4: de 720.000,01 a 1.800.000,00', '1800000.00|10.70|22500.00',
 'LC 123/2006 anexo I - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_1_FAIXA_5', 'Anexo I (comercio por revenda) - faixa 5: de 1.800.000,01 a 3.600.000,00', '3600000.00|14.30|87300.00',
 'LC 123/2006 anexo I - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_1_FAIXA_6', 'Anexo I (comercio por revenda) - faixa 6: de 3.600.000,01 a 4.800.000,00', '4800000.00|19.00|378000.00',
 'LC 123/2006 anexo I - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_2_FAIXA_1', 'Anexo II (industria, produtos fabricados pela empresa) - faixa 1: ate 180.000,00', '180000.00|4.50|0.00',
 'LC 123/2006 anexo II - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_2_FAIXA_2', 'Anexo II (industria, produtos fabricados pela empresa) - faixa 2: de 180.000,01 a 360.000,00', '360000.00|7.80|5940.00',
 'LC 123/2006 anexo II - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_2_FAIXA_3', 'Anexo II (industria, produtos fabricados pela empresa) - faixa 3: de 360.000,01 a 720.000,00', '720000.00|10.00|13860.00',
 'LC 123/2006 anexo II - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_2_FAIXA_4', 'Anexo II (industria, produtos fabricados pela empresa) - faixa 4: de 720.000,01 a 1.800.000,00', '1800000.00|11.20|22500.00',
 'LC 123/2006 anexo II - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_2_FAIXA_5', 'Anexo II (industria, produtos fabricados pela empresa) - faixa 5: de 1.800.000,01 a 3.600.000,00', '3600000.00|14.70|85500.00',
 'LC 123/2006 anexo II - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_2_FAIXA_6', 'Anexo II (industria, produtos fabricados pela empresa) - faixa 6: de 3.600.000,01 a 4.800.000,00', '4800000.00|30.00|720000.00',
 'LC 123/2006 anexo II - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_3_FAIXA_1', 'Anexo III (servicos em geral, saude, educacao, locacao de bens moveis) - faixa 1: ate 180.000,00', '180000.00|6.00|0.00',
 'LC 123/2006 anexo III - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_3_FAIXA_2', 'Anexo III (servicos em geral, saude, educacao, locacao de bens moveis) - faixa 2: de 180.000,01 a 360.000,00', '360000.00|11.20|9360.00',
 'LC 123/2006 anexo III - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_3_FAIXA_3', 'Anexo III (servicos em geral, saude, educacao, locacao de bens moveis) - faixa 3: de 360.000,01 a 720.000,00', '720000.00|13.50|17640.00',
 'LC 123/2006 anexo III - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_3_FAIXA_4', 'Anexo III (servicos em geral, saude, educacao, locacao de bens moveis) - faixa 4: de 720.000,01 a 1.800.000,00', '1800000.00|16.00|35640.00',
 'LC 123/2006 anexo III - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_3_FAIXA_5', 'Anexo III (servicos em geral, saude, educacao, locacao de bens moveis) - faixa 5: de 1.800.000,01 a 3.600.000,00', '3600000.00|21.00|125640.00',
 'LC 123/2006 anexo III - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_3_FAIXA_6', 'Anexo III (servicos em geral, saude, educacao, locacao de bens moveis) - faixa 6: de 3.600.000,01 a 4.800.000,00', '4800000.00|33.00|648000.00',
 'LC 123/2006 anexo III - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_4_FAIXA_1', 'Anexo IV (construcao civil, limpeza, vigilancia, advocacia) - faixa 1: ate 180.000,00', '180000.00|4.50|0.00',
 'LC 123/2006 anexo IV - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_4_FAIXA_2', 'Anexo IV (construcao civil, limpeza, vigilancia, advocacia) - faixa 2: de 180.000,01 a 360.000,00', '360000.00|9.00|8100.00',
 'LC 123/2006 anexo IV - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_4_FAIXA_3', 'Anexo IV (construcao civil, limpeza, vigilancia, advocacia) - faixa 3: de 360.000,01 a 720.000,00', '720000.00|10.20|12420.00',
 'LC 123/2006 anexo IV - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_4_FAIXA_4', 'Anexo IV (construcao civil, limpeza, vigilancia, advocacia) - faixa 4: de 720.000,01 a 1.800.000,00', '1800000.00|14.00|39780.00',
 'LC 123/2006 anexo IV - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_4_FAIXA_5', 'Anexo IV (construcao civil, limpeza, vigilancia, advocacia) - faixa 5: de 1.800.000,01 a 3.600.000,00', '3600000.00|22.00|183780.00',
 'LC 123/2006 anexo IV - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_4_FAIXA_6', 'Anexo IV (construcao civil, limpeza, vigilancia, advocacia) - faixa 6: de 3.600.000,01 a 4.800.000,00', '4800000.00|33.00|828000.00',
 'LC 123/2006 anexo IV - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_5_FAIXA_1', 'Anexo V (consultoria, auditoria, veterinaria, engenharia, publicidade, traducao) - faixa 1: ate 180.000,00', '180000.00|15.50|0.00',
 'LC 123/2006 anexo V - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_5_FAIXA_2', 'Anexo V (consultoria, auditoria, veterinaria, engenharia, publicidade, traducao) - faixa 2: de 180.000,01 a 360.000,00', '360000.00|18.00|4500.00',
 'LC 123/2006 anexo V - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_5_FAIXA_3', 'Anexo V (consultoria, auditoria, veterinaria, engenharia, publicidade, traducao) - faixa 3: de 360.000,01 a 720.000,00', '720000.00|19.50|9900.00',
 'LC 123/2006 anexo V - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_5_FAIXA_4', 'Anexo V (consultoria, auditoria, veterinaria, engenharia, publicidade, traducao) - faixa 4: de 720.000,01 a 1.800.000,00', '1800000.00|20.50|17100.00',
 'LC 123/2006 anexo V - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_5_FAIXA_5', 'Anexo V (consultoria, auditoria, veterinaria, engenharia, publicidade, traducao) - faixa 5: de 1.800.000,01 a 3.600.000,00', '3600000.00|23.00|62100.00',
 'LC 123/2006 anexo V - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_ANEXO_5_FAIXA_6', 'Anexo V (consultoria, auditoria, veterinaria, engenharia, publicidade, traducao) - faixa 6: de 3.600.000,01 a 4.800.000,00', '4800000.00|30.50|540000.00',
 'LC 123/2006 anexo V - tabela oficial de faixas (redacao LC 155/2016, vigencia 01/01/2018), gov.br/planalto', null, null, 2026),
('SIMPLES_LIMITE_ME_RECEITA_ANUAL', 'Teto de receita bruta anual da microempresa (ME)', '360000.00',
 'LC 123/2006 art. 3 inciso I - microempresa: receita bruta anual igual ou inferior a R$ 360.000,00', null, null, 2026),
('SIMPLES_LIMITE_EPP_RECEITA_ANUAL', 'Teto de receita bruta anual da empresa de pequeno porte (EPP), limite do Simples Nacional', '4800000.00',
 'LC 123/2006 art. 3 inciso II (redacao LC 155/2016) - empresa de pequeno porte: ate R$ 4.800.000,00', null, null, 2026),
('SIMPLES_DAS_DIA_VENCIMENTO', 'Dia do mes subsequente a competencia para o vencimento padrao do DAS', '20',
 'Regulamento do recolhimento do Simples Nacional (CGSN) - o DAS emitido pelo PGDAS-D vence no dia 20 do mes subsequente a competencia', null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;
