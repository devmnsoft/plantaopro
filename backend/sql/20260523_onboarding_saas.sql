-- ============================================================================
-- 20260523_onboarding_saas.sql
-- Tabelas e estruturas para Onboarding SaaS, Planos, Assinaturas e Permissões
-- ============================================================================

-- ============================================================================
-- 1. TABELA PLANOS (Completa)
-- ============================================================================
CREATE TABLE IF NOT EXISTS plantaopro.planos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome VARCHAR(120) NOT NULL UNIQUE,
    descricao TEXT,
    valor_mensal NUMERIC(12,2) NOT NULL DEFAULT 0,
    limite_medicos INT NOT NULL DEFAULT 0,
    limite_hospitais INT NOT NULL DEFAULT 0,
    limite_plantoes_mes INT NOT NULL DEFAULT 0,
    permite_relatorios BOOLEAN NOT NULL DEFAULT TRUE,
    permite_api BOOLEAN NOT NULL DEFAULT FALSE,
    permite_notificacao_email BOOLEAN NOT NULL DEFAULT TRUE,
    status VARCHAR(20) NOT NULL DEFAULT 'ATIVO',
    reg_status CHAR(1) NOT NULL DEFAULT 'A',
    reg_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    reg_update TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_planos_status ON plantaopro.planos(status, reg_status);
CREATE INDEX IF NOT EXISTS ix_planos_nome ON plantaopro.planos(nome);

-- ============================================================================
-- 2. TABELA CLIENTES (Extensões)
-- ============================================================================
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS descricao TEXT;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS website VARCHAR(255);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS logotipo_url VARCHAR(500);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS data_suspensao TIMESTAMPTZ;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS motivo_suspensao VARCHAR(500);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS limite_usuarios_ativo INT DEFAULT 10;

CREATE INDEX IF NOT EXISTS ix_clientes_cnpj ON plantaopro.clientes(cnpj);
CREATE INDEX IF NOT EXISTS ix_clientes_email ON plantaopro.clientes(email);

-- ============================================================================
-- 3. TABELA ASSINATURAS
-- ============================================================================
CREATE TABLE IF NOT EXISTS plantaopro.assinaturas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID NOT NULL,
    plano_id UUID NOT NULL,
    data_inicio TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    data_fim TIMESTAMPTZ NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ATIVA',
    valor_contratado NUMERIC(12,2) NOT NULL,
    dia_vencimento INT NOT NULL,
    observacoes TEXT,
    motivo_cancelamento VARCHAR(500),
    data_cancelamento TIMESTAMPTZ,
    reg_status CHAR(1) NOT NULL DEFAULT 'A',
    reg_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    reg_update TIMESTAMPTZ,
    CONSTRAINT fk_assinaturas_clientes FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id),
    CONSTRAINT fk_assinaturas_planos FOREIGN KEY (plano_id) REFERENCES plantaopro.planos(id)
);

CREATE INDEX IF NOT EXISTS ix_assinaturas_cliente_id ON plantaopro.assinaturas(cliente_id, status);
CREATE INDEX IF NOT EXISTS ix_assinaturas_plano_id ON plantaopro.assinaturas(plano_id);
CREATE INDEX IF NOT EXISTS ix_assinaturas_vencimento ON plantaopro.assinaturas(data_fim);

-- ============================================================================
-- 4. TABELA UNIDADES (Física para multiempresa)
-- ============================================================================
CREATE TABLE IF NOT EXISTS plantaopro.unidades (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID NOT NULL,
    nome VARCHAR(180) NOT NULL,
    tipo VARCHAR(50),
    cidade VARCHAR(120),
    estado VARCHAR(2),
    responsavel VARCHAR(180),
    status VARCHAR(20) NOT NULL DEFAULT 'ATIVA',
    reg_status CHAR(1) NOT NULL DEFAULT 'A',
    reg_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    reg_update TIMESTAMPTZ,
    CONSTRAINT fk_unidades_clientes FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id)
);

CREATE INDEX IF NOT EXISTS ix_unidades_cliente_id ON plantaopro.unidades(cliente_id, status);

-- ============================================================================
-- 5. TABELA PERMISSÕES
-- ============================================================================
CREATE TABLE IF NOT EXISTS plantaopro.permissoes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome VARCHAR(100) NOT NULL UNIQUE,
    descricao VARCHAR(255),
    modulo VARCHAR(50),
    acao VARCHAR(50),
    reg_status CHAR(1) NOT NULL DEFAULT 'A',
    reg_date TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_permissoes_nome ON plantaopro.permissoes(nome);

-- ============================================================================
-- 6. TABELA PERFIS_PERMISSÕES
-- ============================================================================
CREATE TABLE IF NOT EXISTS plantaopro.perfis_permissoes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    perfil_id UUID NOT NULL,
    permissao_id UUID NOT NULL,
    reg_status CHAR(1) NOT NULL DEFAULT 'A',
    reg_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_perfis_permissoes_perfis FOREIGN KEY (perfil_id) REFERENCES plantaopro.perfis(id),
    CONSTRAINT fk_perfis_permissoes_permissoes FOREIGN KEY (permissao_id) REFERENCES plantaopro.permissoes(id),
    CONSTRAINT uk_perfis_permissoes UNIQUE(perfil_id, permissao_id)
);

CREATE INDEX IF NOT EXISTS ix_perfis_permissoes_perfil_id ON plantaopro.perfis_permissoes(perfil_id);

-- ============================================================================
-- 7. TABELA ALERTAS OPERACIONAIS
-- ============================================================================
CREATE TABLE IF NOT EXISTS plantaopro.alertas_operacionais (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID,
    tipo VARCHAR(50) NOT NULL,
    titulo VARCHAR(255) NOT NULL,
    descricao TEXT,
    severidade VARCHAR(20) NOT NULL,
    resolvido BOOLEAN NOT NULL DEFAULT FALSE,
    data_resolucao TIMESTAMPTZ,
    entidade_tipo VARCHAR(50),
    entidade_id UUID,
    reg_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_alertas_operacionais_clientes FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id)
);

CREATE INDEX IF NOT EXISTS ix_alertas_operacionais_cliente_resolvido ON plantaopro.alertas_operacionais(cliente_id, resolvido);
CREATE INDEX IF NOT EXISTS ix_alertas_operacionais_severidade ON plantaopro.alertas_operacionais(severidade);

-- ============================================================================
-- 8. SEED: PLANOS
-- ============================================================================
INSERT INTO plantaopro.planos(nome, descricao, valor_mensal, limite_medicos, limite_hospitais, limite_plantoes_mes, permite_relatorios, permite_api, status)
SELECT 'Starter', 'Plano inicial para pequenas operações.', 299.00, 20, 5, 150, TRUE, FALSE, 'ATIVO'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.planos WHERE nome = 'Starter');

INSERT INTO plantaopro.planos(nome, descricao, valor_mensal, limite_medicos, limite_hospitais, limite_plantoes_mes, permite_relatorios, permite_api, status)
SELECT 'Professional', 'Plano para operações médias com mais funcionalidades.', 799.00, 100, 15, 500, TRUE, TRUE, 'ATIVO'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.planos WHERE nome = 'Professional');

INSERT INTO plantaopro.planos(nome, descricao, valor_mensal, limite_medicos, limite_hospitais, limite_plantoes_mes, permite_relatorios, permite_api, status)
SELECT 'Enterprise', 'Plano completo com suporte prioritário e integrações.', 2999.00, 999999, 999, 999999, TRUE, TRUE, 'ATIVO'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.planos WHERE nome = 'Enterprise');

-- ============================================================================
-- 9. SEED: PERMISSÕES PADRÃO
-- ============================================================================
INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'PLANTOES_VER', 'Visualizar plantões', 'Plantões', 'Visualizar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'PLANTOES_VER');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'PLANTOES_CRIAR', 'Criar novos plantões', 'Plantões', 'Criar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'PLANTOES_CRIAR');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'PLANTOES_EDITAR', 'Editar plantões existentes', 'Plantões', 'Editar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'PLANTOES_EDITAR');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'PLANTOES_PUBLICAR', 'Publicar plantões para médicos', 'Plantões', 'Publicar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'PLANTOES_PUBLICAR');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'ESCALAS_VER', 'Visualizar escalas', 'Escalas', 'Visualizar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'ESCALAS_VER');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'ESCALAS_CONFIRMAR', 'Confirmar escalas', 'Escalas', 'Confirmar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'ESCALAS_CONFIRMAR');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'FINANCEIRO_VER', 'Visualizar módulo financeiro', 'Financeiro', 'Visualizar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'FINANCEIRO_VER');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'FINANCEIRO_CONFIRMAR', 'Confirmar pagamentos', 'Financeiro', 'Confirmar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'FINANCEIRO_CONFIRMAR');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'USUARIOS_GERENCIAR', 'Gerenciar usuários', 'Usuários', 'Gerenciar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'USUARIOS_GERENCIAR');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'CLIENTES_GERENCIAR', 'Gerenciar clientes', 'Clientes', 'Gerenciar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'CLIENTES_GERENCIAR');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'ASSINATURAS_GERENCIAR', 'Gerenciar assinaturas', 'Assinaturas', 'Gerenciar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'ASSINATURAS_GERENCIAR');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'RELATORIOS_VER', 'Visualizar relatórios', 'Relatórios', 'Visualizar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'RELATORIOS_VER');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'AUDITORIA_VER', 'Visualizar auditoria', 'Auditoria', 'Visualizar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'AUDITORIA_VER');

INSERT INTO plantaopro.permissoes(nome, descricao, modulo, acao)
SELECT 'CONFIGURACOES_EDITAR', 'Editar configurações do sistema', 'Configurações', 'Editar'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.permissoes WHERE nome = 'CONFIGURACOES_EDITAR');

-- ============================================================================
-- 10. SEED: CLIENTE DEMO
-- ============================================================================
INSERT INTO plantaopro.clientes(razao_social, nome_fantasia, cnpj, email, telefone, cidade, estado, plano_id, status, reg_status)
SELECT 'PlantãoPro Demo LTDA', 'Demo PlantãoPro', '11.222.333/0001-44', 'demo@plantaopro.com', '(11) 99999-9999', 'São Paulo', 'SP',
       (SELECT id FROM plantaopro.planos WHERE nome = 'Professional' LIMIT 1), 'ATIVO', 'A'
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.clientes WHERE cnpj = '11.222.333/0001-44');

-- ============================================================================
-- 11. SEED: ASSINATURA CLIENTE DEMO
-- ============================================================================
INSERT INTO plantaopro.assinaturas(cliente_id, plano_id, data_inicio, data_fim, status, valor_contratado, dia_vencimento, observacoes, reg_status)
SELECT c.id, c.plano_id, NOW(), NOW() + INTERVAL '1 year', 'ATIVA', 799.00, EXTRACT(DAY FROM NOW())::INT, 'Assinatura demo', 'A'
FROM plantaopro.clientes c
WHERE c.nome_fantasia = 'Demo PlantãoPro'
  AND NOT EXISTS(SELECT 1 FROM plantaopro.assinaturas WHERE cliente_id = c.id);

-- ============================================================================
-- 12. Adicionar cliente_id nas tabelas (se ainda não existir)
-- ============================================================================
ALTER TABLE plantaopro.usuarios ADD COLUMN IF NOT EXISTS cliente_id UUID;
ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS cliente_id UUID;
ALTER TABLE plantaopro.medicos ADD COLUMN IF NOT EXISTS cliente_id UUID;
ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS cliente_id UUID;
ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS cliente_id UUID;
ALTER TABLE plantaopro.pagamentos ADD COLUMN IF NOT EXISTS cliente_id UUID;
ALTER TABLE plantaopro.notificacoes ADD COLUMN IF NOT EXISTS cliente_id UUID;
ALTER TABLE plantaopro.auditoria ADD COLUMN IF NOT EXISTS cliente_id UUID;

-- Índices para performance
CREATE INDEX IF NOT EXISTS ix_usuarios_cliente_id ON plantaopro.usuarios(cliente_id);
CREATE INDEX IF NOT EXISTS ix_hospitais_cliente_id ON plantaopro.hospitais(cliente_id);
CREATE INDEX IF NOT EXISTS ix_medicos_cliente_id ON plantaopro.medicos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_plantoes_cliente_id ON plantaopro.plantoes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_escalas_cliente_id ON plantaopro.escalas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_pagamentos_cliente_id ON plantaopro.pagamentos(cliente_id);

-- ============================================================================
-- 13. Atualizar registros existentes com cliente_id padrão (Demo)
-- ============================================================================
UPDATE plantaopro.usuarios
SET cliente_id = (SELECT id FROM plantaopro.clientes WHERE nome_fantasia = 'Demo PlantãoPro' LIMIT 1)
WHERE cliente_id IS NULL AND reg_status = 'A';

UPDATE plantaopro.hospitais
SET cliente_id = (SELECT id FROM plantaopro.clientes WHERE nome_fantasia = 'Demo PlantãoPro' LIMIT 1)
WHERE cliente_id IS NULL AND reg_status = 'A';

UPDATE plantaopro.medicos
SET cliente_id = (SELECT id FROM plantaopro.clientes WHERE nome_fantasia = 'Demo PlantãoPro' LIMIT 1)
WHERE cliente_id IS NULL AND reg_status = 'A';

UPDATE plantaopro.plantoes
SET cliente_id = (SELECT id FROM plantaopro.clientes WHERE nome_fantasia = 'Demo PlantãoPro' LIMIT 1)
WHERE cliente_id IS NULL AND reg_status = 'A';

UPDATE plantaopro.escalas
SET cliente_id = (SELECT id FROM plantaopro.clientes WHERE nome_fantasia = 'Demo PlantãoPro' LIMIT 1)
WHERE cliente_id IS NULL AND reg_status = 'A';

UPDATE plantaopro.pagamentos
SET cliente_id = (SELECT id FROM plantaopro.clientes WHERE nome_fantasia = 'Demo PlantãoPro' LIMIT 1)
WHERE cliente_id IS NULL AND reg_status = 'A';

UPDATE plantaopro.notificacoes
SET cliente_id = (SELECT id FROM plantaopro.clientes WHERE nome_fantasia = 'Demo PlantãoPro' LIMIT 1)
WHERE cliente_id IS NULL AND reg_status = 'A';
