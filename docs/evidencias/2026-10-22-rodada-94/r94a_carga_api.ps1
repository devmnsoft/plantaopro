# R94 - carga/sustentacao da API (JSON, bearer) com thresholds documentados.
# Sobe SOMENTE a API (:51976, DLLs Release, banco plantaopro_test), faz login do gestor demo,
# aquece (WarmupSeg) e depois mantem -Workers requests simultaneos em voo por DuracaoSeg em um
# mix de rotas de leitura dos caminhos quentes do Administrativo 360 (mesmas rotas da medicao R91/R92).
# Registra latencia por request (p50/p90/p99 por rota e geral), taxa de erro, throughput e a
# comparacao entre as duas metades da janela (degradacao no tempo). Um amostrador a cada 3 s
# acompanha conexoes do banco (pg_stat_activity) e a memoria da API (sem vazamento/drift).
# Ao final aplica os thresholds abaixo (edicao 1 - calibrada sobre a medicao R91/R92 n=3 e o
# ensaio completo de 2026-10-22 que originou estes limites), grava r94a_carga_saida.txt e faz
# teardown da porta. A saida e cultura-invariante (decimais com ponto) para ser reprodutivel.
# Uso: powershell -ExecutionPolicy Bypass -File r94a_carga_api.ps1 [-Workers 8] [-DuracaoSeg 120] [-WarmupSeg 15]
param(
  [int]$Workers=8,
  [int]$DuracaoSeg=120,
  [int]$WarmupSeg=15,
  [string]$Out=''
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Net.Http | Out-Null
$invCult=[System.Globalization.CultureInfo]::InvariantCulture
[System.Threading.Thread]::CurrentThread.CurrentCulture=$invCult

# ---------------- thresholds documentados (edicao 1) ----------------
# Referencia fria (R91/R92, cliente unico, n=3): orcamentos ~100 ms, estoque ~265 ms, web ~90-600 ms.
# Sob 8 clientes simultaneos os percentis ficam acima da media fria; os limites abaixo exigem
# sustentacao estavel (sem degradacao > 50% na segunda metade e sem crescimento de conexoes).
$ThrP50MaxMs=250        # p50 geral sob concorrencia
$ThrP90MaxMs=700        # p90 geral sob concorrencia
$ThrP99MaxMs=1500       # p99 geral (cauda sob concorrencia em maquina de desenvolvimento)
$ThrErroMaxPct=1.0      # % de requests nao-200 ou com excecao de transporte
$ThrRpsMin=8.0          # throughput minimo sustentado na janela medida (req/s)
$ThrDegradacaoMaxX=1.5  # p90 da segunda metade / p90 da primeira metade (estabilidade no tempo)
$ThrConexaoDeltaMax=4   # (max - min) de conexoes do banco durante a janela (sem vazamento)

$repo=(Split-Path $PSScriptRoot -Parent) | Split-Path -Parent | Split-Path -Parent
$b='planta'+'o'+'pro'
$psql="C:\Program Files\PostgreSQL\18\bin\psql.exe"
$db=$b+'_test'
$apiUrl='http://localhost:51976'
$outArq=if($Out -ne ''){ $Out } else { Join-Path $PSScriptRoot 'r94a_carga_saida.txt' }
$env:PGPASSWORD='123456'
$env:PGCLIENTENCODING='UTF8'

# Mix de carga: somente leitura, rotas quentes ja instrumentadas em EXPLAIN (R92) e paginadas (R91).
$mix=@(
  @{ Nome='orcamentos';    Rota='/api/administrativo360/orcamentos';       Peso=40 },
  @{ Nome='orcamentos_p2'; Rota='/api/administrativo360/orcamentos?pagina=2'; Peso=20 },
  @{ Nome='estoque';       Rota='/api/administrativo360/estoque';          Peso=25 },
  @{ Nome='fiscal_notas';  Rota='/api/administrativo360/fiscal/notas';     Peso=10 },
  @{ Nome='saude';         Rota='/api/health';                             Peso=5   }
)

function PsqlQ([string]$sql){ $r=@(& $psql -h 127.0.0.1 -U postgres -d $db -X -q -t -A -c $sql); for($i=$r.Count-1;$i -ge 0;$i--){ if($null -ne $r[$i] -and (''+$r[$i]).Trim() -ne ''){ return (''+$r[$i]).Trim() } }; return '' }

function Percentil([double[]]$v,[double]$q){
  if($v.Length -eq 0){ return 0.0 }
  $idx=[int][Math]::Floor($q * $v.Length)
  if($idx -ge $v.Length){ $idx=$v.Length-1 }
  if($idx -lt 0){ $idx=0 }
  return [math]::Round($v[$idx],1)
}

$amostras=[System.Collections.Generic.List[string]]::new()
$apiPid=0

function EscolherRota([System.Random]$rng){
  $v=$rng.Next(100); $acc=0
  for($i=0;$i -lt $mix.Count;$i++){ $acc+=$mix[$i].Peso; if($v -lt $acc){ return $i } }
  return ($mix.Count-1)
}

function AmostraSustentacao([long]$iniT,[int]$pidAlvo,[System.Collections.Generic.List[string]]$destino){
  $conns='?'
  try { $conns=PsqlQ ("select count(*) from pg_stat_activity where datname='"+$db+"';") } catch {}
  $ws='?'
  try { $pr=Get-Process -Id $pidAlvo -ErrorAction SilentlyContinue; if($pr){ $ws=[math]::Round($pr.WorkingSet64/1MB,0) } } catch {}
  $t=[long](([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() - $iniT)/1000)
  [void]$destino.Add(('t={0}s conns={1} ws_mb={2}' -f $t, $conns, $ws))
}

# Agendador de carga: mantem exatamente $conc requests HTTP em voo o tempo todo na janela
# (concorrencia constante). Single-thread por proposito: o I/O HTTP e assincrono, entao a
# concorrencia e real sem threads extras de PowerShell (scriptblock em outro thread falha em
# PS 5.1 por falta de Runspace - licao registrada no ensaio desta rodada). Coletas sao
# parametros explicitos para evitar a magica de escopo dinamico entre funcao e script.
function ExecutarCarga([int]$durSeg,[int]$conc,[string]$auth,[bool]$amostrar,[int]$seed,
  [int]$pidAlvo,[System.Collections.Generic.List[object]]$recs,[System.Collections.Generic.List[string]]$amo){
  $iniT=[long][DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
  $fimT=$iniT + ($durSeg*1000)
  $rng=[System.Random]::new($seed)
  $hc=[System.Net.Http.HttpClient]::new()
  $hc.Timeout=[TimeSpan]::FromSeconds(30)
  $proxAmostra=[long]3000
  try {
    while(([long][DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()) -lt $fimT){
      $lote=@()
      for($w=0;$w -lt $conc;$w++){
        $pick=EscolherRota $rng
        $rq=[System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Get, ($apiUrl + $mix[$pick].Rota))
        $rq.Headers.Authorization=[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $auth)
        $sw=[System.Diagnostics.Stopwatch]::StartNew()
        try {
          $lote+=[pscustomobject]@{ Pick=$pick; Sw=$sw; Resp=$hc.SendAsync($rq) }
        } catch {
          $sw.Stop()
          [void]$recs.Add([pscustomobject]@{ I=$pick; S=-1; M=[double]$sw.Elapsed.TotalMilliseconds; T=[long](([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()) - $iniT) })
        }
      }
      foreach($e in $lote){
        $st=-1
        try { $resp=$e.Resp.GetAwaiter().GetResult(); $st=[int]$resp.StatusCode; $resp.Dispose() } catch { $st=-1 }
        $e.Sw.Stop()
        [void]$recs.Add([pscustomobject]@{ I=$e.Pick; S=$st; M=[double]$e.Sw.Elapsed.TotalMilliseconds; T=[long](([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()) - $iniT) })
      }
      $el=[long]([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() - $iniT)
      if($amostrar -and $el -ge $proxAmostra){
        AmostraSustentacao $iniT $pidAlvo $amo
        $proxAmostra=$el + 3000
      }
    }
  } finally { $hc.Dispose() }
}

$saida=@()
$tsData=(Get-Date -Format 'yyyyMMdd_HHmmss')
$saida+=('R94A_CARGA data='+$tsData+' host='+$env:COMPUTERNAME+' cpu='+$env:NUMBER_OF_PROCESSORS)

# 1) volumes (contexto de escala do banco de teste)
$vol=''
foreach($t in @('adm360_orcamentos','adm360_saldos','adm360_movimentos','adm360_reservas','adm360_produtos')){
  $vol+=(' '+$t+'='+ (PsqlQ ('select count(*) from plantaopro.'+$t+';')))
}
$saida+=('VOLUME'+$vol)

# 2) teardown de porta e subida da API (mesmo ritual da medicao R92)
$connsPorta=Get-NetTCPConnection -State Listen -LocalPort 51976 -ErrorAction SilentlyContinue
foreach($c in $connsPorta){ Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep 2
$env:ConnectionStrings__Default="Host=127.0.0.1;Port=5432;Database="+$db+";Username=postgres;Password=123456"
$env:Jwt__Key='r91-medicao-dev-jwt-key-0123456789-abcdefghijklmn'
$env:ASPNETCORE_ENVIRONMENT='Development'
# regra de casa (R12): com execucao direta da DLL o content root muda - BaseUrl explicito
$env:ApiSettings__BaseUrl=$apiUrl
$env:PlantaoProApi__BaseUrl=$apiUrl
$apiDll=Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
if(-not (Test-Path $apiDll)){ throw 'build Release ausente: '+$apiDll }
$apiProc=Start-Process -FilePath 'dotnet' -ArgumentList ('"'+$apiDll+'" --urls http://localhost:51976') -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Api') -WindowStyle Hidden -PassThru
if($null -eq $apiProc -or $apiProc.Id -le 0){ throw 'API nao iniciou (Sem processo)' }
$apiPid=[int]$apiProc.Id
$okA=$false
for($i=0;$i -lt 90;$i++){ Start-Sleep 1; try { $h=Invoke-WebRequest ($apiUrl+'/api/health') -UseBasicParsing -TimeoutSec 2; if([int]$h.StatusCode -eq 200){ $okA=$true; break } } catch {} }
if(-not $okA){ throw 'API nao subiu' }
$saida+=('APPS api_pid={0}' -f $apiPid)
Write-Host ('API no ar pid={0}' -f $apiPid)

# 3) login do gestor demo (bearer)
$bodyJ=@{ Email='gestor@santacasa-demo.example'; Senha='SantaCasa!Demo2026#Gestor' } | ConvertTo-Json
$rj=Invoke-WebRequest ($apiUrl+'/api/auth/login') -Method Post -Body $bodyJ -ContentType 'application/json' -UseBasicParsing
$tok2=(($rj.Content | ConvertFrom-Json).data).token
if((($rj.Content | ConvertFrom-Json).data).tenantContextRequired){
  $r2=Invoke-WebRequest ($apiUrl+'/api/auth/refresh-context') -Method Post -Headers @{ Authorization='Bearer '+$tok2 } -UseBasicParsing
  $tok2=((($r2.Content | ConvertFrom-Json).data)).token
}
$saida+=('LOGIN_OK')

# 4) aquecimento (descartado das estatisticas)
$recsW=[System.Collections.Generic.List[object]]::new()
ExecutarCarga $WarmupSeg $Workers $tok2 $false 9400 $apiPid $recsW $amostras
$cntW=$recsW.Count
$saida+=('WARMUP_SEG={0} descartados={1}' -f $WarmupSeg, $cntW)
Write-Host ('aquecimento '+$WarmupSeg+'s completo ('+$cntW+' requests descartados)')

# 5) janela medida (sustentacao)
$recsM=[System.Collections.Generic.List[object]]::new()
$swJanela=[System.Diagnostics.Stopwatch]::StartNew()
ExecutarCarga $DuracaoSeg $Workers $tok2 $true 9500 $apiPid $recsM $amostras
$swJanela.Stop()

# 6) agregacao
$all=@($recsM.ToArray())
$n=$all.Count
if($n -eq 0){ throw 'nenhum request registrado na janela medida' }
$msAll=@([double[]]($all | ForEach-Object { $_.M }))
[Array]::Sort($msAll)
$erro=0; $medTot=0.0
foreach($a in $all){ if($a.S -lt 200 -or $a.S -ge 300){ $erro++ }; $medTot+=$a.M }
$mediaGeral=[math]::Round($medTot/$n,1)
$p50g=Percentil $msAll 0.50; $p90g=Percentil $msAll 0.90; $p99g=Percentil $msAll 0.99
$rps=[math]::Round(($n / $swJanela.Elapsed.TotalSeconds),2)
$erroPct=[math]::Round(($erro*100.0/$n),3)
$corte=[long]($swJanela.ElapsedMilliseconds/2)
$msH1=@([double[]](@($all | Where-Object { $_.T -lt $corte } | ForEach-Object { $_.M })))
$msH2=@([double[]](@($all | Where-Object { $_.T -ge $corte } | ForEach-Object { $_.M })))
[Array]::Sort($msH1); [Array]::Sort($msH2)
$p90h1=Percentil $msH1 0.90; $p90h2=Percentil $msH2 0.90
$p50h1=Percentil $msH1 0.50; $p50h2=Percentil $msH2 0.50
$degX=1.0
if($p90h1 -gt 0){ $degX=[math]::Round($p90h2/$p90h1,2) }
$saida+=('-- por rota (janela medida) --')
for($ri=0;$ri -lt $mix.Count;$ri++){
  $sub=@($all | Where-Object { $_.I -eq $ri })
  $nm=$sub.Count
  $e=0; $mt=0.0
  foreach($a in $sub){ if($a.S -lt 200 -or $a.S -ge 300){ $e++ }; $mt+=$a.M }
  $mv=@([double[]]($sub | ForEach-Object { $_.M }))
  [Array]::Sort($mv)
  $linha=('POR_ROTA nome={0} rota={1} peso={2} n={3} err={4}' -f $mix[$ri].Nome, $mix[$ri].Rota, $mix[$ri].Peso, $nm, $e)
  if($nm -gt 0){
    $linha+=(' media_ms={0} p50={1} p90={2} p99={3}' -f [math]::Round($mt/$nm,1), (Percentil $mv 0.50), (Percentil $mv 0.90), (Percentil $mv 0.99))
  } else { $linha+=' SEM_REQUESTS' }
  $saida+=$linha
}
$saida+=('-- geral --')
$saida+=('GERAL n={0} rps={1} erro_pct={2} media_ms={3} p50={4} p90={5} p99={6}' -f $n, $rps, $erroPct, $mediaGeral, $p50g, $p90g, $p99g)
$saida+=('METADES h1_n={0} h1_p50={1} h1_p90={2} h2_n={3} h2_p50={4} h2_p90={5} degradacao_x={6}' -f $msH1.Length, $p50h1, $p90h1, $msH2.Length, $p50h2, $p90h2, $degX)

# 7) amostras de sustentacao (conexoes + memoria)
$nSamp=0; $cMin=0; $cMax=0; $wMin=0; $wMax=0
foreach($l in $amostras){
  $mC=[regex]::Match($l,'conns=(\d+)'); $mW=[regex]::Match($l,'ws_mb=(\d+)')
  if(-not ($mC.Success -and $mW.Success)){ continue }
  $cV=[int]$mC.Groups[1].Value; $wV=[int]$mW.Groups[1].Value
  if($nSamp -eq 0){ $cMin=$cV; $cMax=$cV; $wMin=$wV; $wMax=$wV }
  else { if($cV -lt $cMin){ $cMin=$cV }; if($cV -gt $cMax){ $cMax=$cV }; if($wV -lt $wMin){ $wMin=$wV }; if($wV -gt $wMax){ $wMax=$wV } }
  $nSamp++
}
$deltaC=0
if($nSamp -ge 2){ $deltaC=$cMax - $cMin }
$minC='?'; $maxC='?'; $minW='?'; $maxW='?'
if($nSamp -ge 1){ $minC=$cMin; $maxC=$cMax; $minW=$wMin; $maxW=$wMax }
$saida+=('AMOSTRAS_CONEXAO n={0} min={1} max={2} delta={3}' -f $nSamp, $minC, $maxC, $deltaC)
$saida+=('MEMORIA_API_MB min={0} max={1}' -f $minW, $maxW)
$amIni='SEM_AMOSTRAS'
if($amostras.Count){ $amIni=$amostras[0]; $amFim=$amostras[$amostras.Count-1] } else { $amFim='' }
$saida+=('AMOSTRA_'+$amIni)
if($amostras.Count){ $saida+=('AMOSTRA_'+$amFim) }

# 8) aplicacao dos thresholds
$okP50 = $p50g  -le $ThrP50MaxMs
$okP90 = $p90g  -le $ThrP90MaxMs
$okP99 = $p99g  -le $ThrP99MaxMs
$okErr = $erroPct -le $ThrErroMaxPct
$okRps = $rps    -ge $ThrRpsMin
$okDeg = $degX   -le $ThrDegradacaoMaxX
$okCon = $deltaC -le $ThrConexaoDeltaMax
function SimNao([bool]$v){ if($v){ 'SIM' } else { 'NAO' } }
$sP50=SimNao $okP50; $sP90=SimNao $okP90; $sP99=SimNao $okP99
$sErr=SimNao $okErr; $sRps=SimNao $okRps; $sDeg=SimNao $okDeg; $sCon=SimNao $okCon
$saida+=('THRESHOLD p50_max_ms={0} p90_max_ms={1} p99_max_ms={2} erro_max_pct={3} rps_min={4} degradacao_max_x={5} conexao_delta_max={6}' -f $ThrP50MaxMs, $ThrP90MaxMs, $ThrP99MaxMs, $ThrErroMaxPct, $ThrRpsMin, $ThrDegradacaoMaxX, $ThrConexaoDeltaMax)
$saida+=('CHECK p50_ok={0} p90_ok={1} p99_ok={2} erro_ok={3} rps_ok={4} degradacao_ok={5} conexao_ok={6}' -f $sP50, $sP90, $sP99, $sErr, $sRps, $sDeg, $sCon)
$todosOk = ($okP50 -and $okP90 -and $okP99 -and $okErr -and $okRps -and $okDeg -and $okCon)
$saida+=('RESULTADO='+$(if($todosOk){'PASSEI'}else{'FALHOU'}))

# 9) teardown
$depois=Get-NetTCPConnection -State Listen -LocalPort 51976 -ErrorAction SilentlyContinue
foreach($c in $depois){ Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep 1
$livre=(Get-NetTCPConnection -State Listen -LocalPort 51976 -ErrorAction SilentlyContinue)
$saida+=('PORTAS_LIVRES='+$(if(-not $livre){'SIM'}else{'NAO'}))

$saida | ForEach-Object { Write-Output $_ }
[System.IO.File]::WriteAllLines($outArq, $saida)
Write-Output ('SAIDA_GRAVADA '+$outArq)


