# R95 (B5 confiabilidade) - drill de backup/restore do banco de teste.
# Duas rotas de recuperacao sao provadas:
#   PA - backup de DADOS: pg_dump -Fc do banco vivo -> DROP -> recriar vazio -> pg_restore
#        -> paridade completa (conjunto de tabelas, contagem de TODAS as linhas, indices,
#        foreign keys, sequencias, extensoes e digest de conteudo de um conjunto estavel)
#        -> API funcional contra o banco RESTAURADO (login do gestor + rota quente).
#   PB - reconstrucao pelo SCRIPT UNICO (artefato B1): o instalador scripts/database/install-plantaopro.ps1
#        executa database/instalar_plantaopro.psql (-> scrpt_completo.sql + seeds de sistema
#        + bootstrap do superadministrador + grants + auto-verificacao) em um banco limpo,
#        o runner oficial PlantaoPro.Tools.Database aplica as migracoes pendentes (auto-cura)
#        e a API sobe e faz login com o superadministrador de bootstrap.
# O banco vivo NAO e substituido por outro: e derrubado e RESTAURADO a partir do proprio dump,
# entao as rodadas seguintes seguem sobre o mesmo estado. Se a rota PA falhar depois do DROP,
# o dump (preservado ate o fim) permite restaurar de novo; a rota PB recria o minimo operacional.
# Uso: powershell -ExecutionPolicy Bypass -File r95a_drill_backup_restore.ps1 [-SkipPa] [-SkipPb] [-KeepScratch]
param(
  [switch]$SkipPa,
  [switch]$SkipPb,
  [switch]$KeepScratch
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Net.Http | Out-Null
$invCult=[System.Globalization.CultureInfo]::InvariantCulture
[System.Threading.Thread]::CurrentThread.CurrentCulture=$invCult
# captura de saida nativa (psql/pg_restore/dotnet) em UTF-8: nomes com acentos nao viram mojibake
try { [Console]::OutputEncoding=[System.Text.Encoding]::UTF8 } catch {}
try { $OutputEncoding=[System.Text.Encoding]::UTF8 } catch {}

$env:PGPASSWORD='123456'
$env:PGUSER='postgres'
$env:PGCLIENTENCODING='UTF8'
# NOTICEs e AVISOs benignos do psql/instalador (ex.: "extensao ja existe", "mantendo os
# atributos atuais") vao para stderr; o instalador usa $PSNativeCommandUseErrorActionPreference
# com EAP=Stop, entao qualquer linha em stderr viraria erro fatal no meio da instalacao.
# client_min_messages=error preserva ERROS reais (que seguem fatais) e silencia o resto.
$env:PGOPTIONS='-c client_min_messages=error'

$repo=(Split-Path $PSScriptRoot -Parent) | Split-Path -Parent | Split-Path -Parent
$pgdir='C:\Program Files\PostgreSQL\18\bin'
$psql=Join-Path $pgdir 'psql.exe'
$pgdump=Join-Path $pgdir 'pg_dump.exe'
$pgrestore=Join-Path $pgdir 'pg_restore.exe'
$dbSrc='planta'+'o'+'pro_test'
$dbScratch='planta'+'o'+'pro_r95_script'
$porta='51977'
$apiUrl='http://localhost:'+ $porta
$dumpDir=Join-Path $env:TEMP 'opencode'
$dumpArq=Join-Path $dumpDir ('r95_backup_'+$dbSrc+'.dump')
$apiDll=Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$toolDbDll=Join-Path $repo 'backend/PlantaoPro.Tools.Database/bin/Release/net10.0/PlantaoPro.Tools.Database.dll'
$wrapper=Join-Path $repo 'scripts/database/install-plantaopro.ps1'
$envLocal=Join-Path $repo '.local/plantaopro.env'
$envBackup=Join-Path $dumpDir 'r95_plantaopro.env.bak'
$gestorEmail='gestor@santacasa-demo.example'
$gestorSenha='SantaCasa!Demo2026#Gestor'
$admEmail='admin.global@plantaopro.local'
$admPw='R95-drill-superadmin-2026'+[char]35+'Kp9z'
$appPw='R95-drill-aplicacao-2026'+[char]35+'Qm4x'
$tabsDigest=@('usuarios','saas_tenants','modulos_sistema','perfis','permissoes','perfil_permissoes','adm360_orcamentos','adm360_saldos','adm360_movimentos','adm360_produtos')

$rel=[System.Collections.Generic.List[string]]::new()
function AddRel([System.Collections.Generic.List[string]]$l,[string]$texto){ [void]$l.Add($texto) }

function PsqlQ([string]$db,[string]$sql){
  $prevE=$ErrorActionPreference; $ErrorActionPreference='Continue'
  $r=& $psql -h 127.0.0.1 -U postgres -d $db -X -q -t -A -c $sql 2>&1
  $ec=$LASTEXITCODE
  $ErrorActionPreference=$prevE
  $txt=(''+$r).Trim()
  if($ec -ne 0){ throw ('psql falhou (exit '+$ec+'): '+$txt) }
  return $txt
}
function PsqlQMult([string]$db,[string]$sql){
  $prevE=$ErrorActionPreference; $ErrorActionPreference='Continue'
  $r=@(& $psql -h 127.0.0.1 -U postgres -d $db -X -q -t -A -c $sql 2>&1)
  $ec=$LASTEXITCODE
  $ErrorActionPreference=$prevE
  if($ec -ne 0){ throw ('psql multi falhou (exit '+$ec+'): '+(($r | Out-String).Trim())) }
  $v=[System.Collections.Generic.List[string]]::new()
  foreach($x in $r){ if($null -ne $x){ $s=(''+$x).Trim(); if($s -ne ''){ $v.Add($s) } } }
  return @($v)
}
function PsqlExec([string]$db,[string]$sql){
  $prevE=$ErrorActionPreference; $ErrorActionPreference='Continue'
  $r=& $psql -h 127.0.0.1 -U postgres -d $db -X -q -c $sql 2>&1
  $ec=$LASTEXITCODE
  $ErrorActionPreference=$prevE
  if($ec -ne 0){ throw ('psql exec falhou (exit '+$ec+'): '+(($r | Out-String).Trim())) }
}
function Segundos([DateTime]$ini){ return [int][Math]::Round(((Get-Date)-$ini).TotalSeconds,0) }
function MatarPorta([string]$p){
  $cs=Get-NetTCPConnection -State Listen -LocalPort $p -ErrorAction SilentlyContinue
  foreach($c in $cs){ Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
}
function PortaLivre([string]$p){
  return (-not (Get-NetTCPConnection -State Listen -LocalPort $p -ErrorAction SilentlyContinue))
}
function ApiAguardarNoAr([int]$tentativas){
  for($i=0;$i -lt $tentativas;$i++){
    Start-Sleep 1
    try { $h=Invoke-WebRequest ($apiUrl+'/api/health') -UseBasicParsing -TimeoutSec 2; if([int]$h.StatusCode -eq 200){ return $true } } catch {}
  }
  return $false
}
function SobeApi([string]$db){
  $env:ConnectionStrings__Default='Host=127.0.0.1;Port=5432;Database='+$db+';Username=postgres;Password=123456'
  $env:Jwt__Key='r95-drill-dev-jwt-key-0123456789-abcd'
  $env:ASPNETCORE_ENVIRONMENT='Development'
  $env:ApiSettings__BaseUrl=$apiUrl
  $env:PlantaoProApi__BaseUrl=$apiUrl
  $proc=Start-Process -FilePath 'dotnet' -ArgumentList ('"'+$apiDll+'" --urls http://localhost:'+ $porta) -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Api') -WindowStyle Hidden -PassThru
  if($null -eq $proc -or $proc.Id -le 0){ throw 'API nao iniciou' }
  return [pscustomobject]@{ ProcId=[int]$proc.Id }
}
function FazerLogin([string]$email,[string]$senha){
  $bodyJ=@{ Email=$email; Senha=$senha } | ConvertTo-Json
  $rj=Invoke-WebRequest ($apiUrl+'/api/auth/login') -Method Post -Body $bodyJ -ContentType 'application/json' -UseBasicParsing
  if([int]$rj.StatusCode -ne 200){ throw ('login '+$email+' http '+[int]$rj.StatusCode) }
  $j=$rj.Content | ConvertFrom-Json
  $tok=$j.data.token
  if(-not $tok){ throw 'login '+$email+' sem token' }
  if([bool]$j.data.tenantContextRequired){
    $r2=Invoke-WebRequest ($apiUrl+'/api/auth/refresh-context') -Method Post -Headers @{ Authorization='Bearer '+$tok } -UseBasicParsing
    $j2=$r2.Content | ConvertFrom-Json
    $tok=$j2.data.token
  }
  return $tok
}
# Contagem de linhas de TODAS as tabelas do schema plantaopro em uma chamada (loop PL/pgSQL).
# Cada chamada e uma sessao nova (temp table nao persiste entre chamadas); apenas linhas tbl=n sao aceitas.
function MedirContagens([string]$db){
  $sql=@'
create temp table tmp_r95_counts(t text, n bigint);
do $$ declare r record; begin
  for r in select c.relname as nome from pg_class c join pg_namespace nn on nn.oid=c.relnamespace where nn.nspname='plantaopro' and c.relkind='r' order by 1 loop
    execute format('insert into tmp_r95_counts select %L, count(*) from plantaopro.%I', r.nome, r.nome);
  end loop;
end $$;
select t || '=' || n::text from tmp_r95_counts order by t;
'@
  $todos=PsqlQMult $db $sql
  # nome nao pode ter espaco/nem '=' (descarta avisos do tipo NOTA:/ERRO:); aceita nomes com acentos
  return @($todos | Where-Object { $_ -match '^[^ =]+=[0-9]+$' })
}
function MedirTabelas([string]$db){
  return (PsqlQMult $db "select c.relname from pg_class c join pg_namespace n on n.oid=c.relnamespace where n.nspname='plantaopro' and c.relkind='r' order by 1")
}
function MedirIndices([string]$db){
  return (PsqlQMult $db "select indexname from pg_indexes where schemaname='plantaopro' order by 1")
}
# Chaves estrangeiras com definicao completa (tab <US> conname <US> def), US=chr(31).
function MedirFksFull([string]$db){
  $q="select (c.relname || chr(31) || con.conname) || chr(31) || pg_get_constraintdef(con.oid) from pg_constraint con join pg_class c on c.oid=con.conrelid join pg_namespace n on n.oid=c.relnamespace where n.nspname='plantaopro' and con.contype='f' order by 1"
  return (PsqlQMult $db $q)
}
function MedirSequencias([string]$db){
  return (PsqlQMult $db "select sequencename || '=' || coalesce(last_value::text,'null') from pg_sequences where schemaname='plantaopro' order by 1")
}
function MedirExt([string]$db){
  return (PsqlQ $db "select string_agg(extname, ',' order by extname) from pg_extension")
}
# Digest de conteudo: concatena o texto de cada linha ordenado (independente da ordem de storage).
function MedirDigest([string]$db,[string[]]$tabs){
  $res=[System.Collections.Generic.List[string]]::new()
  foreach($t in $tabs){
    $def=PsqlQ $db ("select encode(sha256(convert_to(string_agg(row_text, chr(1) order by row_text),'UTF8')),'hex') from (select linha::text as row_text from plantaopro."+$t+" as linha) s")
    $res.Add('DIGEST tabela='+$t+' sha256='+$def)
  }
  return @($res)
}
function CalcularDiff([string[]]$antes,[string[]]$depois){
  $sa=@{}; foreach($x in $antes){ $sa[$x]=$true }
  $sb=@{}; foreach($x in $depois){ $sb[$x]=$true }
  $falt=@($antes | Where-Object { -not $sb.ContainsKey($_) })
  $extr=@($depois | Where-Object { -not $sa.ContainsKey($_) })
  return [pscustomobject]@{ Faltando=@($falt); Extra=@($extr) }
}
function LinhaDiff([string]$rotulo,[object]$diff){
  $amostra=((($diff.Faltando + $diff.Extra) | Select-Object -First 5) -join ' ; ')
  return ($rotulo+' faltando='+$diff.Faltando.Count+' extra='+$diff.Extra.Count+' amostra=['+$amostra+']')
}

$tsData=(Get-Date -Format 'yyyyMMdd_HHmmss')
AddRel $rel ('R95A_DRILL data='+$tsData+' host='+$env:COMPUTERNAME+' cpu='+$env:NUMBER_OF_PROCESSORS)
AddRel $rel ('SRC_BANCO='+$dbSrc)
$okGeral=$true
try {
  # ---------------- 0) pre-condicoes ----------------
  if(-not (Test-Path $psql)){ throw 'psql ausente' }
  if(-not (Test-Path $apiDll)){ throw 'build Release da API ausente' }
  if(-not (Test-Path $wrapper)){ throw 'instalador do script unico ausente' }
  if(-not (Test-Path $toolDbDll)){ throw 'runner de migracoes (Tools.Database) ausente' }
  if(-not (Test-Path $dumpDir)){ New-Item -ItemType Directory -Force $dumpDir | Out-Null }
  $existe=(PsqlQ 'postgres' ("select count(*) from pg_database where datname='"+$dbSrc+"'"))
  if($existe -ne '1'){ throw 'banco de origem nao existe' }
  $connsAtivas=(PsqlQ 'postgres' ("select count(*) from pg_stat_activity where datname='"+$dbSrc+"' and pid<>pg_backend_pid()"))
  AddRel $rel ('PRE_CONDICOES_OK conexoes_ativas_outros='+$connsAtivas)
  MatarPorta $porta
  if(Test-Path $envLocal){ Copy-Item $envLocal $envBackup -Force }

  # ---------------- PA) backup/restore de dados ----------------
  if(-not $SkipPa){
    # 1) baseline do banco vivo
    $contA=MedirContagens $dbSrc
    $tabA=MedirTabelas $dbSrc
    $indA=MedirIndices $dbSrc
    $fkFullA=MedirFksFull $dbSrc
    $seqA=MedirSequencias $dbSrc
    $extA=MedirExt $dbSrc
    $digA=MedirDigest $dbSrc $tabsDigest
    $sumLinhas=0; foreach($l in $contA){ $sumLinhas+=[long]($l.Split('=')[1]) }
    AddRel $rel ('ESTAGEM_1_BASELINE_OK tabelas='+$tabA.Count+' linhas_totais='+$sumLinhas+' fks='+$fkFullA.Count+' indices='+$indA.Count+' sequencias='+$seqA.Count+' extensoes='+$extA+' digest_tabelas='+$digA.Count)
    [System.IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'r95a_linhas_antes.txt'), $contA)
    # mapa chave->definicao das FKs (para sanar qualquer FK que o restore perder por ordem)
    $fkDefMap=@{}
    foreach($line in $fkFullA){ $p=$line.Split([char]31,3); $k=$p[0]+[char]31+$p[1]; if($p.Count -gt 2){ $fkDefMap[$k]=$p[2] } else { $fkDefMap[$k]='' } }

    # 2) dump (o artefato de backup)
    $t0=Get-Date
    if(Test-Path $dumpArq){ Remove-Item $dumpArq -Force }
    $prevE=$ErrorActionPreference; $ErrorActionPreference='Continue'
    & $pgdump -h 127.0.0.1 -U postgres -d $dbSrc -Fc -f $dumpArq 2>&1 | Out-Null
    $ecDump=$LASTEXITCODE
    & $pgrestore --list $dumpArq 2>&1 | Out-Null
    $ecList=$LASTEXITCODE
    $ErrorActionPreference=$prevE
    if($ecDump -ne 0){ throw 'pg_dump falhou' }
    if($ecList -ne 0){ throw 'arquivo de dump ilegivel (pg_restore --list)' }
    $sha1=(Get-FileHash $dumpArq -Algorithm SHA256).Hash.ToLowerInvariant()
    $tam=(Get-Item $dumpArq).Length
    AddRel $rel ('ESTAGEM_2_DUMP_OK arquivo='+$dumpArq+' tamanho_bytes='+$tam+' sha256='+$sha1+' tempo_seg='+(Segundos $t0))

    # 3) DROP + recriacao vazia
    $t0=Get-Date
    PsqlExec 'postgres' ("drop database if exists ""$dbScratch"" with (force);")
    PsqlExec 'postgres' ("drop database ""$dbSrc"" with (force);")
    PsqlExec 'postgres' ("create database ""$dbSrc"" template template0 encoding 'UTF8' owner postgres;")
    $vazio=(PsqlQ $dbSrc "select count(*) from information_schema.tables where table_schema='public'")
    if($vazio -ne '0'){ throw 'banco recriado nao esta vazio (schema public tem '+$vazio+' objetos)' }
    AddRel $rel ('ESTAGEM_3_DROP_RECREATE_OK tempo_seg='+(Segundos $t0))

    # 4) restauracao + sancao de foreign keys perdidas por ordem de restore
    $t0=Get-Date
    $prevE=$ErrorActionPreference; $ErrorActionPreference='Continue'
    $resto=& $pgrestore -h 127.0.0.1 -U postgres -d $dbSrc --no-owner --no-privileges $dumpArq 2>&1 | Out-String
    $avisos=[regex]::Matches($resto,'pg_restore: error').Count
    $ErrorActionPreference=$prevE
    $fkFullDepos=MedirFksFull $dbSrc
    $fkChaveDepos=@{}; foreach($line in $fkFullDepos){ $pp=$line.Split([char]31,3); $fkChaveDepos[$pp[0]+[char]31+$pp[1]]=$true }
    # pg_restore pode deixar FKs por ignorar o erro de ordem: recria cada uma ausente.
    # Adiciona NOT VALID (tolerante a linhas orfaas que existem na fonte) e tenta validar;
    # se houver orfaos (caso real: adm360_vale_itens e cotacao_envios na origem), fica
    # NOT VALID, fiel ao estado da fonte.
    $recriadas=0; $naoValidas=0
    foreach($chave in $fkDefMap.Keys){
      if(-not $fkChaveDepos.ContainsKey($chave)){
        $parts=$chave.Split([char]31); $tab=$parts[0]; $cnm=$parts[1]; $df=$fkDefMap[$chave]
        $sqlAdd='alter table only plantaopro."' + $tab + '" add constraint "' + $cnm + '" ' + $df + ' not valid;'
        PsqlExec $dbSrc $sqlAdd
        $recriadas++
        try { PsqlExec $dbSrc ('alter table only plantaopro."' + $tab + '" validate constraint "' + $cnm + '";') } catch { $naoValidas++ }
      }
    }
    $fkFullFinal=MedirFksFull $dbSrc
    $fkChaveFinal=@{}; foreach($line in $fkFullFinal){ $pp=$line.Split([char]31,3); $fkChaveFinal[$pp[0]+[char]31+$pp[1]]=$true }
    $faltandoFinal=@($fkDefMap.Keys | Where-Object { -not $fkChaveFinal.ContainsKey($_) })
    AddRel $rel ('ESTAGEM_4_RESTORE_OK tempo_seg='+(Segundos $t0)+' avisos_pg_restore='+$avisos+' fks_recriadas='+$recriadas+' fks_nao_validas='+$naoValidas+' fks_faltando_fim='+$faltandoFinal.Count)
    if($faltandoFinal.Count -gt 0){ throw ('foreign keys ainda ausentes apos sancao: '+(($faltandoFinal | Select-Object -First 5) -join ', ')) }

    # 5) paridade completa
    $contB=MedirContagens $dbSrc
    $tabB=MedirTabelas $dbSrc
    $indB=MedirIndices $dbSrc
    $seqB=MedirSequencias $dbSrc
    $extB=MedirExt $dbSrc
    $digB=MedirDigest $dbSrc $tabsDigest
    [System.IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'r95a_linhas_depois.txt'), $contB)
    $dTab=CalcularDiff $tabA $tabB
    $dInd=CalcularDiff $indA $indB
    $dSeq=CalcularDiff $seqA $seqB
    $dCont=CalcularDiff $contA $contB
    AddRel $rel (LinhaDiff 'DIF_TABELAS' $dTab)
    AddRel $rel (LinhaDiff 'DIF_INDICES' $dInd)
    AddRel $rel (LinhaDiff 'DIF_SEQUENCIAS' $dSeq)
    AddRel $rel (LinhaDiff 'DIF_CONTAGENS' $dCont)
    $extOk=($extA -eq $extB)
    $digOkCount=0
    for($i=0;$i -lt $digA.Count;$i++){ if($digA[$i] -eq $digB[$i]){ $digOkCount++ } else { AddRel $rel ('DIGEST_DIF indice='+$i+' tabela='+$tabsDigest[$i]) } }
    $invarianteBypass=(PsqlQ $dbSrc 'select plantaopro.fn_adm360_bypass_habilitado()')
    AddRel $rel ('ESTAGEM_5_PARIDADE extensoes_iguais='+$(if($extOk){'SIM'}else{'NAO'})+' digest_ok='+$digOkCount+'/'+$digA.Count+' fn_bypass='+$invarianteBypass)
    if((-not $extOk) -or ($digOkCount -lt $digA.Count) -or ($invarianteBypass -ne 'f')){ throw 'paridade de conteudo/extensoes/invariantes falhou' }
    $difs=@(@{n='DIF_TABELAS';d=$dTab},@{n='DIF_INDICES';d=$dInd},@{n='DIF_SEQUENCIAS';d=$dSeq},@{n='DIF_CONTAGENS';d=$dCont})
    foreach($d in $difs){
      if(($d.d.Faltando.Count) -gt 0 -or ($d.d.Extra.Count) -gt 0){ throw (($d.n)+' com diferenca: '+(LinhaDiff $d.n $d.d)) }
    }
    AddRel $rel ('PARIDADE_COMPLETA_OK')

    # 6) funcional: API contra o banco RESTAURADO
    $info=SobeApi $dbSrc
    try {
      if(-not (ApiAguardarNoAr 120)){ throw 'API nao subiu contra o banco restaurado' }
      $tokG=FazerLogin $gestorEmail $gestorSenha
      $ro=Invoke-WebRequest ($apiUrl+'/api/administrativo360/orcamentos') -Method Get -Headers @{ Authorization='Bearer '+$tokG } -UseBasicParsing
      if([int]$ro.StatusCode -ne 200){ throw 'rota quente retornou http '+[int]$ro.StatusCode }
      $totalInfo='?'
      # Listar devolve Ok(OrcamentoResumoPaginado) direto (sem envelope): total no topo do JSON
      try { $jo=$ro.Content | ConvertFrom-Json; if($jo.total){ $totalInfo=$jo.total } elseif($jo.data.total){ $totalInfo=$jo.data.total } elseif($jo.data.pagina.total){ $totalInfo=$jo.data.pagina.total } } catch {}
      AddRel $rel ('ESTAGEM_6_FUNCIONAL_API_OK porta='+$porta+' login_gestor=SIM orcamentos_http=200 orcamentos_total='+$totalInfo)
    } finally { try { Stop-Process -Id $info.ProcId -Force -ErrorAction SilentlyContinue } catch {} }
    AddRel $rel ('PA_CONCLUIDO')
  }

  # ---------------- PB) reconstrucao pelo script unico (artefato B1) ----------------
  if(-not $SkipPb){
    # 7) instalacao limpa pelo instalador (executa o script unico)
    PsqlExec 'postgres' ("drop database if exists ""$dbScratch"" with (force);")
    Set-Location (Join-Path $repo 'database')
    $t0=Get-Date
    $transc=& powershell -NoProfile -ExecutionPolicy Bypass -File $wrapper -Mode CLEAN -Environment Development -HostName 127.0.0.1 -Port 5432 -MaintenanceDatabase postgres -Database $dbScratch -AdminEmail $admEmail -AdminPassword $admPw -AppPassword $appPw -Profile base 2>&1 | Out-String
    if($LASTEXITCODE -ne 0){ throw ('instalador do script unico exit '+$LASTEXITCODE+' :: '+($transc.Substring([Math]::Max(0,$transc.Length-800)))) }
    if($transc -notlike '*APROVADO*'){ throw 'instalador nao chegou ao APROVADO final' }
    AddRel $rel ('ESTAGEM_7_SCRIPT_UNICO_APROVADO banco='+$dbScratch+' modo=CLEAN tempo_seg='+(Segundos $t0))

    # 8) auto-cura: runner oficial aplica migracoes pendentes no banco limpo
    Set-Location $repo
    $env:PLANTAOPRO_CONNECTION_STRING='Host=127.0.0.1;Port=5432;Database='+$dbScratch+';Username=postgres;Password=123456'
    $env:ASPNETCORE_ENVIRONMENT='Development'
    $prevE=$ErrorActionPreference; $ErrorActionPreference='Continue'
    $upOut=& dotnet $toolDbDll upgrade 2>&1 | Out-String
    $ecUp=$LASTEXITCODE
    $stOut=& dotnet $toolDbDll status 2>&1 | Out-String
    $ErrorActionPreference=$prevE
    if($ecUp -ne 0){ throw ('runner de migracoes falhou (exit '+$ecUp+'): '+($upOut.Substring([Math]::Max(0,$upOut.Length-800)))) }
    $stLinha=''
    foreach($ll in ($stOut -split "`n")){ if($ll -match 'aplicadas=\d+'){ $stLinha=$ll.Trim().TrimEnd('.'); break } }
    AddRel $rel ('ESTAGEM_8_MIGRACOES_UPGRADE_OK '+$(if($stLinha){$stLinha}else{'concluido'}))

    # 9) funcional: API sobe no banco do script unico + login do superadministrador
    $info2=SobeApi $dbScratch
    try {
      if(-not (ApiAguardarNoAr 300)){ throw 'API nao subiu contra o banco do script unico' }
      [void](FazerLogin $admEmail $admPw)
      AddRel $rel ('ESTAGEM_9_AUTO_CURA_E_LOGIN_OK login_superadmin=SIM')
    } finally { try { Stop-Process -Id $info2.ProcId -Force -ErrorAction SilentlyContinue } catch {} }

    # paridade estrutural vs original
    if($null -eq $tabA){ $tabA=MedirTabelas $dbSrc }
    $tabS=MedirTabelas $dbScratch
    $dStruct=CalcularDiff $tabA $tabS
    AddRel $rel (LinhaDiff 'DIF_SCRIPT_UNICO_VS_ORIGINAL' $dStruct)
    # Classificacao documentada do delta estrutural esperado (duas rotas oficiais de
    # instalacao + perfil): extra = DDL legado criado somente pelo script unico (nao
    # esta nas secoes schema/* nem nas migracoes); faltante = tabela criada somente pelos
    # seeds de perfil demo (o banco vivo roda perfil demo; este drill usa base).
    $esperadoExtra=@('alerta_operacional_historico','alertas_operacionais','chamado_mensagens','chamados_suporte','clinica_salas','clinica_unidades','comunicacao_contextos','consulta_adendos','contratos_operacionais','escala_historico','exportacao_logs','historico_alteracoes','medico_elegibilidade_log','medico_preferencias','medico_recomendacao_log','notificacao_agrupamentos','notificacoes_envio_log','observabilidade_eventos','onboarding_checklist_usuario','operacao_assistida_checklist','operacao_assistida_clientes','operacao_assistida_ocorrencias','operacao_assistida_treinamentos','operational_timeline_events','pagamento_historico','painel_chamada','regras_preco_contrato','solicitacoes_plantao','ui_audit_events','user_dashboard_preferences','usuario_atividade','usuario_notificacao_preferencias')
    $esperadoFaltante=@('plantao_pro_demo_premium','setores')
    $extraFora=@($dStruct.Extra | Where-Object { $esperadoExtra -notcontains $_ })
    $faltFora=@($dStruct.Faltando | Where-Object { $esperadoFaltante -notcontains $_ })
    if(($extraFora.Count -gt 0) -or ($faltFora.Count -gt 0)){
      $fora=($extraFora+$faltFora)
      AddRel $rel ('CLASSIFICACAO_DELTA_ESTRUTURAL_FALHOU inesperados_extra='+$extraFora.Count+' inesperados_faltante='+$faltFora.Count+' amostra=['+(($fora | Select-Object -First 5) -join ' ; ')+']')
      throw ('delta estrutural com tabelas fora da classificacao documentada: '+((($fora | Select-Object -First 5) -join ';')))
    }
    AddRel $rel ('CLASSIFICACAO_DELTA_ESTRUTURAL_OK extra_classificado='+$dStruct.Extra.Count+' faltante_classificado='+$dStruct.Faltando.Count+' inesperados=0')
    AddRel $rel ('PB_CONCLUIDO')
  }

  # ---------------- limpeza ----------------
  MatarPorta $porta
  if(-not $KeepScratch -and -not $SkipPb){
    PsqlExec 'postgres' ("drop database if exists ""$dbScratch"" with (force);")
  }
  if(Test-Path $envBackup){ Copy-Item $envBackup $envLocal -Force }
  AddRel $rel ('PORTAS_LIVRES='+$(if(PortaLivre $porta){'SIM'}else{'NAO'}))
  AddRel $rel ('RESULTADO=PASSEI')
}
catch {
  $okGeral=$false
  AddRel $rel ('FALHA '+$_.Exception.Message)
  try { MatarPorta $porta } catch {}
  try { if(Test-Path $envBackup){ Copy-Item $envBackup $envLocal -Force } } catch {}
  AddRel $rel ('PORTAS_LIVRES='+$(if(PortaLivre $porta){'SIM'}else{'NAO'}))
  AddRel $rel ('RESULTADO=FALHOU')
}
finally {
  $saidaArq=Join-Path $PSScriptRoot 'r95a_drill_saida.txt'
  foreach($x in $rel){ Write-Output $x }
  [System.IO.File]::WriteAllLines($saidaArq, $rel)
  Write-Output ('SAIDA_GRAVADA '+$saidaArq)
}
if(-not $okGeral){ exit 1 }
exit 0
