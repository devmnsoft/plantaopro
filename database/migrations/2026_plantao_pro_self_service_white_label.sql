-- PlantãoPro SaaS multiempresa, white label e self-service
-- Idempotente: usa schema plantaopro, CREATE/ALTER IF NOT EXISTS e constraints via pg_constraint.
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.tenants(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NULL,
    nome text NOT NULL,
    slug text NOT NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    plano_id uuid NULL,
    criado_por uuid NULL,
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS plano_id uuid NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS subdominio text NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS dominio_customizado text NULL;
ALTER TABLE plantaopro.tenants ADD COLUMN IF NOT EXISTS motivo_suspensao text NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenants_slug ON plantaopro.tenants(lower(slug)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenants_cliente_status_regdate ON plantaopro.tenants(cliente_id,status,reg_date);

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_tenants_status' AND conrelid='plantaopro.tenants'::regclass) THEN
        ALTER TABLE plantaopro.tenants ADD CONSTRAINT ck_tenants_status CHECK (status IN ('ATIVO','TRIAL','SUSPENSO','CANCELADO','PENDENTE'));
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS plantaopro.modulos_sistema(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_modulos_sistema_codigo ON plantaopro.modulos_sistema(lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_modulos_sistema_status_regdate ON plantaopro.modulos_sistema(status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.acoes_sistema(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_acoes_sistema_codigo ON plantaopro.acoes_sistema(lower(codigo)) WHERE reg_status='A';
CREATE TABLE IF NOT EXISTS plantaopro.permissoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), modulo_id uuid NOT NULL, acao_id uuid NOT NULL, codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', sensivel boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_permissoes_codigo ON plantaopro.permissoes(lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_permissoes_modulo_status_regdate ON plantaopro.permissoes(modulo_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.tenant_configuracoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', categoria text NOT NULL DEFAULT 'GERAL', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_configuracoes_chave ON plantaopro.tenant_configuracoes(tenant_id, lower(categoria), lower(chave)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_configuracoes_tenant_status_regdate ON plantaopro.tenant_configuracoes(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, modulo_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'PLANO', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_modulos_codigo ON plantaopro.tenant_modulos(tenant_id, lower(codigo_modulo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_modulos_tenant_status_regdate ON plantaopro.tenant_modulos(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_parametros(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, categoria text NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', tipo text NOT NULL DEFAULT 'texto', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_parametros_chave ON plantaopro.tenant_parametros(tenant_id, lower(categoria), lower(chave)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_parametros_tenant_status_regdate ON plantaopro.tenant_parametros(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_dominios(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, dominio text NOT NULL, tipo text NOT NULL DEFAULT 'SUBDOMINIO', verificado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_dominios_dominio ON plantaopro.tenant_dominios(lower(dominio)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_dominios_tenant_status_regdate ON plantaopro.tenant_dominios(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_white_label(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, nome_plataforma text NOT NULL DEFAULT 'PlantãoPro', cliente_nome text NOT NULL DEFAULT '', slogan text NOT NULL DEFAULT '', logo_url text NOT NULL DEFAULT '', logo_reduzida_url text NOT NULL DEFAULT '', favicon_url text NOT NULL DEFAULT '', cor_primaria text NOT NULL DEFAULT '#0d6efd', cor_secundaria text NOT NULL DEFAULT '#20c997', cor_fundo text NOT NULL DEFAULT '#f8fafc', cor_menu text NOT NULL DEFAULT '#0f172a', tema text NOT NULL DEFAULT 'claro', email_remetente text NOT NULL DEFAULT '', texto_boas_vindas text NOT NULL DEFAULT '', texto_rodape text NOT NULL DEFAULT '', login_banner_url text NOT NULL DEFAULT '', mobile_json jsonb NOT NULL DEFAULT '{}'::jsonb, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_white_label_tenant ON plantaopro.tenant_white_label(tenant_id) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_white_label_tenant_status_regdate ON plantaopro.tenant_white_label(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_onboarding(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, cliente_id uuid NULL, status text NOT NULL DEFAULT 'EM_ANDAMENTO', progresso int NOT NULL DEFAULT 0, proxima_acao text NOT NULL DEFAULT '', iniciado_em timestamp NOT NULL DEFAULT now(), finalizado_em timestamp NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_tenant_onboarding_tenant_status_regdate ON plantaopro.tenant_onboarding(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_onboarding_checklist(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), onboarding_id uuid NOT NULL, tenant_id uuid NOT NULL, cliente_id uuid NULL, codigo text NOT NULL, titulo text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, obrigatorio boolean NOT NULL DEFAULT true, concluido boolean NOT NULL DEFAULT false, concluido_em timestamp NULL, link_acao text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'PENDENTE', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_onboarding_checklist_codigo ON plantaopro.tenant_onboarding_checklist(onboarding_id, lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_tenant_onboarding_checklist_tenant_status_regdate ON plantaopro.tenant_onboarding_checklist(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_auditoria_configuracoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, auditar_configuracoes boolean NOT NULL DEFAULT true, auditar_permissoes boolean NOT NULL DEFAULT true, auditar_lgpd boolean NOT NULL DEFAULT true, retencao_dias int NOT NULL DEFAULT 1825, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_tenant_auditoria_configuracoes_tenant_status_regdate ON plantaopro.tenant_auditoria_configuracoes(tenant_id,status,reg_date);

ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS slug text NULL;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS destaque boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS publico boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_trial boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS dias_trial int NOT NULL DEFAULT 14;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_white_label boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_perfis_customizados boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS ordem int NOT NULL DEFAULT 0;
CREATE INDEX IF NOT EXISTS ix_planos_status_regdate ON plantaopro.planos(status,reg_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_planos_slug ON plantaopro.planos(lower(slug)) WHERE reg_status='A' AND slug IS NOT NULL;

CREATE TABLE IF NOT EXISTS plantaopro.plano_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, modulo_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_modulos_plano_status_regdate ON plantaopro.plano_modulos(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_precos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, periodicidade text NOT NULL DEFAULT 'MENSAL', valor numeric(12,2) NOT NULL DEFAULT 0, moeda text NOT NULL DEFAULT 'BRL', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_precos_plano_status_regdate ON plantaopro.plano_precos(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_limites(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, codigo text NOT NULL, limite int NULL, ilimitado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_limites_plano_status_regdate ON plantaopro.plano_limites(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_comparativo(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NOT NULL, grupo text NOT NULL, recurso text NOT NULL, valor text NOT NULL DEFAULT '', incluido boolean NOT NULL DEFAULT false, ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_comparativo_plano_status_regdate ON plantaopro.plano_comparativo(plano_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.plano_faq(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plano_id uuid NULL, pergunta text NOT NULL, resposta text NOT NULL, ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_plano_faq_plano_status_regdate ON plantaopro.plano_faq(plano_id,status,reg_date);
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS periodicidade text NOT NULL DEFAULT 'MENSAL';
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS renovacao_automatica boolean NOT NULL DEFAULT true;
CREATE INDEX IF NOT EXISTS ix_assinaturas_tenant_cliente_status_regdate ON plantaopro.assinaturas(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, status_anterior text NOT NULL DEFAULT '', status_novo text NOT NULL, motivo text NOT NULL DEFAULT '', usuario_id uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_historico_tenant_cliente_status_regdate ON plantaopro.assinatura_historico(tenant_id,cliente_id,status_novo,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, competencia date NOT NULL, medicos_usados int NOT NULL DEFAULT 0, hospitais_usados int NOT NULL DEFAULT 0, usuarios_usados int NOT NULL DEFAULT 0, plantoes_usados int NOT NULL DEFAULT 0, convites_usados int NOT NULL DEFAULT 0, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_uso_tenant_cliente_status_regdate ON plantaopro.assinatura_uso(tenant_id,cliente_id,competencia,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, codigo_modulo text NOT NULL, habilitado boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'PLANO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_modulos_tenant_cliente_status_regdate ON plantaopro.assinatura_modulos(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_bloqueios(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), assinatura_id uuid NULL, tenant_id uuid NULL, cliente_id uuid NULL, tipo text NOT NULL, mensagem text NOT NULL, resolvido boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_assinatura_bloqueios_tenant_cliente_status_regdate ON plantaopro.assinatura_bloqueios(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.upgrade_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, assinatura_id uuid NULL, plano_atual_id uuid NULL, plano_destino_id uuid NOT NULL, motivo text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'SOLICITADO', solicitado_por uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_upgrade_solicitacoes_tenant_cliente_status_regdate ON plantaopro.upgrade_solicitacoes(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.downgrade_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, assinatura_id uuid NULL, plano_atual_id uuid NULL, plano_destino_id uuid NOT NULL, motivo text NOT NULL DEFAULT '', impacto_validado boolean NOT NULL DEFAULT false, bloqueado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'SOLICITADO', solicitado_por uuid NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_downgrade_solicitacoes_tenant_cliente_status_regdate ON plantaopro.downgrade_solicitacoes(tenant_id,cliente_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_solicitacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, plano_id uuid NULL, nome_fantasia text NOT NULL DEFAULT '', razao_social text NOT NULL DEFAULT '', cnpj text NOT NULL DEFAULT '', segmento text NOT NULL DEFAULT '', qtd_medicos int NOT NULL DEFAULT 0, qtd_hospitais int NOT NULL DEFAULT 0, volume_plantoes_mes int NOT NULL DEFAULT 0, cidade text NOT NULL DEFAULT '', uf text NOT NULL DEFAULT '', telefone text NOT NULL DEFAULT '', email_corporativo text NOT NULL DEFAULT '', responsavel_nome text NOT NULL DEFAULT '', responsavel_email text NOT NULL DEFAULT '', responsavel_telefone text NOT NULL DEFAULT '', responsavel_cargo text NOT NULL DEFAULT '', periodicidade text NOT NULL DEFAULT 'MENSAL', aceite_termos boolean NOT NULL DEFAULT false, aceite_privacidade boolean NOT NULL DEFAULT false, consentimento_lgpd boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'INICIADO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_solicitacoes_tenant_cliente_status_regdate ON plantaopro.cadastro_cliente_solicitacoes(tenant_id,cliente_id,status,reg_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_cadastro_cliente_solicitacoes_cnpj_ativo ON plantaopro.cadastro_cliente_solicitacoes(regexp_replace(cnpj,'\D','','g')) WHERE reg_status='A' AND status <> 'CANCELADO';
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_etapas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, etapa text NOT NULL, concluida boolean NOT NULL DEFAULT false, dados jsonb NOT NULL DEFAULT '{}'::jsonb, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_etapas_status_regdate ON plantaopro.cadastro_cliente_etapas(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_validacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, campo text NOT NULL, mensagem text NOT NULL, valido boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_validacoes_status_regdate ON plantaopro.cadastro_cliente_validacoes(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_convites(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, email text NOT NULL, perfil text NOT NULL DEFAULT 'ADMINISTRADOR_CLIENTE', token_hash text NOT NULL DEFAULT '', aceito boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_convites_status_regdate ON plantaopro.cadastro_cliente_convites(solicitacao_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.cadastro_cliente_pagamentos_iniciais(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), solicitacao_id uuid NOT NULL, cliente_id uuid NULL, assinatura_id uuid NULL, valor numeric(12,2) NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ABERTO', vencimento date NOT NULL DEFAULT (current_date + 7), reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_cadastro_cliente_pagamentos_status_regdate ON plantaopro.cadastro_cliente_pagamentos_iniciais(cliente_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.perfis(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', base_sistema boolean NOT NULL DEFAULT false, customizado boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_perfis_tenant_codigo ON plantaopro.perfis(coalesce(tenant_id,'00000000-0000-0000-0000-000000000000'::uuid), lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_perfis_tenant_cliente_status_regdate ON plantaopro.perfis(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.perfil_permissoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, bloqueado_por_plano boolean NOT NULL DEFAULT false, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_perfil_permissoes_status_regdate ON plantaopro.perfil_permissoes(perfil_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.perfil_modulos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, modulo_id uuid NOT NULL, habilitado boolean NOT NULL DEFAULT true, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_perfil_modulos_status_regdate ON plantaopro.perfil_modulos(perfil_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.usuarios_perfis(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, perfil_id uuid NOT NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_usuarios_perfis_tenant_cliente_status_regdate ON plantaopro.usuarios_perfis(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.usuario_permissoes_especiais(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, justificativa text NOT NULL DEFAULT '', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_usuario_permissoes_especiais_tenant_cliente_status_regdate ON plantaopro.usuario_permissoes_especiais(tenant_id,cliente_id,reg_status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.white_label_temas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, nome text NOT NULL, tema_json jsonb NOT NULL DEFAULT '{}'::jsonb, padrao boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_temas_tenant_status_regdate ON plantaopro.white_label_temas(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_assets(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tipo text NOT NULL, url text NOT NULL, content_type text NOT NULL DEFAULT '', tamanho_bytes bigint NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_assets_tenant_status_regdate ON plantaopro.white_label_assets(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_textos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_textos_tenant_status_regdate ON plantaopro.white_label_textos(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_emails(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tipo text NOT NULL, assunto text NOT NULL DEFAULT '', corpo_html text NOT NULL DEFAULT '', remetente text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_emails_tenant_status_regdate ON plantaopro.white_label_emails(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.white_label_parametros_mobile(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, chave text NOT NULL, valor text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'ATIVO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_white_label_parametros_mobile_tenant_status_regdate ON plantaopro.white_label_parametros_mobile(tenant_id,status,reg_date);

CREATE TABLE IF NOT EXISTS plantaopro.lgpd_consentimentos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, titular_email text NOT NULL DEFAULT '', politica_id uuid NULL, finalidade text NOT NULL, versao_politica text NOT NULL DEFAULT '1.0', aceito boolean NOT NULL DEFAULT true, origem text NOT NULL DEFAULT 'SELF_SERVICE', ip_origem text NOT NULL DEFAULT '', user_agent text NOT NULL DEFAULT '', reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_tenant_cliente_status_regdate ON plantaopro.lgpd_consentimentos(tenant_id,cliente_id,reg_status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_politicas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, versao text NOT NULL, titulo text NOT NULL, conteudo text NOT NULL, vigente boolean NOT NULL DEFAULT false, publicada_em timestamp NULL, status text NOT NULL DEFAULT 'RASCUNHO', reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_politicas_tenant_status_regdate ON plantaopro.lgpd_politicas(tenant_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_solicitacoes_titular(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, tipo text NOT NULL, descricao text NOT NULL, status text NOT NULL DEFAULT 'ABERTA', prazo_resposta date NULL, reg_date timestamp NOT NULL DEFAULT now(), reg_update timestamp NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_solicitacoes_titular_tenant_cliente_status_regdate ON plantaopro.lgpd_solicitacoes_titular(tenant_id,cliente_id,status,reg_date);
CREATE TABLE IF NOT EXISTS plantaopro.lgpd_eventos_privacidade(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NULL, tipo text NOT NULL, descricao text NOT NULL, severidade text NOT NULL DEFAULT 'INFO', reg_date timestamp NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE INDEX IF NOT EXISTS ix_lgpd_eventos_privacidade_tenant_cliente_status_regdate ON plantaopro.lgpd_eventos_privacidade(tenant_id,cliente_id,reg_status,reg_date);

INSERT INTO plantaopro.modulos_sistema(codigo,nome,descricao,ordem)
SELECT x.codigo,x.nome,x.descricao,x.ordem FROM (VALUES
('DASHBOARD','Dashboard','Indicadores e visão geral',10),('MEDICOS','Médicos','Cadastro e gestão de médicos',20),('HOSPITAIS','Hospitais','Unidades e hospitais',30),('ESPECIALIDADES','Especialidades','Especialidades médicas',40),('PLANTOES','Plantões','Criação e publicação de plantões',50),('ESCALAS','Escalas','Escalas e substituições',60),('CONVITES','Convites','Convites aos médicos',70),('AGENDA','Agenda','Agenda operacional',80),('FINANCEIRO','Financeiro','Pagamentos e regras financeiras',90),('PAGAMENTOS','Pagamentos','Pagamentos médicos',100),('NOTIFICACOES','Notificações','Notificações internas e e-mail',110),('COMUNICACAO','Comunicação','Mensagens e comunicados',120),('RELATORIOS','Relatórios','Relatórios operacionais',130),('BI','BI','Inteligência de negócios',140),('OPERACAO_ASSISTIDA','Operação Assistida','Acompanhamento de implantação',150),('SAAS','SaaS','Gestão SaaS',160),('CLIENTES','Clientes','Clientes e tenants',170),('PLANOS','Planos','Planos comerciais',180),('ASSINATURAS','Assinaturas','Assinaturas e uso',190),('FATURAMENTO_SAAS','Faturamento SaaS','Faturas SaaS',200),('CUSTOMER_SUCCESS','Customer Success','Saúde e sucesso do cliente',210),('COMERCIAL','Comercial','Leads e oportunidades',220),('JORNADA_CLIENTE','Jornada Cliente','Jornada e tarefas',230),('LGPD','LGPD','Privacidade e direitos do titular',240),('AJUDA','Ajuda','Central de ajuda',250),('AUDITORIA','Auditoria','Logs e trilhas',260),('OBSERVABILIDADE','Observabilidade','Métricas e logs',270),('CONFIGURACOES','Configurações','Parametrizações',280),('WHITE_LABEL','White Label','Identidade visual por tenant',290)
) AS x(codigo,nome,descricao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.modulos_sistema m WHERE lower(m.codigo)=lower(x.codigo) AND m.reg_status='A');

INSERT INTO plantaopro.acoes_sistema(codigo,nome,descricao,ordem)
SELECT x.codigo,x.nome,x.descricao,x.ordem FROM (VALUES
('VISUALIZAR','Visualizar','Visualizar dados',10),('CRIAR','Criar','Criar registros',20),('EDITAR','Editar','Editar registros',30),('INATIVAR','Inativar','Inativar registros',40),('CANCELAR','Cancelar','Cancelar operações',50),('REATIVAR','Reativar','Reativar registros',60),('APROVAR','Aprovar','Aprovar solicitações',70),('RECUSAR','Recusar','Recusar solicitações',80),('EXPORTAR','Exportar','Exportar dados',90),('CONFIGURAR','Configurar','Configurar recursos',100),('ADMINISTRAR','Administrar','Administração total',110),('VER_DADOS_SENSIVEIS','VerDadosSensiveis','Visualizar dados sensíveis',120)
) AS x(codigo,nome,descricao,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.acoes_sistema a WHERE lower(a.codigo)=lower(x.codigo) AND a.reg_status='A');

INSERT INTO plantaopro.permissoes(modulo_id,acao_id,codigo,nome,descricao,sensivel)
SELECT m.id,a.id,m.codigo || '.' || a.codigo,m.nome || ' - ' || a.nome,'Permissão ' || a.nome || ' no módulo ' || m.nome,(a.codigo='VER_DADOS_SENSIVEIS')
FROM plantaopro.modulos_sistema m CROSS JOIN plantaopro.acoes_sistema a
WHERE m.reg_status='A' AND a.reg_status='A'
AND NOT EXISTS (SELECT 1 FROM plantaopro.permissoes p WHERE p.codigo=m.codigo || '.' || a.codigo AND p.reg_status='A');

INSERT INTO plantaopro.perfis(codigo,nome,descricao,base_sistema,customizado)
SELECT x.codigo,x.nome,x.descricao,true,false FROM (VALUES
('ADMINISTRADOR_GLOBAL','Administrador global','Acesso global à plataforma'),('ADMINISTRADOR_CLIENTE','Administrador cliente','Administração do tenant'),('DIRETOR','Diretor','Visão executiva'),('COORDENADOR','Coordenador','Coordenação operacional'),('OPERADOR','Operador','Operação diária'),('FINANCEIRO','Financeiro','Gestão financeira'),('MEDICO','Médico','Área médica'),('HOSPITAL','Hospital','Usuário hospital/unidade'),('SUPORTE','Suporte','Suporte e atendimento'),('AUDITOR','Auditor','Auditoria e observabilidade'),('COMERCIAL','Comercial','Vendas e upgrades'),('CUSTOMER_SUCCESS','Customer Success','Sucesso do cliente')
) AS x(codigo,nome,descricao)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfis p WHERE p.tenant_id IS NULL AND lower(p.codigo)=lower(x.codigo) AND p.reg_status='A');

-- Seeds comerciais sugeridos, preservando planos existentes.
INSERT INTO plantaopro.planos(id,nome,descricao,valor_mensal,limite_medicos,limite_hospitais,limite_plantoes_mes,limite_usuarios,limite_convites_mes,permite_mobile,permite_bi,permite_relatorios_avancados,permite_integracoes,permite_operacao_assistida,permite_suporte_prioritario,permite_white_label,permite_perfis_customizados,publico,destaque,status,reg_status,reg_date,slug,ordem)
SELECT gen_random_uuid(),x.nome,x.descricao,x.valor,x.medicos,x.hospitais,x.plantoes,x.usuarios,x.convites,x.mobile,x.bi,x.rel_avancado,x.integracoes,x.operacao,x.suporte,x.white_label,true,true,x.destaque,'ATIVO','A',now(),x.slug,x.ordem
FROM (VALUES
('Essencial','Para equipes iniciando a gestão digital de plantões',399.00,20,2,100,5,300,false,false,false,false,false,false,false,false,'essencial',10),
('Profissional','Para operações em crescimento com relatórios avançados e mobile',899.00,100,10,500,20,1500,true,false,true,false,true,true,false,true,'profissional',20),
('Enterprise','Para redes com white label, BI, integrações e SLA customizado',1999.00,0,0,0,0,0,true,true,true,true,true,true,true,false,'enterprise',30),
('Custom','Projeto sob medida com contrato personalizado',0.00,0,0,0,0,0,true,true,true,true,true,true,true,false,'custom',40)
) AS x(nome,descricao,valor,medicos,hospitais,plantoes,usuarios,convites,mobile,bi,rel_avancado,integracoes,operacao,suporte,white_label,destaque,slug,ordem)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.planos p WHERE lower(coalesce(p.slug,p.nome))=lower(x.slug) AND p.reg_status='A');

-- Complemento da rodada PLANTÃOPRO — MULTI-TENANT SELF-SERVICE WHITE LABEL COM PLANOS COMERCIAIS
ALTER TABLE plantaopro.cadastro_cliente_pagamentos_iniciais ADD COLUMN IF NOT EXISTS descricao text NOT NULL DEFAULT 'Fatura SaaS inicial';
ALTER TABLE plantaopro.assinatura_uso ADD COLUMN IF NOT EXISTS recurso text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.assinatura_uso ADD COLUMN IF NOT EXISTS quantidade int NOT NULL DEFAULT 0;
CREATE INDEX IF NOT EXISTS ix_assinatura_uso_recurso_competencia ON plantaopro.assinatura_uso(cliente_id, recurso, competencia) WHERE reg_status='A';

INSERT INTO plantaopro.plano_faq(id, pergunta, resposta, ordem, status, reg_date, reg_status)
SELECT gen_random_uuid(), v.pergunta, v.resposta, v.ordem, 'ATIVO', now(), 'A'
FROM (VALUES
    ('Posso começar sem implantação manual?', 'Sim. O cadastro self-service provisiona tenant, cliente, assinatura, administrador, LGPD, white label padrão e onboarding.', 10),
    ('White label está disponível em todos os planos?', 'White label é liberado conforme regra comercial do plano e possui fallback visual PlantãoPro.', 20),
    ('Como funcionam upgrade e downgrade?', 'Upgrade registra solicitação comercial; downgrade valida limites atuais para evitar impacto operacional.', 30)
) AS v(pergunta,resposta,ordem)
WHERE NOT EXISTS (
    SELECT 1 FROM plantaopro.plano_faq f WHERE lower(f.pergunta)=lower(v.pergunta) AND f.reg_status='A'
);
