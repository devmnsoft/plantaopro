-- PlantãoPro v2.34.1 (R7-A1): invariantes das verificações de conflito/elegibilidade unificadas do profissional.
-- O fluxo canônico de aceite/confirmacao de escala (EscalaService/MedicoElegibilidadeService) consulta
-- medicos.bloqueado e medico_especialidades; ambas as estruturas eram referenciadas pelo código mas
-- ausentes do banco (falha 500 latente em todo aceite de convite operacional). Esta migration materializa
-- a estrutura mínima exigida pela regra, sem alterar histórico.

alter table plantaopro.medicos
    add column if not exists bloqueado boolean not null default false;

create table if not exists plantaopro.medico_especialidades (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    medico_id uuid not null references plantaopro.medicos(id),
    especialidade_id uuid not null references plantaopro.especialidades(id),
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

-- Vínculo ativo único por médico/especialidade; o histórico é preservado via reg_status.
create unique index if not exists ux_medico_especialidade_ativa
    on plantaopro.medico_especialidades(medico_id, especialidade_id)
    where reg_status='A';
