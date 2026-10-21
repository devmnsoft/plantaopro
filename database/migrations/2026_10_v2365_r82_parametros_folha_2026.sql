-- PlantaoPro v2.36.5 (R82 - bloco B4, folha de pagamento PF): parametros legais que a
-- folha consome alem dos vetores de tributacao da v2364. Mesma filosofia da fonte unica:
-- nenhum numero legal vive no codigo; cada linha tem fonte citavel.
-- Derivados do salario minimo 2026 (1.518,00 do plano r66a0) ganham vigencia anual;
-- regras estaveis da CLT ficam fixos (vigencia nula), padrao estabelecido na v2363.
-- Idempotente (on conflict do nothing, padrao v2362/v2363/v2364).

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('HORA_EXTRA_ACRESCIMO_PCT', 'Acrescimo minimo legal da hora extra sobre a hora normal', '50.00',
 'CLT art. 7 inciso XIII (EC 72/2013) - acrescimo minimo de 50%', null, null, 2026),
('DIAS_MES_TRABALHADO_HORAS', 'Jornada mensal canonica para apuracao da hora normal', '220',
 'Jornada mensal de 220 horas (CLT art. 58; INSS instrucao geral 120.301)', null, null, 2026),
('DEPENDENTE_DEDUCAO_MENSAL', 'Deducao mensal por dependente na base de IRRF', '189.59',
 'RFB - tabela mensal IRRF vigente com minimo 1.518,00 (deducao por dependente, Lei 14.848/2024)', date '2026-01-01', date '2026-12-31', 2026),
('DEDUCAO_SIMPLIFICADA_MENSAL', 'Limite mensal da deducao simplificada (alternativa aos dependentes)', '564.80',
 'RFB - tabela mensal IRRF vigente com minimo 1.518,00 (deducao simplificada, Lei 14.848/2024)', date '2026-01-01', date '2026-12-31', 2026),
('PISO_ISENCAO_IRRF_DECIMO_TERCEIRO', 'Parte do 13o isenta de IRRF (2x salario minimo)', '3036.00',
 'RIR art. 788 (Decreto 9.580/2018) - isento o 13o ate 2x minimo (2 x 1.518,00)', date '2026-01-01', date '2026-12-31', 2026),
('FGTS_MULTA_RESCISAO_PCT', 'Multa sobre o FGTS na rescisao sem justa causa', '40.00',
 'CLT art. 18 paragrafo unico (Lei 5.889/1973) - multa de 40% do FGTS depositado', null, null, 2026),
('DIAS_CONTAGEM_MES_CREDITADO', 'Dias trabalhados no mes para o mes contar como cheio nas proporcioes', '15',
 'Regra canonica de proporcionais (mes trabalhado a partir de 15 dias conta como cheio - INSS instrucao geral 120.318)', null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;

comment on column plantaopro.parametros_legais.valor is
    'Texto canonico: ponto decimal para numerico ("1518.00"), digito puro para dia do mes ou percentual ("20"); faixas de tributo usam "limite|aliquota_pct|parcela_a_deduzir" (v2364); exibicao formata.';
