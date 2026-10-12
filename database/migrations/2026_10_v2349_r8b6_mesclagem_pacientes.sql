-- PlantãoPro v2.34.9 (R8-B6): trilha de mesclagem de pacientes.
-- A operação (mesclagem controlada) re-aponta os vínculos do paciente de origem para o alvo e
-- inativa a origem; estas colunas tornam a absorção auditável e idempotente:
--   mesclado_em   -> paciente alvo da absorção (não nula = já mesclado)
--   mesclado_em_em-> instante da mesclagem
--   mesclado_por  -> usuário que executou
-- Idempotente: add column if not exists + índice condicional.

alter table plantaopro.pacientes add column if not exists mesclado_em uuid null;
alter table plantaopro.pacientes add column if not exists mesclado_em_em timestamptz null;
alter table plantaopro.pacientes add column if not exists mesclado_por uuid null;

create index if not exists ix_pacientes_mesclado_em on plantaopro.pacientes (mesclado_em) where mesclado_em is not null;
