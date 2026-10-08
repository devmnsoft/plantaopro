-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2322_adm360_grants_adm_cliente_r5_a2
-- Administrativo 360 - Fiscal (R5-A2): grants ADM360 para ADMINISTRADOR_CLIENTE.
--
-- Contexto: a API fiscal (api/administrativo360/fiscal) autoriza POR ACAO
-- (Adm360.Ver/Configurar/Criar/Editar/Reabrir/Confirmar/Cancelar + as acoes de
-- XML ja existentes). Perfis ADMINISTRADOR_CLIENTE criados antes desta rodada
-- (seed antigo ou provisionamento manual) nao possuem esses grants, entao as
-- telas fiscais negariam tudo com PERMISSAO_NEGADA mesmo para o gestor do
-- cliente. Esta migration concede os 13 codigos ADM360 aos perfis ativos
-- codigo='ADMINISTRADOR_CLIENTE'.
--
-- Dedupe: permissoes.codigo mistura ponto e dois-pontos (linhas historicas
-- duplicadas, ex.: ADM360.VER e ADM360:VER). A escolha e deterministica
-- (distinct on do codigo normalizado ':' -> '.', menor id) e a insercao so
-- acontece quando o perfil ainda nao tem NENHUM grant com o mesmo codigo
-- normalizado — reaplicacao nao duplica nada (idempotente).
-- ============================================================================

DO $migration$
BEGIN
    -- Sanity: os 13 codigos precisam existir (forma ponto ou dois-pontos).
    IF EXISTS (
        SELECT 1
          FROM (VALUES ('ADM360.VER'),('ADM360.EXPORTAR'),('ADM360.IMPORTAR_XML'),
                       ('ADM360.CRIAR'),('ADM360.EDITAR'),('ADM360.CONFIGURAR'),
                       ('ADM360.REABRIR'),('ADM360.CONFIRMAR'),('ADM360.CANCELAR'),
                       ('ADM360.CONFERIR'),('ADM360.MANIFESTAR_DFE'),
                       ('ADM360.VINCULAR_DOCUMENTOS'),('ADM360.TRANSMITIR_RESPOSTA')) AS alvo(codigo)
         WHERE NOT EXISTS (
             SELECT 1 FROM plantaopro.permissoes pe
              WHERE replace(pe.codigo, ':', '.') = alvo.codigo)
    ) THEN
        RAISE EXCEPTION 'v2322: codigo ADM360 esperado ausente em plantaopro.permissoes';
    END IF;

    INSERT INTO plantaopro.perfil_permissoes(id, perfil_id, permissao_id, permitido, reg_date, reg_status)
    SELECT gen_random_uuid(), pf.id, esc.id, true, now(), 'A'
      FROM plantaopro.perfis pf
      CROSS JOIN (VALUES ('ADM360.VER'),('ADM360.EXPORTAR'),('ADM360.IMPORTAR_XML'),
                         ('ADM360.CRIAR'),('ADM360.EDITAR'),('ADM360.CONFIGURAR'),
                         ('ADM360.REABRIR'),('ADM360.CONFIRMAR'),('ADM360.CANCELAR'),
                         ('ADM360.CONFERIR'),('ADM360.MANIFESTAR_DFE'),
                         ('ADM360.VINCULAR_DOCUMENTOS'),('ADM360.TRANSMITIR_RESPOSTA')) AS alvo(codigo)
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
