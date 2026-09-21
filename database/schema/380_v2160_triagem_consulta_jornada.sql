-- PlantãoPro v2.16.0 — identidade, concorrência e evidência da jornada clínica.
-- Migration incremental: não altera artefatos já aplicados.
set search_path to plantaopro, public;

alter table plantaopro.triagens
    add column if not exists versao integer not null default 1,
    add column if not exists finalizada_em timestamptz,
    add column if not exists finalizada_por uuid,
    add column if not exists assumida_por uuid,
    add column if not exists assumida_em timestamptz,
    add column if not exists atendimento_id uuid,
    add column if not exists unidade_id uuid,
    add column if not exists cliente_id uuid,
    add column if not exists paciente_id uuid,
    add column if not exists agendamento_id uuid,
    add column if not exists classificacao_risco text,
    add column if not exists queixa_principal text,
    add column if not exists pressao_sistolica numeric(6,2),
    add column if not exists pressao_diastolica numeric(6,2),
    add column if not exists frequencia_cardiaca numeric(6,2),
    add column if not exists frequencia_respiratoria numeric(6,2),
    add column if not exists temperatura numeric(6,2),
    add column if not exists saturacao numeric(6,2),
    add column if not exists peso numeric(8,2),
    add column if not exists altura numeric(5,2),
    add column if not exists imc numeric(8,2),
    add column if not exists glicemia numeric(8,2),
    add column if not exists alergias_relatadas text,
    add column if not exists medicamentos_uso text,
    add column if not exists observacoes text,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists created_by uuid,
    add column if not exists updated_by uuid;

create table if not exists plantaopro.atendimentos_fila (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    unidade_id uuid null,
    agendamento_id uuid not null,
    paciente_id uuid null,
    senha text null,
    status text not null default 'AGUARDANDO',
    prioridade integer not null default 0,
    checkin_em timestamptz not null default now(),
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    reg_update timestamptz null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

alter table plantaopro.consultas
    add column if not exists cliente_id uuid,
    add column if not exists paciente_id uuid,
    add column if not exists unidade_id uuid,
    add column if not exists triagem_id uuid,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now();

create table if not exists plantaopro.triagem_encaminhamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    triagem_id uuid not null,
    destino text not null default 'CONSULTA',
    status text not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    paciente_id uuid null,
    consulta_id uuid null,
    evento varchar(50) not null,
    versao integer not null default 1,
    created_by uuid null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

do $$
declare conflitos text;
begin
  select string_agg(format('tenant=%s agendamento=%s atendimentos=%s', cliente_id, agendamento_id, ids), '; ')
    into conflitos
    from (
      select cliente_id, agendamento_id, string_agg(id::text, ',' order by id) ids
        from plantaopro.atendimentos_fila
       where status not in ('FINALIZADO','CANCELADO')
       group by cliente_id, agendamento_id having count(*) > 1
    ) d;
  if conflitos is not null then
    raise exception 'V2160_ATENDIMENTOS_ATIVOS_DUPLICADOS: %. Corrija os estados com registro auditável antes de repetir a migration.', conflitos;
  end if;
end $$;

-- Recupera a identidade operacional sem apagar ou fundir registros legados.
update plantaopro.triagens t
   set atendimento_id = a.id,
       unidade_id = coalesce(t.unidade_id, a.unidade_id)
  from plantaopro.atendimentos_fila a
 where t.atendimento_id is null
   and t.agendamento_id = a.agendamento_id
   and t.cliente_id = a.cliente_id
   and a.status not in ('FINALIZADO','CANCELADO');

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
declare deve_capturar boolean;
begin
  if tg_op = 'INSERT' then
    deve_capturar := true;
  else
    deve_capturar := new.triagem_id is distinct from old.triagem_id;
  end if;
  if new.triagem_id is not null and deve_capturar then
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
      where t.id=new.triagem_id and t.cliente_id=new.cliente_id
        and t.paciente_id=new.paciente_id and t.reg_status='A';
    if new.triagem_snapshot is null then
      raise exception 'V2160_TRIAGEM_INCOMPATIVEL: triagem %, consulta %, tenant % e paciente % não possuem vínculo ativo correspondente.', new.triagem_id, new.id, new.cliente_id, new.paciente_id;
    end if;
    new.triagem_snapshot_em=now();
  end if;
  return new;
end $$;
drop trigger if exists tr_v2160_snapshot_triagem_insert on plantaopro.consultas;
drop trigger if exists tr_v2160_snapshot_triagem_update on plantaopro.consultas;
drop trigger if exists tr_v2160_snapshot_triagem on plantaopro.consultas;
create trigger tr_v2160_snapshot_triagem_insert before insert
on plantaopro.consultas for each row execute function plantaopro.v2160_snapshot_triagem_consulta();
create trigger tr_v2160_snapshot_triagem_update before update of triagem_id
on plantaopro.consultas for each row
when (new.triagem_id is distinct from old.triagem_id)
execute function plantaopro.v2160_snapshot_triagem_consulta();
