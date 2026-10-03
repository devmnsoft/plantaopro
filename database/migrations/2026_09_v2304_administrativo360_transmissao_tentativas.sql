-- ============================================================================
-- Migration: 2026_09_v2304_administrativo360_transmissao_tentativas.sql
-- Objetivo: B5 do Administrativo 360 - Cotações e Transmissão.
--   1. Tabela adm360_cotacao_envios: tentativa PERSISTIDA de transmissão de
--      resposta de cotação (outbox de evidência). Cada envio gera exatamente
--      uma linha (UNIQUE tenant_id + resposta_id + tentativa) com:
--        * canal (provedor no momento da tentativa);
--        * status_transmissao: INICIADA enquanto a chamada externa ocorre e,
--          ao final, o mesmo vocabulário da resposta (EXPORTADA_MANUALMENTE,
--          CONFIGURACAO_PENDENTE, ACEITA_PELO_PORTAL, REJEITADA_PELO_PORTAL,
--          RESULTADO_DESCONHECIDO) ou INTERRUPTA quando a conciliação de
--          envios interrompidos (Adm360TransmissaoRecovery) fecha uma linha
--          INICIADA cuja resposta já saiu de ENVIANDO sem registro de
--          resultado (processo caiu entre a marcação e a finalização).
--      A linha é a evidência que impede retransmissão cega: o operador vê
--      quantas tentativas houve, por qual canal e com qual retorno antes de
--      confirmar nova transmissão a partir de RESULTADO_DESCONHECIDO.
--   2. A chamada externa do conector foi retirada da transação longa de
--      banco: marcação (INICIADA + ENVIANDO) e finalização (estado final +
--      arquivo imutável + evento) agora são duas transações curtas, com a
--      rede entre elas. A marcação usa SELECT ... FOR UPDATE para controle
--      de concorrência (duas transmissões simultâneas nunca ambas avançam).
-- Idempotencia: CREATE TABLE IF NOT EXISTS + CREATE INDEX IF NOT EXISTS;
--               pode ser reaplicada em qualquer ordem/replicação do banco.
-- ============================================================================

CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_envios (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    resposta_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacao_respostas(id),
    tentativa int NOT NULL CHECK (tentativa >= 1),
    canal varchar(40) NOT NULL,
    status_transmissao varchar(40) NOT NULL CHECK (status_transmissao IN (
        'INICIADA',
        'ENVIANDO',
        'EXPORTADA_MANUALMENTE',
        'CONFIGURACAO_PENDENTE',
        'ACEITA_PELO_PORTAL',
        'REJEITADA_PELO_PORTAL',
        'RESULTADO_DESCONHECIDO',
        'INTERRUPTA'
    )),
    protocolo_externo varchar(120),
    mensagem varchar(2000),
    iniciado_em timestamptz NOT NULL DEFAULT now(),
    finalizado_em timestamptz,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_adm360_cotacao_envios PRIMARY KEY (id),
    CONSTRAINT uq_adm360_cotacao_envios_tentativa UNIQUE (tenant_id, resposta_id, tentativa)
);

CREATE INDEX IF NOT EXISTS ix_adm360_envios_resposta ON plantaopro.adm360_cotacao_envios(resposta_id, tenant_id);
CREATE INDEX IF NOT EXISTS ix_adm360_envios_tenant_status ON plantaopro.adm360_cotacao_envios(tenant_id, status_transmissao);

COMMENT ON TABLE plantaopro.adm360_cotacao_envios IS
  'B5: tentativas persistidas de transmissão de respostas de cotação (evidência para idempotência, reconciliação e bloqueio de retransmissão cega de resultado desconhecido).';
COMMENT ON COLUMN plantaopro.adm360_cotacao_envios.tentativa IS
  'Nº sequencial da tentativa para a resposta (1 = primeira). UNIQUE com tenant+resposta garante uma linha por tentativa.';
COMMENT ON COLUMN plantaopro.adm360_cotacao_envios.canal IS
  'Provedor/canal utilizado na tentativa (IMPORTACAO_MANUAL, OPMENEXO, INPART ou conta não encontrada = IMPORTACAO_MANUAL).';
COMMENT ON COLUMN plantaopro.adm360_cotacao_envios.status_transmissao IS
  'INICIADA durante a chamada externa; estado final espelha o da resposta ou INTERRUPTA quando a conciliação fecha envio interrompido.';
