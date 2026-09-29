-- ============================================================================
-- PlantaPro — Administrativo 360 | Migration v2300
-- Bloco 7 — Exportação e Integrações
-- Estende o constraint de verificação de status_transmissao nas respostas de
-- cotação para incluir os estados introduzidos pelo canal de exportação manual
-- (IMPORTACAO_MANUAL) e pelos provedores ainda não integrados:
--   * EXPORTADA_MANUALMENTE  — proposta aprovada exportada como arquivo imutável
--                              pelo operador (canal manual); sem protocolo externo.
--   * CONFIGURACAO_PENDENTE  — tentativa de transmissão em provedor oficial com
--                              integração pendente (OPMENEXO/INPART): sem protocolo,
--                              a cotação segue PRONTA_PARA_ENVIO.
-- Mantém os estados existentes: NA_FILA, ENVIANDO, ACEITA_PELO_PORTAL,
-- REJEITADA_PELO_PORTAL, RESULTADO_DESCONHECIDO.
-- Idempotente: DROP CONSTRAINT IF EXISTS + ADD CONSTRAINT.
-- ============================================================================

ALTER TABLE plantaopro.adm360_cotacao_respostas
    DROP CONSTRAINT IF EXISTS adm360_cotacao_respostas_status_transmissao_check;

ALTER TABLE plantaopro.adm360_cotacao_respostas
    ADD CONSTRAINT adm360_cotacao_respostas_status_transmissao_check
    CHECK (status_transmissao IN (
        'NA_FILA',
        'ENVIANDO',
        'EXPORTADA_MANUALMENTE',
        'CONFIGURACAO_PENDENTE',
        'ACEITA_PELO_PORTAL',
        'REJEITADA_PELO_PORTAL',
        'RESULTADO_DESCONHECIDO'
    ));
