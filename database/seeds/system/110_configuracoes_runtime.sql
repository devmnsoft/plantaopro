-- Checkpoint de schema concluído somente após todas as estruturas e seeds anteriores.
INSERT INTO plantaopro.schema_migrations(id,versao,nome,script_path,checksum,iniciado_em,applied_at,aplicado_em,duracao_ms,status,executado_por,ambiente)
SELECT 'v1.39.0','v1.39.0','One-click database runtime-ready','database/install-manifest.json','manifest-managed',now(),now(),now(),0,'APLICADA',current_user,'INSTALL'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE id='v1.39.0');
