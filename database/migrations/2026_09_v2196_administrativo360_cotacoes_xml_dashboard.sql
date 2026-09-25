-- Migration 2026_09_v2196_administrativo360_cotacoes_xml_dashboard.sql
-- Ampliação do Administrativo 360 do PlantãoPro:
-- 1. Estabelecimentos e Capacidades Contratadas (Portais de Cotação, XML Recebidos, Dashboard Gerencial)
-- 2. Conexões de Portais de Cotação (OPMENEXO, INPART Saúde, Importação Manual)
-- 3. Captação de Cotações, Itens e Anexos
-- 4. Relacionamento de Dados (De/Para de Hospitais, Médicos, Unidades, Produtos)
-- 5. Outbox de Respostas de Cotação e Orçamento Vinculado
-- 6. Sincronização DF-e e Documentos XML Recebidos (NF-e modelo 55), Itens e Eventos

-- 1. Estabelecimentos (Unidades / CNPJs autorizados no Tenant)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_estabelecimentos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cnpj varchar(14) NOT NULL,
    razao_social varchar(160) NOT NULL,
    nome_fantasia varchar(160),
    inscricao_estadual varchar(30),
    cnae varchar(20),
    ambiente varchar(20) NOT NULL DEFAULT 'HOMOLOGACAO' CHECK(ambiente IN ('HOMOLOGACAO', 'PRODUCAO')),
    ativo boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, cnpj)
);

CREATE INDEX IF NOT EXISTS ix_adm360_estab_tenant_cnpj ON plantaopro.adm360_estabelecimentos(tenant_id, cnpj);

-- 2. Capacidades Contratadas por Tenant
CREATE TABLE IF NOT EXISTS plantaopro.adm360_capacidades_contratadas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    capacidade varchar(40) NOT NULL CHECK(capacidade IN ('PORTAIS_COTACAO', 'XML_RECEBIDOS', 'DASHBOARD_GERENCIAL')),
    habilitado boolean NOT NULL DEFAULT true,
    ativado_em timestamptz NOT NULL DEFAULT now(),
    configuracoes jsonb NOT NULL DEFAULT '{}'::jsonb,
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, capacidade)
);

-- 3. Contas de Conexão com Portais Externos (OPMENEXO, INPART, Importação Manual)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_portal_contas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    estabelecimento_id uuid NOT NULL REFERENCES plantaopro.adm360_estabelecimentos(id),
    provedor varchar(30) NOT NULL CHECK(provedor IN ('OPMENEXO', 'INPART', 'IMPORTACAO_MANUAL')),
    nome_conta varchar(100) NOT NULL,
    identificador_externo varchar(100),
    usuario_acesso varchar(100),
    segredo_referencia text,
    ambiente varchar(20) NOT NULL DEFAULT 'HOMOLOGACAO' CHECK(ambiente IN ('HOMOLOGACAO', 'PRODUCAO')),
    status_integracao varchar(30) NOT NULL DEFAULT 'NAO_CONFIGURADA' CHECK(status_integracao IN ('NAO_CONFIGURADA', 'CONFIGURADA', 'BLOQUEADA', 'ERRO_AUTENTICACAO')),
    motivo_bloqueio text,
    ultima_sincronizacao timestamptz,
    ativo boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, provedor, identificador_externo)
);

CREATE INDEX IF NOT EXISTS ix_adm360_pcontas_tenant_prov ON plantaopro.adm360_portal_contas(tenant_id, provedor, status_integracao);

-- 4. De/Para de Dados e Cadastros (Hospitais, Médicos, Unidades, Produtos)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_mapeamentos_de_para (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    portal_conta_id uuid REFERENCES plantaopro.adm360_portal_contas(id),
    provedor varchar(30) NOT NULL,
    tipo_entidade varchar(30) NOT NULL CHECK(tipo_entidade IN ('HOSPITAL', 'SOLICITANTE', 'MEDICO', 'OPERADORA', 'PAGADOR', 'UNIDADE_MEDIDA', 'PRODUTO')),
    codigo_externo varchar(100) NOT NULL,
    descricao_externa varchar(250) NOT NULL,
    entidade_interna_id uuid,
    entidade_interna_descricao varchar(250) NOT NULL,
    fator_conversao numeric(18,4) NOT NULL DEFAULT 1.0000,
    situacao varchar(20) NOT NULL DEFAULT 'ATIVO' CHECK(situacao IN ('ATIVO', 'INATIVO')),
    criado_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, provedor, tipo_entidade, codigo_externo)
);

CREATE INDEX IF NOT EXISTS ix_adm360_depara_tenant_tipo ON plantaopro.adm360_mapeamentos_de_para(tenant_id, provedor, tipo_entidade, codigo_externo);

-- 5. Cotações Pré-Cirúrgicas Captadas
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacoes (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    estabelecimento_id uuid NOT NULL REFERENCES plantaopro.adm360_estabelecimentos(id),
    portal_conta_id uuid NOT NULL REFERENCES plantaopro.adm360_portal_contas(id),
    provedor varchar(30) NOT NULL,
    identificador_externo varchar(100) NOT NULL,
    revisao_externa integer NOT NULL DEFAULT 1,
    hospital_id uuid REFERENCES plantaopro.adm360_parceiros(id),
    hospital_solicitante_externo varchar(200),
    paciente_iniciais varchar(50),
    procedimento varchar(250),
    data_prevista date,
    prazo_resposta timestamptz NOT NULL,
    fuso_horario varchar(40) NOT NULL DEFAULT 'America/Sao_Paulo',
    status_interno varchar(30) NOT NULL DEFAULT 'RECEBIDA' CHECK(status_interno IN ('RECEBIDA', 'EM_RELACIONAMENTO', 'EM_ORCAMENTO', 'AGUARDANDO_APROVACAO', 'PRONTA_PARA_ENVIO', 'RESPONDIDA', 'CANCELADA', 'EXPIRADA')),
    status_externo varchar(40) NOT NULL DEFAULT 'ABERTA',
    origem varchar(30) NOT NULL DEFAULT 'PORTAL_OFICIAL' CHECK(origem IN ('PORTAL_OFICIAL', 'IMPORTACAO_MANUAL', 'DADOS_DE_TESTE')),
    orcamento_id uuid REFERENCES plantaopro.adm360_orcamentos(id),
    payload_original text NOT NULL DEFAULT '{}',
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    capturada_em timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, portal_conta_id, identificador_externo, revisao_externa)
);

CREATE INDEX IF NOT EXISTS ix_adm360_cot_tenant_prazo ON plantaopro.adm360_cotacoes(tenant_id, prazo_resposta, status_interno);
CREATE INDEX IF NOT EXISTS ix_adm360_cot_tenant_ext ON plantaopro.adm360_cotacoes(tenant_id, provedor, identificador_externo);

-- 6. Itens da Cotação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cotacao_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacoes(id) ON DELETE CASCADE,
    numero_item integer NOT NULL,
    codigo_externo varchar(80),
    descricao_externa varchar(250) NOT NULL,
    fabricante_externo varchar(120),
    modelo_externo varchar(120),
    quantidade_solicitada numeric(18,4) NOT NULL CHECK(quantidade_solicitada > 0),
    unidade_solicitada varchar(30) NOT NULL,
    produto_id uuid REFERENCES plantaopro.adm360_produtos(id),
    unidade_interna varchar(30),
    fator_conversao numeric(18,4) NOT NULL DEFAULT 1.0000 CHECK(fator_conversao > 0),
    quantidade_convertida numeric(18,4) NOT NULL DEFAULT 0,
    preco_unitario_ofertado numeric(18,4) NOT NULL DEFAULT 0 CHECK(preco_unitario_ofertado >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    preco_total_ofertado numeric(18,4) NOT NULL DEFAULT 0 CHECK(preco_total_ofertado >= 0),
    material_ofertado varchar(250),
    justificativa_substituicao text,
    status_relacionamento varchar(30) NOT NULL DEFAULT 'PENDENTE' CHECK(status_relacionamento IN ('PENDENTE', 'RELACIONADO', 'NAO_ATENDIDO')),
    motivo_nao_atendimento text,
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, cotacao_id, numero_item)
);

-- 7. Anexos de Cotação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_anexos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cotacao_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacoes(id) ON DELETE CASCADE,
    nome_arquivo varchar(250) NOT NULL,
    tamanho_bytes bigint NOT NULL CHECK(tamanho_bytes >= 0),
    content_type varchar(100) NOT NULL,
    sha256_hash varchar(64) NOT NULL,
    conteudo bytea,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

-- 8. Fila Outbox de Respostas de Cotação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_respostas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cotacao_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacoes(id),
    orcamento_id uuid NOT NULL REFERENCES plantaopro.adm360_orcamentos(id),
    revisao integer NOT NULL DEFAULT 1,
    snapshot_proposta jsonb NOT NULL,
    status_transmissao varchar(30) NOT NULL DEFAULT 'NA_FILA' CHECK(status_transmissao IN ('NA_FILA', 'ENVIANDO', 'ACEITA_PELO_PORTAL', 'REJEITADA_PELO_PORTAL', 'RESULTADO_DESCONHECIDO')),
    status_comercial_externo varchar(40) NOT NULL DEFAULT 'AGUARDANDO_DECISAO' CHECK(status_comercial_externo IN ('AGUARDANDO_DECISAO', 'VENCEDORA', 'PERDIDA', 'CANCELADA_PORTAL')),
    tentativas integer NOT NULL DEFAULT 0,
    max_tentativas integer NOT NULL DEFAULT 3,
    proxima_tentativa timestamptz NOT NULL DEFAULT now(),
    protocolo_externo varchar(120),
    mensagem_retorno text,
    aprovado_por uuid,
    aprovado_em timestamptz,
    enviado_em timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_respcot_tenant_status ON plantaopro.adm360_cotacao_respostas(tenant_id, status_transmissao, proxima_tentativa);

-- 9. Sincronização DF-e (NFeDistribuicaoDFe)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_dfe_sincronizacoes (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    estabelecimento_id uuid NOT NULL REFERENCES plantaopro.adm360_estabelecimentos(id),
    cnpj varchar(14) NOT NULL,
    ambiente varchar(20) NOT NULL DEFAULT 'HOMOLOGACAO' CHECK(ambiente IN ('HOMOLOGACAO', 'PRODUCAO')),
    provedor varchar(30) NOT NULL DEFAULT 'SEFAZ_DISTRIBUICAO_DFE',
    ultimo_nsu varchar(20) NOT NULL DEFAULT '0',
    max_nsu varchar(20) NOT NULL DEFAULT '0',
    data_consulta timestamptz,
    proxima_consulta_permitida timestamptz NOT NULL DEFAULT now(),
    status varchar(30) NOT NULL DEFAULT 'PRONTO' CHECK(status IN ('PRONTO', 'SINCRONIZANDO', 'ERRO', 'AGUARDANDO_INTERVALO')),
    mensagem_erro text,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, estabelecimento_id, ambiente)
);

-- 10. Documentos Fiscais Recebidos (NF-e modelo 55)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_documentos_recebidos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    estabelecimento_id uuid NOT NULL REFERENCES plantaopro.adm360_estabelecimentos(id),
    chave_acesso varchar(44) NOT NULL,
    numero varchar(20) NOT NULL,
    serie varchar(10) NOT NULL,
    modelo varchar(5) NOT NULL DEFAULT '55',
    data_emissao timestamptz NOT NULL,
    emitente_cnpj varchar(14) NOT NULL,
    emitente_nome varchar(160) NOT NULL,
    destinatario_cnpj varchar(14) NOT NULL,
    destinatario_nome varchar(160) NOT NULL,
    valor_total numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_total >= 0),
    valor_produtos numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_produtos >= 0),
    tipo_documento varchar(20) NOT NULL DEFAULT 'NFE_COMPLETA' CHECK(tipo_documento IN ('NFE_COMPLETA', 'RESUMO')),
    status_manifestacao varchar(35) NOT NULL DEFAULT 'SEM_MANIFESTACAO' CHECK(status_manifestacao IN ('SEM_MANIFESTACAO', 'CIENCIA_DA_OPERACAO', 'CONFIRMACAO_DA_OPERACAO', 'DESCONHECIMENTO', 'OPERACAO_NAO_REALIZADA')),
    status_conferencia varchar(25) NOT NULL DEFAULT 'PENDENTE' CHECK(status_conferencia IN ('PENDENTE', 'CONFERIDO', 'VINCULADO', 'DIVERGENTE')),
    pedido_id uuid REFERENCES plantaopro.adm360_pedidos(id),
    recebimento_id uuid REFERENCES plantaopro.adm360_recebimentos(id),
    titulo_pagar_id uuid REFERENCES plantaopro.adm360_titulos_pagar(id),
    xml_conteudo text NOT NULL,
    xml_hash varchar(64) NOT NULL,
    nsu varchar(20),
    quarentena boolean NOT NULL DEFAULT false,
    motivo_quarentena text,
    origem varchar(30) NOT NULL DEFAULT 'IMPORTACAO_MANUAL' CHECK(origem IN ('IMPORTACAO_MANUAL', 'DFE_SINCRONIZACAO', 'DADOS_DE_TESTE')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, chave_acesso)
);

CREATE INDEX IF NOT EXISTS ix_adm360_docrec_tenant_chave ON plantaopro.adm360_documentos_recebidos(tenant_id, chave_acesso);
CREATE INDEX IF NOT EXISTS ix_adm360_docrec_tenant_status ON plantaopro.adm360_documentos_recebidos(tenant_id, status_conferencia, quarentena);

-- 11. Itens do Documento Fiscal Recebido
CREATE TABLE IF NOT EXISTS plantaopro.adm360_documento_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    documento_id uuid NOT NULL REFERENCES plantaopro.adm360_documentos_recebidos(id) ON DELETE CASCADE,
    numero_item integer NOT NULL,
    codigo_produto_emitente varchar(60) NOT NULL,
    descricao_produto_emitente varchar(250) NOT NULL,
    ncm varchar(10),
    cfop varchar(10),
    unidade_comercial varchar(20) NOT NULL,
    quantidade_comercial numeric(18,4) NOT NULL CHECK(quantidade_comercial > 0),
    valor_unitario numeric(18,4) NOT NULL CHECK(valor_unitario >= 0),
    valor_total numeric(18,4) NOT NULL CHECK(valor_total >= 0),
    produto_id uuid REFERENCES plantaopro.adm360_produtos(id),
    unidade_interna varchar(20),
    fator_conversao numeric(18,4) NOT NULL DEFAULT 1.0000,
    quantidade_convertida numeric(18,4) NOT NULL DEFAULT 0,
    conferido boolean NOT NULL DEFAULT false,
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, documento_id, numero_item)
);

-- 12. Eventos e Histórico do Documento Fiscal
CREATE TABLE IF NOT EXISTS plantaopro.adm360_documento_eventos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    documento_id uuid NOT NULL REFERENCES plantaopro.adm360_documentos_recebidos(id) ON DELETE CASCADE,
    tipo_evento varchar(40) NOT NULL,
    sequencia_evento integer NOT NULL DEFAULT 1,
    descricao_evento varchar(250) NOT NULL,
    data_evento timestamptz NOT NULL DEFAULT now(),
    protocolo varchar(60),
    detalhes text,
    registrado_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);
