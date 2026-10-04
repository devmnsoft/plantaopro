-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2306_saude360_plano_principal_unico
-- P1 Saúde 360: garantia de banco para a troca ATÔMICA do plano principal.
--
--   1. Deduplicação: pacientes com mais de uma linha ativa marcada principal=true
--      mantêm apenas a mais recentemente atualizada (empate resolvido por id desc)
--      e as demais são despromovidas.
--   2. Índice parcial único garante no máximo 1 plano principal ativo por
--      paciente em nível de banco — a corrida de escrita residual (ex.: paciente
--      sem linhas anteriores) vira violação de constraint que o serviço converte
--      em 409.
-- Idempotência: o UPDATE só afeta linhas duplicadas (reaplicação sem dados novos
--      não altera nada); CREATE UNIQUE INDEX IF NOT EXISTS.
-- ============================================================================

WITH ranking AS (
    SELECT id,
           row_number() OVER (
               PARTITION BY cliente_id, paciente_id
               ORDER BY coalesce(updated_at, reg_date) DESC, id DESC
           ) AS rn
    FROM plantaopro.plano_saude_pacientes
    WHERE reg_status = 'A' AND principal = TRUE
)
UPDATE plantaopro.plano_saude_pacientes p
SET principal = FALSE, updated_at = now()
FROM ranking r
WHERE p.id = r.id AND r.rn > 1;

CREATE UNIQUE INDEX IF NOT EXISTS ux_plano_saude_pacientes_principal
    ON plantaopro.plano_saude_pacientes (paciente_id)
    WHERE principal = TRUE AND reg_status = 'A' AND paciente_id IS NOT NULL;
