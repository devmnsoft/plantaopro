-- OPT-IN: dados fictícios para uso exclusivo em ambiente local/homologação.
-- Os hashes abaixo são BCrypt cost 11, compatíveis com BCrypt.Net.BCrypt.Verify.
-- As senhas abertas existem somente em docs/usuarios-teste.md.
SET search_path TO plantaopro, public;

DO $seed$
DECLARE
    v_tenant uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca001';
    v_cliente uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca002';
    v_plano uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca003';
    v_tenant_isolamento uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca101';
    v_cliente_isolamento uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca102';
    v_medico uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca020';
    v_usuario_medico uuid;
    v_rows integer;
    v_account record;
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

    -- Contexto sem usuários demonstrativos, reservado a testes negativos de
    -- isolamento. Identificadores previsíveis facilitam testes sem conceder
    -- ao navegador qualquer autoridade sobre o tenant consultado.
    INSERT INTO plantaopro.tenants(id,tenant_id,codigo,nome,status,dados)
    VALUES(v_tenant_isolamento,v_tenant_isolamento,'CLINICA_ISOLAMENTO','Clínica Sintética Isolamento','ATIVO',
      jsonb_build_object('planoId',v_plano,'ambiente','HOMOLOGACAO','finalidade','ISOLAMENTO'))
    ON CONFLICT(id) DO UPDATE SET nome=excluded.nome,status='ATIVO',atualizado_em=now();
    INSERT INTO plantaopro.clientes(id,tenant_id,codigo,nome,status,dados)
    VALUES(v_cliente_isolamento,v_tenant_isolamento,'CLINICA_ISOLAMENTO','Clínica Sintética Isolamento','ATIVO',
      jsonb_build_object('planoId',v_plano,'ambiente','HOMOLOGACAO','finalidade','ISOLAMENTO'))
    ON CONFLICT(id) DO UPDATE SET tenant_id=v_tenant_isolamento,nome=excluded.nome,status='ATIVO',atualizado_em=now();

    -- O login global tambem precisa ser autocontido: bases antigas ou parciais
    -- podem nao ter recebido o perfil criado pela migration de identidade.
    UPDATE plantaopro.perfis SET status='ATIVO',reg_status='A',reg_update=now()
    WHERE tenant_id IS NULL AND codigo='ADMINISTRADOR_GLOBAL';
    INSERT INTO plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status)
    SELECT md5('homolog-profile:ADMINISTRADOR_GLOBAL')::uuid,NULL,NULL,'ADMINISTRADOR_GLOBAL','Administrador Global',
      'Acesso administrativo global do sistema',true,false,'ATIVO','A'
    WHERE NOT EXISTS (
      SELECT 1 FROM plantaopro.perfis WHERE tenant_id IS NULL AND codigo='ADMINISTRADOR_GLOBAL' AND reg_status='A'
    )
    ON CONFLICT DO NOTHING;

    UPDATE plantaopro.perfis p SET cliente_id=v_cliente,status='ATIVO',reg_status='A',reg_update=now()
    WHERE p.tenant_id=v_tenant AND p.codigo IN ('ADMINISTRADOR','MEDICO','RECEPCAO','FINANCEIRO');
    INSERT INTO plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status)
    SELECT md5('homolog-profile:'||x.codigo)::uuid,v_tenant,v_cliente,x.codigo,x.nome,x.descricao,true,false,'ATIVO','A'
    FROM (VALUES
      ('ADMINISTRADOR','Administrador','Administração do próprio tenant'),
      ('MEDICO','Médico','Acesso clínico aos próprios atendimentos'),
      ('RECEPCAO','Recepção','Agenda, cadastro e check-in'),
      ('FINANCEIRO','Financeiro','Operação financeira sem acesso clínico')
    ) x(codigo,nome,descricao)
    WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfis p WHERE p.tenant_id=v_tenant AND p.codigo=x.codigo AND p.reg_status='A')
    ON CONFLICT DO NOTHING;

    -- Atualiza pelo identificador natural antes de inserir. Assim o seed também é
    -- idempotente em bases onde a conta já existe com outro UUID.
    FOR v_account IN SELECT * FROM (VALUES
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca010'::uuid,NULL::uuid,NULL::uuid,'Super Admin PlantãoPro','superadmin@plantaopro.local','$2a$11$KZ80jdGp.ymLQ/E6zk8vluf6o4/.Ur2cEKxjD4Hp4jx5YETf7OaUG'),
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca011'::uuid,v_tenant,v_cliente,'Administrador Clínica Modelo','admin.clinica@plantaopro.local','$2a$11$4jafymzm6xqC48JdaVE3GuH0Dy2evtr/dqT7sKDqUe92OwpbaLbX2'),
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca012'::uuid,v_tenant,v_cliente,'Médico de Teste','medico@plantaopro.local','$2a$11$EIMmoQs8gPeaCFShI4.ACeL2WdJFeFulTrXL4JjBKFgJLCmqc11q2'),
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca013'::uuid,v_tenant,v_cliente,'Recepção de Teste','recepcao@plantaopro.local','$2a$11$1biWFM2YemJyh9DaoRRjXe3K5UpzJdN.GYJMeglzoODIBi9BTW5yK'),
      ('8b0c8e74-a81b-4ea2-b499-94755a1ca014'::uuid,v_tenant,v_cliente,'Financeiro de Teste','financeiro@plantaopro.local','$2a$11$9URjd.sZ/id/DeASc.y4a.s/fX3GbcINasNKsgRtMwi10u1/b7Ss2')
    ) a(id,tenant_id,cliente_id,nome,email,senha_hash)
    LOOP
      UPDATE plantaopro.usuarios SET tenant_id=v_account.tenant_id,cliente_id=v_account.cliente_id,nome=v_account.nome,
        email=v_account.email,email_normalizado=lower(v_account.email),status='ATIVO',reg_status='A',
        senha_alteracao_obrigatoria=false,bloqueado_ate=NULL,reg_update=now()
      WHERE lower(email)=lower(v_account.email) OR lower(email_normalizado)=lower(v_account.email);
      GET DIAGNOSTICS v_rows = ROW_COUNT;
      IF v_rows = 0 THEN
        INSERT INTO plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria,bloqueado_ate)
        VALUES(v_account.id,v_account.tenant_id,v_account.cliente_id,v_account.nome,v_account.email,lower(v_account.email),v_account.senha_hash,'ATIVO','A',false,NULL);
      END IF;
    END LOOP;

    -- Remove privilégios residuais das identidades fixas antes de atribuir o
    -- único perfil esperado. Isso impede que uma reaplicação preserve um papel
    -- incompatível criado manualmente ou por um seed anterior.
    UPDATE plantaopro.usuarios_perfis up SET reg_status='I',reg_update=now()
    FROM plantaopro.usuarios u
    WHERE up.usuario_id=u.id
      AND lower(u.email) IN ('superadmin@plantaopro.local','admin.clinica@plantaopro.local','medico@plantaopro.local','recepcao@plantaopro.local','financeiro@plantaopro.local');

    INSERT INTO plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_status)
    SELECT md5('homolog-role:'||u.email)::uuid,u.tenant_id,u.cliente_id,u.id,p.id,'A'
    FROM plantaopro.usuarios u
    JOIN plantaopro.perfis p ON p.codigo=CASE lower(u.email)
      WHEN 'superadmin@plantaopro.local' THEN 'ADMINISTRADOR_GLOBAL'
      WHEN 'admin.clinica@plantaopro.local' THEN 'ADMINISTRADOR'
      WHEN 'medico@plantaopro.local' THEN 'MEDICO'
      WHEN 'recepcao@plantaopro.local' THEN 'RECEPCAO'
      WHEN 'financeiro@plantaopro.local' THEN 'FINANCEIRO' END AND p.reg_status='A'
      AND ((lower(u.email)='superadmin@plantaopro.local' AND p.tenant_id IS NULL)
        OR (lower(u.email)<>'superadmin@plantaopro.local' AND p.tenant_id=v_tenant))
    WHERE lower(u.email) IN ('superadmin@plantaopro.local','admin.clinica@plantaopro.local','medico@plantaopro.local','recepcao@plantaopro.local','financeiro@plantaopro.local')
    ON CONFLICT(id) DO UPDATE SET tenant_id=excluded.tenant_id,cliente_id=excluded.cliente_id,
      usuario_id=excluded.usuario_id,perfil_id=excluded.perfil_id,reg_status='A',reg_update=now();

    INSERT INTO plantaopro.hospitais(id,tenant_id,codigo,nome,status,dados)
    VALUES('8b0c8e74-a81b-4ea2-b499-94755a1ca030',v_tenant,'UNIDADE_MODELO','Unidade Clínica Modelo','ATIVO','{}')
    ON CONFLICT(id) DO UPDATE SET status='ATIVO',atualizado_em=now();
    INSERT INTO plantaopro.especialidades(id,tenant_id,codigo,nome,status,dados)
    VALUES('8b0c8e74-a81b-4ea2-b499-94755a1ca031',v_tenant,'CLINICA_MEDICA','Clínica Médica','ATIVO','{}')
    ON CONFLICT(id) DO UPDATE SET status='ATIVO',atualizado_em=now();
    SELECT id INTO STRICT v_usuario_medico FROM plantaopro.usuarios WHERE lower(email)='medico@plantaopro.local' AND reg_status='A';
    INSERT INTO plantaopro.medicos(id,tenant_id,usuario_id,codigo,nome,status,reg_status,dados)
    VALUES(v_medico,v_tenant,v_usuario_medico,'MEDICO_TESTE','Médico de Teste','ATIVO','A',jsonb_build_object('usuarioId',v_usuario_medico,'especialidadeId','8b0c8e74-a81b-4ea2-b499-94755a1ca031'))
    ON CONFLICT(id) DO UPDATE SET usuario_id=v_usuario_medico,status='ATIVO',reg_status='A',dados=excluded.dados,atualizado_em=now();

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

    -- Falha fechada: o seed só conclui quando as cinco contas possuem tenant e
    -- exatamente um perfil ativo coerente. Qualquer divergência aborta toda a
    -- transação, em vez de deixar credenciais parcialmente utilizáveis.
    IF (SELECT count(*) FROM plantaopro.usuarios
        WHERE lower(email) IN ('superadmin@plantaopro.local','admin.clinica@plantaopro.local','medico@plantaopro.local','recepcao@plantaopro.local','financeiro@plantaopro.local')
          AND status='ATIVO' AND reg_status='A') <> 5 THEN
      RAISE EXCEPTION 'Seed de homologação inválido: as cinco contas ativas não foram provisionadas.';
    END IF;
    IF EXISTS (
      SELECT 1
      FROM plantaopro.usuarios u
      LEFT JOIN plantaopro.usuarios_perfis up ON up.usuario_id=u.id AND up.reg_status='A'
      LEFT JOIN plantaopro.perfis p ON p.id=up.perfil_id AND p.reg_status='A'
      WHERE lower(u.email) IN ('superadmin@plantaopro.local','admin.clinica@plantaopro.local','medico@plantaopro.local','recepcao@plantaopro.local','financeiro@plantaopro.local')
      GROUP BY u.id,u.email,u.tenant_id
      HAVING count(p.id) <> 1
        OR bool_or(p.codigo IS DISTINCT FROM CASE lower(u.email)
          WHEN 'superadmin@plantaopro.local' THEN 'ADMINISTRADOR_GLOBAL'
          WHEN 'admin.clinica@plantaopro.local' THEN 'ADMINISTRADOR'
          WHEN 'medico@plantaopro.local' THEN 'MEDICO'
          WHEN 'recepcao@plantaopro.local' THEN 'RECEPCAO'
          WHEN 'financeiro@plantaopro.local' THEN 'FINANCEIRO' END)
        OR bool_or((lower(u.email)='superadmin@plantaopro.local') IS DISTINCT FROM (u.tenant_id IS NULL))
    ) THEN
      RAISE EXCEPTION 'Seed de homologação inválido: tenant ou perfil ativo divergente.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM plantaopro.tenants WHERE id=v_tenant_isolamento AND status='ATIVO') THEN
      RAISE EXCEPTION 'Seed de homologação inválido: tenant sintético de isolamento ausente.';
    END IF;
END
$seed$;
