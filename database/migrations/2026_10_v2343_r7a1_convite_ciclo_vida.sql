-- PlantãoPro v2.34.3 (R7-A1): colunas de ciclo de vida de convites operacionais ausentes da estrutura.
-- O código canônico grava/lê data_resposta e motivo_recusa (aceite, recusa, cancelamento e listagens no
-- portal profissional/móvel) e reg_update e reg_date (aceite de convite e envio de convite); nenhuma
-- dessas colunas existia em plantao_convites (falha 500 latente no aceite/recusa e 500 latente nos
-- seletores de convites do portal e do app).

alter table plantaopro.plantao_convites
    add column if not exists data_resposta timestamptz,
    add column if not exists motivo_recusa text,
    add column if not exists reg_update timestamptz,
    add column if not exists reg_date timestamptz not null default now();
