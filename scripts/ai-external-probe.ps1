<#
  ai-external-probe.ps1  (Rodada 4 - B10 IA)
  ---------------------------------------------------------------------------
  Probe de homologacao EXTERNA da camada de assistentes IA.

  Faz UMA chamada real e minima ao provedor para provar, sem banco nem JWT:
    - a chave e valida,
    - a rede alcanca o endpoint,
    - o contrato de wire usado pelos adaptadores da API responde (Groq/DeepSeek
      via OpenAI-compatible chat/completions + Authorization Bearer; Gemini via
      v1beta models/{modelo}:generateContent + header x-goog-api-key).

  As chaves sao lidas das MESMAS variaveis de ambiente da configuracao da API:
      Ai__Providers__Groq__ApiKey
      Ai__Providers__Gemini__ApiKey
      Ai__Providers__DeepSeek__ApiKey
  Opcional: sobe um .env na raiz do repo (ja git-ignorado) no formato KEY=VALUE;
  este script o importa se existir.
  Opcional: sobrescrever o modelo via AI_GROQ_MODEL / AI_GEMINI_MODEL /
  AI_DEEPSEEK_MODEL (padrao: modelos vigentes documentados em AiGateway.cs).

  Uso:
      powershell -File scripts\ai-external-probe.ps1

  Veredito por provedor:  [OK]  |  [FALHA <http>]  |  [SEM_CHAVE]
  Regra da pauta: SEM_CHAVE != OK. Mocks nao declaram homologacao externa.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch { }

# Resultados em escopo de script, acessiveis dentro das funcoes.
$script:results = @()

function Import-DotEnv([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    foreach ($line in Get-Content -LiteralPath $Path) {
        $t = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($t) -or $t.StartsWith("#")) { continue }
        $eq = $t.IndexOf("=")
        if ($eq -le 0) { continue }
        $k = $t.Substring(0, $eq).Trim()
        $v = $t.Substring($eq + 1).Trim()
        if ($v.Length -ge 2 -and $v.StartsWith('"') -and $v.EndsWith('"')) { $v = $v.Substring(1, $v.Length - 2) }
        if ($v.Length -ge 2 -and $v.StartsWith("'") -and $v.EndsWith("'")) { $v = $v.Substring(1, $v.Length - 2) }
        Set-Item -Path ("Env:" + $k) -Value $v
    }
}

function Mask-Key([string]$k) {
    if ([string]::IsNullOrWhiteSpace($k)) { return "(sem chave)" }
    if ($k.Length -le 4) { return "****" }
    return ("..." + $k.Substring($k.Length - 4))
}

function New-ProbeClient {
    Add-Type -AssemblyName System.Net.Http 2>$null | Out-Null
    $c = New-Object System.Net.Http.HttpClient
    $c.Timeout = [TimeSpan]::FromSeconds(30)
    return $c
}

# Cliente compartilhado. O probe usa HttpClient (nao Invoke-WebRequest) por
# confiabilidade de TLS/timeout: contra alguns endpoints o cliente legado engasga
# e o HttpClient responde (confirmado na homologacao externa de 2026-10-07).
$script:httpClient = New-ProbeClient

# POST JSON -> @{ Code = <int>; Body = <string> }. Code = -1 em erro de transporte/timeout.
function Invoke-JsonPost([string]$Url, [string]$AuthHeader, [string]$AuthValue, [string]$Json) {
    $req = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Post, $Url)
    $req.Content = New-Object System.Net.Http.StringContent($Json, [System.Text.Encoding]::Utf8, "application/json")
    $req.Headers.Add($AuthHeader, $AuthValue)
    try {
        $resp = $script:httpClient.SendAsync($req).GetAwaiter().GetResult()
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($null -eq $body) { $body = "" }
        return @{ Code = [int]$resp.StatusCode; Body = $body }
    } catch {
        return @{ Code = -1; Body = $_.Exception.Message }
    }
}

# --- Provedores OpenAI-compatible (Groq, DeepSeek) -------------------------
# Retry em falha transitoria (5xx, 429 cota, ou timeout sem resposta); falha
# deterministica (401/404/400/402 ...) nao repete.
function Probe-OpenAiCompat([string]$Name, [string]$Base, [string]$Model, [string]$Key) {
    Write-Host ("==> " + $Name + "  [openai-compat]  model=" + $Model + "  key=" + (Mask-Key $Key))
    $url = $Base.TrimEnd('/') + "/chat/completions"
    $payload = [ordered]@{
        model       = $Model
        max_tokens  = 16
        temperature = 0.2
        messages    = @(@{ role = "user"; content = "Responda apenas: ok." })
    } | ConvertTo-Json -Depth 6
    for ($a = 1; $a -le 3; $a++) {
        $res = Invoke-JsonPost $url "Authorization" ("Bearer " + $Key) $payload
        $code = $res.Code
        if ($code -eq 200 -and $res.Body -match '"content"\s*:') {
            Write-Host ("[OK] " + $Name + " respondeu HTTP 200 com texto gerado.")
            $script:results += ,@($Name, "OK"); return
        }
        $transient = ($code -ge 500) -or ($code -eq 429) -or ($code -eq -1)
        if ($transient -and $a -lt 3) {
            Write-Host ("  [tentativa $a/3] " + $Name + " http=" + $code + " (transitoria); aguardando e repetindo...")
            Start-Sleep -Seconds 3; continue
        }
        $label = if ($code -eq -1) { "TIMEOUT" } else { [string]$code }
        Write-Host ("[FALHA] " + $Name + " http=" + $label)
        if ($res.Body) { $s = $res.Body; Write-Host $s.Substring(0, [Math]::Min(300, $s.Length)) }
        $script:results += ,@($Name, ("FALHA_" + $label)); return
    }
}

# --- Google Gemini (wire format proprio) -----------------------------------
function Probe-Gemini([string]$Name, [string]$Base, [string]$Model, [string]$Key) {
    Write-Host ("==> " + $Name + "  [google v1beta]  model=" + $Model + "  key=" + (Mask-Key $Key))
    $url = $Base.TrimEnd('/') + "/v1beta/models/" + $Model + ":generateContent"
    $payload = [ordered]@{
        contents         = @(@{ parts = @(@{ text = "Responda apenas: ok." }) })
        generationConfig = @{ maxOutputTokens = 32; temperature = 0.2 }
    } | ConvertTo-Json -Depth 8
    for ($a = 1; $a -le 3; $a++) {
        $res = Invoke-JsonPost $url "x-goog-api-key" $Key $payload
        $code = $res.Code
        if ($code -eq 200 -and $res.Body -match '"candidates"\s*:') {
            Write-Host ("[OK] " + $Name + " respondeu HTTP 200 com candidato gerado.")
            $script:results += ,@($Name, "OK"); return
        }
        $transient = ($code -ge 500) -or ($code -eq 429) -or ($code -eq -1)
        if ($transient -and $a -lt 3) {
            Write-Host ("  [tentativa $a/3] " + $Name + " http=" + $code + " (transitoria); aguardando e repetindo...")
            Start-Sleep -Seconds 3; continue
        }
        $label = if ($code -eq -1) { "TIMEOUT" } else { [string]$code }
        Write-Host ("[FALHA] " + $Name + " http=" + $label)
        if ($res.Body) { $s = $res.Body; Write-Host $s.Substring(0, [Math]::Min(300, $s.Length)) }
        $script:results += ,@($Name, ("FALHA_" + $label)); return
    }
}

# ---------------------------------------------------------------------------
# Importa .env (raiz do repo e pasta da API), se existir.
Import-DotEnv (Join-Path $PSScriptRoot "..\.env")
Import-DotEnv (Join-Path $PSScriptRoot "..\backend\PlantaoPro.Api\.env")

$groqKey   = $env:Ai__Providers__Groq__ApiKey
$geminiKey = $env:Ai__Providers__Gemini__ApiKey
$dsKey     = $env:Ai__Providers__DeepSeek__ApiKey

if (-not $groqKey)   { $groqKey   = $env:AI_GROQ_KEY }
if (-not $geminiKey) { $geminiKey = $env:AI_GEMINI_KEY }
if (-not $dsKey)     { $dsKey     = $env:AI_DEEPSEEK_KEY }

# Modelos vigentes (fonte: AiGateway.cs; confirmados por chamada real em 2026-10-07).
# Groq expoe o id com prefixo openai/; Gemini usa o alias estavel flash-latest.
$groqModel   = if ($env:AI_GROQ_MODEL)     { $env:AI_GROQ_MODEL }     else { "openai/gpt-oss-20b" }
$geminiModel = if ($env:AI_GEMINI_MODEL)   { $env:AI_GEMINI_MODEL }   else { "gemini-flash-latest" }
$dsModel     = if ($env:AI_DEEPSEEK_MODEL) { $env:AI_DEEPSEEK_MODEL } else { "deepseek-flash" }

Write-Host "=============================================================="
Write-Host "PlantaoPro - Probe de homologacao externa (B10 IA)"
Write-Host ("  Groq     key=" + (Mask-Key $groqKey))
Write-Host ("  Gemini   key=" + (Mask-Key $geminiKey))
Write-Host ("  DeepSeek key=" + (Mask-Key $dsKey))
Write-Host "=============================================================="
Write-Host ""

if ($groqKey)   { Probe-OpenAiCompat "GROQ"     "https://api.groq.com/openai/v1/"          $groqModel   $groqKey }
else { Write-Host "==> GROQ     [SEM_CHAVE]  definir Ai__Providers__Groq__ApiKey" }

if ($geminiKey) { Probe-Gemini "GEMINI"  "https://generativelanguage.googleapis.com/" $geminiModel $geminiKey }
else { Write-Host "==> GEMINI   [SEM_CHAVE]  definir Ai__Providers__Gemini__ApiKey" }

if ($dsKey)     { Probe-OpenAiCompat "DEEPSEEK" "https://api.deepseek.com/"               $dsModel     $dsKey }
else { Write-Host "==> DEEPSEEK [SEM_CHAVE]  definir Ai__Providers__DeepSeek__ApiKey" }

if (-not $groqKey -and -not $geminiKey -and -not $dsKey) {
    Write-Host ""
    Write-Host "[INFO] Nenhuma chave presente. Estado esperado: tudo BLOQUEADO p/ chave."
}

Write-Host ""
Write-Host "--------------------------------------"
Write-Host "Veredito:"
foreach ($x in $script:results) { Write-Host ("  " + $x[0].PadRight(9) + " : " + $x[1]) }
$ready = @($script:results | Where-Object { $_[1] -eq "OK" } | ForEach-Object { $_[0] })
if ($ready.Count -gt 0) {
    Write-Host ("PROVEDORES COM HOMOLOGACAO EXTERNA (chave real + resposta real): " + ($ready -join ", "))
    exit 0
} else {
    Write-Host "NENHUM provedor homologado externamente (falta chave real). Estado: BLOQUEADO p/ chave."
    exit 2
}
