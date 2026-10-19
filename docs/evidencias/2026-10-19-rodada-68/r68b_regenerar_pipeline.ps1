# R68 (bloco B1): regenera o pipeline canonico apos o r68c (patch de normalizacao de
# permissoes aplicado aos backups de base). Sequencia documentada da R66:
#   restaurar _backup_* -> r66a -> r66b_fix_v1410 -> r66b_fix_wrappers -> r66c -> r66h
# Saida esperada: 208 secoes SOURCE nos dois scripts, manifests re-travados, e o
# arquivo v2360 de convergencia regenerado byte identico (verificar via git diff).
$ErrorActionPreference = 'Stop'
$root = 'C:\MNSOFT\plantaopro'
$b66 = Join-Path $root 'docs/evidencias/2026-10-19-rodada-66'

# guarda pre-restauracao: os backups de base precisam ja conter o patch R68
$chkB = ([regex]::Matches([IO.File]::ReadAllText((Join-Path $b66 '_backup_scrpt_completo.sql'), [Text.UTF8Encoding]::new($false)), [regex]::Escape('-- R68: o separador canonico'))).Count
$chkP = ([regex]::Matches([IO.File]::ReadAllText((Join-Path $b66 '_backup_instalar_no_banco_atual.sql'), [Text.UTF8Encoding]::new($false)), [regex]::Escape('-- R68: o separador canonico'))).Count
if ($chkB -ne 2 -or $chkP -ne 2) { throw ('backups sem patch R68 (scrpt=' + $chkB + ' pgadmin=' + $chkP + ')') }
Write-Host ('GUARDA_BACKUPS_OK scrpt=' + $chkB + ' pgadmin=' + $chkP)

function Run-Stage([string]$label, [string]$script) {
  Write-Host ('--- ' + $label + ' ---')
  & powershell -NoProfile -ExecutionPolicy Bypass -File $script
  if ($LASTEXITCODE -ne 0) { throw ($label + ' EXIT=' + $LASTEXITCODE) }
}

# ---- restaura a base do backup do pipeline R66 (com o patch R68 incorporado) ----
Copy-Item (Join-Path $b66 '_backup_scrpt_completo.sql') (Join-Path $root 'database/scrpt_completo.sql') -Force
Copy-Item (Join-Path $b66 '_backup_instalar_no_banco_atual.sql') (Join-Path $root 'database/pgadmin/instalar_no_banco_atual.sql') -Force
Copy-Item (Join-Path $b66 '_backup_source-checksums.json') (Join-Path $root 'database/source-checksums.json') -Force
Write-Host 'RESTORE_OK'

Run-Stage 'r66a_gap' (Join-Path $b66 'r66a_append_gap_instalacao.ps1')
Run-Stage 'r66b_v1410' (Join-Path $b66 'r66b_fix_v1410.ps1')
Run-Stage 'r66b_wrappers' (Join-Path $b66 'r66b_fix_wrappers.ps1')
Run-Stage 'r66c_convergencia' (Join-Path $b66 'r66c_gen_convergencia.ps1')
Run-Stage 'r66h_backend' (Join-Path $b66 'r66h_append_backend_sql.ps1')
Write-Host 'PIPELINE_OK'
