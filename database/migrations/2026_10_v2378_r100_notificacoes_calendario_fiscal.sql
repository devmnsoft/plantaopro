-- PlantaoPro v2.36.8 (R100 - noticacoes pauta-das do calendario fiscal): parametro de
-- escalonamento do alerta de obrigacoes fiscais por tenant (R84).
--
-- Escopo da rodada (plano r66a0, R100): notificacoes pautadas no calendario fiscal — aviso
-- antecipado para obrigacoes com vencimento dentro da janela oficial (MEDIA), que vencem hoje
-- (ALTA) e atrasadas (CRITICO) — com escalonamento ao dono da organizacao quando o atraso apos
-- o vencimento atinge o limiar operacional fixado aqui (dias). Seguindo a convencao da fonte
-- unica (v2363), o numero vive em parametros_legais como parametro operacional citavel: nenhum
-- numero de escalonamento vive no codigo. A janela de alerta antecipado ja vive aqui (R84).
--
-- Idempotente: insert ... on conflict do nothing (padrao v2363).

insert into plantaopro.parametros_legais (codigo, descricao, valor, fonte_oficial, vigencia_inicio, vigencia_fim, ano) values
('OBRIGACOES_FISCAIS_ESCALONAMENTO_ATRASO_DIAS', 'Atraso minimo (dias) apos o vencimento de uma obrigacao fiscal para escalar o alerta para o dono da organizacao', '7',
 'Parametro operacional fixado no plano r66a0 (R100) - simplificacao de planejamento, nao valor legal', null, null, 2026)
on conflict (codigo, ano) where reg_status = 'A' do nothing;
