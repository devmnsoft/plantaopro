-- OPT-IN: dados fictícios para uso exclusivo em ambiente local/homologação.
-- Os hashes abaixo são BCrypt cost 11, compatíveis com BCrypt.Net.BCrypt.Verify.
-- As senhas abertas existem somente em docs/usuarios-teste.md.
SET search_path TO plantaopro, public;

DO $seed$
DECLARE
    v_tenant uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca001';
    v_cliente uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca002';
    v_plano uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca003';
    v_medico uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca020';
BEGIN
    IF current_database() IN ('template0', 'template1') THEN
        RAISE EXCEPTION 'Seed de homologação recusado no banco %.', current_database();
    END IF;
    IF to_regclass('plantaopro.usuarios') IS NULL OR to_regclass('plantaopro.perfis') IS NULL THEN
        RAISE EXCEPTION 'Instale o schema canônico antes do seed de homologação.';
    END IF;
    PERFORM pg_advisory_xact_lock(12220260922);

    INSERT INTO plantaopro.planos(id,tenant_id,codigo,nome,status,dados)
    VALUES(v_plano,NULL,'HOMOLOGACAO_360','Plano Homologação Saúde 360','ATIVO','{"whiteLabel":true,"ambiente":"HOMOLOGACAO"}'::jsonb)
    ON CONFLICT(id) DO UPDATE SET status='ATIVO', atualizado_em=now();

    INSERT INTO plantaopro.tenants(id,tenant_id,codigo,nome,status,dados)
    VALUES(v_tenant,v_tenant,'CLINICA_MODELO','Clínica Modelo PlantãoPro','ATIVO',jsonb_build_object('planoId',v_plano,'ambiente','HOMOLOGACAO'))
    ON CONFLICT(id) DO UPDATE SET nome=excluded.nome,status='ATIVO',atualizado_em=now();

    INSERT INTO plantaopro.clientes(id,tenant_id,codigo,nome,status,dados)
    VALUES(v_cliente,v_tenant,'CLINICA_MODELO','Clínica Modelo PlantãoPro','ATIVO',jsonb_build_object('planoId',v_plano,'ambiente','HOMOLOGACAO'))
    ON CONFLICT(id) DO UPDATE SET tenant_id=v_tenant,nome=excluded.nome,status='ATIVO',atualizado_em=now();

    INSERT INTO plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status)
    VALUES(md5('profile:ADMINISTRADOR')::uuid,v_tenant,v_cliente,'ADMINISTRADOR','Administrador','Administração do próprio tenant',true,false,'ATIVO','A')
    ON CONFLICT(id) DO UPDATE SET tenant_id=v_tenant,cliente_id=v_cliente,status='ATIVO',reg_status='A';

    -- Cria/atualiza contas sempre com hashes BCrypt prontos; nunca persiste segredo em claro.
    INSERT INTO plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria,bloqueado_ate)
    VALUES
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca010',NULL,NULL,'Super Admin PlantãoPro','superadmin@plantaopro.local','superadmin@plantaopro.local','$2a$11$KZ80jdGp.ymLQ/E6zk8vluf6o4/.Ur2cEKxjD4Hp4jx5YETf7OaUG','ATIVO','A',false,NULL),
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca011',v_tenant,v_cliente,'Administrador Clínica Modelo','admin.clinica@plantaopro.local','admin.clinica@plantaopro.local','$2a$11$4jafymzm6xqC48JdaVE3GuH0Dy2evtr/dqT7sKDqUe92OwpbaLbX2','ATIVO','A',false,NULL),
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca012',v_tenant,v_cliente,'Médico de Teste','medico@plantaopro.local','medico@plantaopro.local','$2a$11$EIMmoQs8gPeaCFShI4.ACeL2WdJFeFulTrXL4JjBKFgJLCmqc11q2','ATIVO','A',false,NULL),
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca013',v_tenant,v_cliente,'Recepção de Teste','recepcao@plantaopro.local','recepcao@plantaopro.local','$2a$11$1biWFM2YemJyh9DaoRRjXe3K5UpzJdN.GYJMeglzoODIBi9BTW5yK','ATIVO','A',false,NULL),
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca014',v_tenant,v_cliente,'Financeiro de Teste','financeiro@plantaopro.local','financeiro@plantaopro.local','$2a$11$9URjd.sZ/id/DeASc.y4a.s/fX3GbcINasNKsgRtMwi10u1/b7Ss2','ATIVO','A',false,NULL)
    ON CONFLICT(id) DO UPDATE SET tenant_id=excluded.tenant_id,cliente_id=excluded.cliente_id,nome=excluded.nome,email=excluded.email,
      email_normalizado=excluded.email_normalizado,senha_hash=excluded.senha_hash,status='ATIVO',reg_status='A',
      senha_alteracao_obrigatoria=false,bloqueado_ate=NULL,reg_update=now();

    INSERT INTO plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_status)
    SELECT md5('homolog-role:'||u.email)::uuid,u.tenant_id,u.cliente_id,u.id,p.id,'A'
    FROM plantaopro.usuarios u
    JOIN plantaopro.perfis p ON p.codigo=CASE u.email
      WHEN 'superadmin@plantaopro.local' THEN 'ADMINISTRADOR_GLOBAL'
      WHEN 'admin.clinica@plantaopro.local' THEN 'ADMINISTRADOR'
      WHEN 'medico@plantaopro.local' THEN 'MEDICO'
      WHEN 'recepcao@plantaopro.local' THEN 'RECEPCAO'
      WHEN 'financeiro@plantaopro.local' THEN 'FINANCEIRO' END AND p.reg_status='A'
    WHERE u.email LIKE '%@plantaopro.local'
    ON CONFLICT(id) DO UPDATE SET perfil_id=excluded.perfil_id,tenant_id=excluded.tenant_id,cliente_id=excluded.cliente_id,reg_status='A';

    INSERT INTO plantaopro.hospitais(id,tenant_id,codigo,nome,status,dados)
    VALUES('8b0c8e74-a81b-4ea2-b499-94755a1ca030',v_tenant,'UNIDADE_MODELO','Unidade Clínica Modelo','ATIVO','{}')
    ON CONFLICT(id) DO UPDATE SET status='ATIVO',atualizado_em=now();
    INSERT INTO plantaopro.especialidades(id,tenant_id,codigo,nome,status,dados)
    VALUES('8b0c8e74-a81b-4ea2-b499-94755a1ca031',v_tenant,'CLINICA_MEDICA','Clínica Médica','ATIVO','{}')
    ON CONFLICT(id) DO UPDATE SET status='ATIVO',atualizado_em=now();
    INSERT INTO plantaopro.medicos(id,tenant_id,codigo,nome,status,dados)
    VALUES(v_medico,v_tenant,'MEDICO_TESTE','Médico de Teste','ATIVO',jsonb_build_object('usuarioId','8b0c8e74-a81b-4ea2-b499-94755a1ca012','especialidadeId','8b0c8e74-a81b-4ea2-b499-94755a1ca031'))
    ON CONFLICT(id) DO UPDATE SET status='ATIVO',dados=excluded.dados,atualizado_em=now();

    INSERT INTO plantaopro.tenant_modulos(id,tenant_id,codigo,nome,status,dados)
    SELECT md5('homolog-module:'||m)::uuid,v_tenant,m,m,'ATIVO',jsonb_build_object('habilitado',true,'planoId',v_plano)
    FROM unnest(ARRAY['USUARIOS','PACIENTES','AGENDAMENTOS','PLANTOES','ESCALAS','FINANCEIRO','CONSULTAS','NOTIFICACOES','RELATORIOS','WHITE_LABEL']) m
    ON CONFLICT(id) DO UPDATE SET status='ATIVO',dados=excluded.dados,atualizado_em=now();

    -- Administrador do tenant herda o catálogo operacional, nunca o catálogo SaaS global.
    INSERT INTO plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status)
    SELECT md5('homolog-admin-permission:'||x.codigo)::uuid,p.id,x.id,true,false,'A'
    FROM plantaopro.perfis p CROSS JOIN plantaopro.permissoes x
    WHERE p.codigo='ADMINISTRADOR' AND p.tenant_id=v_tenant AND x.reg_status='A'
      AND upper(coalesce(x.modulo,'')) NOT IN ('SAAS','AUDITORIA_GLOBAL')
    ON CONFLICT(id) DO UPDATE SET permitido=true,bloqueado_por_plano=false,reg_status='A';
END
$seed$;
