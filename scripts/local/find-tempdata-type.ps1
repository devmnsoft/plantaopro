$p = Get-ChildItem 'C:\Program Files\dotnet\packs\Microsoft.AspNetCore.App.Ref' -Recurse -Filter 'Microsoft.AspNetCore.Mvc.ViewFeatures.xml' -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
if ($null -eq $p) { Write-Output 'XML NAO ENCONTRADO'; exit 1 }
Write-Output $p.FullName
Select-String -Path $p.FullName -Pattern 'TempData' | ForEach-Object { $_.Line.Trim() }
