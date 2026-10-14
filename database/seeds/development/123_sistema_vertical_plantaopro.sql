-- OPT-IN: dados fictícios para uso exclusivo em ambiente local/homologação.
-- Cria o cliente/organização SaaS "Sistema Vertical" e seu administrador do PRÓPRIO tenant.
-- Acesso de TESTE: admin.sistemavertical@plantaopro.local (explicitamente identificado).
-- O hash abaixo é BCrypt cost 11, compatível com BCrypt.Net.BCrypt.Verify.
-- A senha aberta existe somente em docs/usuarios-teste.md (não versionar em produção).
--
-- Propriedades:
--   * Idempotente: reexecutar não duplica organização, cliente, perfil, usuário nem assinatura.
--   * Não reescreve silenciosamente a senha de conta já existente (upsert por e-mail preserva senha_hash).
--   * Reutiliza o plano de homologação existente (HOMOLOGACAO_360, mesma identidade do seed 122).
--   * Diferencia o cliente SaaS dos clientes comerciais do Adm360 (não cria adm360_parceiros cruzado).
--   * Sem privilégio global: perfil ADMINISTRADOR escopo ao próprio tenant (exclui SAAS e AUDITORIA_GLOBAL).
--
-- Execução manual (não faz parte da instalação canônica limpa):
--   psql "$ConnectionStrings__Default" -v ON_ERROR_STOP=1 -f database/seeds/development/123_sistema_vertical_plantaopro.sql
SET search_path TO plantaopro, public;

DO $seed$
DECLARE
    v_tenant  uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca301';
    v_cliente uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca302';
    v_usuario uuid := '8b0c8e74-a81b-4ea2-b499-94755a1ca310';
    v_plano   uuid;
    v_rows    integer;
    v_email   text := 'admin.sistemavertical@plantaopro.local';
    v_hash    text := '$2a$11$iTTjI7RBZxuEMRYfS5inNuXqQ3EdyX3Eldyfgr2iE7frFkuxfycGG';
BEGIN
    IF current_database() IN ('template0', 'template1') THEN
        RAISE EXCEPTION 'Seed Sistema Vertical recusado no banco %.', current_database();
    END IF;
    IF to_regclass('plantaopro.usuarios') IS NULL OR to_regclass('plantaopro.perfis') IS NULL THEN
        RAISE EXCEPTION 'Instale o schema canônico antes do seed Sistema Vertical.';
    END IF;
    PERFORM pg_advisory_xact_lock(12320260922);

    -- Plano de homologação existente (mesmo id/nome do seed 122); cria apenas se ausente.
    IF NOT EXISTS (SELECT 1 FROM plantaopro.planos WHERE codigo='HOMOLOGACAO_360') THEN
        INSERT INTO plantaopro.planos(id,tenant_id,codigo,nome,status,dados)
        VALUES('8b0c8e74-a81b-4ea2-b499-94755a1ca003',NULL,'HOMOLOGACAO_360','Plano Homologação Saúde 360','ATIVO','{"whiteLabel":true,"ambiente":"HOMOLOGACAO"}'::jsonb);
    END IF;
    SELECT id INTO STRICT v_plano FROM plantaopro.planos WHERE codigo='HOMOLOGACAO_360' AND reg_status='A';

    -- Organização/tenant SaaS "Sistema Vertical".
    INSERT INTO plantaopro.tenants(id,tenant_id,codigo,nome,status,dados)
    VALUES(v_tenant,v_tenant,'SISTEMA_VERTICAL','Sistema Vertical','ATIVO',
      jsonb_build_object('planoId',v_plano,'ambiente','HOMOLOGACAO','finalidade','SISTEMA_VERTICAL'))
    ON CONFLICT(id) DO UPDATE SET nome=excluded.nome,status='ATIVO',atualizado_em=now();

    -- Cliente comercial PRÓPRIO dentro do tenant. CNPJ nulo: não inventar documento real.
    INSERT INTO plantaopro.clientes(id,tenant_id,codigo,nome,razao_social,nome_fantasia,cnpj,status,dados)
    VALUES(v_cliente,v_tenant,'SISTEMA_VERTICAL','Sistema Vertical','Sistema Vertical','Sistema Vertical',NULL,'ATIVO',
      jsonb_build_object('planoId',v_plano,'ambiente','HOMOLOGACAO'))
    ON CONFLICT(id) DO UPDATE SET tenant_id=v_tenant,nome=excluded.nome,status='ATIVO',atualizado_em=now();

    -- Perfil administrador do PRÓPRIO tenant (nunca ADMINISTRADOR_GLOBAL).
    UPDATE plantaopro.perfis p SET cliente_id=v_cliente,status='ATIVO',reg_status='A',reg_update=now()
    WHERE p.tenant_id=v_tenant AND p.codigo='ADMINISTRADOR';
    INSERT INTO plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status)
    SELECT md5('sv-profile:ADMINISTRADOR')::uuid,v_tenant,v_cliente,'ADMINISTRADOR','Administrador Sistema Vertical',
      'Administração do próprio tenant Sistema Vertical',true,false,'ATIVO','A'
    WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfis p WHERE p.tenant_id=v_tenant AND p.codigo='ADMINISTRADOR' AND p.reg_status='A')
    ON CONFLICT DO NOTHING;

    -- Usuário administrador (acesso de teste). Atualiza por e-mail antes de inserir; preserva senha existente.
    UPDATE plantaopro.usuarios SET tenant_id=v_tenant,cliente_id=v_cliente,nome='Administrador Sistema Vertical',
      email=v_email,email_normalizado=lower(v_email),status='ATIVO',reg_status='A',
      senha_alteracao_obrigatoria=false,bloqueado_ate=NULL,reg_update=now()
    WHERE lower(email)=lower(v_email) OR lower(email_normalizado)=lower(v_email);
    GET DIAGNOSTICS v_rows = ROW_COUNT;
    IF v_rows = 0 THEN
        INSERT INTO plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria,bloqueado_ate)
        VALUES(v_usuario,v_tenant,v_cliente,'Administrador Sistema Vertical',v_email,lower(v_email),v_hash,'ATIVO','A',false,NULL);
    END IF;

    -- Exatamente um perfil ativo coerente (remove papéis residuais antes).
    UPDATE plantaopro.usuarios_perfis up SET reg_status='I',reg_update=now()
    FROM plantaopro.usuarios u
    WHERE up.usuario_id=u.id AND lower(u.email)=lower(v_email);
    INSERT INTO plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_status)
    SELECT md5('sv-role:'||u.email)::uuid,u.tenant_id,u.cliente_id,u.id,p.id,'A'
    FROM plantaopro.usuarios u
    JOIN plantaopro.perfis p ON p.codigo='ADMINISTRADOR' AND p.tenant_id=v_tenant AND p.reg_status='A'
    WHERE lower(u.email)=lower(v_email)
    ON CONFLICT(id) DO UPDATE SET tenant_id=excluded.tenant_id,cliente_id=excluded.cliente_id,
      usuario_id=excluded.usuario_id,perfil_id=excluded.perfil_id,reg_status='A',reg_update=now();

    -- Modulos contratados no tenant (Administrativo 360 + modulos operacionais do plano).
    -- Resolve modulo_id E codigo_modulo contra o catalogo canonico (modulos_sistema): a funcao
    -- plantaopro.modulos_efetivos() (v2339/v2340) e a fonte unica do kernel de acesso e so
    -- reconhece contratos por modulo_id ou codigo_modulo (nunca por codigo livre).
    INSERT INTO plantaopro.tenant_modulos(id,tenant_id,modulo_id,codigo,codigo_modulo,nome,status,dados)
    SELECT md5('sv-module:'||upper(ms.codigo))::uuid,v_tenant,ms.id,ms.codigo,ms.codigo,ms.nome,'ATIVO',
      jsonb_build_object('habilitado',true,'planoId',v_plano)
    FROM plantaopro.modulos_sistema ms
    WHERE upper(ms.codigo) IN ('ADM360','FINANCEIRO','RELATORIOS','SAUDE360','USUARIOS')
      AND ms.reg_status='A' AND upper(coalesce(ms.status,'ATIVO'))='ATIVO'
    ON CONFLICT(id) DO UPDATE SET modulo_id=excluded.modulo_id,codigo_modulo=excluded.codigo_modulo,
      status='ATIVO',habilitado=true,dados=excluded.dados;

    -- Permissoes ESCOPADAS ao contrato do tenant (Administrativo 360 + modulos contratados do plano,
    -- incluindo as capacidades-filhas do pacote SAUDE360 que o app checa nas telas de saude).
    -- Escopo compacto de propośito: o cookie de sessao Web embute o claim combinado e um catalogo
    -- completo (~35 KB) estourava o limite de cabeçalhos do transporte de homologacao visual (502).
    INSERT INTO plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status)
    SELECT md5('sv-admin-permission:'||x.codigo)::uuid,p.id,x.id,true,false,'A'
    FROM plantaopro.perfis p CROSS JOIN plantaopro.permissoes x
    WHERE p.codigo='ADMINISTRADOR' AND p.tenant_id=v_tenant AND p.reg_status='A'
      AND x.reg_status='A'
      AND upper(coalesce(x.modulo,'')) LIKE ANY (ARRAY['ADM360%','AGENDAMENTOS%','CONSULTAS%','ESCALAS%','FINANCEIRO%','PACIENTES%','PLANTOES%','RELATORIOS%','SAUDE360%','USUARIOS%'])
    ON CONFLICT(id) DO UPDATE SET permitido=true,bloqueado_por_plano=false,reg_status='A';

    -- Converge reexecuções: desativa concessões ativas que ficaram fora do escopo corrente.
    UPDATE plantaopro.perfil_permissoes pp
    SET reg_status='I'
    FROM plantaopro.perfis p
    WHERE p.id=pp.perfil_id
      AND p.codigo='ADMINISTRADOR' AND p.tenant_id=v_tenant AND p.reg_status='A'
      AND pp.reg_status='A'
      AND NOT EXISTS (
        SELECT 1 FROM plantaopro.permissoes x
        WHERE x.id=pp.permissao_id AND x.reg_status='A'
          AND upper(coalesce(x.modulo,'')) LIKE ANY (ARRAY['ADM360%','AGENDAMENTOS%','CONSULTAS%','ESCALAS%','FINANCEIRO%','PACIENTES%','PLANTOES%','RELATORIOS%','SAUDE360%','USUARIOS%'])
      );

    -- Falha fechada: uma conta ativa com um único perfil ativo coerente (ADMINISTRADOR do próprio tenant).
    IF (SELECT count(*) FROM plantaopro.usuarios
        WHERE lower(email)=lower(v_email) AND status='ATIVO' AND reg_status='A') <> 1 THEN
        RAISE EXCEPTION 'Seed Sistema Vertical inválido: o administrador ativo não foi provisionado.';
    END IF;
    IF EXISTS (
      SELECT 1 FROM plantaopro.usuarios u
      LEFT JOIN plantaopro.usuarios_perfis up ON up.usuario_id=u.id AND up.reg_status='A'
      LEFT JOIN plantaopro.perfis p ON p.id=up.perfil_id AND p.reg_status='A'
      WHERE lower(u.email)=lower(v_email)
      GROUP BY u.id,u.email,u.tenant_id
      HAVING count(p.id) <> 1
        OR bool_or(p.codigo IS DISTINCT FROM 'ADMINISTRADOR')
        OR bool_or(u.tenant_id IS DISTINCT FROM v_tenant)
    ) THEN
        RAISE EXCEPTION 'Seed Sistema Vertical inválido: tenant ou perfil ativo divergente.';
    END IF;
END
$seed$;
