-- Global recebe o catálogo; demais perfis recebem somente famílias necessárias ao trabalho.
INSERT INTO plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status)
SELECT md5('profile-permission:'||p.codigo||':'||x.codigo)::uuid,p.id,x.id,true,false,'A'
FROM plantaopro.perfis p CROSS JOIN plantaopro.permissoes x
WHERE p.codigo='ADMINISTRADOR_GLOBAL' AND p.reg_status='A' AND x.reg_status='A'
ON CONFLICT DO NOTHING;
WITH matrix(perfil,modulo) AS (VALUES
 ('ADMINISTRADOR_CLIENTE','USUARIOS'),('ADMINISTRADOR_CLIENTE','PERFIS'),('ADMINISTRADOR_CLIENTE','PLANTOES'),('ADMINISTRADOR_CLIENTE','ESCALAS'),
 ('ADMINISTRADOR_CLINICA','USUARIOS'),('ADMINISTRADOR_CLINICA','PLANTOES'),('ADMINISTRADOR_CLINICA','ESCALAS'),('COORDENACAO','PLANTOES'),('COORDENACAO','ESCALAS'),
 ('OPERADOR','PLANTOES'),('OPERADOR','ESCALAS'),('MEDICO','PLANTOES'),('MEDICO','ESCALAS'),('HOSPITAL','PLANTOES'),('HOSPITAL','ESCALAS'),
 ('RECEPCAO','PACIENTES'),('TRIAGEM','PACIENTES'),('TRIAGEM','CONSULTAS'),('ENFERMAGEM','PACIENTES'),('ENFERMAGEM','CONSULTAS'),
 ('FINANCEIRO','FINANCEIRO'),('FATURAMENTO_CONVENIO','FINANCEIRO'),('AUDITOR','AUDITORIA'),('AUDITOR_CLINICO','AUDITORIA'),('SUPORTE','USUARIOS')
)
INSERT INTO plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status)
SELECT md5('profile-permission:'||p.codigo||':'||x.codigo)::uuid,p.id,x.id,true,false,'A'
FROM matrix m JOIN plantaopro.perfis p ON p.codigo=m.perfil AND p.reg_status='A'
JOIN plantaopro.permissoes x ON x.modulo=m.modulo AND x.reg_status='A'
ON CONFLICT DO NOTHING;
