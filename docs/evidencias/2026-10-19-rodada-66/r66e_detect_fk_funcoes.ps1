# R66: detecta (apenas leitura) riscos de divergencia nas migrations do gap:
#   A) ADD CONSTRAINT ... FOREIGN KEY cujo nome/definicao diverge do estado sabio (test)
#   B) CREATE [OR REPLACE] FUNCTION cujo corpo difere entre o estado sabio e o que o bloco traria
#      (o bloco roda DEPOIS das secoes originais: corpo do bloco prevalece na instalacao limpa)
$ErrorActionPreference = 'Continue'
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$dir = 'C:\MNSOFT\plantaopro\database\migrations'
$list57 = @('20260607_saas_lgpd_jornada_comercial_ajuda','2026_fix_api_error_logs_defaults','2026_fix_api_error_logs_not_null','2026_fix_cid_tabela_schema','2026_fix_clinica_financeiro_minimo','2026_plantao_pro_automacao_operacional_fase4','2026_plantao_pro_piloto_comercial_operacao_b2b','2026_plantao_pro_saas_comercial_lgpd_jornada','2026_plantao_pro_saas_inteligente','2026_plantao_pro_saas_inteligente_auditavel','2026_plantao_pro_saas_inteligente_funcional','2026_plantao_pro_saas_jornada_lgpd_inteligencia','2026_plantao_pro_saas_lgpd_jornada_inteligente','2026_plantao_pro_saude360_base_clinica','2026_plantao_pro_saude360_modulos_clinicos','2026_plantao_pro_self_service_white_label','2026_plantao_pro_versao_comercial_demo','2026_plantao_pro_white_label_b2b_launch','2026_plantao_pro_white_label_self_service','2026_saas_comercial_core','2026_saude360_base_clinica_minima','2026_saude360_bi_integracoes_mobile','2026_saude360_cid_oficial','2026_saude360_cid_prescricao','2026_saude360_consultas_base','2026_saude360_convenios_planos_saude','2026_saude360_financeiro_clinica','2026_saude360_indices_performance','2026_v116_consolidacao_operacional_final','2026_v117_hardening_v116_runtime','2026_v1186_schema_permissoes_compatibilidade','2026_v1187_central_seguranca_acessos','2026_v1188_implantacao_go_live','2026_v1189_operacoes_continuidade','2026_v1189_identity_schema_login','2026_v1201_experiencia_premium_meu_dia','2026_v1250_jornada_clinica_operacional','2026_v2159_saude360_agenda_recepcao','2026_v2160_triagem_consulta_jornada','2026_v2167_execucao_conferencia','2026_08_v187_fechamento_operacional_financeiro','2026_08_v188_prontuario_longitudinal','2026_08_v189_clinical_operational_hardening','2026_08_v193_saude360_production','2026_09_v2158_financeiro_medico_conferencia','2026_v1190_contexto_multiempresa','2026_v1243_painel_publico_seguro','2026_v1310_consolidacao_operacao_assistida','2026_v1350_runtime_clinico_tipado_operacao_real','2026_v1360_reconciliar_perfis_duplicados','2026_v1370_bootstrap_superadmin','2026_v1400_produto_operacional_premium','2026_v1420_interface_fluxos_operacionais','2026_v1440_produto_vendavel_design_mobile_operacao','2026_v1450_design_system_executivo_operacao_comercial','2026_v2149_saas_core_modulos_perfis_menus')

function ColunasDe([string]$db,[string]$tabela){
    $r = @(& $psql -h 127.0.0.1 -U postgres -d $db -Atc ("select a.attname from pg_attribute a join pg_class t on t.oid=a.attrelid join pg_namespace n on n.oid=t.relnamespace where n.nspname='plantaopro' and t.relname='" + $tabela + "' and a.attnum>0") 2>$null)
    return @($r | Where-Object { $_ -ne '' })
}

# ---------- A) foreign keys -------------------------------------------------------------
Write-Output '=== FK ==='
$rxFk = [regex]'(?i)(add\s+constraint\s+([a-z][a-z0-9_]*)\s+foreign\s+key\s*\(\s*([a-z][a-z0-9_]*)\s*\)\s*references\s+(?:plantaopro\.)?([a-z][a-z0-9_]*)\s*\(\s*([a-z][a-z0-9_]*)'
foreach ($n in $list57) {
    $ls = Get-Content (Join-Path $dir ($n + '.sql'))
    for ($i = 0; $i -lt $ls.Count; $i++) {
        foreach ($m in $rxFk.Matches($ls[$i])) {
            $nomeFk = $m.Groups[2].Value; $colLocal = $m.Groups[3].Value; $tbRef = $m.Groups[4].Value; $colRef = $m.Groups[5].Value
            $naTeste = ColunasDe 'plantaopro_test' $tbRef
            $okRef = $naTeste.Count -gt 0 -and ($naTeste -contains $colRef)
            # tabela local do add constraint: procura 'alter table X add constraint' na linha
            $mAl = [regex]::Match($ls[$i], '(?i)alter\s+table\s+(?:plantaopro\.)?([a-z][a-z0-9_]*)')
            $okLocal = $true
            if ($mAl.Success) { $cl = ColunasDe 'plantaopro_test' $mAl.Groups[1].Value; $okLocal = $cl.Count -gt 0 -and ($cl -contains $colLocal) }
            if (-not ($okRef -and $okLocal)) { Write-Output ('FK_RISCO {0}:{1} :: {2} ({3}) -> {4}({5}) okRef={6} okLocal={7}' -f $n, ($i + 1), $nomeFk, $colLocal, $tbRef, $colRef, $okRef, $okLocal) }
        }
    }
}

# ---------- B) funcoes --------------------------------------------------------------------
Write-Output '=== FUNCOES ==='
$rxFn = [regex]'(?im)^\s*create\s+(or\s+replace\s+)?(aggregate\s+)?function\s+(?:plantaopro\.)?([a-z][a-z0-9_]*\s*\(.*?\))\s'
$nomesFn = [System.Collections.Generic.HashSet[string]]::new()
$mapFn = @{}   # 'nome(args)' -> ultimo corpo visto (na ordem dos arquivos da lista)
foreach ($n in $list57) {
    $t = [IO.File]::ReadAllText((Join-Path $dir ($n + '.sql')))
    foreach ($m in [regex]::Matches($t, '(?is)create\s+(or\s+replace\s+)?function\s+(?:plantaopro\.)?([a-z][a-z0-9_]*\s*\([^)]*\))')) {
        $sig = ([regex]::Replace($m.Groups[2].Value, '\s+', ' ').Trim()).ToLowerInvariant()
        [void]$nomesFn.Add($sig)
        $fim = $t.IndexOf(';', $m.Index + $m.Length)
        if ($fim -ge 0) { $corpo = $t.Substring($m.Index, $fim - $m.Index); $mapFn[$sig] = @{ Corpo = ([regex]::Replace($corpo, '\s+', ' ').Trim()); Origem = $n } }
    }
}
Write-Output ('FUNCOES_NO_GAP=' + $nomesFn.Count)
if ($nomesFn.Count -gt 0) {
    $likeLista = '(' + (($nomesFn | ForEach-Object { "'" + ($_ -replace "'", "''") + "'" }) -join ',') + ')'
    $rows = @(& $psql -h 127.0.0.1 -U postgres -d plantaopro_test -Atc ("select p.proname||' '||pg_get_function_arguments(p.oid)||'|'||md5(p.prosrc) from pg_proc p join pg_namespace n on n.oid=p.pronamespace where n.nspname='plantaopro' and (p.proname||' '||pg_get_function_arguments(p.oid)) in " + $likeLista) 2>$null)
    $hashTeste = @{}
    foreach ($r in $rows) { if ($r -and $r.Contains('|')) { $p2 = $r.Split('|', 2); $hashTeste[$p2[0].ToLowerInvariant()] = $p2[1] } }
    foreach ($sig in ($mapFn.Keys | Sort-Object)) {
        $hashBloco = (Convert.ToHexString([System.Security.Cryptography.MD5]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes($mapFn[$sig].Corpo)))).ToLowerInvariant()
        if (-not $hashTeste.ContainsKey($sig)) { Write-Output ('FN_NOVA no teste: {0} (origem {1})' -f $sig, $mapFn[$sig].Origem) }
        elseif ($hashTeste[$sig] -ne $hashBloco) { Write-Output ('FN_DIVERGE: {0} (bloco traz versao de {1})' -f $sig, $mapFn[$sig].Origem) }
    }
}
Write-Output '=== FIM ==='
