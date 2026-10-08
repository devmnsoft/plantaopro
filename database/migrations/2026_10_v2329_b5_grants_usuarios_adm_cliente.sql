-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2329_b5_grants_usuarios_adm_cliente
-- B5 (convidar equipe): grants USUARIOS para ADMINISTRADOR_CLIENTE.
-- As telas de equipe/convites autorizam POR ACAO (USUARIOS.VER/CRIAR/EDITAR/
-- CONVIDAR); perfis de cliente criados antes desta rodada nao tem esses
-- grants e negariam tudo com PERMISSAO_NEGADA. Mesmo padrao dedupe da v2322
-- (distinct por codigo normalizado + not-exists = idempotente).
-- ============================================================================

DO $migration$
BEGIN
    IF EXISTS (
        SELECT 1
          FROM (VALUES ('USUARIOS.VER'),('USUARIOS.CRIAR'),('USUARIOS.EDITAR'),('USUARIOS.CONVIDAR')) AS alvo(codigo)
         WHERE NOT EXISTS (
             SELECT 1 FROM plantaopro.permissoes pe
              WHERE replace(pe.codigo, ':', '.') = alvo.codigo)
    ) THEN
        RAISE EXCEPTION 'v2329: codigo USUARIOS esperado ausente em plantaopro.permissoes';
    END IF;

    INSERT INTO plantaopro.perfil_permissoes(id, perfil_id, permissao_id, permitido, reg_date, reg_status)
    SELECT gen_random_uuid(), pf.id, esc.id, true, now(), 'A'
      FROM plantaopro.perfis pf
      CROSS JOIN (VALUES ('USUARIOS.VER'),('USUARIOS.CRIAR'),('USUARIOS.EDITAR'),('USUARIOS.CONVIDAR')) AS alvo(codigo)
      JOIN LATERAL (
          SELECT pe.id
            FROM plantaopro.permissoes pe
           WHERE replace(pe.codigo, ':', '.') = alvo.codigo
           ORDER BY pe.id
           LIMIT 1
      ) AS esc ON true
     WHERE pf.codigo = 'ADMINISTRADOR_CLIENTE'
       AND pf.reg_status = 'A'
       AND NOT EXISTS (
           SELECT 1
             FROM plantaopro.perfil_permissoes pp
             JOIN plantaopro.permissoes pe2 ON pe2.id = pp.permissao_id
            WHERE pp.perfil_id = pf.id
              AND pp.reg_status = 'A'
              AND replace(pe2.codigo, ':', '.') = alvo.codigo
       );
END $migration$;
