-- Índices incrementais para estabilizar a API Mobile MVP de convites.
-- Seguro para execução repetida em PostgreSQL.

create schema if not exists plantaopro;

create index if not exists idx_plantao_convites_medico_status
    on plantaopro.plantao_convites(medico_id, status, reg_status, data_envio desc);

create index if not exists idx_plantao_convites_medico_id
    on plantaopro.plantao_convites(id, medico_id, reg_status);
