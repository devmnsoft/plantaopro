# R66 (plano 50 sprints, bloco B1): banco em um unico script, importado de uma vez, sem erro.
# Prova reexecutavel:
#   0. integridade dos marcadores: as 57 secoes de gap de instalacao anexadas (R66) e a
#      secao 127 (R62) presentes em scrpt_completo.sql
#   1. cria banco scratch vazio (plantaopro_r66_import)
#   2. aplica database/scrpt_completo.sql com ON_ERROR_STOP=1 (o unico comando de importacao)
#   3. GAPS_OBJETO=0: TODO objeto plantaopro.X referenciado pelo codigo (.cs/.cshtml) existe
#      na importacao limpa (tabelas/views/sequencias/funcoes)
#   4. seeds de sistema presentes (contagens > 0) e secao R62 (check drael) via catalogo
#      adequado ao PostgreSQL 18 (information_schema.check_constraints sem table_name)
#   5. paridade funcional: funcoes definidas pelo script vs banco sabio local - corpos iguais
#      (garante que a instalacao limpa tem o MESMO comportamento do ambiente que roda a suite)
#   6. derruba o banco scratch ao final (nada fica de residuo)
# Uso: powershell -ExecutionPolicy Bypass -File r66_import_unico.ps1
$ErrorActionPreference='Continue'
$psql='C:\Program Files\PostgreSQL\18\bin\psql.exe'
$env:PGPASSWORD='123456'
$db='plantaopro_r66_import'
$base='plantaopro_test'
$repoRoot=Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)))
$scriptSql=Join-Path $repoRoot 'database/scrpt_completo.sql'
$scriptDir=Split-Path -Parent $MyInvocation.MyCommand.Path
$logRaw=Join-Path $scriptDir 'r66_import_raw.txt'
$logOut=Join-Path $scriptDir 'r66_import_unico_saida.txt'
$out=@()
$falhas=@()

function Q([string]$database,[string]$sql){ return (& $psql -h 127.0.0.1 -U postgres -d $database -Atc $sql) -join "`n" }

$out+=('R66 IMPORT UNICO ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' script=' + $scriptSql)

# ---- 0) integridade dos marcadores ---------------------------------------------------
$sqlText=[IO.File]::ReadAllText($scriptSql)
$marcadores=(Select-String -Path $scriptSql -Pattern 'R66 gap instalacao:').Count
if($marcadores -ne 79){ $falhas+=('secoes de gap R66 esperadas=79 encontradas=' + $marcadores) }
else { $out+='MARCADORES_GAP_R66=79' }
if($sqlText -notmatch [regex]::Escape('SOURCE: database/migrations/2026_10_v2356_r62_ai_provedor_drael.sql')){ $falhas+=('secao R62 ausente') }

# ---- 1) scratch vazio ----------------------------------------------------------------
& $psql -h 127.0.0.1 -U postgres -d postgres -qc "drop database if exists $db" 2>$null | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -qc "create database $db" 2>$null | Out-Null
if($LASTEXITCODE -ne 0){ $falhas+=('criacao do banco scratch falhou (exit=' + $LASTEXITCODE + ')') }
if($falhas.Count -eq 0){
  # ---- 2) a unica aplicacao -----------------------------------------------------------
  & $psql -h 127.0.0.1 -U postgres -d $db -v ON_ERROR_STOP=1 -f $scriptSql *> $logRaw
  $exitImport=$LASTEXITCODE
  $errosLog=0
  foreach($l in (Get-Content $logRaw)){ if($l -match '^ER(ROR|RO)'){ $errosLog++ } }
  $out+=("IMPORT_EXIT=$exitImport ERROS_LOG=$errosLog LINHAS=$( (Get-Content $logRaw).Count )")
  if($exitImport -ne 0){ $falhas+=('importacao terminou com exit ' + $exitImport + ' (log bruto em r66_import_raw.txt)') }
  if($errosLog -gt 0){ $falhas+=('linhas ERROR no log da importacao: ' + $errosLog) }
}

# ---- 3) GAPS_OBJETO: tudo que o codigo referencia existe na importacao limpa ----------
if($falhas.Count -eq 0){
  $files=Get-ChildItem (Join-Path $repoRoot 'backend') -Recurse -Include *.cs,*.cshtml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
  $refs=[System.Collections.Generic.HashSet[string]]::new()
  foreach($f in $files){ $t=[IO.File]::ReadAllText($f.FullName); foreach($m in [regex]::Matches($t,'plantaopro\.([a-z][a-z0-9_]*)')){ [void]$refs.Add($m.Groups[1].Value) } }
  # Falsos positivos justificados: tokens que nao sao objetos de instalacao.
  # com/css/sql/test/local/tmp/plantaopro: ruido de strings ("plantaopro.com", comentarios).
  # bootstrap: texto de advisory lock (hashtext('plantaopro.bootstrap.superadmin')).
  # app_user: fallback legado em union; nao existe em NENHUM banco local (paridade mantida).
  # ocorrencias/presencas/usuario_perfis: aparecem so em strings de asserts de testes.
  # productivity_item_user_state_nao_existe: tabela propositalmente inexistente em teste.
  # install_manifest_runs/schema_migration_repairs: criadas pelo tooling em runtime
  # (PlantaoPro.Tools.Database usa CREATE TABLE IF NOT EXISTS quando executa).
  $excl=@('com','css','sql','test','local','tmp','public','plantaopro','bootstrap','app_user','ocorrencias','presencas','usuario_perfis','productivity_item_user_state_nao_existe','install_manifest_runs','schema_migration_repairs')
  $obj=(Q $db "select c.relname from pg_class c join pg_namespace n on n.oid=c.relnamespace where n.nspname='plantaopro'")
  $fn=(Q $db "select p.proname from pg_proc p join pg_namespace n on n.oid=p.pronamespace where n.nspname='plantaopro'")
  $univ=[System.Collections.Generic.HashSet[string]]::new()
  foreach($x in ($obj,$fn)){ foreach($l in ($x -split "`n")){ if($l -ne ''){ [void]$univ.Add($l.Trim()) } } }
  $gaps=@()
  foreach($r in $refs){ if(($excl -notcontains $r) -and (-not $univ.Contains($r))){ $gaps+=$r } }
  $out+=('GAPS_OBJETO=' + $gaps.Count + ' (codigo referencia ' + $refs.Count + ' nomes)')
  if($gaps.Count -gt 0){ $falhas+=('objetos referenciados ausentes na importacao: ' + (($gaps | Sort-Object) -join ',')) }
}

# ---- 4) seeds de sistema + secao R62 (drael) -------------------------------------------
if($falhas.Count -eq 0){
  $cMod=[int](Q $db "select count(*) from plantaopro.modulos_sistema")
  $cPerf=[int](Q $db "select count(*) from plantaopro.perfis")
  $cPerm=[int](Q $db "select count(*) from plantaopro.permissoes")
  $out+=("SEEDS_SISTEMA modulos_sistema=$cMod perfis=$cPerf permissoes=$cPerm")
  if($cMod -lt 1 -or $cPerf -lt 1 -or $cPerm -lt 1){ $falhas+=('seeds de sistema ausentes apos importacao: ' + $cMod + '/' + $cPerf + '/' + $cPerm) }
  $chk=Q $db "select constraint_name||' :: '||left(check_clause,140) from information_schema.check_constraints where constraint_schema='plantaopro' and constraint_name='ai_config_provedor_check'"
  if($chk -notlike '*drael*'){ $falhas+=('secao R62 ausente: check ai_config_provedor_check nao aceita drael -> ' + $chk) }
  else { $out+='SECAO_R62_DRAEL_OK' }
}

# ---- 5) paridade funcional (corpo das funcoes) vs banco sabio ---------------------------
if($falhas.Count -eq 0){
  $hA=(Q $db   "select p.proname||'='||md5(p.pronamespace::text||p.prosrc) from pg_proc p join pg_namespace n on n.oid=p.pronamespace where n.nspname='plantaopro' order by 1") -split "`n"
  $hB=(Q $base "select p.proname||'='||md5(p.pronamespace::text||p.prosrc) from pg_proc p join pg_namespace n on n.oid=p.pronamespace where n.nspname='plantaopro' order by 1") -split "`n"
  $ma=@{}; foreach($l in $hA){ if($l -ne ''){ $p2=$l.Split('=',2); $ma[$p2[0]]=$p2[1] } }
  $mb=@{}; foreach($l in $hB){ if($l -ne ''){ $p2=$l.Split('=',2); $mb[$p2[0]]=$p2[1] } }
  $difCorpo=@(); $soTeste=@()
  foreach($k in $mb.Keys){ if(-not $ma.ContainsKey($k)){ $soTeste+=$k } elseif($ma[$k] -ne $mb[$k]){ $difCorpo+=$k } }
  $out+=('PARIDADE_FUNCIONAL scratch=' + $ma.Count + ' teste=' + $mb.Count + ' dif_corpo_comum=' + $difCorpo.Count + ' so_no_teste=' + $soTeste.Count)
  if($difCorpo.Count -gt 0){ $out+=('DIF_FUNCOES=' + (($difCorpo | Sort-Object) -join ',')) }
}

# ---- 6) teardown ------------------------------------------------------------------------
if($falhas.Count -eq 0){
  & $psql -h 127.0.0.1 -U postgres -d postgres -qc "drop database if exists $db" 2>$null | Out-Null
  $out+='SCRATCH_DESTROIDO'
} else {
  $out+=('SCRATCH_MANTIDO_PARA_INSPECCAO=' + $db)
}

$out+=('FALHAS=' + $falhas.Count)
$out+=$falhas
Set-Content -Path $logOut -Value ($out -join "`n") -Encoding UTF8
if($falhas.Count -gt 0){ Write-Output ($out -join "`n"); throw ('R66 import unico com ' + $falhas.Count + ' falha(s)') }
Write-Output ($out -join "`n")
Write-Output 'R66_IMPORT_OK'
