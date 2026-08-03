-- Catálogo real de módulos consumidos pelo runtime. IDs determinísticos tornam o replay seguro.
WITH catalog(codigo,nome,ordem) AS (VALUES
 ('ADMIN_SAAS','Administração SaaS',10),('TENANTS','Tenants',20),('CLIENTES','Clientes',30),('PLANOS','Planos',40),
 ('ASSINATURAS','Assinaturas',50),('USUARIOS','Usuários',60),('PERFIS','Perfis',70),('PERMISSOES','Permissões',80),
 ('PLANTOES','Plantões',90),('ESCALAS','Escalas',100),('PACIENTES','Pacientes',110),('CONSULTAS','Consultas',120),
 ('FINANCEIRO','Financeiro',130),('RELATORIOS','Relatórios',140),('AUDITORIA','Auditoria',150),('SEGURANCA','Segurança',160)
)
INSERT INTO plantaopro.modulos_sistema(id,codigo,nome,descricao,ordem,status,reg_status)
SELECT md5('module:'||codigo)::uuid,codigo,nome,'Módulo canônico PlantãoPro',ordem,'ATIVO','A' FROM catalog
ON CONFLICT DO NOTHING;
