# R68 (plano 50 sprints, bloco B1): quebra o loop de amplificacao dos catalogos de
# permissoes na reaplicacao do script completo (2o defeito de idempotencia da R68).
#
# Diagnostico (banco de prova com 2 aplicacoes completas + 1 parcial):
#   permissoes 1850 -> 556.860 | acoes_sistema 398 -> 25.518 | modulos_sistema 1399 -> 3099
# Causa raiz: o UPDATE de normalizacao presente nos arquivos-fonte
#   database/schema/000_schema_canonico_base.sql
#   database/schema/010_identity_access.sql
#   database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql
# preenchia modulo/acao a partir do codigo separando apenas por '_', mas os codigos
# canonicos usam '.' (MODULO.ACAO). O ponto ficava colado ao token e o DISTINCT
# derivava modulos compostos falsos (TENANTS_SUSPENDER) e acoes compostas crescentes
# (SUSPENDER_VER, SUSPENDER_SUSPENDER_VER, ...) que o cross join modulo x acao das
# secoes LGPD materializava em novas permissoes - crescimento fractal em cada
# instalacao seguinte.
# Correcao: normaliza '.' para '_' antes de parsear (fixed point em 1 passada).
# Aplicacao: a MESMA substituicao na migration-fonte v1186, nos dois scripts completos
# e nos dois backups do pipeline R66 (as secoes de base 000/010 vivem neles; a secao
# v1186 e reanexada pelo r66a na regeneracao). Atualiza tambem os hashes dos fontes
# 000/010/v1186 nos dois source-checksums.json e os marcadores -- SOURCE-SHA256 das
# secoes 000/010 nos quatro arquivos grandes.
# Uso: powershell -ExecutionPolicy Bypass -File r68c_fix_normalizacao_permissoes.ps1
# Idempotente: detecta a nova linha e os hashes ja atualizados.
$ErrorActionPreference = 'Stop'
$root = 'C:\MNSOFT\plantaopro'
$enc = New-Object System.Text.UTF8Encoding($false)

$oldLine = "UPDATE plantaopro.permissoes SET modulo = coalesce(nullif(modulo,''), split_part(codigo,'_',1), 'GERAL'), acao = coalesce(nullif(acao,''), nullif(array_to_string((regexp_split_to_array(codigo,'_'))[2:array_length(regexp_split_to_array(codigo,'_'),1)], '_'), ''), 'ACESSAR'), nome = coalesce(nullif(nome,''), codigo), descricao = coalesce(descricao,''), sensivel = coalesce(sensivel,false), status = coalesce(nullif(status,''),'ATIVO'), reg_status = coalesce(nullif(reg_status,''),'A'), reg_date = coalesce(reg_date, now());"

$newComments = @(
  '-- R68: ponto (.) separa modulo.acao em codigos canonicos; normaliza para _ ANTES de',
  '-- parsear para o derivado (modulo=1o segmento, acao=resto) nao fabricar modulos/acoes',
  '-- compostos falsos a cada reaplicacao (loop de amplificacao identificado na R68).'
)
$newUpdate = "UPDATE plantaopro.permissoes SET modulo = coalesce(nullif(modulo,''), split_part(regexp_replace(codigo, '[.]', '_'), '_', 1), 'GERAL'), acao = coalesce(nullif(acao,''), nullif(array_to_string((regexp_split_to_array(regexp_replace(codigo, '[.]', '_'), '_'))[2:array_length(regexp_split_to_array(regexp_replace(codigo, '[.]', '_'), '_'), 1)], '_'), ''), 'ACESSAR'), nome = coalesce(nullif(nome,''), codigo), descricao = coalesce(descricao,''), sensivel = coalesce(sensivel,false), status = coalesce(nullif(status,''),'ATIVO'), reg_status = coalesce(nullif(reg_status,''),'A'), reg_date = coalesce(reg_date, now());"
$newBlock = @($newComments[0], $newComments[1], $newComments[2], $newUpdate)
$markerTxt = '-- R68: ponto (.) separa'

# auto-teste do mecanismo de captura/substituicao posicional (protecao contra
# regressao em grupos de regex: a substituicao usa somente o indice do grupo)
$stHex = 'a' * 64
$stTxt = 'X' + ':' + '  "' + $stHex + '"'
$stRe = New-Object System.Text.RegularExpressions.Regex (':\s*"([0-9a-f]{64})')
$stMs = $stRe.Matches($stTxt)
if ($stMs.Count -ne 1 -or $stMs[0].Groups[1].Value -ne $stHex) { throw 'SELFTEST_FALLHOU' }
$stGi = $stMs[0].Groups[1].Index
$stOut = $stTxt.Substring(0, $stGi) + ('b' * 64) + $stTxt.Substring($stGi + 64)
if ($stOut -ne ('X' + ':  "' + ('b' * 64) + '"')) { throw 'SELFTEST_FALLHOU_2' }

function Patch-Body([string]$p, [int]$esperado) {
  $t = [IO.File]::ReadAllText($p, $enc)
  if ($t.Contains($markerTxt)) { Write-Host ('JAPLICADO ' + (Split-Path $p -Leaf)); return }
  $cont = ([regex]::Matches($t, [regex]::Escape($oldLine))).Count
  if ($cont -ne $esperado) { throw ((Split-Path $p -Leaf) + ': ocorrencias esperadas=' + $esperado + ' encontradas=' + $cont) }
  $eol = "`n"; if ($t.Contains("`r`n")) { $eol = "`r`n" }
  $t2 = $t.Replace($oldLine, ($newBlock -join $eol))
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
  $so = ([regex]::Matches($t, [regex]::Escape($oldLine))).Count
  $nv = ([regex]::Matches($t, [regex]::Escape($newUpdate))).Count
  Write-Host ('FINAL ' + (Split-Path $bf -Leaf) + ' antiga=' + $so + ' nova=' + $nv)
  if ($so -ne 0 -or $nv -ne 3) { throw ('verificacao final falhou em ' + (Split-Path $bf -Leaf)) }
}
Write-Host ('H000=' + $keys[0][1])
Write-Host ('H010=' + $keys[1][1])
Write-Host ('HV1186=' + $keys[2][1])
Write-Host 'R68C_OK'
