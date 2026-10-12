-- PlantãoPro v2.34.8 (R8-B5): backfill de medico_especialidades a partir de medicos.especialidade_id.
-- O M:N nasceu em v2341 para sustentar a elegibilidade por especialidade e o score de recomendação
-- (BusinessRulesServices), mas nenhum writer produtivo o populava — a tabela nasce vazia em banco real
-- (matriz r8a0 linha "Especialidades na operação" / varredura A2). A partir daqui o MedicoService
-- espelha a especialidade do formulário (replace-all, histórico via reg_status); este backfill aplica a
-- mesma regra aos médicos já existentes: reativa a linha histórica quando existe, senão insere.
-- Idempotente: repetir não cria duplicatas nem desativa vínculos.
-- Defensivo: médicos com especialidade_id órfã (especialidade já inexistente) são pulados no insert.

update plantaopro.medico_especialidades me set reg_status='A'
from plantaopro.medicos m
where me.medico_id = m.id
  and me.especialidade_id = m.especialidade_id
  and me.reg_status <> 'A'
  and m.reg_status = 'A'
  and m.especialidade_id is not null
  and m.especialidade_id <> '00000000-0000-0000-0000-000000000000';

insert into plantaopro.medico_especialidades(medico_id, especialidade_id, reg_status)
select m.id, m.especialidade_id, 'A'
from plantaopro.medicos m
where m.reg_status = 'A'
  and m.especialidade_id is not null
  and m.especialidade_id <> '00000000-0000-0000-0000-000000000000'
  and not exists(select 1 from plantaopro.medico_especialidades me where me.medico_id = m.id and me.especialidade_id = m.especialidade_id)
  and exists(select 1 from plantaopro.especialidades e where e.id = m.especialidade_id);
