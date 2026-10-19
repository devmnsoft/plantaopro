# R66 v2 (batch): gera database/migrations/2026_10_v2360_r66_gap_instalacao_convergencia.sql
# Mesma logica da v1, mas com queries em lote (uma por banco) em vez de um psql por tabela.
$ErrorActionPreference = 'Continue'
$root = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$env:PGPASSWORD = '123456'
$enc = New-Object System.Text.UTF8Encoding($false)
function Q([string]$database,[string]$sql){
    $r = & $psql -h 127.0.0.1 -U postgres -d $database -Atc $sql 2> (Join-Path $PSScriptRoot 'r66c_psql_err.txt')
    if ($LASTEXITCODE -ne 0) { $det = (Get-Content (Join-Path $PSScriptRoot 'r66c_psql_err.txt') -ErrorAction SilentlyContinue) -join ' / '; throw 'query falhou (db=' + $database + ' exit=' + $LASTEXITCODE + '): ' + $det }
    return @($r | Where-Object { $_ -ne '' })
}

# ---- 1) baseline ------------------------------------------------------------------------
& $psql -h 127.0.0.1 -U postgres -d postgres -qc "drop database if exists plantaopro_r66_base with (force)" 2>$null | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -qc "create database plantaopro_r66_base" 2>$null | Out-Null
& $psql -h 127.0.0.1 -U postgres -d plantaopro_r66_base -v ON_ERROR_STOP=1 -f (Join-Path $PSScriptRoot '_backup_scrpt_completo.sql') *> (Join-Path $PSScriptRoot 'r66c_base_import_raw.txt')
if ($LASTEXITCODE -ne 0) { throw 'importacao do baseline falhou (exit ' + $LASTEXITCODE + ')' }
$baseline = Q 'plantaopro_r66_base' "select c.relname from pg_class c join pg_namespace n on n.oid=c.relnamespace where n.nspname='plantaopro' and c.relkind='r'"
Write-Output ('BASELINE_TABELAS=' + $baseline.Count)

# ---- 2) dominio ---------------------------------------------------------------------------
$dirMig = Join-Path $root 'database\migrations'
$list57 = @('20260607_saas_lgpd_jornada_comercial_ajuda','2026_fix_api_error_logs_defaults','2026_fix_api_error_logs_not_null','2026_fix_cid_tabela_schema','2026_fix_clinica_financeiro_minimo','2026_plantao_pro_automacao_operacional_fase4','2026_plantao_pro_piloto_comercial_operacao_b2b','2026_plantao_pro_saas_comercial_lgpd_jornada','2026_plantao_pro_saas_inteligente','2026_plantao_pro_saas_inteligente_auditavel','2026_plantao_pro_saas_inteligente_funcional','2026_plantao_pro_saas_jornada_lgpd_inteligencia','2026_plantao_pro_saas_lgpd_jornada_inteligente','2026_plantao_pro_saude360_base_clinica','2026_plantao_pro_saude360_modulos_clinicos','2026_plantao_pro_self_service_white_label','2026_plantao_pro_versao_comercial_demo','2026_plantao_pro_white_label_b2b_launch','2026_plantao_pro_white_label_self_service','2026_saas_comercial_core','2026_saude360_base_clinica_minima','2026_saude360_bi_integracoes_mobile','2026_saude360_cid_oficial','2026_saude360_cid_prescricao','2026_saude360_consultas_base','2026_saude360_convenios_planos_saude','2026_saude360_financeiro_clinica','2026_saude360_indices_performance','2026_v116_consolidacao_operacional_final','2026_v117_hardening_v116_runtime','2026_v1186_schema_permissoes_compatibilidade','2026_v1187_central_seguranca_acessos','2026_v1188_implantacao_go_live','2026_v1189_operacoes_continuidade','2026_v1189_identity_schema_login','2026_v1201_experiencia_premium_meu_dia','2026_v1250_jornada_clinica_operacional','2026_v2159_saude360_agenda_recepcao','2026_v2160_triagem_consulta_jornada','2026_v2167_execucao_conferencia','2026_08_v187_fechamento_operacional_financeiro','2026_08_v188_prontuario_longitudinal','2026_08_v189_clinical_operational_hardening','2026_08_v193_saude360_production','2026_09_v2158_financeiro_medico_conferencia','2026_v1190_contexto_multiempresa','2026_v1243_painel_publico_seguro','2026_v1310_consolidacao_operacao_assistida','2026_v1350_runtime_clinico_tipado_operacao_real','2026_v1360_reconciliar_perfis_duplicados','2026_v1370_bootstrap_superadmin','2026_v1400_produto_operacional_premium','2026_v1420_interface_fluxos_operacionais','2026_v1440_produto_vendavel_design_mobile_operacao','2026_v1450_design_system_executivo_operacao_comercial','2026_v2149_saas_core_modulos_perfis_menus')
$definidas57 = [System.Collections.Generic.HashSet[string]]::new()
foreach ($n in $list57) {
    $t = [IO.File]::ReadAllText((Join-Path $dirMig ($n + '.sql')))
    foreach ($m in [regex]::Matches($t, '(?i)create\s+table\s+if\s+not\s+exists\s+(?:plantaopro\.)?([a-z][a-z0-9_]*)')) { [void]$definidas57.Add($m.Groups[1].Value.ToLowerInvariant()) }
}
$dominio = @($baseline | Where-Object { $_ -ne '' } | Sort-Object)
if ($dominio.Count -eq 0) { throw 'dominio de convergencia vazio' }
Write-Output ('DOMINIO_CONVERGENCIA=' + $dominio.Count)
$inLista = '(' + (($dominio | ForEach-Object { "'" + $_ + "'" }) -join ',') + ')'

# lote: colunas sabias (tabela|col|tipo|notnull) e colunas base (apenas tabela|col)
$sabias = Q 'plantaopro_test' ("select t.relname||'|'||a.attname||'|'||format_type(a.atttypid,a.atttypmod)||'|'||case when a.attnotnull and coalesce(pg_get_expr(ad.adbin,ad.adrelid),'')='' then '1' else '0' end from pg_attribute a join pg_class t on t.oid=a.attrelid join pg_namespace n on n.oid=t.relnamespace left join pg_attrdef ad on ad.adrelid=a.attrelid and ad.adnum=a.attnum where n.nspname='plantaopro' and t.relname in " + $inLista + " and a.attnum>0 and not a.attisdropped")
$naBase = Q 'plantaopro_r66_base' ("select t.relname||'|'||a.attname from pg_attribute a join pg_class t on t.oid=a.attrelid join pg_namespace n on n.oid=t.relnamespace where n.nspname='plantaopro' and t.relname in " + $inLista + " and a.attnum>0 and not a.attisdropped")
# linhas presentes em cada tabela do dominio na base (reltuples: -1 = desconhecido -> trata como pode ter dados)
$ocupa = Q 'plantaopro_r66_base' ("select t.relname||'|'||coalesce(t.reltuples,-1)::text from pg_class t join pg_namespace n on n.oid=t.relnamespace where n.nspname='plantaopro' and t.relname in " + $inLista)
$comLinhas = @{}
foreach ($l in $ocupa) { $p = $l.Split('|'); if ($p[1] -ne '0') { $comLinhas[$p[0]] = $true } }
$colsBase = @{}
foreach ($l in $naBase) { $p = $l.Split('|'); if (-not $colsBase.ContainsKey($p[0])) { $colsBase[$p[0]] = [System.Collections.Generic.HashSet[string]]::new() }; [void]$colsBase[$p[0]].Add($p[1]) }

$linhasSql = New-Object System.Collections.Generic.List[string]
$linhasSql.Add('-- ============================================================================')
$linhasSql.Add('-- PlantaoPro | Migration: 2026_10_v2360_r66_gap_instalacao_convergencia')
$linhasSql.Add('-- R66 (plano 50 sprints, bloco B1): convergencia de colunas para a importacao unica.')
$linhasSql.Add('-- As secoes curadas criam tabelas com menos colunas que as definicoes legadas que')
$linhasSql.Add('-- passam a compor o mesmo script; esta secao alinha as colunas ao estado sabio do')
$linhasSql.Add('-- ambiente em operacao (gerado da diferenca entre o banco local em uso e a importacao')
$linhasSql.Add('-- limpa do script original). Idempotente: ADD COLUMN IF NOT EXISTS.')
$linhasSql.Add('-- ============================================================================')
$totalCols = 0
foreach ($l in $sabias) {
    $p = $l.Split('|')
    $tb = $p[0]; $col = $p[1]; $tipo = $p[2]
    if (-not $colsBase.ContainsKey($tb) -or -not $colsBase[$tb].Contains($col)) {
        if ($p[3] -eq '1') {
            if ($comLinhas.ContainsKey($tb)) { $tipo = $tipo + '' } else { $tipo = $tipo + ' not null' }
        }
        $linhasSql.Add('alter table plantaopro.' + $tb + ' add column if not exists ' + $col + ' ' + $tipo + ';')
        $totalCols++
    }
}
# clinica_unidades / clinica_salas: referenciadas pelo codigo (CentralAtendimentoService)
# sem criador em nenhum arquivo do repo; definicao minima para a instalacao unica fechar.
$linhasSql.Add('-- clinica_unidades / clinica_salas: referenciadas pelo codigo (CentralAtendimentoService)')
$linhasSql.Add('-- sem criador em nenhum arquivo do repo; definicao minima para a instalacao unica fechar.')
$linhasSql.Add('create table if not exists plantaopro.clinica_unidades (')
$linhasSql.Add('    id uuid primary key default gen_random_uuid(),')
$linhasSql.Add('    tenant_id uuid null,')
$linhasSql.Add('    nome varchar(200) not null,')
$linhasSql.Add('    reg_date timestamptz not null default now(),')
$linhasSql.Add('    reg_update timestamptz,')
$linhasSql.Add("    reg_status char(1) not null default 'A'")
$linhasSql.Add(');')
$linhasSql.Add('create table if not exists plantaopro.clinica_salas (')
$linhasSql.Add('    id uuid primary key default gen_random_uuid(),')
$linhasSql.Add('    unidade_id uuid null,')
$linhasSql.Add('    tenant_id uuid null,')
$linhasSql.Add('    nome varchar(200) not null,')
$linhasSql.Add('    reg_date timestamptz not null default now(),')
$linhasSql.Add('    reg_update timestamptz,')
$linhasSql.Add("    reg_status char(1) not null default 'A'")
$linhasSql.Add(');')
$linhasSql.Add('-- fim convergencia r66 (colunas adicionadas: ' + $totalCols + ')')
Write-Output ('COLUNAS_A_CONVERGIR=' + $totalCols)
$migPath = Join-Path $dirMig '2026_10_v2360_r66_gap_instalacao_convergencia.sql'
[IO.File]::WriteAllLines($migPath, $linhasSql, $enc)
$sha = (Get-FileHash $migPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output ('MIG_GERADA bytes=' + (Get-Item $migPath).Length + ' SHA=' + $sha)

# ---- 3) insere secao 128 antes da primeira secao R66 e renumera ---------------------------
$novoNomeSecao = 'R66 gap instalacao: 2026_10_v2360_r66_gap_instalacao_convergencia'
foreach ($alvo in @('database/scrpt_completo.sql', 'database/pgadmin/instalar_no_banco_atual.sql')) {
    $p = Join-Path $root $alvo
    $linhas = @(Get-Content $p -Encoding UTF8)
    if (($linhas | Where-Object { $_ -match [regex]::Escape($novoNomeSecao) }).Count -gt 0) { Write-Output ('JAINSERIDA_PULAR=' + $alvo); continue }
    $renum = 0
    foreach ($i in 0..($linhas.Count - 1)) {
        if ($linhas[$i] -match '^-- Se[çc][aã]o (\d+) — R66 gap instalacao:') {
            $novo = [int]$Matches[1] + 1
            $linhas[$i] = '-- Seção ' + $novo + ' — R66 gap instalacao:' + $linhas[$i].Substring($linhas[$i].IndexOf('R66 gap'))
            $renum++
        }
    }
    $iPrimeiroHdr = -1
    for ($i = 0; $i -lt $linhas.Count; $i++) { if ($linhas[$i] -match '^-- Se[çc][aã]o 129 — R66 gap instalacao:') { $iPrimeiroHdr = $i; break } }
    if ($iPrimeiroHdr -lt 0) { throw 'seccao 129 (primeira R66) nao encontrada em ' + $alvo }
    $sep = $linhas[$iPrimeiroHdr - 1]
    $hdr = '-- Seção 128 — ' + $novoNomeSecao
    $bloco = @($sep, $hdr, $sep, '', ('-- SOURCE: database/migrations/2026_10_v2360_r66_gap_instalacao_convergencia.sql'), ('-- SOURCE-SHA256: ' + $sha)) + [string[]]$linhasSql
    $novas = $linhas[0..($iPrimeiroHdr - 2)] + $bloco + $linhas[($iPrimeiroHdr - 1)..($linhas.Count - 1)]
    [IO.File]::WriteAllLines($p, $novas, $enc)
    Write-Output ('SECAO128_INSERIDA=' + $alvo + ' RENUM=' + $renum + ' LINHAS_NOVAS=' + $novas.Count)
}

# ---- 4) checksums + manifest ---------------------------------------------------------------
$chkPath = Join-Path $root 'database/source-checksums.json'
$raw = [IO.File]::ReadAllText($chkPath, [System.Text.Encoding]::UTF8)
$temBom = $raw.StartsWith("`u{FEFF}"); if ($temBom) { $raw = $raw.Substring(1) }
$usaCrlf = $raw.Contains("`r`n")
$dict = @{}
foreach ($prop in ($raw | ConvertFrom-Json).PSObject.Properties) { $dict[$prop.Name] = $prop.Value }
$dict['database/migrations/2026_10_v2360_r66_gap_instalacao_convergencia.sql'] = $sha
$obj = [ordered]@{}
foreach ($k in ($dict.Keys | Sort-Object)) { $obj[$k] = $dict[$k] }
$jsonNovo = ([PSCustomObject]$obj | ConvertTo-Json -Depth 3) -replace "`r`n", "`n"
if ($usaCrlf) { $jsonNovo = $jsonNovo -replace "`n", "`r`n" }
[IO.File]::WriteAllText($chkPath, ($jsonNovo.TrimEnd("`n") + [Environment]::NewLine), (New-Object System.Text.UTF8Encoding($temBom)))
Write-Output ('CHECKSUMS_TOTAL=' + $dict.Count)

$manPath = Join-Path $root 'database/migration-manifest.json'
$raw = [IO.File]::ReadAllText($manPath, [System.Text.Encoding]::UTF8)
$temBom = $raw.StartsWith("`u{FEFF}"); if ($temBom) { $raw = $raw.Substring(1) }
$usaCrlf = $raw.Contains("`r`n")
$man = $raw | ConvertFrom-Json
if (-not (@($man.migrations | Where-Object { $_.version -eq '2026_10_v2360_r66_gap_instalacao_convergencia' }).Count -gt 0)) {
    $novo = [PSCustomObject]@{
        version = '2026_10_v2360_r66_gap_instalacao_convergencia'
        source = 'database/migrations/2026_10_v2360_r66_gap_instalacao_convergencia.sql'
        checksum = $sha
        dependsOn = @()
        status = 'active'
        transactional = $true
    }
    $man.migrations = @($man.migrations) + $novo
}
$jsonNovo = ($man | ConvertTo-Json -Depth 5) -replace "`r`n", "`n"
if ($usaCrlf) { $jsonNovo = $jsonNovo -replace "`n", "`r`n" }
[IO.File]::WriteAllText($manPath, ($jsonNovo.TrimEnd("`n") + [Environment]::NewLine), (New-Object System.Text.UTF8Encoding($temBom)))
Write-Output ('MANIFEST_TOTAL=' + @($man.migrations).Count)
& $psql -h 127.0.0.1 -U postgres -d postgres -qc "drop database if exists plantaopro_r66_base with (force)" 2>$null | Out-Null
Write-Output 'R66C_OK'
