-- PlantãoPro v2.16.0 — identidade, concorrência e evidência da jornada clínica.
-- Migration incremental: não altera artefatos já aplicados.
set search_path to plantaopro, public;

alter table plantaopro.triagens add column if not exists versao integer not null default 1;
alter table plantaopro.triagens add column if not exists finalizada_em timestamptz;
alter table plantaopro.triagens add column if not exists finalizada_por uuid;
alter table plantaopro.triagens add column if not exists assumida_por uuid;
alter table plantaopro.triagens add column if not exists assumida_em timestamptz;

alter table plantaopro.consultas add column if not exists assumida_por uuid;
alter table plantaopro.consultas add column if not exists assumida_em timestamptz;
alter table plantaopro.consultas add column if not exists triagem_snapshot jsonb;
alter table plantaopro.consultas add column if not exists triagem_snapshot_em timestamptz;

-- Um agendamento só pode possuir um atendimento ainda ativo. Walk-ins continuam
-- identificados pelo próprio atendimento e jamais por nome/data do paciente.
create unique index if not exists ux_v2160_atendimento_agendamento_ativo
    on plantaopro.atendimentos_fila(cliente_id, agendamento_id)
    where status not in ('FINALIZADO','CANCELADO');
create unique index if not exists ux_v2160_encaminhamento_triagem_consulta
    on plantaopro.triagem_encaminhamentos(cliente_id, triagem_id, destino)
    where destino='CONSULTA' and reg_status='A';
create index if not exists ix_v2160_triagem_fila_estavel
    on plantaopro.triagens(cliente_id, status, reg_date, id)
    where reg_status='A';
create index if not exists ix_v2160_consulta_fila_estavel
    on plantaopro.consultas(cliente_id, unidade_id, status, reg_date, id)
    where reg_status='A';
create index if not exists ix_v2160_historico_paciente
    on plantaopro.consulta_historico(cliente_id, paciente_id, reg_date desc, id);

-- Congela a evidência de triagem usada quando a consulta é criada/vinculada.
create or replace function plantaopro.v2160_snapshot_triagem_consulta() returns trigger
language plpgsql as $$
begin
  if new.triagem_id is not null and (new.triagem_snapshot is null or new.triagem_id is distinct from old.triagem_id) then
    select jsonb_build_object(
      'triagemId', t.id, 'versao', t.versao, 'classificacaoRisco', t.classificacao_risco,
      'queixaPrincipal', t.queixa_principal, 'pressaoSistolica', t.pressao_sistolica,
      'pressaoDiastolica', t.pressao_diastolica, 'frequenciaCardiaca', t.frequencia_cardiaca,
      'frequenciaRespiratoria', t.frequencia_respiratoria, 'temperatura', t.temperatura,
      'saturacao', t.saturacao, 'glicemia', t.glicemia, 'peso', t.peso, 'altura', t.altura,
      'imc', t.imc, 'alergiasRelatadas', t.alergias_relatadas,
      'medicamentosEmUso', t.medicamentos_uso, 'observacoes', t.observacoes,
      'autorId', coalesce(t.finalizada_por,t.updated_by,t.created_by),
      'finalizadaEm', t.finalizada_em)
      into new.triagem_snapshot
      from plantaopro.triagens t
      where t.id=new.triagem_id and t.cliente_id=new.cliente_id and t.reg_status='A';
    if new.triagem_snapshot is not null then new.triagem_snapshot_em=now(); end if;
  end if;
  return new;
end $$;
drop trigger if exists tr_v2160_snapshot_triagem on plantaopro.consultas;
create trigger tr_v2160_snapshot_triagem before insert or update of triagem_id
on plantaopro.consultas for each row execute function plantaopro.v2160_snapshot_triagem_consulta();
