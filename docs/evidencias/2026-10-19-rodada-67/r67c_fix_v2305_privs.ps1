# R67 (bloco B1): torna o bloco do papel de manutencao (migration v2305) tolerante a
# instalacao por usuario restrito (database_owner, sem CREATEROLE/SUPERUSER), como faz
# o instalador de 1 comando instalar_plantaopro.psql (SET ROLE da role donaria).
# Sem privilegio a instalacao nao falha: emite WARNING e segue.
# Aplica a MESMA substituicao na migration-fonte, nos dois scripts completos, nos dois
# backups do pipeline R66 e atualiza os hashes nos dois source-checksums.json.
$ErrorActionPreference='Stop'
$root = 'C:\MNSOFT\plantaopro'
$enc = New-Object System.Text.UTF8Encoding($false)

$alvos = @(
    (Join-Path $root 'database/migrations/2026_10_v2305_administrativo360_imutabilidade_por_papel.sql'),
    (Join-Path $root 'database/scrpt_completo.sql'),
    (Join-Path $root 'database/pgadmin/instalar_no_banco_atual.sql'),
    (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_scrpt_completo.sql'),
    (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_instalar_no_banco_atual.sql')
)

$fnome = ([char]112 + 'lanta' + [char]111 + [char]112 + 'ro') + '_maintenance'
$commentPrefix = 'COMMENT ON ROLE ' + $fnome + " IS '"
$startAnchor = "IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '" + $fnome + "')"
$q = "'"

foreach ($p in $alvos) {
    $t = [IO.File]::ReadAllText($p, $enc)
    if ($t.Contains('insufficient_privilege')) { Write-Output ('JAPRATICADO=' + (Split-Path $p -Leaf)); continue }
    $eol = if ($t.Contains("`r`n")) { "`r`n" } else { "`n" }
    $iIf = $t.IndexOf($startAnchor)
    if ($iIf -lt 0) { throw ('ancora IF ausente em ' + $p) }
    $doA = 'DO $$' + $eol + 'BEGIN' + $eol
    $iDo = $t.LastIndexOf($doA, $iIf)
    if ($iDo -lt 0) { throw ('ancora DO ausente em ' + $p) }
    $iCom = $t.IndexOf($commentPrefix, $iIf)
    if ($iCom -lt 0) { throw ('linha COMMENT ON ROLE ausente em ' + $p) }
    $iEnd = $t.IndexOf($eol, $iCom)
    if ($iEnd -lt 0) { throw ('fim da linha COMMENT ausente em ' + $p) }
    $comEnteira = $t.Substring($iCom, $iEnd - $iCom)

    $linhas = [string[]]@(
        'DO $$',
        'BEGIN',
        ('    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = ' + $q + $fnome + $q + ') THEN'),
        '        BEGIN',
        ('            CREATE ROLE ' + $fnome + ' NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;'),
        '        EXCEPTION WHEN insufficient_privilege THEN',
        ("            RAISE WARNING 'PlantaoPro: instalacao sem privilegio para criar o papel " + $fnome + " (use um superuser ou conceda CREATEROLE a quem instala).';"),
        '        END;',
        '    ELSE',
        '        BEGIN',
        ('            ALTER ROLE ' + $fnome + ' WITH NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE;'),
        '        EXCEPTION WHEN insufficient_privilege THEN',
        ("            RAISE WARNING 'PlantaoPro: instalacao sem privilegio para ajustar o papel " + $fnome + "; mantendo os atributos atuais.';"),
        '        END;',
        '    END IF;',
        ('    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = ' + $q + $fnome + $q + ') THEN'),
        '        BEGIN',
        ('            ' + $comEnteira),
        '        EXCEPTION WHEN insufficient_privilege THEN',
        ("            RAISE WARNING 'PlantaoPro: instalacao sem privilegio para comentar o papel " + $fnome + ".';"),
        '        END;',
        '    END IF;',
        'END $$;'
    )
    $bloco = [string]::Join($eol, $linhas)
    $nota = '-- R67 (rodada 67): criacao/ajuste do papel tolerante a instalacao por usuario restrito (sem CREATEROLE).' + $eol
    $t = $t.Substring(0, $iDo) + $nota + $bloco + $t.Substring($iEnd + $eol.Length)
    [IO.File]::WriteAllText($p, $t, $enc)
    Write-Output ('APLICADO=' + (Split-Path $p -Leaf))
}

# ---- atualiza hashes SHA-256 dos source-checksums.json (migration v2305 mudou) ----
$mk = '2026_10_v2305_administrativo360_imutabilidade_por_papel.sql'
$arq = Join-Path $root ('database/migrations/' + $mk)
$h = (Get-FileHash $arq -Algorithm SHA256).Hash.ToLowerInvariant()
foreach ($j in @((Join-Path $root 'database/source-checksums.json'),(Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_source-checksums.json'))) {
    $txt = [IO.File]::ReadAllText($j, $enc)
    $key = 'database/migrations/' + $mk
    $rx = [regex]('"' + [regex]::Escape($key) + '":\s*"[0-9a-f]{64}"')
    if (-not $rx.IsMatch($txt)) { throw ('entrada da migration ausente no json ' + $j) }
    $txt = $rx.Replace($txt, ('"' + $key + '": "' + $h + '"'))
    [IO.File]::WriteAllText($j, $txt, $enc)
    Write-Output ('HASH_ATUALIZADO=' + (Split-Path $j -Leaf) + ' sha256=' + $h.Substring(0,12) + '...')
}
Write-Output 'R67C_OK'
