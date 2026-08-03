-- v1.37.0: catálogo mínimo determinístico e infraestrutura do bootstrap.
SET search_path TO plantaopro, public;
SELECT pg_advisory_lock(hashtext('plantaopro.install.v1370'));

ALTER TABLE plantaopro.schema_migrations
    ADD COLUMN IF NOT EXISTS versao text,
    ADD COLUMN IF NOT EXISTS nome text,
    ADD COLUMN IF NOT EXISTS iniciado_em timestamptz,
    ADD COLUMN IF NOT EXISTS aplicado_em timestamptz,
    ADD COLUMN IF NOT EXISTS duracao_ms bigint,
    ADD COLUMN IF NOT EXISTS status text,
    ADD COLUMN IF NOT EXISTS erro_resumido text,
    ADD COLUMN IF NOT EXISTS executado_por text,
    ADD COLUMN IF NOT EXISTS ambiente text;
UPDATE plantaopro.schema_migrations
SET versao=coalesce(versao,id), nome=coalesce(nome,script_path), iniciado_em=coalesce(iniciado_em,applied_at), aplicado_em=coalesce(aplicado_em,applied_at),
    duracao_ms=coalesce(duracao_ms,0), status=coalesce(status,'APLICADA'), executado_por=coalesce(executado_por,current_user),
    ambiente=coalesce(ambiente,'UNKNOWN');
ALTER TABLE plantaopro.schema_migrations
    ALTER COLUMN versao SET DEFAULT '', ALTER COLUMN versao SET NOT NULL,
    ALTER COLUMN nome SET DEFAULT '', ALTER COLUMN nome SET NOT NULL,
    ALTER COLUMN duracao_ms SET DEFAULT 0, ALTER COLUMN duracao_ms SET NOT NULL,
    ALTER COLUMN status SET DEFAULT 'APLICADA', ALTER COLUMN status SET NOT NULL;


WITH catalog(codigo,nome,ordem) AS (VALUES
 ('ADMIN_SAAS','Administração SaaS',10),('TENANTS','Tenants',20),('CLIENTES','Clientes',30),('PLANOS','Planos',40),
 ('ASSINATURAS','Assinaturas',50),('USUARIOS','Usuários',60),('PERFIS','Perfis',70),('PERMISSOES','Permissões',80),
 ('AUDITORIA','Auditoria',90),('SEGURANCA','Segurança',100),('CONFIGURACOES','Configurações',110),
 ('FEATURE_FLAGS','Feature flags',120),('OBSERVABILIDADE','Observabilidade',130),('SUPORTE','Suporte',140),
 ('OPERACAO360','Operação 360',150),('SAUDE360','Saúde 360',160),('FINANCEIRO','Financeiro',170),('RELATORIOS','Relatórios',180)
)
INSERT INTO plantaopro.modulos_sistema(id,codigo,nome,descricao,ordem,status,reg_status)
SELECT md5('module:'||c.codigo)::uuid,c.codigo,c.nome,'Módulo canônico PlantãoPro',c.ordem,'ATIVO','A' FROM catalog c
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.modulos_sistema m WHERE upper(btrim(m.codigo))=c.codigo AND m.reg_status='A');

WITH catalog(codigo,nome,ordem,sensivel) AS (VALUES
 ('VER','Ver',10,false),('LISTAR','Listar',20,false),('CRIAR','Criar',30,false),('EDITAR','Editar',40,false),
 ('INATIVAR','Inativar',50,true),('REATIVAR','Reativar',60,true),('APROVAR','Aprovar',70,true),('CANCELAR','Cancelar',80,true),
 ('EXCLUIR','Excluir',90,true),('EXPORTAR','Exportar',100,true),('CONFIGURAR','Configurar',110,true),
 ('GERENCIAR','Gerenciar',120,true),('IMPERSONAR','Impersonar',130,true),('AUDITAR','Auditar',140,true),
 ('EXECUTAR','Executar',150,true),('REPROCESSAR','Reprocessar',160,true),('VER_DADOS_SENSIVEIS','Ver dados sensíveis',170,true)
)
INSERT INTO plantaopro.acoes_sistema(id,codigo,nome,descricao,ordem,sensivel,status,reg_status)
SELECT md5('action:'||c.codigo)::uuid,c.codigo,c.nome,'Ação canônica PlantãoPro',c.ordem,c.sensivel,'ATIVO','A' FROM catalog c
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.acoes_sistema a WHERE upper(btrim(a.codigo))=c.codigo AND a.reg_status='A');

WITH combinations(modulo,acao) AS (VALUES
 ('ADMIN_SAAS','VER'),('ADMIN_SAAS','GERENCIAR'),('TENANTS','LISTAR'),('TENANTS','CRIAR'),('TENANTS','EDITAR'),('TENANTS','INATIVAR'),('TENANTS','REATIVAR'),
 ('TENANTS','IMPERSONAR'),('CLIENTES','LISTAR'),('CLIENTES','GERENCIAR'),('PLANOS','LISTAR'),('PLANOS','GERENCIAR'),('ASSINATURAS','LISTAR'),
 ('ASSINATURAS','CANCELAR'),('USUARIOS','LISTAR'),('USUARIOS','GERENCIAR'),('PERFIS','LISTAR'),('PERFIS','GERENCIAR'),
 ('PERMISSOES','LISTAR'),('PERMISSOES','GERENCIAR'),('AUDITORIA','AUDITAR'),('AUDITORIA','EXPORTAR'),('SEGURANCA','VER'),
 ('SEGURANCA','GERENCIAR'),('CONFIGURACOES','CONFIGURAR'),('FEATURE_FLAGS','CONFIGURAR'),('OBSERVABILIDADE','VER'),
 ('SUPORTE','GERENCIAR'),('OPERACAO360','VER'),('OPERACAO360','EXECUTAR'),('SAUDE360','VER'),('SAUDE360','VER_DADOS_SENSIVEIS'),
 ('FINANCEIRO','VER'),('FINANCEIRO','EXPORTAR'),('RELATORIOS','VER'),('RELATORIOS','EXPORTAR')
)
INSERT INTO plantaopro.permissoes(id,codigo,nome,descricao,modulo,acao,modulo_id,acao_id,sensivel,status,reg_status)
SELECT md5('permission:'||c.modulo||':'||c.acao)::uuid,c.modulo||'.'||c.acao,c.modulo||' '||c.acao,
       'Permissão canônica PlantãoPro',c.modulo,c.acao,m.id,a.id,a.sensivel,'ATIVO','A'
FROM combinations c
JOIN plantaopro.modulos_sistema m ON upper(btrim(m.codigo))=c.modulo AND m.reg_status='A'
JOIN plantaopro.acoes_sistema a ON upper(btrim(a.codigo))=c.acao AND a.reg_status='A'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.permissoes p WHERE upper(btrim(p.codigo))=c.modulo||'.'||c.acao AND p.reg_status='A');
