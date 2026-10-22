# R91 - medicao de listas densas na base atual (fechamento da pendencia 1 do §7 r53b1)
# Sobe API :51976 + Web :51977 (DLLs Release, banco plantaopro_test) e mede, por rota,
# peso (bytes) e latencia (ms) com n=3 apos warm-up. Deixa os apps no ar ao final
# (para a smoke R52 e o pager e2e R54; teardown por porta depois).
# Uso: powershell -ExecutionPolicy Bypass -File r91a_medicao_listas.ps1
$ErrorActionPreference='Stop'
$repo='C:\MNSOFT\plantaopro'
$b='planta'+'o'+'pro'
$psql="C:\Program Files\PostgreSQL\18\bin\psql.exe"
$outArq=Join-Path $PSScriptRoot 'r91a_medicao_saida.txt'
$env:PGPASSWORD='123456'
$env:PGCLIENTENCODING='UTF8'

function MedirRotas([string]$base,[object]$sessao,[string[]]$rotas,[int]$n){
  $res=@()
  foreach($r in $rotas){
    $bytes=0; $totMs=0; $ok=0
    for($i=0;$i -lt $n;$i++){
      $sw=[System.Diagnostics.Stopwatch]::StartNew()
      try {
        if($null -eq $sessao){ $q=Invoke-WebRequest -Uri ($base+$r) -UseBasicParsing -TimeoutSec 120 -MaximumRedirection 10 }
        else { $q=Invoke-WebRequest -Uri ($base+$r) -WebSession $sessao -UseBasicParsing -TimeoutSec 120 -MaximumRedirection 10 }
        if([int]$q.StatusCode -eq 200){ $bytes=[int]$q.Content.Length; $totMs+=$sw.ElapsedMilliseconds; $ok++ }
        else { $res+=('FALHA '+$r+' status='+[int]$q.StatusCode); return $res }
      } catch { $res+=('ERRO '+$r+' '+$_.Exception.Message); return $res }
    }
    $res+=('{0} bytes={1} media_ms={2}' -f $r, $bytes, [math]::Round($totMs/$n,1))
  }
  return $res
}

function PsqlQ([string]$sql){ $r=@(& $psql -h 127.0.0.1 -U postgres -d ('planta'+'o'+'pro'+'_test') -t -c $sql); for($i=$r.Count-1;$i -ge 0;$i--){ if($null -ne $r[$i] -and $r[$i].Trim() -ne ''){ return $r[$i].Trim() } }; return '' }
$out=@()
# 1) volumes das tabelas densas (contexto de escala)
$vol=@()
$vol+='-- volumes (banco de teste)'
foreach($t in @('adm360_orcamentos','adm360_saldos','adm360_movimentos','adm360_lotes','adm360_reservas','adm360_produtos')){
  $cnt=(PsqlQ ('set search_path=plantaopro; select count(*) from '+$t+';')).Trim()
  $vol+=('  linhas '+$t+'='+$cnt)
}
$tablesNova=(& $psql -h 127.0.0.1 -U postgres -d ('planta'+'o'+'pro'+'_test') -t -c "select tablename from pg_tables where schemaname='plantaopro' and (tablename like '%colaborador%' or tablename like '%internac%' or tablename like 'cotacoes%' or tablename like 'paises%' or tablename like 'notas_int%') order by 1;")
foreach($tt in $tablesNova){
  $tno=$tt.Trim()
  if($tno -eq ''){ continue }
  $cnt=(PsqlQ ('select count(*) from "plantaopro."'+$tno+';')).Trim()
  $vol+=('  linhas '+$tno+'='+$cnt)
}
$out+=$vol

# 2) teardown de porta e subida dos apps
$conns=Get-NetTCPConnection -State Listen -LocalPort 51976,51977 -ErrorAction SilentlyContinue
foreach($c in $conns){ Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep 2
$env:ConnectionStrings__Default="Host=127.0.0.1;Port=5432;Database="+$b+"_test;Username=postgres;Password=123456"
$env:Jwt__Key='r91-medicao-dev-jwt-key-0123456789-abcdefghijklmn'
$env:ASPNETCORE_ENVIRONMENT='Development'
# regra de casa (R12): com execucao direta da DLL o content root muda - BaseUrl explicito
$env:ApiSettings__BaseUrl='http://localhost:51976'
$env:PlantaoProApi__BaseUrl='http://localhost:51976'
$apiDll=Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$webDll=Join-Path $repo 'backend/PlantaoPro.Web/bin/Release/net10.0/PlantaoPro.Web.dll'
if(-not (Test-Path $apiDll)){ throw 'build Release ausente: '+$apiDll }
$apiDir=Join-Path $repo 'backend/PlantaoPro.Api'
$webDir=Join-Path $repo 'backend/PlantaoPro.Web'
$apiProc=Start-Process -FilePath 'dotnet' -ArgumentList ('"'+$apiDll+'" --urls http://localhost:51976') -WorkingDirectory $apiDir -WindowStyle Hidden -PassThru
$webProc=Start-Process -FilePath 'dotnet' -ArgumentList ('"'+$webDll+'" --urls http://localhost:51977') -WorkingDirectory $webDir -WindowStyle Hidden -PassThru
$okA=$false
for($i=0;$i -lt 90;$i++){ Start-Sleep 1; try { $h=Invoke-WebRequest 'http://localhost:51976/api/health' -UseBasicParsing -TimeoutSec 2; if([int]$h.StatusCode -eq 200){ $okA=$true; break } } catch {} }
if(-not $okA){ throw 'API nao subiu' }
$okW=$false
for($i=0;$i -lt 90;$i++){ Start-Sleep 1; try { Invoke-WebRequest 'http://localhost:51977/Account/Login' -UseBasicParsing -TimeoutSec 2 | Out-Null; $okW=$true; break } catch { if($_.Exception.Response){ $okW=$true; break } } }
if(-not $okW){ throw 'Web nao subiu' }
$out+=('APPS_NO_AR api_pid='+$apiProc.Id+' web_pid='+$webProc.Id)

# 3) login gestor demo (sessao cookie da Web)
$s=New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pg0=Invoke-WebRequest -Uri 'http://localhost:51977/Account/Login' -WebSession $s -UseBasicParsing
$html=$pg0.Content
$tokPos=$html.IndexOf('name="__RequestVerificationToken"')
$valPos=$html.IndexOf('value="',$tokPos)
$tokStart=$valPos+'value="'.Length
$tokEnd=$html.IndexOf('"',$tokStart)
$tok=$html.Substring($tokStart,$tokEnd-$tokStart)
$body='Email='+[System.Uri]::EscapeDataString('gestor@santacasa-demo.example')+'&Senha='+[System.Uri]::EscapeDataString('SantaCasa!Demo2026#Gestor')+'&__RequestVerificationToken='+[System.Uri]::EscapeDataString($tok)
$post=Invoke-WebRequest -Uri 'http://localhost:51977/Account/Login' -Method Post -WebSession $s -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -MaximumRedirection 10
if([int]$post.StatusCode -ne 200){ throw ('login falhou status='+[int]$post.StatusCode) }
$out+=('LOGIN_OK')
for($w=0;$w -lt 8;$w++){ try { $wq=Invoke-WebRequest -Uri 'http://localhost:51977/Ajuda/Index' -WebSession $s -UseBasicParsing -TimeoutSec 30; if([int]$wq.StatusCode -eq 200){ break } } catch { Start-Sleep -Seconds 2 } }
$out+=('WARMUP_OK')

# 4) rotas Web (HTML renderizado) - as duas canonicas + demais listas ADM360 + superficies B4
$rotasWeb=@(
  '/Administrativo360/Orcamentos','/Administrativo360/Orcamentos?pagina=2',
  '/Administrativo360/Estoque','/Administrativo360/Estoque?pagina=2',
  '/Administrativo360/Vendas','/Administrativo360/PedidosCompra',
  '/Administrativo360/TitulosReceber','/Administrativo360/RelatoriosFinanceiros',
  '/FolhaWeb/Index','/SimplesWeb/Index','/CalendarioFiscalWeb/Index',
  '/DfeNacionalWeb/Index','/TrilhosPagamentoWeb/Index','/EsocialTrabalhoWeb/Index',
  '/VendasInternacionaisWeb/Index','/LegislacaoWeb/Index'
)
$out+=('-- paginas web (HTML) n=3')
$out+=(MedirRotas 'http://localhost:51977' $s $rotasWeb 3)

# 4b) rotas raiz (1x, sem abortar o laço): a desambiguacao da R91 exige que "/" resolva um unico
# endpoint (antes: 9 controllers disputavam "" => 500 AmbiguousMatch) e "/index" um unico tambem.
$out+=('-- rotas raiz (1x)')
foreach($rr in @('/','/index')){
  try {
    $rq=Invoke-WebRequest -Uri ('http://localhost:51977'+$rr) -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 0
    $out+=($rr+' status='+[int]$rq.StatusCode+' bytes='+$rq.Content.Length)
  } catch {
    if($_.Exception.Response){ $out+=($rr+' status='+[int]$_.Exception.Response.StatusCode.Value__) }
    else { $out+=($rr+' ERRO '+$_.Exception.Message) }
  }
}

# 5) endpoints API canonicos (JSON) via bearer
$bodyJ=@{ Email='gestor@santacasa-demo.example'; Senha='SantaCasa!Demo2026#Gestor' } | ConvertTo-Json
$rj=Invoke-WebRequest 'http://localhost:51976/api/auth/login' -Method Post -Body $bodyJ -ContentType 'application/json' -UseBasicParsing
$tok2=($rj.Content | ConvertFrom-Json).data.token
if((($rj.Content | ConvertFrom-Json).data).tenantContextRequired){
  $r2=Invoke-WebRequest 'http://localhost:51976/api/auth/refresh-context' -Method Post -Headers @{ Authorization='Bearer '+$tok2 } -UseBasicParsing
  $tok2= (($r2.Content | ConvertFrom-Json).data).token
}
$hdr=@{ Authorization='Bearer '+$tok2 }
$rotasApi=@(
  '/api/administrativo360/orcamentos','/api/administrativo360/orcamentos?pagina=2',
  '/api/administrativo360/estoque','/api/administrativo360/estoque?pagina=2'
)
$out+=('-- endpoints api (JSON) n=3')
$resApi=@()
foreach($ra in $rotasApi){
  $bytes=0; $totMs=0
  for($i=0;$i -lt 3;$i++){
    $sw=[System.Diagnostics.Stopwatch]::StartNew()
    $qa=Invoke-WebRequest -Uri ('http://localhost:51976'+$ra) -Headers $hdr -UseBasicParsing -TimeoutSec 120
    if([int]$qa.StatusCode -ne 200){ $resApi+=('FALHA '+$ra+' status='+[int]$qa.StatusCode); break }
    $bytes=[int]$qa.Content.Length; $totMs+=$sw.ElapsedMilliseconds
  }
  $resApi+=('{0} bytes={1} media_ms={2}' -f $ra, $bytes, [math]::Round($totMs/3,1))
}
$out+=$resApi

$out+=('APPS_AINDA_NO_AR (teardown por porta ao fim da rodada)')
$out | ForEach-Object { Write-Output $_ }
Set-Content -Path $outArq -Value ($out -join "`n") -Encoding UTF8
Write-Output ('SAIDA_GRAVADA '+$outArq)
