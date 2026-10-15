-- ============================================================================
-- PlantaoPro | Schema canonico de instalacao limpa: 450_v2219_tabelas_legado_gap_instalacao
-- Rodada 9 (r9i) - fechamento do debito de instalacao consolidada (auditoria 3.10).
--
-- Contexto: oito tabelas criadas por migrations legadas (antes da curadoria do
-- install-manifest) nunca foram incorporadas ao caminho de instalacao limpa,
-- quebrando as migrations subsequentes v2312-v2353 em banco novo:
--   plano_modulos              (2026_plantao_pro_self_service_white_label e similares)
--   plano_recursos             (2026_plantao_pro_saas_comercial_lgpd_jornada e similares)
--   lgpd_consentimentos        (20260607_saas_lgpd_jornada_comercial_ajuda e similares)
--   v116_faturamento_lote_itens (2026_v116_consolidacao_operacional_final)
--   cid_tabela                 (2026_fix_cid_tabela_schema / saude360_cid_*)
--   clinica_lancamentos        (2026_plantao_pro_saude360_modulos_clinicos / 2026_saude360_financeiro_clinica)
--   clinica_estornos           (2026_saude360_financeiro_clinica)
--   convenio_autorizacoes      (2026_plantao_pro_saude360_modulos_clinicos / 2026_saude360_convenios_planos_saude)
--
-- Conteudo: snapshot de schema (colunas, constraints, indices e sequencias)
-- gerado do catalogo do banco de homologacao em 2026-10-15 (pg_attribute,
-- pg_constraint/pg_get_constraintdef, pg_index, pg_depend). As migrations
-- v2315/v2324/v2325/v2331/v2350/v2351 sao aditivas e idempotentes (ADD COLUMN IF
-- NOT EXISTS / DROP CONSTRAINT IF EXISTS), portanto convergem sobre este
-- snapshot sem conflito em banco novo nem legado.
--
-- Idempotencia: CREATE TABLE/SEQUENCE/INDEX IF NOT EXISTS; seguro dentro da
-- transacao unica do comando install. Inclui bloco final de garantia dos
-- codigos ADM360/USUARIOS exigidos pelos v2322/v2329 ("garantia codigos").
-- ============================================================================

CREATE TABLE IF NOT EXISTS plantaopro.plano_modulos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    plano_id uuid NOT NULL,
    modulo_id uuid,
    codigo_modulo text NOT NULL,
    habilitado boolean NOT NULL DEFAULT true,
    status text NOT NULL DEFAULT 'ATIVO'::text,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_update timestamp without time zone,
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    tenant_id uuid,
    cliente_id uuid,
    parceiro_id uuid,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    incluido boolean NOT NULL DEFAULT true,
    limite integer,
    preco_adicional numeric(14,2),
    PRIMARY KEY (id)
);

CREATE INDEX ix_plano_modulos_cliente_id ON plantaopro.plano_modulos USING btree (cliente_id);

CREATE INDEX ix_plano_modulos_plano_status_regdate ON plantaopro.plano_modulos USING btree (plano_id, status, reg_date);

CREATE INDEX ix_plano_modulos_reg_date ON plantaopro.plano_modulos USING btree (reg_date DESC);

CREATE INDEX ix_plano_modulos_status ON plantaopro.plano_modulos USING btree (status);

CREATE INDEX ix_plano_modulos_tenant_id ON plantaopro.plano_modulos USING btree (tenant_id);

CREATE UNIQUE INDEX ux_plano_modulos_ativo ON plantaopro.plano_modulos USING btree (plano_id, modulo_id) WHERE ((reg_status = 'A'::bpchar) AND (modulo_id IS NOT NULL));

CREATE TABLE IF NOT EXISTS plantaopro.plano_recursos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    plano_id uuid NOT NULL,
    recurso character varying(80) NOT NULL,
    habilitado boolean NOT NULL DEFAULT true,
    limite integer,
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_update timestamp without time zone,
    codigo character varying(80),
    tenant_id uuid,
    cliente_id uuid,
    parceiro_id uuid,
    status character varying(40) NOT NULL DEFAULT 'ATIVO'::character varying,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    nome character varying(160),
    descricao text,
    FOREIGN KEY (plano_id) REFERENCES plantaopro.planos(id),
    PRIMARY KEY (id),
    UNIQUE (plano_id, codigo)
);

CREATE INDEX ix_plano_recursos_cliente_id ON plantaopro.plano_recursos USING btree (cliente_id);

CREATE INDEX ix_plano_recursos_plano_codigo ON plantaopro.plano_recursos USING btree (plano_id, codigo, reg_status);

CREATE INDEX ix_plano_recursos_reg_date ON plantaopro.plano_recursos USING btree (reg_date DESC);

CREATE INDEX ix_plano_recursos_status ON plantaopro.plano_recursos USING btree (status);

CREATE INDEX ix_plano_recursos_tenant_id ON plantaopro.plano_recursos USING btree (tenant_id);

CREATE TABLE IF NOT EXISTS plantaopro.lgpd_consentimentos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    usuario_id uuid,
    finalidade character varying(160) NOT NULL,
    base_legal character varying(120) NOT NULL,
    versao_politica character varying(30) NOT NULL,
    consentido boolean NOT NULL DEFAULT true,
    ip character varying(80),
    user_agent text,
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_update timestamp without time zone,
    cliente_id uuid,
    tenant_id uuid,
    parceiro_id uuid,
    plano_id uuid,
    status character varying(40) NOT NULL DEFAULT 'ATIVO'::character varying,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    titular_email text,
    politica_id uuid,
    aceito boolean,
    origem text,
    ip_origem text,
    PRIMARY KEY (id)
);

CREATE INDEX ix_lgpd_consentimentos_cliente_id ON plantaopro.lgpd_consentimentos USING btree (cliente_id);

CREATE INDEX ix_lgpd_consentimentos_reg_date ON plantaopro.lgpd_consentimentos USING btree (reg_date DESC);

CREATE INDEX ix_lgpd_consentimentos_status ON plantaopro.lgpd_consentimentos USING btree (status);

CREATE INDEX ix_lgpd_consentimentos_tenant_cliente_status_regdate ON plantaopro.lgpd_consentimentos USING btree (tenant_id, cliente_id, reg_status, reg_date);

CREATE INDEX ix_lgpd_consentimentos_tenant_id ON plantaopro.lgpd_consentimentos USING btree (tenant_id);

CREATE INDEX ix_lgpd_consentimentos_usuario ON plantaopro.lgpd_consentimentos USING btree (usuario_id, finalidade);

CREATE INDEX ix_lgpd_consentimentos_usuario_finalidade ON plantaopro.lgpd_consentimentos USING btree (usuario_id, finalidade, reg_date DESC);

CREATE INDEX ix_lgpd_consentimentos_usuario_reg_date ON plantaopro.lgpd_consentimentos USING btree (usuario_id, reg_date DESC);

CREATE TABLE IF NOT EXISTS plantaopro.v116_faturamento_lote_itens (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status character varying(40) NOT NULL DEFAULT 'ATIVO'::character varying,
    descricao text,
    status_operacional character varying(60),
    valor numeric(14,2),
    data_referencia timestamp with time zone,
    payload_demo jsonb,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by character varying(120),
    updated_at timestamp with time zone,
    updated_by character varying(120),
    lote_id uuid,
    PRIMARY KEY (id)
);

CREATE INDEX ix_v116_faturamento_lote_itens_tenant_status_data ON plantaopro.v116_faturamento_lote_itens USING btree (tenant_id, reg_status, created_at);

CREATE INDEX ix_v116_lote_itens_tenant_lote ON plantaopro.v116_faturamento_lote_itens USING btree (tenant_id, lote_id);

CREATE TABLE IF NOT EXISTS plantaopro.cid_tabela (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    cliente_id uuid,
    tenant_id uuid,
    codigo text NOT NULL,
    descricao text NOT NULL,
    categoria text NOT NULL DEFAULT ''::text,
    status text NOT NULL DEFAULT 'ATIVO'::text,
    created_by uuid,
    reg_date timestamp with time zone NOT NULL DEFAULT now(),
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    capitulo text NOT NULL DEFAULT ''::text,
    updated_by uuid,
    reg_update timestamp without time zone,
    updated_at timestamp without time zone,
    data_fim timestamp with time zone,
    data_inicio timestamp with time zone,
    versao text NOT NULL DEFAULT 'CID-10'::text,
    descricao_normalizada text NOT NULL DEFAULT ''::text,
    capitulo_codigo text NOT NULL DEFAULT ''::text,
    capitulo_nome text NOT NULL DEFAULT ''::text,
    grupo_codigo text NOT NULL DEFAULT ''::text,
    grupo_nome text NOT NULL DEFAULT ''::text,
    fonte text NOT NULL DEFAULT ''::text,
    fonte_url text NOT NULL DEFAULT ''::text,
    data_importacao timestamp without time zone NOT NULL DEFAULT now(),
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

CREATE INDEX ix_cid_tabela_busca ON plantaopro.cid_tabela USING btree (codigo, descricao);

CREATE INDEX ix_cid_tabela_capitulo_codigo ON plantaopro.cid_tabela USING btree (capitulo_codigo);

CREATE INDEX ix_cid_tabela_cliente_id ON plantaopro.cid_tabela USING btree (cliente_id);

CREATE INDEX ix_cid_tabela_codigo ON plantaopro.cid_tabela USING btree (codigo);

CREATE INDEX ix_cid_tabela_descricao ON plantaopro.cid_tabela USING btree (descricao);

CREATE INDEX ix_cid_tabela_descricao_normalizada ON plantaopro.cid_tabela USING btree (descricao_normalizada);

CREATE INDEX ix_cid_tabela_grupo_codigo ON plantaopro.cid_tabela USING btree (grupo_codigo);

CREATE INDEX ix_cid_tabela_reg_date ON plantaopro.cid_tabela USING btree (reg_date);

CREATE INDEX ix_cid_tabela_reg_status ON plantaopro.cid_tabela USING btree (reg_status);

CREATE INDEX ix_cid_tabela_status ON plantaopro.cid_tabela USING btree (status);

CREATE INDEX ix_cid_tabela_versao ON plantaopro.cid_tabela USING btree (versao);

CREATE INDEX ix_saude360_cid_codigo ON plantaopro.cid_tabela USING btree (codigo);

CREATE INDEX ix_saude360_cid_descricao ON plantaopro.cid_tabela USING btree (descricao);

CREATE UNIQUE INDEX ux_cid_tabela_versao_codigo_ativo ON plantaopro.cid_tabela USING btree (versao, upper(codigo)) WHERE ((reg_status = 'A'::bpchar) AND (codigo <> ''::text));

CREATE TABLE IF NOT EXISTS plantaopro.clinica_lancamentos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    cliente_id uuid,
    caixa_id uuid,
    tipo text NOT NULL,
    descricao text NOT NULL,
    valor numeric(12,2) NOT NULL,
    status text NOT NULL DEFAULT 'REGISTRADO'::text,
    created_by uuid,
    updated_by uuid,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone,
    reg_date timestamp with time zone NOT NULL DEFAULT now(),
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    PRIMARY KEY (id)
);

CREATE INDEX ix_clinica_lancamentos_cliente_caixa ON plantaopro.clinica_lancamentos USING btree (cliente_id, caixa_id);

CREATE INDEX ix_clinica_lancamentos_cliente_id ON plantaopro.clinica_lancamentos USING btree (cliente_id);

CREATE INDEX ix_clinica_lancamentos_reg_date ON plantaopro.clinica_lancamentos USING btree (reg_date);

CREATE INDEX ix_clinica_lancamentos_reg_status ON plantaopro.clinica_lancamentos USING btree (reg_status);

CREATE INDEX ix_clinica_lancamentos_status ON plantaopro.clinica_lancamentos USING btree (status);

CREATE TABLE IF NOT EXISTS plantaopro.clinica_estornos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    recebimento_id uuid,
    motivo text NOT NULL DEFAULT ''::text,
    valor numeric(14,2) NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'ESTORNADO'::text,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    PRIMARY KEY (id)
);

CREATE INDEX ix_clinica_estornos_recebimento ON plantaopro.clinica_estornos USING btree (recebimento_id);

CREATE TABLE IF NOT EXISTS plantaopro.convenio_autorizacoes (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    paciente_id uuid,
    convenio_id uuid,
    plano_saude_id uuid,
    agendamento_id uuid,
    consulta_id uuid,
    numero_guia text NOT NULL DEFAULT ''::text,
    senha_autorizacao text NOT NULL DEFAULT ''::text,
    procedimento text NOT NULL DEFAULT ''::text,
    procedimento_id uuid,
    motivo text NOT NULL DEFAULT ''::text,
    valor_autorizado numeric(14,2) NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'PENDENTE'::text,
    motivo_negativa text NOT NULL DEFAULT ''::text,
    validade date,
    created_by uuid,
    updated_by uuid,
    created_at timestamp without time zone NOT NULL DEFAULT now(),
    updated_at timestamp without time zone,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_update timestamp without time zone,
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    PRIMARY KEY (id)
);

CREATE INDEX ix_convenio_autorizacoes_agendamento_id ON plantaopro.convenio_autorizacoes USING btree (agendamento_id);

CREATE INDEX ix_convenio_autorizacoes_cliente_consulta ON plantaopro.convenio_autorizacoes USING btree (cliente_id, consulta_id);

CREATE INDEX ix_convenio_autorizacoes_cliente_id ON plantaopro.convenio_autorizacoes USING btree (cliente_id);

CREATE INDEX ix_convenio_autorizacoes_consulta_id ON plantaopro.convenio_autorizacoes USING btree (consulta_id);

CREATE INDEX ix_convenio_autorizacoes_convenio_id ON plantaopro.convenio_autorizacoes USING btree (convenio_id);

CREATE INDEX ix_convenio_autorizacoes_paciente_id ON plantaopro.convenio_autorizacoes USING btree (paciente_id);

CREATE INDEX ix_convenio_autorizacoes_reg_date ON plantaopro.convenio_autorizacoes USING btree (reg_date);

CREATE INDEX ix_convenio_autorizacoes_reg_status ON plantaopro.convenio_autorizacoes USING btree (reg_status);

CREATE INDEX ix_convenio_autorizacoes_status ON plantaopro.convenio_autorizacoes USING btree (status);

-- ============================================================================
-- Garantia dos codigos de permissao de instalacao limpa. Este bloco
-- garante os codigos exigidos pelos sanity-checks do v2322 (grants
-- ADMINISTRADOR_CLIENTE ADM360) e do v2329 (grants ADMINISTRADOR_CLIENTE
-- USUARIOS). Em instalacao limpa o seeder canonico (v2198 / seeds/system)
-- cobre apenas parte deles e o restante (v2338) so existe depois do v2322 na
-- ordem de versao; nos bancos legados esses codigos vieram dos scripts
-- consolidados pre-manifest. Bloco idempotente: insere somente o que falta
-- (dedupe por codigo normalizado ':' -> '.', mesmo padrao dos sanity-checks),
-- usando o formato canonico com dois-pontos, o modulo ADM360 garantido pelo
-- v2197 e as acoes/modulo USUARIOS canonicos dos seeds/system.
-- ============================================================================
DO $garantia_codigos$
DECLARE
    v_acao_acessar_id uuid;
    v_acao_editar_id uuid;
    v_acao_usr_id uuid;
    v_modulo_adm_id uuid;
    v_modulo_usr_id uuid;
    v_suffix text;
BEGIN
    select id into v_acao_acessar_id from plantaopro.acoes_sistema where codigo in ('ACESSAR', 'LISTAR') order by id limit 1;
    if v_acao_acessar_id is null then
        v_acao_acessar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_acessar_id, 'ACESSAR', 'Acessar', 'ATIVO', 'A', now());
    end if;

    select id into v_acao_editar_id from plantaopro.acoes_sistema where codigo in ('EDITAR', 'SALVAR') order by id limit 1;
    if v_acao_editar_id is null then
        v_acao_editar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_editar_id, 'EDITAR', 'Editar', 'ATIVO', 'A', now());
    end if;

    select id into v_modulo_adm_id from plantaopro.modulos_sistema where upper(btrim(codigo)) = 'ADM360' and reg_status = 'A' limit 1;

    foreach v_suffix in array array[
        'VER',
        'EXPORTAR',
        'IMPORTAR_XML',
        'CRIAR',
        'EDITAR',
        'CONFIGURAR',
        'REABRIR',
        'CONFIRMAR',
        'CANCELAR',
        'CONFERIR',
        'MANIFESTAR_DFE',
        'VINCULAR_DOCUMENTOS',
        'TRANSMITIR_RESPOSTA'
    ] loop
        if not exists (
            select 1 from plantaopro.permissoes
             where replace(upper(btrim(codigo)), ':', '.') = ('ADM360.' || v_suffix)
               and reg_status = 'A'
        ) then
            insert into plantaopro.permissoes (id, acao_id, modulo_id, codigo, nome, status, reg_status, reg_date)
            values (gen_random_uuid(),
                    case when v_suffix = 'VER' then v_acao_acessar_id else v_acao_editar_id end,
                    v_modulo_adm_id, 'ADM360:' || v_suffix, replace('ADM360:' || v_suffix, ':', ' - '), 'ATIVO', 'A', now());
        end if;
    end loop;

    select id into v_modulo_usr_id from plantaopro.modulos_sistema where upper(btrim(codigo)) = 'USUARIOS' and reg_status = 'A' limit 1;
    foreach v_suffix in array array['VER', 'CRIAR', 'EDITAR', 'CONVIDAR'] loop
        if not exists (
            select 1 from plantaopro.permissoes
             where replace(upper(btrim(codigo)), ':', '.') = ('USUARIOS.' || v_suffix)
               and reg_status = 'A'
        ) then
            select id into v_acao_usr_id from plantaopro.acoes_sistema where codigo = v_suffix and reg_status = 'A' order by id limit 1;
            if v_acao_usr_id is null then
                v_acao_usr_id := gen_random_uuid();
                insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
                values (v_acao_usr_id, v_suffix, v_suffix, 'ATIVO', 'A', now());
            end if;
            insert into plantaopro.permissoes (id, acao_id, modulo_id, codigo, nome, status, reg_status, reg_date)
            values (gen_random_uuid(), v_acao_usr_id, v_modulo_usr_id, 'USUARIOS:' || v_suffix, 'USUARIOS ' || v_suffix, 'ATIVO', 'A', now());
        end if;
    end loop;
END
$garantia_codigos$;

