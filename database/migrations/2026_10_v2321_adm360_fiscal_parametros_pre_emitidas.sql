-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2321_adm360_fiscal_parametros_pre_emitidas
-- Administrativo 360 - Fiscal (item 9 do plano d1 / requisito A29 do MVP):
-- parametros de emissao, pre-nota interna e seus itens.
--
-- Escopo oficial do bloco: decidir PRIMEIRO operacao/UF/provedor (decisao
-- comercial registrada como PENDENCIA quando ausente) e avancar o que e
-- independente dela: cadastro, seguranca (credenciais fora do banco, A33) e
-- modelagem de estados. Homologacao externa BLOQUEADA ate haver credenciais;
-- portanto esta migration cria APENAS o mundo interno: nenhum status externo
-- pode surgir sem evidencia valida (chave de acesso 44 digitos + momento).
--
-- Alteracoes oficiais:
--   1. adm360_parametros_fiscais: UMA linha por tenant com os parametros da
--      emissao (UF, municipio, regime, operacao, CFOPs por operacao em jsonb
--      - sem CFOP universal hardcodado -, responsavel, ambiente, provedor e
--      certificado_referencia = NOME do segredo no user-secrets/ambiente,
--      nunca o valor — A33). A decisao comercial ausente fica explicita:
--      qualquer campo obrigatorio NULL => status PENDENTE_DE_CONFIGURACAO,
--      garantida por CHECK (status "CONFIGURADO"/"BLOQUEADO" so com o
--      conjunto completo + ambiente escolhido). O operador ve "PENDENTE DE
--      CONFIGURACAO" na tela exatamente enquanto houver pendencia (P2),
--      sem sucesso ficticio de emissao.
--   2. adm360_notas_pre_emitidas: pre-nota interna (A29 MVP: pre-documento +
--      conferencia de referencias; SEM emissao autorizada — isso e P1).
--      Maquina de estados: RASCUNHO -> PRONTA_PARA_EMISSAO -> ENVIANDO ->
--      AUTORIZADA | REJEITADA (+ PENDENTE_CONFIRMACAO para resultado duvidoso)
--      e CANCELADA. INVARIANTE DE EVIDENCIA no proprio banco (H19: emitir
--      bloqueado != sucesso falso):
--        * AUTORIZADA exige chave_acesso_externa (44 digitos) + emitida_em;
--        * REJEITADA exige emitida_em;
--        * CANCELADA exige cancelada_em + motivo_cancelamento.
--      Numero interno sequencial ('NPE-' + 8 digitos) via sequencia, mesmo
--      padrao dos pedidos ADM360 ('PC-').
--   3. adm360_nota_pre_emitida_itens: itens da pre-nota (descricao,
--      quantidade, preco unitario, total). Sem calculo automatico de
--      tributos (A30): o total e registro interno declarado pelo operador.
--   4. adm360_eventos: ampliacao idempotente do CHECK de tipo_evento com
--      EMISSAO_FISCAL (mesmo padrao nomeado do v2314), permitindo a trilha
--      imutavel de transicoes de estado da pre-nota (rastreabilidade B7).
--
-- Idempotencia: CREATE SEQUENCE/TABLE/INDEX IF NOT EXISTS + bloco DO que so
-- recria a constraint quando nenhuma versao contendo EMISSAO_FISCAL existe;
-- pode ser reaplicada em qualquer ordem/replicacao do banco.
-- ============================================================================

-- 1. Parametros fiscais de emissao (uma linha por tenant) -------------------
CREATE TABLE IF NOT EXISTS plantaopro.adm360_parametros_fiscais(
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null references plantaopro.tenants(id),
    uf char(2) check(uf is null or length(btrim(uf)) = 2),
    municipio varchar(120),
    regime_fiscal varchar(20) check(regime_fiscal is null or regime_fiscal in ('SIMPLES_NACIONAL','LUCRO_PRESUMIDO','LUCRO_REAL')),
    operacao_fiscal varchar(12) check(operacao_fiscal is null or operacao_fiscal in ('VENDA','REMESSA','RETORNO')),
    cfops jsonb not null default '{}'::jsonb,
    responsavel_id uuid,
    responsavel_nome varchar(160),
    ambiente varchar(12) not null default 'PENDENTE' check(ambiente in ('PENDENTE','HOMOLOGACAO','PRODUCAO')),
    provedor varchar(12) check(provedor is null or provedor in ('SEFAZ_DIRETO','OPMENEXO','INPART','OUTRO')),
    certificado_referencia varchar(120),
    status varchar(32) not null default 'PENDENTE_DE_CONFIGURACAO'
        check(status in ('PENDENTE_DE_CONFIGURACAO','CONFIGURADO','BLOQUEADO')),
    observacao text,
    created_by uuid,
    updated_by uuid,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    unique(tenant_id),
    unique(tenant_id,id),
    -- Pendencia P2 formalizada: status "decidido" (CONFIGURADO/BLOQUEADO)
    -- somente com o conjunto comercial completo e ambiente escolhido;
    -- enquanto faltar qualquer campo, a situacao e PENDENTE_DE_CONFIGURACAO.
    check((uf is not null and municipio is not null and regime_fiscal is not null
           and operacao_fiscal is not null and provedor is not null
           and ambiente in ('HOMOLOGACAO','PRODUCAO'))
          = (status in ('CONFIGURADO','BLOQUEADO')))
);

comment on table plantaopro.adm360_parametros_fiscais is
    'ADM360 Fiscal: parametros de emissao por tenant. Campos nulos registram a pendencia de decisao comercial (operacao/UF/regime/provedor) — status PENDENTE_DE_CONFIGURACAO ate completar.';
comment on column plantaopro.adm360_parametros_fiscais.cfops is
    'CFOP por operacao (jsonb: {"VENDA":"5101","REMESSA":"5405",...}). Sem CFOP universal hardcodado; ausente/{} significa pendencia de parametrizacao.';
comment on column plantaopro.adm360_parametros_fiscais.certificado_referencia is
    'NOME da referencia do segredo (user-secrets/variavel de ambiente) que guarda senha/token/certificado A1 — nunca o valor em si (A33).';
comment on column plantaopro.adm360_parametros_fiscais.status is
    'PENDENTE_DE_CONFIGURACAO: falta decisao comercial (P2). CONFIGURADO: conjunto completo + ambiente. BLOQUEADO: completo, mas emissao externa bloqueada (ex.: credencial ainda indisponivel).';

-- 2. Pre-nota de emissao (documento interno — A29 MVP) -----------------------
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_nota_pre_numero;

CREATE TABLE IF NOT EXISTS plantaopro.adm360_notas_pre_emitidas(
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null references plantaopro.tenants(id),
    numero varchar(24) not null,
    origem_tipo varchar(12) not null check(origem_tipo in ('MANUAL','VENDA','ORCAMENTO','COTACAO')),
    origem_id uuid,
    situacao varchar(24) not null default 'RASCUNHO'
        check(situacao in ('RASCUNHO','PRONTA_PARA_EMISSAO','ENVIANDO','AUTORIZADA','REJEITADA','PENDENTE_CONFIRMACAO','CANCELADA')),
    destinatario_nome varchar(180) not null,
    destinatario_documento varchar(20),
    itens_total numeric(18,4) not null default 0 check(itens_total >= 0),
    valor_total numeric(18,4) not null default 0 check(valor_total >= 0),
    chave_acesso_externa varchar(44) check(chave_acesso_externa is null or chave_acesso_externa ~ '^[0-9]{44}$'),
    protocolo_externo varchar(80),
    emitida_em timestamptz,
    rejeicao_mensagem text,
    cancelada_em timestamptz,
    motivo_cancelamento text,
    idempotency_key varchar(160),
    versao bigint not null default 1,
    created_by uuid,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    updated_by uuid,
    unique(tenant_id,id),
    unique(tenant_id,numero),
    unique(tenant_id,idempotency_key),
    -- Invariante de evidencia (A29/H19): status EXTERNO somente com evidencia
    -- valida gravada — o banco impede "NF-e autorizada" sem chave + momento.
    check(situacao <> 'AUTORIZADA' or (chave_acesso_externa is not null and emitida_em is not null)),
    check(situacao <> 'REJEITADA' or emitida_em is not null),
    check((situacao = 'CANCELADA') = (cancelada_em is not null)),
    check(situacao <> 'CANCELADA' or motivo_cancelamento is not null)
);

create index if not exists ix_adm360_notas_pre_filtro
    on plantaopro.adm360_notas_pre_emitidas(tenant_id,situacao,created_at desc);

comment on table plantaopro.adm360_notas_pre_emitidas is
    'ADM360 Fiscal: pre-nota interna (MVP A29). Sem emissao autorizada externa no MVP (isso e P1); o documento vive nos estados internos e apenas AUTORIZADA/REJEITADA exigem evidencia externa valida (CHECK).';
comment on column plantaopro.adm360_notas_pre_emitidas.numero is
    'Numero interno sequencial por tenant ("NPE-" + 8 digitos, sequencia adm360_nota_pre_numero) — identificacao interna, nao numero fiscal.';
comment on column plantaopro.adm360_notas_pre_emitidas.chave_acesso_externa is
    'Chave de acesso NF-e (44 digitos) devolvida pela autorizacao externa — evidencia obrigatoria para situacao AUTORIZADA.';

-- 3. Itens da pre-nota ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS plantaopro.adm360_nota_pre_emitida_itens(
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    nota_id uuid not null,
    descricao varchar(180) not null,
    quantidade numeric(18,4) not null check(quantidade > 0),
    preco_unitario numeric(18,4) not null default 0 check(preco_unitario >= 0),
    total numeric(18,4) not null default 0 check(total >= 0),
    created_at timestamptz not null default now(),
    unique(tenant_id,id),
    foreign key(tenant_id,nota_id) references plantaopro.adm360_notas_pre_emitidas(tenant_id,id)
);

create index if not exists ix_adm360_notas_pre_itens_nota
    on plantaopro.adm360_nota_pre_emitida_itens(tenant_id,nota_id);

comment on table plantaopro.adm360_nota_pre_emitida_itens is
    'ADM360 Fiscal: itens da pre-nota (sem calculo automatico de tributos — A30; total e valor interno declarado).';

-- 4. Ampliacao do CHECK de tipo_evento com EMISSAO_FISCAL --------------------
DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT IF EXISTS ck_adm360_eventos_tipo_evento;

  -- Remove checks legados anteriores (definicao varia entre instalacoes) que
  -- ja contenham TRIAGEM (v2314) mas ainda nao contenham EMISSAO_FISCAL.
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_evento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_eventos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%''TRIAGEM'''
      AND pg_get_constraintdef(con.oid) NOT LIKE '%''EMISSAO_FISCAL'''
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_eventos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''EMISSAO_FISCAL''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_eventos
      ADD CONSTRAINT ck_adm360_eventos_tipo_evento
      CHECK (tipo_evento IN ('APROVACAO', 'ARQUIVO', 'DECLARACAO_MANUAL', 'RETORNO_EXTERNO',
                             'CONFIRMACAO_CONFERENCIA', 'ESTORNO', 'CANCELAMENTO', 'TRIAGEM',
                             'EMISSAO_FISCAL'));
  END IF;
END $migration$;
