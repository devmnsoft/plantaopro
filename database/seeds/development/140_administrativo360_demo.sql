\set ON_ERROR_STOP on
-- Execute depois de 121_acesso_demo_local.sql e da migration v2190.
DO $$
DECLARE t uuid; d uuid := 'a3600000-0000-4000-8000-000000000001'; c uuid := 'a3600000-0000-4000-8000-000000000002'; p uuid := 'a3600000-0000-4000-8000-000000000003';
BEGIN
 SELECT id INTO t FROM plantaopro.tenants WHERE id='d3f6584c-2c64-4e5a-9ea9-4e1428647502';
 IF t IS NULL THEN RAISE EXCEPTION 'Tenant demo ausente. Execute 121_acesso_demo_local.sql primeiro.'; END IF;
 INSERT INTO plantaopro.adm_departamentos(id,tenant_id,codigo,nome) VALUES(d,t,'ASSISTENCIAL','Assistencial') ON CONFLICT(id) DO UPDATE SET tenant_id=excluded.tenant_id,nome=excluded.nome;
 INSERT INTO plantaopro.adm_cargos(id,tenant_id,departamento_id,codigo,nome) VALUES(c,t,d,'MEDICO-PLANTONISTA','Médico plantonista') ON CONFLICT(id) DO UPDATE SET tenant_id=excluded.tenant_id,departamento_id=excluded.departamento_id,nome=excluded.nome;
 INSERT INTO plantaopro.adm_colaboradores(id,tenant_id,cargo_id,matricula,nome,cpf,email) VALUES(p,t,c,'DEMO-001','Dra. Marina Costa','52998224725','medico@santacasa-demo.example') ON CONFLICT(id) DO UPDATE SET tenant_id=excluded.tenant_id,cargo_id=excluded.cargo_id,email=excluded.email;
 INSERT INTO plantaopro.adm_contratos_trabalho(id,tenant_id,colaborador_id,tipo,inicio,salario,carga_horaria_semanal)
 VALUES('a3600000-0000-4000-8000-000000000004',t,p,'CLT',DATE '2026-01-01',12500,24) ON CONFLICT(id) DO NOTHING;
END $$;

-- Login demonstrativo é exclusivamente o usuário real criado pelo seed 121.
DO $$ BEGIN
 IF NOT EXISTS(SELECT 1 FROM plantaopro.usuarios WHERE lower(email)='gestor@santacasa-demo.example' AND status='ATIVO' AND reg_status='A' AND senha_hash LIKE '$2%') THEN
  RAISE EXCEPTION 'Login demo persistido não está disponível; execute 121_acesso_demo_local.sql.';
 END IF;
END $$;
