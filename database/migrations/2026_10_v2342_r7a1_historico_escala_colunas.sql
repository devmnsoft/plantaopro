-- PlantãoPro v2.34.2 (R7-A1): colunas do histórico de escalas referenciadas pelo código canônico.
-- As transições de escala (EscalaService.AddHistoricoAsync) gravam escala_id/status_anterior/status_novo/
-- justificativa/usuario_id/reg_date e o Portal Profissional lê escala_id/status_novo/reg_date para
-- derivar "confirmado em" e "pode confirmar"; todas essas colunas estavam ausentes da tabela
-- (falha 500 latente no aceite de convite e na listagem do portal).

alter table plantaopro.historico_escala
    add column if not exists escala_id uuid,
    add column if not exists status_anterior text,
    add column if not exists status_novo text,
    add column if not exists justificativa text,
    add column if not exists usuario_id uuid,
    add column if not exists reg_date timestamptz;

create index if not exists ix_historico_escala_escala
    on plantaopro.historico_escala(escala_id);
