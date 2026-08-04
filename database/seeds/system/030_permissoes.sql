WITH catalog(modulo,acao) AS (VALUES
 ('ADMIN_SAAS','VER'),('ADMIN_SAAS','GERENCIAR'),('TENANTS','LISTAR'),('TENANTS','CRIAR'),('TENANTS','EDITAR'),('TENANTS','SUSPENDER'),('TENANTS','IMPERSONAR'),
 ('USUARIOS','LISTAR'),('USUARIOS','CRIAR'),('USUARIOS','EDITAR'),('PERFIS','LISTAR'),('PERFIS','GERENCIAR'),
 ('PLANTOES','LISTAR'),('PLANTOES','CRIAR'),('PLANTOES','EDITAR'),('PLANTOES','PUBLICAR'),('PLANTOES','CANCELAR'),
 ('ESCALAS','LISTAR'),('ESCALAS','CONFIRMAR'),('ESCALAS','RECUSAR'),('ESCALAS','SUBSTITUIR'),
 ('PACIENTES','LISTAR'),('PACIENTES','CRIAR'),('CONSULTAS','INICIAR'),('CONSULTAS','EDITAR'),('CONSULTAS','FINALIZAR'),
 ('FINANCEIRO','VER'),('FINANCEIRO','GERENCIAR'),('RELATORIOS','VER'),('RELATORIOS','EXPORTAR'),('AUDITORIA','VER'),('SEGURANCA','GERENCIAR'),
 ('COBERTURA','VER'),('COBERTURA','GERENCIAR'),('COBERTURA','CONVIDAR'),
 ('ESCALAS','REALIZAR'),('FECHAMENTO','VER'),('FECHAMENTO','CONFERIR'),('FECHAMENTO','APROVAR'),('FECHAMENTO','REABRIR'),
 ('FINANCEIRO','APROVAR'),('FINANCEIRO','PAGAR'),('FINANCEIRO','CANCELAR'),('FINANCEIRO','EXPORTAR')
)
INSERT INTO plantaopro.permissoes(id,codigo,nome,descricao,modulo,acao,modulo_id,acao_id,sensivel,status,reg_status)
SELECT md5('permission:'||c.modulo||':'||c.acao)::uuid,c.modulo||'.'||c.acao,c.modulo||' '||c.acao,'Permissão canônica',c.modulo,c.acao,m.id,a.id,a.sensivel,'ATIVO','A'
FROM catalog c JOIN plantaopro.modulos_sistema m ON m.codigo=c.modulo AND m.reg_status='A'
JOIN plantaopro.acoes_sistema a ON a.codigo=c.acao AND a.reg_status='A'
ON CONFLICT DO NOTHING;
