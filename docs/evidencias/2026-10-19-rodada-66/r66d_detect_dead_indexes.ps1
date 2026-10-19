# R66: detecta (apenas leitura) indices e FKs "mortos" nas migrations do gap de instalacao:
# colunas citadas que nao existem no estado sabio do banco local (plantaopro_test).
$ErrorActionPreference = 'Continue'
$env:PGPASSWORD = '123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$dir = 'C:\MNSOFT\plantaopro\database\migrations'
$list57 = @('20260607_saas_lgpd_jornada_comercial_ajuda','2026_fix_api_error_logs_defaults','2026_fix_api_error_logs_not_null','2026_fix_cid_tabela_schema','2026_fix_clinica_financeiro_minimo','2026_plantao_pro_automacao_operacional_fase4','2026_plantao_pro_piloto_comercial_operacao_b2b','2026_plantao_pro_saas_comercial_lgpd_jornada','2026_plantao_pro_saas_inteligente','2026_plantao_pro_saas_inteligente_auditavel','2026_plantao_pro_saas_inteligente_funcional','2026_plantao_pro_saas_jornada_lgpd_inteligencia','2026_plantao_pro_saas_lgpd_jornada_inteligente','2026_plantao_pro_saude360_base_clinica','2026_plantao_pro_saude360_modulos_clinicos','2026_plantao_pro_self_service_white_label','2026_plantao_pro_versao_comercial_demo','2026_plantao_pro_white_label_b2b_launch','2026_plantao_pro_white_label_self_service','2026_saas_comercial_core','2026_saude360_base_clinica_minima','2026_saude360_bi_integracoes_mobile','2026_saude360_cid_oficial','2026_saude360_cid_prescricao','2026_saude360_consultas_base','2026_saude360_convenios_planos_saude','2026_saude360_financeiro_clinica','2026_saude360_indices_performance','2026_v116_consolidacao_operacional_final','2026_v117_hardening_v116_runtime','2026_v1186_schema_permissoes_compatibilidade','2026_v1187_central_seguranca_acessos','2026_v1188_implantacao_go_live','2026_v1189_operacoes_continuidade','2026_v1189_identity_schema_login','2026_v1201_experiencia_premium_meu_dia','2026_v1250_jornada_clinica_operacional','2026_v2159_saude360_agenda_recepcao','2026_v2160_triagem_consulta_jornada','2026_v2167_execucao_conferencia','2026_08_v187_fechamento_operacional_financeiro','2026_08_v188_prontuario_longitudinal','2026_08_v189_clinical_operational_hardening','2026_08_v193_saude360_production','2026_09_v2158_financeiro_medico_conferencia','2026_v1190_contexto_multiempresa','2026_v1243_painel_publico_seguro','2026_v1310_consolidacao_operacao_assistida','2026_v1350_runtime_clinico_tipado_operacao_real','2026_v1360_reconciliar_perfis_duplicados','2026_v1370_bootstrap_superadmin','2026_v1400_produto_operacional_premium','2026_v1420_interface_fluxos_operacionais','2026_v1440_produto_vendavel_design_mobile_operacao','2026_v1450_design_system_executivo_operacao_comercial','2026_v2149_saas_core_modulos_perfis_menus')

$rxIdx = [regex]'(?i)^\s*create\s+(unique\s+)?index\s+if\s+not\s+exists\s+([a-z][a-z0-9_]*)\s+on\s+(?:only\s+)?(?:plantaopro\.)?([a-z][a-z0-9_]*)\s*\(([^)]*)\)'
$tabelas = [System.Collections.Generic.HashSet[string]]::new()
$candidatos = New-Object System.Collections.Generic.List[object]
foreach ($n in $list57) {
    $ls = Get-Content (Join-Path $dir ($n + '.sql'))
    for ($i = 0; $i -lt $ls.Count; $i++) {
        $m = $rxIdx.Match($ls[$i])
        if ($m.Success) {
            $tb = $m.Groups[3].Value
            $colunas = @($m.Groups[4].Value -split ',' | ForEach-Object { ($_ -split '\s+')[0].Trim('"') } | Where-Object { $_ -match '^[a-z][a-z0-9_]*$' })
            [void]$tabelas.Add($tb)
            $candidatos.Add([PSCustomObject]@{ Arq = $n; Linha = ($i + 1); Nome = $m.Groups[2].Value; Tabela = $tb; Cols = $colunas })
        }
    }
}
Write-Output ('INDICES_CANDIDATOS=' + $candidatos.Count + ' TABELAS=' + $tabelas.Count)
$inL = '(' + (($tabelas | ForEach-Object { "'" + $_ + "'" }) -join ',') + ')'
$rows = @(& $psql -h 127.0.0.1 -U postgres -d plantaopro_test -Atc ("select t.relname||'|'||a.attname from pg_attribute a join pg_class t on t.oid=a.attrelid join pg_namespace n on n.oid=t.relnamespace where n.nspname='plantaopro' and t.relname in " + $inL + " and a.attnum>0") 2>$null)
$colsTeste = @{}
foreach ($r in $rows) { if ($r -and $r.Contains('|')) { $p = $r.Split('|'); if (-not $colsTeste.ContainsKey($p[0])) { $colsTeste[$p[0]] = @() }; $colsTeste[$p[0]] += $p[1] } }
$mortos = @()
foreach ($c in $candidatos) {
    $naoExisteTabela = -not $colsTeste.ContainsKey($c.Tabela)
    $faltam = @($c.Cols | Where-Object { $naoExisteTabela -or ($colsTeste[$c.Tabela] -notcontains $_) })
    if ($faltam.Count -gt 0) { $mortos += ('{0}:{1} :: {2} on {3} faltam=[{4}]' -f $c.Arq, $c.Linha, $c.Nome, $c.Tabela, ($faltam -join ',')) }
}
Write-Output ('INDICES_MORTOS=' + $mortos.Count)
$mortos | ForEach-Object { Write-Output $_ }
