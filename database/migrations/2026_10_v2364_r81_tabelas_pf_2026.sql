-- PlantaoPro v2.36.4 (R81 - bloco B4, legislacao brasileira): vetores oficiais de
-- tributacao pessoa fisica 2026 (INSS progressivo, IRRF mensal, teto) na fonte unica.
--
-- Ancoragem: no mundo do plano r66a0 o salario minimo 2026 permanece 1.518,00
-- (travado em SALARIO_MINIMO, rodada 80). Os vetores abaixo sao as tabelas oficiais
-- ancoradas nesse minimo: Portaria Interministerial MPS/MF n. 6/2025 (INSS, Anexo II)
-- e a tabela mensal de IRRF vigente com esse minimo (Lei 14.848/2024 / RFB). A fonte
-- de cada linha cita a base legal; a R90 fecha a bateria de contratos legais.
--
-- Formato das faixas (valor = "limite_superior|aliquota_percentual|parcela_a_deduzir",
-- ponto decimal canonico; limite vazio = faixa final sem teto): o desconto e
-- aliquota x base - parcela (metodo "parcela a deduzir"), arredondado a 2 casas.
-- O piso de isencao do IRRF ja existe como PISO_ISENCAO_IRRF_MENSAL (rodada 80).
-- Idempotente (on conflict do nothing, padrao v2362/v2363).

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('INSS_TETO_CONTRIBUICAO', 'Teto do salario-de-contribuicao mensal (INSS)', '8157.41',
 'Portaria Interministerial MPS/MF n. 6/2025 art. 2 (teto com minimo 1.518,00; 2026 do plano r66a0 mantem esse minimo)', date '2026-01-01', date '2026-12-31', 2026),
('INSS_FAIXA_1', 'Faixa INSS progressiva 1 (aliquota sobre ate o limite)', '1518.00|7.5|0.00',
 'Portaria Interministerial MPS/MF n. 6/2025 Anexo II (faixas com minimo 1.518,00)', date '2026-01-01', date '2026-12-31', 2026),
('INSS_FAIXA_2', 'Faixa INSS progressiva 2 (aliquota com parcela a deduzir)', '2793.88|9.0|22.77',
 'Portaria Interministerial MPS/MF n. 6/2025 Anexo II (faixas com minimo 1.518,00)', date '2026-01-01', date '2026-12-31', 2026),
('INSS_FAIXA_3', 'Faixa INSS progressiva 3 (aliquota com parcela a deduzir)', '4190.83|12.0|106.59',
 'Portaria Interministerial MPS/MF n. 6/2025 Anexo II (faixas com minimo 1.518,00)', date '2026-01-01', date '2026-12-31', 2026),
('INSS_FAIXA_4', 'Faixa INSS progressiva 4 (aliquota com parcela a deduzir)', '8157.41|14.0|190.40',
 'Portaria Interministerial MPS/MF n. 6/2025 Anexo II (faixas com minimo 1.518,00)', date '2026-01-01', date '2026-12-31', 2026),
('IRRF_FAIXA_1', 'Faixa IRRF mensal 1 (aliquota com parcela a deduzir)', '2826.65|7.5|169.44',
 'RFB - tabela mensal IRRF vigente com minimo 1.518,00 (Lei 14.848/2024)', date '2026-01-01', date '2026-12-31', 2026),
('IRRF_FAIXA_2', 'Faixa IRRF mensal 2 (aliquota com parcela a deduzir)', '3751.05|15.0|381.44',
 'RFB - tabela mensal IRRF vigente com minimo 1.518,00 (Lei 14.848/2024)', date '2026-01-01', date '2026-12-31', 2026),
('IRRF_FAIXA_3', 'Faixa IRRF mensal 3 (aliquota com parcela a deduzir)', '4664.68|22.5|662.77',
 'RFB - tabela mensal IRRF vigente com minimo 1.518,00 (Lei 14.848/2024)', date '2026-01-01', date '2026-12-31', 2026),
('IRRF_FAIXA_4', 'Faixa IRRF mensal 4 (faixa final, sem teto; aliquota com parcela a deduzir)', '|27.5|896.00',
 'RFB - tabela mensal IRRF vigente com minimo 1.518,00 (Lei 14.848/2024)', date '2026-01-01', date '2026-12-31', 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ("20"); faixas de tributo usam "limite|aliquota_pct|parcela_a_deduzir" (v2364).';
