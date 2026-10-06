-- R4-B6 (rodada 4, item B6 governanca SaaS): colunas comerciais ausentes em plantaopro.assinaturas.
-- Os endpoints /api/assinaturas (Criar/AlterarStatus) e o SELECT de ObterAssinaturaAtual
-- escrevem/lemem data_inicio, observacoes, motivo_cancelamento e data_cancelamento;
-- nenhuma migracao anterior as criou, o que causava erro 42703 em tempo de execucao
-- sem cobertura de teste viva (defeito preexistente, fechado em B6).
-- Adicao aditiva e idempotente: todas as colunas aceitam null e nao afetam linhas existentes.

alter table plantaopro.assinaturas add column if not exists data_inicio date null;
alter table plantaopro.assinaturas add column if not exists observacoes text null;
alter table plantaopro.assinaturas add column if not exists motivo_cancelamento text null;
alter table plantaopro.assinaturas add column if not exists data_cancelamento timestamptz null;
