WITH catalog(codigo,nome) AS (VALUES
 ('ADMINISTRADOR_GLOBAL','Administrador global'),('ADMINISTRADOR_CLIENTE','Administrador do cliente'),('ADMINISTRADOR_CLINICA','Administrador da clínica'),
 ('COORDENACAO','Coordenação'),('OPERADOR','Operador'),('MEDICO','Médico'),('HOSPITAL','Hospital'),('RECEPCAO','Recepção'),('TRIAGEM','Triagem'),
 ('ENFERMAGEM','Enfermagem'),('FINANCEIRO','Financeiro'),('FATURAMENTO_CONVENIO','Faturamento de convênio'),('AUDITOR','Auditor'),
 ('AUDITOR_CLINICO','Auditor clínico'),('SUPORTE','Suporte')
)
INSERT INTO plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status)
SELECT md5('profile:'||codigo)::uuid,NULL,NULL,codigo,nome,'Perfil canônico de sistema',true,false,'ATIVO','A' FROM catalog
ON CONFLICT DO NOTHING;
