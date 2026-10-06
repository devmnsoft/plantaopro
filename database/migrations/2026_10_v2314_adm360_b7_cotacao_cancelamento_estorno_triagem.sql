-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2314_adm360_b7_cotacao_cancelamento_estorno_triagem
-- Administrativo 360 - B7: cancelamento de cotacao, estorno de resposta e
-- triagem responsavel por documento em quarentena
--
-- Alteracoes oficiais:
--   1. adm360_cotacoes: colunas de cancelamento auditavel (cancelado_em e
--      motivo_cancelamento). O estado CANCELADA ja existia na maquina de
--      estados do modulo, mas nao havia registro do momento nem do motivo.
--   2. adm360_cotacao_respostas: indice unico PARCIAL garante no maximo uma
--      resposta NAO RESOLVIDA por cotacao (estados operacionais NA_FILA,
--      ENVIANDO, RESULTADO_DESCONHECIDO, CONFIGURACAO_PENDENTE,
--      REJEITADA_PELO_PORTAL e EXPORTADA_MANUALMENTE). Estados resolvidos
--      (ACEITA_PELO_PORTAL) ficam fora do indice: preserva o historico e
--      permite estorno + re-aprovacao sem duplicidade (a aplicacao reutiliza
--      a linha existente em vez de inserir nova).
--      Backstop de banco complementa a guarda serializada da aplicacao
--      (lock de cotacao -> lock de resposta na mesma ordem em todos os fluxos).
--   3. adm360_documentos_recebidos: responsabilidade e prazo da triagem de
--      quarentena (triagem_responsavel_id, triagem_aberta_em, triagem_prazo,
--      triagem_observacao e triagem_resolvida_em) + indice parcial para a
--      fila de triagens abertas. Resolver a triagem apenas sai do quarentena:
--      a conferencia (status_conferencia) continua uma decisao separada.
--   4. adm360_eventos: ampliacao idempotente do CHECK de tipo_evento com
--      ESTORNO, CANCELAMENTO e TRIAGEM (mesmo padrao nomeado do v2310).
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS / CREATE INDEX IF NOT EXISTS; a
-- ampliacao do CHECK remove qualquer versao anterior sem os novos valores e
-- recria a constraint nomeada apenas quando nao existe versao atualizada.
-- ============================================================================

ALTER TABLE plantaopro.adm360_cotacoes
    ADD COLUMN IF NOT EXISTS cancelado_em timestamptz;

ALTER TABLE plantaopro.adm360_cotacoes
    ADD COLUMN IF NOT EXISTS motivo_cancelamento text;

COMMENT ON COLUMN plantaopro.adm360_cotacoes.cancelado_em IS 'B7: momento do cancelamento interno (NULL quando nao cancelada).';
COMMENT ON COLUMN plantaopro.adm360_cotacoes.motivo_cancelamento IS 'B7: motivo registrado pelo operador no cancelamento.';

CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_resposta_cotacao_nao_resolvida
    ON plantaopro.adm360_cotacao_respostas (tenant_id, cotacao_id)
    WHERE status_transmissao IN ('NA_FILA', 'ENVIANDO', 'RESULTADO_DESCONHECIDO',
                                 'CONFIGURACAO_PENDENTE', 'REJEITADA_PELO_PORTAL',
                                 'EXPORTADA_MANUALMENTE');

CREATE INDEX IF NOT EXISTS ix_adm360_respostas_cotacao_ordem
    ON plantaopro.adm360_cotacao_respostas (tenant_id, cotacao_id, created_at);

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_responsavel_id uuid REFERENCES plantaopro.usuarios(id);

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_aberta_em timestamptz;

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_prazo timestamptz;

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_observacao text;

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_resolvida_em timestamptz;

COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_responsavel_id IS 'B7: usuario do tenant responsavel pela analise da quarentena.';
COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_aberta_em IS 'B7: momento em que a triagem foi aberta (reabrindo reinicia).';
COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_prazo IS 'B7: prazo UTC definido no ato da atribuicao.';
COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_observacao IS 'B7: observacao registrada ao abrir a triagem.';
COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_resolvida_em IS 'B7: momento da resolucao (saida da quarentena); NULL enquanto aberta.';

CREATE INDEX IF NOT EXISTS ix_adm360_docrec_triagem
    ON plantaopro.adm360_documentos_recebidos (tenant_id, quarentena, triagem_prazo)
    WHERE quarentena;

-- 4. Ampliacao do CHECK de tipo_evento (padrao nomeado do v2310, agora com
-- ESTORNO, CANCELAMENTO e TRIAGEM). Remove a constraint nomeada anterior e
-- quaisquer checks legados contendo APROVACAO sem TRIAGEM, e so recria quando
-- nenhuma versao atualizada existe.
DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  -- Remove primeiro a constraint nomeada anterior (podem existir instalacoes
  -- ja criadas pelo v2310 com apenas os cinco tipos originais ampliados).
  ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT IF EXISTS ck_adm360_eventos_tipo_evento;

  -- Remove qualquer CHECK legado restante da coluna (definicao varia entre
  -- instalacoes por ter sido criada sem nome explicito no v2301).
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_evento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_eventos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%''APROVACAO''%'
      AND pg_get_constraintdef(con.oid) NOT LIKE '%''TRIAGEM'''
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  -- So recria se nenhuma versao ja contiver o novo conjunto completo.
  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_eventos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''TRIAGEM''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_eventos
      ADD CONSTRAINT ck_adm360_eventos_tipo_evento
      CHECK (tipo_evento IN ('APROVACAO', 'ARQUIVO', 'DECLARACAO_MANUAL', 'RETORNO_EXTERNO',
                             'CONFIRMACAO_CONFERENCIA', 'ESTORNO', 'CANCELAMENTO', 'TRIAGEM'));
  END IF;
END $migration$;
