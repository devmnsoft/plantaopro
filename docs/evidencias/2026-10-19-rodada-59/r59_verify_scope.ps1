$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$root=Split-Path -Parent $root
$root=Split-Path -Parent $root
$root=Split-Path -Parent $root
$views=Get-ChildItem (Join-Path $root 'backend\PlantaoPro.Web\Views') -Recurse -Filter *.cshtml
$realTh=0; $realScoped=0; $theadAny=0; $sem=@()
foreach($f in $views){
  $t=[IO.File]::ReadAllText($f.FullName)
  foreach($m in [regex]::Matches($t,'<th(?=[\s>])([^>]*)>')){
    $realTh++
    if($m.Value -notmatch 'scope='){ $realScoped--; $sem += ($f.Name + ' | ' + $m.Value.Substring(0,[Math]::Min(60,$m.Value.Length))) } else { $realScoped++ }
  }
  $theadAny += ([regex]::Matches($t,'<t(head|foot)[^>]*scope=')).Count
}
"REAL_TH=$realTh REAL_SCOPED=$realScoped THEAD_TFOOT_COM_SCOPE=$theadAny SEM_SCOPE=" + $sem.Count
$sem | Select-Object -First 5
