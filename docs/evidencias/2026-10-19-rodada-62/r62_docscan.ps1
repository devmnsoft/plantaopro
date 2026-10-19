$moj = [string][char]195
$paths = @()
$paths += (git diff --name-only HEAD)
$paths += (git ls-files --others --exclude-standard)
$bad = @()
$n = 0
foreach ($p in ($paths | Where-Object { $_ -match '\.(md|ps1|cs|cshtml|css|txt|sql|json)$' } | Sort-Object -Unique)) {
    if (Test-Path $p) {
        $n++
        $t = [IO.File]::ReadAllText($p, [Text.Encoding]::UTF8)
        if ($t.IndexOf($moj) -ge 0) { $bad += $p }
    }
}
Write-Output ("ARQ_VERIFICADOS=" + $n)
Write-Output ("DOCSCAN_BAD=" + @($bad).Count)
@($bad) | ForEach-Object { Write-Output ("BAD=" + $_) }
