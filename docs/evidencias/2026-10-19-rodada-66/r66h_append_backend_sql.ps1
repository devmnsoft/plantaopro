# R66 (bloco B1): anexa as migrations legadas de backend/sql/ (23 arquivos, ordem cronologica)
# + database/schema/150_portal_unidade_contratos_v2100.sql ao scrpt_completo.sql e a variante
# pgadmin, como secoes numeradas a partir da proxima disponivel (183+), com marcadores
# SOURCE/SOURCE-SHA256 e atualizacao do source-checksums.json (ordem alfabetica preservada).
# Motivo: esses arquivos criam tabelas referenciadas pelo codigo que a importacao unica
# nao produzia (chamados_suporte, operacao_assistida_*, alertas_operacionais, solicitacoes_plantao,
# contratos_operacionais, usuario_notificacao_preferencias e colunas incrementais).
$ErrorActionPreference = 'Stop'
$root = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$enc = New-Object System.Text.UTF8Encoding($false)

$itens = @(
  'backend/sql|20260519_plantoes_incremental.sql',
  'backend/sql|20260520_escalas_incremental.sql',
  'backend/sql|20260520_medicos_usuario_id_incremental.sql',
  'backend/sql|20260520_pagamentos_incremental.sql',
  'backend/sql|20260520_premium_saas_incremental.sql',
  'backend/sql|20260521_premium_automacao_kpis.sql',
  'backend/sql|20260521_premium_permissoes_notificacoes.sql',
  'backend/sql|20260522_saas_multiempresa.sql',
  'backend/sql|20260523_comunicacao_convites_mobile_incremental.sql',
  'backend/sql|20260523_enterprise_observabilidade.sql',
  'backend/sql|20260523_onboarding_saas.sql',
  'backend/sql|20260523_saas_premium_modulos_incremental.sql',
  'backend/sql|20260526_motor_elegibilidade_conflitos.sql',
  'backend/sql|20260529_pre_producao_homologacao.sql',
  'backend/sql|20260531_enterprise_auditoria_observabilidade.sql',
  'backend/sql|20260602_mobile_convites_indices.sql',
  'backend/sql|20260602_robustez_auditoria_observabilidade.sql',
  'backend/sql|20260602_suporte_mobile_rc.sql',
  'backend/sql|2026-06-03_corrigir_auditoria_request_logs.sql',
  'backend/sql|20260603_mvp_comercial_avancado.sql',
  'backend/sql|20260603_operacao_assistida_beta.sql',
  'backend/sql|20260605_beta_homologavel_constraints.sql',
  'backend/sql|20260610_saude360_base_clinica_idempotente.sql',
  'database/schema|150_portal_unidade_contratos_v2100.sql'
)
Write-Output ('TOTAL_A_ANEXAR=' + $itens.Count)

$alvos = @('database/scrpt_completo.sql', 'database/pgadmin/instalar_no_banco_atual.sql')
$linhas = Get-Content (Join-Path $root 'database/scrpt_completo.sql') -Encoding UTF8
if (($linhas | Where-Object { $_ -match 'R66 gap instalacao: 20260610_saude360_base_clinica_idempotente' }).Count -gt 0) {
    Write-Output 'JAPENDIDO_PULAR'
    exit 0
}
$idxHdr = -1
for ($i = 0; $i -lt $linhas.Count; $i++) { if ($linhas[$i] -match 'R62 AI Provedor Drael') { $idxHdr = $i; break } }
if ($idxHdr -lt 0) { throw 'cabecalho da seccao 127 nao encontrado' }
$sep = $linhas[$idxHdr - 1]
$hdrRef = $linhas[$idxHdr]
$maxNum = 0
foreach ($l in $linhas) {
    $m = [regex]::Match($l, '^-- Se[çc][aã]o (\d+) ')
    if ($m.Success -and [int]$m.Groups[1].Value -gt $maxNum) { $maxNum = [int]$m.Groups[1].Value }
}
Write-Output ('NUM_INICIAL=' + ($maxNum + 1))

$hashes = @{}
$blocks = New-Object System.Collections.Generic.List[string]
$n = $maxNum + 1
foreach ($item in $itens) {
    $parts = $item.Split('|')
    $rel = $parts[0] + '/' + $parts[1]
    $p = Join-Path $root $rel.Replace('/', '\')
    if (-not (Test-Path $p)) { throw 'arquivo ausente: ' + $p }
    $sha = (Get-FileHash $p -Algorithm SHA256).Hash.ToLowerInvariant()
    $hashes[$rel] = $sha
    $titulo = 'R66 gap instalacao: ' + ($parts[1] -replace '\.sql$','')
    $hdr = $hdrRef -replace [regex]::Escape('127'), ('{0}' -f $n) -replace [regex]::Escape('R62 AI Provedor Drael'), $titulo
    $txt = (Get-Content $p -Encoding UTF8) -join "`r`n"
    $blocks.Add("$sep`r`n$hdr`r`n$sep`r`n`r`n-- SOURCE: $rel`r`n-- SOURCE-SHA256: $sha`r`n$txt`r`n") | Out-Null
    $n++
}

foreach ($alvo in $alvos) {
    $path = Join-Path $root $alvo
    $conteudo = [IO.File]::ReadAllText($path, $enc)
    if (-not $conteudo.EndsWith("`n")) { $blocks.Insert(0, "`r`n") }
    [IO.File]::AppendAllText($path, ($blocks -join ''), $enc)
    Write-Output ('APENDIDO=' + $alvo + ' BLOCOS=' + $blocks.Count)
}

$chkPath = Join-Path $root 'database/source-checksums.json'
$rawBytes = [IO.File]::ReadAllBytes($chkPath)
$temBom = ($rawBytes.Length -ge 3 -and $rawBytes[0] -eq 0xEF -and $rawBytes[1] -eq 0xBB -and $rawBytes[2] -eq 0xBF)
$jsonRaw = [IO.File]::ReadAllText($chkPath, [System.Text.Encoding]::UTF8)
$usaCrlf = $jsonRaw.Contains("`r`n")
$dict = @{}
$doc = $jsonRaw | ConvertFrom-Json
foreach ($prop in $doc.PSObject.Properties) { $dict[$prop.Name] = $prop.Value }
$adicionados = 0
foreach ($k in $hashes.Keys) { if (-not $dict.ContainsKey($k)) { $dict[$k] = $hashes[$k]; $adicionados++ } }
$ordenadas = $dict.Keys | Sort-Object
$obj = [ordered]@{}
foreach ($k in $ordenadas) { $obj[$k] = $dict[$k] }
$jsonNovo = ([PSCustomObject]$obj | ConvertTo-Json -Depth 3) -replace "`r`n", "`n"
if ($usaCrlf) { $jsonNovo = $jsonNovo -replace "`n", "`r`n" }
$bom = New-Object System.Text.UTF8Encoding($temBom)
[IO.File]::WriteAllText($chkPath, ($jsonNovo.TrimEnd("`n") + [Environment]::NewLine), $bom)
Write-Output ('CHECKSUMS_ATUALIZADOS entradas_novas=' + $adicionados + ' total=' + $ordenadas.Count)
Write-Output 'R66H_OK'
