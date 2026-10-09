-- R5-D11: completude do catalogo de permissoes canonicas do ADMINISTRADOR_CLIENTE para os modulos
-- finos de jornada Saude 360. Contexto: a homologacao D11 mostrou que o contrato SAUDE360 habilitado
-- (B5) e os grants do suite generico SAUDE360.* (v2334) ainda deixam o gestor sem os codigos finos
-- que o guard Web cobra por controller (SaasRouteGuardFilter: Pacientes->PACIENTES, Consultas->
-- CONSULTAS, Triagem->TRIAGEM, Agendamentos->AGENDAMENTOS, ClinicaDashboard->CLINICA_DASHBOARD).
-- Sem PACIENTES.* no perfil, /Pacientes caia em PERMISSAO_NEGADA mesmo com modulo PACIENTES ATIVO no
-- contrato - o mesmo defeito que v2334 corrigiu para PLANTOES/SAUDE360, agora na camada fina.
-- Mesmo contrato de v2334: quem contrata um modulo administra o modulo contratado; o gate de
-- vigencia (tenant_modulos + B4/B6) continua obrigatorio e codigos inexistentes em modulos_sistema
-- (ex.: TRIAGEM, ate decisao de catalogo E13) viram no-op natural do join. Desta vez cobre tambem os
-- perfis base por tenant criados pelo kernel B5 (ba7faac0-... nao recebia nada dos suites).
-- Aditiva e idempotente: so insere o que falta; nunca remove ou reduz grants existentes.
insert into plantaopro.perfil_permissoes (
    id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
)
select
    gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
from plantaopro.perfis pf
join plantaopro.permissoes p on p.reg_status = 'A'
join plantaopro.modulos_sistema ms on ms.id = p.modulo_id
    and upper(ms.codigo) in ('PACIENTES','CONSULTAS','AGENDAMENTOS','TRIAGEM','UNIDADES','CLINICA_DASHBOARD')
where upper(pf.codigo) = 'ADMINISTRADOR_CLIENTE'
  and pf.reg_status = 'A'
  and not exists (
      select 1 from plantaopro.perfil_permissoes pp
      where pp.perfil_id = pf.id and pp.permissao_id = p.id
  );
