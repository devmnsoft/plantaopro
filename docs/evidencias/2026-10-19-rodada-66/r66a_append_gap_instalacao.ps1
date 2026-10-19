# R66 (plano 50 sprints, bloco B1): anexar as 57 migrations ausentes do scrpt_completo.sql
# Descoberta da rodada: importacao limpa criava 350 tabelas, mas o codigo referencia
# objetos que somente essas 57 migrations criam (111 gaps de objeto) - a instalacao
# unica estava quebrada em banco novo. Correcao: anexo em ordem canonica
# (41 na ordem do migration-manifest.json + 16 legacy fora do manifest, por versao)
# nos dois scripts de instalacao (scrpt_completo.sql e pgadmin/instalar_no_banco_atual.sql),
# com secoes numeradas 128+ e marcadores SOURCE/SOURCE-SHA256, e atualiza
# database/source-checksums.json (ordem alfabetica preservada).
# Uso: powershell -ExecutionPolicy Bypass -File r66a_append_gap_instalacao.ps1
$ErrorActionPreference = 'Stop'
$root = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$migDir = Join-Path $root 'database/migrations'
$enc = New-Object System.Text.UTF8Encoding($false)

# Ordem: 41 na sequencia do manifest (dependencias canonicas) + 16 legacy por versao.
$ordem = @(
  '20260607_saas_lgpd_jornada_comercial_ajuda','2026_fix_api_error_logs_defaults','2026_fix_api_error_logs_not_null','2026_fix_cid_tabela_schema','2026_fix_clinica_financeiro_minimo','2026_plantao_pro_automacao_operacional_fase4','2026_plantao_pro_piloto_comercial_operacao_b2b','2026_plantao_pro_saas_comercial_lgpd_jornada','2026_plantao_pro_saas_inteligente','2026_plantao_pro_saas_inteligente_auditavel','2026_plantao_pro_saas_inteligente_funcional','2026_plantao_pro_saas_jornada_lgpd_inteligencia','2026_plantao_pro_saas_lgpd_jornada_inteligente','2026_plantao_pro_saude360_base_clinica','2026_plantao_pro_saude360_modulos_clinicos','2026_plantao_pro_self_service_white_label','2026_plantao_pro_versao_comercial_demo','2026_plantao_pro_white_label_b2b_launch','2026_plantao_pro_white_label_self_service','2026_saas_comercial_core','2026_saude360_base_clinica_minima','2026_saude360_bi_integracoes_mobile','2026_saude360_cid_oficial','2026_saude360_cid_prescricao','2026_saude360_consultas_base','2026_saude360_convenios_planos_saude','2026_saude360_financeiro_clinica','2026_saude360_indices_performance','2026_v116_consolidacao_operacional_final','2026_v117_hardening_v116_runtime','2026_v1186_schema_permissoes_compatibilidade','2026_v1187_central_seguranca_acessos','2026_v1188_implantacao_go_live','2026_v1189_operacoes_continuidade','2026_v1189_identity_schema_login','2026_v1201_experiencia_premium_meu_dia','2026_v1250_jornada_clinica_operacional','2026_v1410_cobertura_escalas_fechamento_financeiro','2026_v2159_saude360_agenda_recepcao','2026_v2160_triagem_consulta_jornada','2026_v2167_execucao_conferencia','2026_08_v187_fechamento_operacional_financeiro','2026_08_v188_prontuario_longitudinal','2026_08_v189_clinical_operational_hardening','2026_08_v193_saude360_production','2026_09_v2158_financeiro_medico_conferencia','2026_v1190_contexto_multiempresa','2026_v1243_painel_publico_seguro','2026_v1310_consolidacao_operacao_assistida','2026_v1350_runtime_clinico_tipado_operacao_real','2026_v1360_reconciliar_perfis_duplicados','2026_v1370_bootstrap_superadmin','2026_v1400_produto_operacional_premium','2026_v1420_interface_fluxos_operacionais','2026_v1440_produto_vendavel_design_mobile_operacao','2026_v1450_design_system_executivo_operacao_comercial','2026_v2149_saas_core_modulos_perfis_menus'
)
Write-Output ('TOTAL_A_ANEXAR=' + $ordem.Count)

# Alvos e padrao de cabecalho (clona a seccao 127 existente para manter codificacao/dash).
$alvos = @('database/scrpt_completo.sql', 'database/pgadmin/instalar_no_banco_atual.sql')
$linhas = Get-Content (Join-Path $root 'database/scrpt_completo.sql') -Encoding UTF8
$idxHdr = -1
for ($i = 0; $i -lt $linhas.Count; $i++) { if ($linhas[$i] -match 'R62 AI Provedor Drael') { $idxHdr = $i; break } }
if ($idxHdr -lt 0) { throw 'cabecalho da seccao 127 nao encontrado' }
$sep = $linhas[$idxHdr - 1]
$hdrRef = $linhas[$idxHdr]
Write-Output ('SEP_OK=' + ($sep.Length) + ' HDRREF_OK=' + ($hdrRef.Length))

# Bloco por migration (secao 128+ na ordem do anexo).
$hashes = @{}
$blocks = New-Object System.Collections.Generic.List[string]
$n = 128
foreach ($nome in $ordem) {
    $p = Join-Path $migDir ($nome + '.sql')
    if (-not (Test-Path $p)) { throw 'migration ausente: ' + $p }
    $sha = (Get-FileHash $p -Algorithm SHA256).Hash.ToLowerInvariant()
    $hashes['database/migrations/' + $nome + '.sql'] = $sha
    $hdr = $hdrRef -replace [regex]::Escape('127'), ('{0}' -f $n) -replace [regex]::Escape('R62 AI Provedor Drael'), ('R66 gap instalacao: ' + $nome)
    $migText = (Get-Content $p -Encoding UTF8) -join "`r`n"
    $blocks.Add("$sep`r`n$hdr`r`n$sep`r`n`r`n-- SOURCE: database/migrations/$nome.sql`r`n-- SOURCE-SHA256: $sha`r`n$migText`r`n") | Out-Null
    $n++
}

foreach ($alvo in $alvos) {
    $path = Join-Path $root $alvo
    $conteudo = [IO.File]::ReadAllText($path, $enc)
    if ($conteudo -notmatch [regex]::Escape('R66 gap instalacao:')) {
        if (-not $conteudo.EndsWith("`n")) { $blocks.Insert(0, "`r`n") }
        [IO.File]::AppendAllText($path, (($blocks -join '') ), $enc)
        Write-Output ('APENDIDO=' + $alvo + ' BLOCOS=' + $blocks.Count)
    } else {
        Write-Output ('JAPENDIDO_PULAR=' + $alvo)
    }
}

# source-checksums.json: preserva ordenacao alfabetica, indentacao e BOM/LF existentes.
$chkPath = Join-Path $root 'database/source-checksums.json'
$rawBytes = [IO.File]::ReadAllBytes($chkPath)
$temBom = ($rawBytes.Length -ge 3 -and $rawBytes[0] -eq 0xEF -and $rawBytes[1] -eq 0xBB -and $rawBytes[2] -eq 0xBF)
$jsonRaw = [IO.File]::ReadAllText($chkPath, [System.Text.Encoding]::UTF8)
$usaCrlf = $jsonRaw.Contains("`r`n")
$dict = @{}
$doc = $jsonRaw | ConvertFrom-Json
foreach ($prop in $doc.PSObject.Properties) { $dict[$prop.Name] = $prop.Value }
$adicionados = 0
foreach ($k in $hashes.Keys) { if (-not $dict.ContainsKey($k)) { $dict[$k] = $hashes[$k]; $adicionados++ } }
$ordenadas = $dict.Keys | Sort-Object
$obj = [ordered]@{}
foreach ($k in $ordenadas) { $obj[$k] = $dict[$k] }
$jsonNovo = ([PSCustomObject]$obj | ConvertTo-Json -Depth 3) -replace "`r`n", "`n"
if ($usaCrlf) { $jsonNovo = $jsonNovo -replace "`n", "`r`n" }
$bom = New-Object System.Text.UTF8Encoding($temBom)
[IO.File]::WriteAllText($chkPath, ($jsonNovo.TrimEnd("`n") + [Environment]::NewLine), $bom)
Write-Output ('CHECKSUMS_ATUALIZADOS entradas_novas=' + $adicionados + ' total=' + $ordenadas.Count + ' bom=' + $temBom + ' crlf=' + $usaCrlf)
