-- Administrativo360 / Prompt 1: base multiempresa, cadastros e contratação.
-- Idempotente e seguro para upgrade: todos os vínculos carregam tenant_id.
CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE TABLE IF NOT EXISTS plantaopro.adm_departamentos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
 codigo varchar(30) NOT NULL, nome varchar(120) NOT NULL, ativo boolean NOT NULL DEFAULT true,
 created_by uuid NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_adm_departamentos_reg_status CHECK(reg_status IN ('A','I'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm_departamentos_tenant_codigo ON plantaopro.adm_departamentos(tenant_id,lower(codigo)) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.adm_cargos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
 departamento_id uuid NULL REFERENCES plantaopro.adm_departamentos(id), codigo varchar(30) NOT NULL, nome varchar(120) NOT NULL,
 ativo boolean NOT NULL DEFAULT true, created_by uuid NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_adm_cargos_reg_status CHECK(reg_status IN ('A','I'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm_cargos_tenant_codigo ON plantaopro.adm_cargos(tenant_id,lower(codigo)) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.adm_colaboradores (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id), cargo_id uuid NOT NULL REFERENCES plantaopro.adm_cargos(id),
 matricula varchar(30) NOT NULL, nome varchar(160) NOT NULL, cpf char(11) NOT NULL, email varchar(180) NOT NULL, status varchar(20) NOT NULL DEFAULT 'ATIVO',
 created_by uuid NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_adm_colaborador_cpf CHECK(cpf ~ '^[0-9]{11}$'), CONSTRAINT ck_adm_colaborador_status CHECK(status IN ('ATIVO','AFASTADO','DESLIGADO')),
 CONSTRAINT ck_adm_colaborador_reg_status CHECK(reg_status IN ('A','I'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm_colaboradores_tenant_matricula ON plantaopro.adm_colaboradores(tenant_id,lower(matricula)) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm_colaboradores_tenant_cpf ON plantaopro.adm_colaboradores(tenant_id,cpf) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.adm_contratos_trabalho (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id), colaborador_id uuid NOT NULL REFERENCES plantaopro.adm_colaboradores(id),
 tipo varchar(20) NOT NULL, inicio date NOT NULL, fim date NULL, salario numeric(14,2) NOT NULL, carga_horaria_semanal smallint NOT NULL,
 status varchar(20) NOT NULL DEFAULT 'VIGENTE', created_by uuid NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_adm_contrato_tipo CHECK(tipo IN ('CLT','PJ','ESTAGIO','TEMPORARIO')), CONSTRAINT ck_adm_contrato_vigencia CHECK(fim IS NULL OR fim>=inicio),
 CONSTRAINT ck_adm_contrato_valores CHECK(salario>0 AND carga_horaria_semanal BETWEEN 1 AND 60), CONSTRAINT ck_adm_contrato_status CHECK(status IN ('VIGENTE','ENCERRADO','CANCELADO')),
 CONSTRAINT ck_adm_contrato_reg_status CHECK(reg_status IN ('A','I'))
);
CREATE INDEX IF NOT EXISTS ix_adm_contratos_tenant_status ON plantaopro.adm_contratos_trabalho(tenant_id,status) WHERE reg_status='A';

-- Impede referências cruzadas entre tenants, inclusive por SQL direto.
CREATE OR REPLACE FUNCTION plantaopro.adm360_validar_tenant() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_TABLE_NAME='adm_cargos' AND NEW.departamento_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM plantaopro.adm_departamentos d WHERE d.id=NEW.departamento_id AND d.tenant_id=NEW.tenant_id) THEN RAISE EXCEPTION 'Departamento pertence a outro tenant'; END IF;
 IF TG_TABLE_NAME='adm_colaboradores' AND NOT EXISTS(SELECT 1 FROM plantaopro.adm_cargos c WHERE c.id=NEW.cargo_id AND c.tenant_id=NEW.tenant_id) THEN RAISE EXCEPTION 'Cargo pertence a outro tenant'; END IF;
 IF TG_TABLE_NAME='adm_contratos_trabalho' AND NOT EXISTS(SELECT 1 FROM plantaopro.adm_colaboradores p WHERE p.id=NEW.colaborador_id AND p.tenant_id=NEW.tenant_id) THEN RAISE EXCEPTION 'Colaborador pertence a outro tenant'; END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_adm_cargos_tenant ON plantaopro.adm_cargos;
CREATE TRIGGER trg_adm_cargos_tenant BEFORE INSERT OR UPDATE ON plantaopro.adm_cargos FOR EACH ROW EXECUTE FUNCTION plantaopro.adm360_validar_tenant();
DROP TRIGGER IF EXISTS trg_adm_colaboradores_tenant ON plantaopro.adm_colaboradores;
CREATE TRIGGER trg_adm_colaboradores_tenant BEFORE INSERT OR UPDATE ON plantaopro.adm_colaboradores FOR EACH ROW EXECUTE FUNCTION plantaopro.adm360_validar_tenant();
DROP TRIGGER IF EXISTS trg_adm_contratos_tenant ON plantaopro.adm_contratos_trabalho;
CREATE TRIGGER trg_adm_contratos_tenant BEFORE INSERT OR UPDATE ON plantaopro.adm_contratos_trabalho FOR EACH ROW EXECUTE FUNCTION plantaopro.adm360_validar_tenant();
