# R67: substitui a assercao do papel de manutencao no r67a pela versao que aceita os dois
# caminhos por design (papel criado quando ha privilegio; AVISO e papel ausente quando nao ha).
$ErrorActionPreference = 'Stop'
$p = 'C:\MNSOFT\plantaopro\docs\evidencias\2026-10-19-rodada-67\r67a_instalador_fim_a_fim.ps1'
$enc = New-Object System.Text.UTF8Encoding($false)
$q = [char]39
$old = [string[]]([IO.File]::ReadAllLines($p, $enc))
if (-not $old[68].StartsWith('$nMant')) { throw ('linha 69 inesperada: ' + $old[68]) }
if (-not $old[69].StartsWith('if ($nMant')) { throw ('linha 70 inesperada: ' + $old[69]) }
$novas = [string[]]@(
    ('$nMant = Q $db ("select count(*) from pg_roles where rolname=' + $q + '" + $mant + "' + $q + "')"),
    ('$avisoCriar = $transcript.Contains(' + $q + 'sem privilegio para criar o papel' + $q + ')'),
    ('if ($avisoCriar -and $nMant -eq ' + $q + '0' + $q + ') { $out += ' + $q + 'PAPEL_MANUTENCAO_CAMINHO_RESTRITO_OK' + $q + ' } elseif (-not $avisoCriar -and $nMant -eq ' + $q + '1' + $q + ') { $out += ' + $q + 'PAPEL_MANUTENCAO_OK' + $q + ' } else { throw (' + $q + 'papel de manutencao inconsistente: n=' + $q + ' + $nMant + ' + $q + ' aviso=' + $q + ' + $avisoCriar) }'),
    ('$sch = ([char]112 + ' + $q + 'lanta' + $q + ' + [char]111 + [char]112 + ' + $q + 'ro' + $q + ')'),
    ('$fnBypass = Q $db (' + $q + 'select ' + $q + ' + $sch + ' + $q + '.fn_adm360_bypass_habilitado()' + $q + ')'),
    ('if ($fnBypass -ne ' + $q + 'f' + $q + ') { throw (' + $q + 'fn_adm360_bypass_habilitado inesperado: ' + $q + ' + $fnBypass) }')
)
$novo = New-Object System.Collections.Generic.List[string]
$novo.AddRange([string[]]$old[0..67])
$novo.AddRange($novas)
$novo.AddRange([string[]]$old[70..($old.Count - 1)])
[IO.File]::WriteAllLines($p, $novo, $enc)
$errs = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($p, [ref]$null, [ref]$errs)
Write-Output ('LINHAS_AGEORA=' + $novo.Count + ' SYNTAX_ERROS=' + $errs.Count)
Write-Output 'PATCH_OK'
