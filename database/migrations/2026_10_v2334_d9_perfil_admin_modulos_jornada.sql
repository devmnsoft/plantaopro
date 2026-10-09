-- R5-D9: completude de permissoes canonicas do perfil ADMINISTRADOR_CLIENTE para modulos de jornada.
-- Contexto: a homologacao D9 mostrou que habilitar um modulo no contrato (B5 habilitar-tenant) ativa o
-- claim de modulo, mas o PERFIL do administrador do cliente so tinha grants parciais semeados:
-- PLANTOES com 5 de 31 acoes (sem PLANTOES.VER -> pagina /Plantoes caia em PERMISSAO_NEGADA apesar de
-- contrato ATIVO) e SAUDE360 sem nenhuma acao. O precedente do catalogo e o suite ADM360, que foi
-- semeado completo (39 acoes) para este mesmo perfil. Esta migration segue o mesmo contrato:
-- quem contrata um modulo administra o modulo contratado; o gate de contrato (tenant_modulos +
-- vigencia B4/B6) continua obrigatorio - aqui só se completa o catalogo de capacidades do perfil,
-- que o proprio cliente pode revogar por override depois (usuario_permissoes_especiais).
-- Aditiva e idempotente: so insere o que ainda nao existe; nunca remove ou reduz grants.
insert into plantaopro.perfil_permissoes (
    id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
)
select
    gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
from plantaopro.perfis pf
join plantaopro.permissoes p on p.reg_status = 'A'
join plantaopro.modulos_sistema ms on ms.id = p.modulo_id and upper(ms.codigo) in ('PLANTOES','SAUDE360')
where upper(pf.codigo) = 'ADMINISTRADOR_CLIENTE'
  and pf.tenant_id is null
  and pf.reg_status = 'A'
  and not exists (
      select 1 from plantaopro.perfil_permissoes pp
      where pp.perfil_id = pf.id and pp.permissao_id = p.id
  );
