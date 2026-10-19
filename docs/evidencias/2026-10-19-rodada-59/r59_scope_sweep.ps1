# R59 - varredura deterministica de th scope (D-R59-1)
# Regra: todo <th sem atributo scope= ganha scope="col" ANTES do fechamento da tag,
# preservando todos os demais atributos. O padrao <th\b nao casa com <thead>/<tfoot>
# ('e'/'o' sao caracteres de palavra: sem fronteira). Idempotente: pula tags c/ scope=.
# A linha de totais do Fechamentos.cshtml foi corrigida a mao ANTES desta varredura
# (label scope="row"; celulas de valor vira <td>).
# Uso: powershell -ExecutionPolicy Bypass -File r59_scope_sweep.ps1
$ErrorActionPreference='Stop'
$dir=Split-Path -Parent $MyInvocation.MyCommand.Path          # .../2026-10-19-rodada-59
$dir=Split-Path -Parent $dir                                   # .../2026-10-19
$dir=Split-Path -Parent $dir                                   # .../evidencias
$dir=Split-Path -Parent $dir                                   # raiz do repo
$views=Join-Path $dir 'backend\PlantaoPro.Web\Views'
$alterados=@()
$insercoes=0
Get-ChildItem $views -Recurse -Filter *.cshtml | ForEach-Object {
  $p=$_.FullName
  $t=[IO.File]::ReadAllText($p)
  $novo=[regex]::Replace($t,'<th\b[^>]*>',{ param($m)
    $tag=$m.Value
    if($tag -match 'scope='){ return $tag }
    $script:insercoes++
    return ($tag -replace '>\s*$',' scope="col">')
  })
  if($novo -ne $t){
    [IO.File]::WriteAllText($p,$novo,(New-Object System.Text.UTF8Encoding $false))
    $alterados += $_.FullName.Replace($dir+'\','')
  }
}
"INSERCOES=$insercoes"
"ARQUIVOS_ALTERADOS=" + $alterados.Count
# Re-chave: todo <th (fronteira de palavra; ignora thead/tfoot) deve ter scope=.
$restantes=@()
Get-ChildItem $views -Recurse -Filter *.cshtml | ForEach-Object {
  $t=[IO.File]::ReadAllText($_.FullName)
  foreach($m in [regex]::Matches($t,'<th\b[^>]*>')){
    if($m.Value -notmatch 'scope='){ $restantes += ($_.Name + ' | ' + $m.Value.Substring(0,[Math]::Min(70,$m.Value.Length))) }
  }
}
"RESTANTES_SEM_SCOPE=" + $restantes.Count
$restantes | Select-Object -First 5
