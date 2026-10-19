# R68 (plano 50 sprints, bloco B1, iteracao 2 do fix de idempotencia): parse do
# codigo canonico MODULO.ACAO pelo 1o PONTO.
#
# A iteracao 1 (r68c) normalizava '.' para '_' antes de parsear por '_' - correto
# quando os componentes nao tem underscore, mas os canonicos podem conter
# underscore (OPERACAO_ASSISTIDA, FEATURE_FLAGS, CUSTOMER_SUCCESS, ...). Cada modulo
# com underscore M=MH_MT fazia a grade 'M.A' parsear como modulo=MH / acao=MT_A:
# ~45 caudas x cada acao existente = ~46x novas acoes falsas por passada (sonda:
# acoes 397 -> 18.307 -> 842.167 em 3 passadas completas; permissoes 1.885.679 na
# 3a). Acaos compostas entram sempre como ACO e nao retroalimentam o parse, entao o
# crescimento multiplicava o vocabulario inteiro a cada instalacao.
#
# Correcao final: o separador entre modulo e acao e SOMENTE o 1o ponto; os
# componentes preservam os underscores (ex.: OPERACAO_ASSISTIDA.VISUALIZAR ->
# modulo=OPERACAO_ASSISTIDA, acao=VISUALIZAR). O '_' segue valendo apenas como
# fallback para codigos legados sem ponto (comportamento original mantido).
#
# Mesma superficie que o r68c (idempotente): 7 arquivos com o bloco comentado +
# UPDATE (fontes 000/010/v1186, scripts completos, backups do pipeline R66) e os
# hashes/marcadores correspondentes (000/010 nos dois source-checksums.json e nas
# secoes embutidas; v1186 e reanexado pelo r66a na regeneracao).
# Uso: powershell -ExecutionPolicy Bypass -File r68e_fix_parse_ponto_canonico.ps1
$ErrorActionPreference = 'Stop'
$root = 'C:\MNSOFT\plantaopro'
$enc = New-Object System.Text.UTF8Encoding($false)

# bloco atual (escrito pelo r68c): 3 comentarios + UPDATE com regexp_replace do ponto
$oldComments = @(
  '-- R68: ponto (.) separa modulo.acao em codigos canonicos; normaliza para _ ANTES de',
  '-- parsear para o derivado (modulo=1o segmento, acao=resto) nao fabricar modulos/acoes',
  '-- compostos falsos a cada reaplicacao (loop de amplificacao identificado na R68).'
)
$oldUpdate = "UPDATE plantaopro.permissoes SET modulo = coalesce(nullif(modulo,''), split_part(regexp_replace(codigo, '[.]', '_'), '_', 1), 'GERAL'), acao = coalesce(nullif(acao,''), nullif(array_to_string((regexp_split_to_array(regexp_replace(codigo, '[.]', '_'), '_'))[2:array_length(regexp_split_to_array(regexp_replace(codigo, '[.]', '_'), '_'), 1)], '_'), ''), 'ACESSAR'), nome = coalesce(nullif(nome,''), codigo), descricao = coalesce(descricao,''), sensivel = coalesce(sensivel,false), status = coalesce(nullif(status,''),'ATIVO'), reg_status = coalesce(nullif(reg_status,''),'A'), reg_date = coalesce(reg_date, now());"
$oldLines = @($oldComments[0], $oldComments[1], $oldComments[2], $oldUpdate)

# bloco novo: parse pelo 1o ponto; fallback '_' somente para codigos legados
$newComments = @(
  '-- R68: o separador canonico entre modulo e acao e o PONTO (MODULO.ACAO) e os componentes',
  '-- podem conter _ (ex.: OPERACAO_ASSISTIDA.VISUALIZAR); o parse separa pelo 1o ponto e',
  '-- usa o _ apenas como fallback em codigos legados sem ponto (loop de amplificacao da R68).'
)
$newUpdate = "UPDATE plantaopro.permissoes SET modulo = coalesce(nullif(modulo,''), CASE WHEN codigo IS NOT NULL AND position('.' in codigo) > 0 THEN split_part(codigo,'.',1) WHEN codigo IS NOT NULL THEN split_part(codigo,'_',1) END, 'GERAL'), acao = coalesce(nullif(acao,''), CASE WHEN codigo IS NOT NULL AND position('.' in codigo) > 0 THEN nullif(substring(codigo from position('.' in codigo) + 1),'') WHEN codigo IS NOT NULL THEN nullif(array_to_string((regexp_split_to_array(codigo,'_'))[2:array_length(regexp_split_to_array(codigo,'_'),1)],'_'),'') END, 'ACESSAR'), nome = coalesce(nullif(nome,''), codigo), descricao = coalesce(descricao,''), sensivel = coalesce(sensivel,false), status = coalesce(nullif(status,''),'ATIVO'), reg_status = coalesce(nullif(reg_status,''),'A'), reg_date = coalesce(reg_date, now());"
$newLines = @($newComments[0], $newComments[1], $newComments[2], $newUpdate)
$markerTxt = '-- R68: o separador canonico'

function Patch-Body([string]$p, [int]$esperado) {
  $t = [IO.File]::ReadAllText($p, $enc)
  if ($t.Contains($markerTxt)) { Write-Host ('JAPLICADO ' + (Split-Path $p -Leaf)); return }
  $eol = "`n"; if ($t.Contains("`r`n")) { $eol = "`r`n" }
  $oldBlock = ($oldLines -join $eol)
  $cont = ([regex]::Matches($t, [regex]::Escape($oldBlock))).Count
  if ($cont -ne $esperado) { throw ((Split-Path $p -Leaf) + ': ocorrencias esperadas=' + $esperado + ' encontradas=' + $cont) }
  $t2 = $t.Replace($oldBlock, ($newLines -join $eol))
  $chk = ([regex]::Matches($t2, [regex]::Escape($newUpdate))).Count
  if ($chk -ne $esperado) { throw ((Split-Path $p -Leaf) + ': checagem pos-substituicao=' + $chk + ' esperado=' + $esperado) }
  [IO.File]::WriteAllText($p, $t2, $enc)
  Write-Host ('APLICADO  ' + (Split-Path $p -Leaf) + ' subs=' + $cont)
}

Patch-Body (Join-Path $root 'database/schema/000_schema_canonico_base.sql') 1
Patch-Body (Join-Path $root 'database/schema/010_identity_access.sql') 1
Patch-Body (Join-Path $root 'database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql') 1
Patch-Body (Join-Path $root 'database/scrpt_completo.sql') 3
Patch-Body (Join-Path $root 'database/pgadmin/instalar_no_banco_atual.sql') 3
Patch-Body (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_scrpt_completo.sql') 2
Patch-Body (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_instalar_no_banco_atual.sql') 2

# ---- hashes novos dos arquivos-fonte (bytes no disco, mesma convencao do r66a/r67c) ----
$f000  = Join-Path $root 'database/schema/000_schema_canonico_base.sql'
$f010  = Join-Path $root 'database/schema/010_identity_access.sql'
$f1186 = Join-Path $root 'database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql'
$keys = @(
  ,@('database/schema/000_schema_canonico_base.sql', (Get-FileHash $f000 -Algorithm SHA256).Hash.ToLowerInvariant())
  ,@('database/schema/010_identity_access.sql', (Get-FileHash $f010 -Algorithm SHA256).Hash.ToLowerInvariant())
  ,@('database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql', (Get-FileHash $f1186 -Algorithm SHA256).Hash.ToLowerInvariant())
)

# ---- source-checksums.json (atual e backup do pipeline) ----
$jfiles = @(
  (Join-Path $root 'database/source-checksums.json'),
  (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_source-checksums.json')
)
foreach ($jf in $jfiles) {
  $j = [IO.File]::ReadAllText($jf, $enc)
  foreach ($k in $keys) {
    $patJ = ('"' + [regex]::Escape($k[0]) + '":\s*"') + '([0-9a-f]{64})'
    $rm = (New-Object System.Text.RegularExpressions.Regex $patJ).Matches($j)
    if ($rm.Count -eq 0) {
      # chave ausente no manifest de base: secoes anexadas pelo r66a entram com
      # hash recalculado na propria regeneracao (ex.: v1186 no _backup_source-checksums.json)
      Write-Host ('SEM_CHAVE ' + (Split-Path $jf -Leaf) + ' ' + (Split-Path $k[0] -Leaf))
      continue
    }
    if ($rm.Count -ne 1) { throw ((Split-Path $jf -Leaf) + ' chave ' + $k[0] + ' ocorrencias=' + $rm.Count) }
    $gi = $rm[0].Groups[1].Index
    $oh = $rm[0].Groups[1].Value
    if ($oh -ne $k[1]) {
      $j = $j.Substring(0, $gi) + $k[1] + $j.Substring($gi + 64)
      Write-Host ('HASH_JSON ' + (Split-Path $jf -Leaf) + ' ' + (Split-Path $k[0] -Leaf))
    }
  }
  $null = $j | ConvertFrom-Json
  [IO.File]::WriteAllText($jf, $j, $enc)
}

# ---- marcadores -- SOURCE-SHA256 das secoes de base 000/010 nos 4 arquivos grandes ----
# (o marcador da secao v1186 e recalculado pelo proprio r66a na regeneracao)
$big4 = @(
  (Join-Path $root 'database/scrpt_completo.sql'),
  (Join-Path $root 'database/pgadmin/instalar_no_banco_atual.sql'),
  (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_scrpt_completo.sql'),
  (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_instalar_no_banco_atual.sql')
)
foreach ($bf in $big4) {
  $t = [IO.File]::ReadAllText($bf, $enc)
  foreach ($k in @($keys[0], $keys[1])) {
    $patM = '-- SOURCE: ' + [regex]::Escape($k[0]) + '\r?\n-- SOURCE-SHA256: ' + '([0-9a-f]{64})'
    $rm = (New-Object System.Text.RegularExpressions.Regex $patM).Matches($t)
    if ($rm.Count -ne 1) { throw ((Split-Path $bf -Leaf) + ' marcador ' + $k[0] + ' ocorrencias=' + $rm.Count) }
    $gi = $rm[0].Groups[1].Index
    $oh = $rm[0].Groups[1].Value
    if ($oh -ne $k[1]) {
      $t = $t.Substring(0, $gi) + $k[1] + $t.Substring($gi + 64)
      Write-Host ('MARKER    ' + (Split-Path $bf -Leaf) + ' ' + (Split-Path $k[0] -Leaf))
    }
  }
  [IO.File]::WriteAllText($bf, $t, $enc)
}

# ---- verificacao final ----
foreach ($bf in @((Join-Path $root 'database/scrpt_completo.sql'), (Join-Path $root 'database/pgadmin/instalar_no_banco_atual.sql'))) {
  $t = [IO.File]::ReadAllText($bf, $enc)
  $so = ([regex]::Matches($t, [regex]::Escape($oldUpdate))).Count
  $nv = ([regex]::Matches($t, [regex]::Escape($newUpdate))).Count
  Write-Host ('FINAL ' + (Split-Path $bf -Leaf) + ' r68c_restante=' + $so + ' nova=' + $nv)
  if ($so -ne 0 -or $nv -ne 3) { throw ('verificacao final falhou em ' + (Split-Path $bf -Leaf)) }
}
Write-Host ('H000=' + $keys[0][1])
Write-Host ('H010=' + $keys[1][1])
Write-Host ('HV1186=' + $keys[2][1])
Write-Host 'R68E_OK'
