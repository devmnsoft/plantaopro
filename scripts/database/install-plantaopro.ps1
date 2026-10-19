[CmdletBinding()]
param([ValidateSet('CLEAN','UPGRADE','REPAIR','RECREATE_DEVELOPMENT')][string]$Mode='UPGRADE',[string]$Environment='Development',[string]$HostName='localhost',[int]$Port=5432,[string]$MaintenanceDatabase='postgres',[string]$Database='plantaopro',[string]$OwnerRole='plantaopro_owner',[string]$ApplicationRole='plantaopro_app',[string]$AdminEmail='admin.global@plantaopro.local',[switch]$RecreateDatabase,[string]$AppPassword='',[string]$AdminPassword='',[ValidateSet('base','demo')][string]$Profile='base')
$ErrorActionPreference='Stop'; $PSNativeCommandUseErrorActionPreference=$true
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$psql=(Get-Command psql -ErrorAction SilentlyContinue); if(-not $psql){$candidates=Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin\psql.exe' -ErrorAction SilentlyContinue|Sort-Object FullName -Descending; $psql=$candidates|Select-Object -First 1}; if(-not $psql){throw 'psql.exe 16+ não encontrado.'}
$dotnet=Get-Command dotnet -ErrorAction SilentlyContinue; if(-not $dotnet){throw 'dotnet não encontrado.'}
if([int]((& $psql --version)-replace '^.*?([0-9]+)\..*$','$1') -lt 16){throw 'psql 16+ é obrigatório.'}
function Convert-Secure([Security.SecureString]$value){$ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($value);try{[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)}}
if([string]::IsNullOrWhiteSpace($AppPassword)){ $appSecure=Read-Host 'Senha da role da aplicacao' -AsSecureString; $AppPassword=Convert-Secure $appSecure }
$appPassword=$AppPassword
if([string]::IsNullOrWhiteSpace($env:PLANTAOPRO_BOOTSTRAP_PASSWORD)){ if([string]::IsNullOrWhiteSpace($AdminPassword)){ $adminSecure=Read-Host 'Senha inicial do superadministrador' -AsSecureString; $AdminPassword=Convert-Secure $adminSecure }; $env:PLANTAOPRO_BOOTSTRAP_PASSWORD=$AdminPassword }
$script:bootEnvDefault=[string]::IsNullOrWhiteSpace($env:PLANTAOPRO_BOOTSTRAP_ENVIRONMENT); if($script:bootEnvDefault){$env:PLANTAOPRO_BOOTSTRAP_ENVIRONMENT=$Environment}
try {
 $hash=& $dotnet run --project (Join-Path $root 'backend/PlantaoPro.Tools.Bootstrap/PlantaoPro.Tools.Bootstrap.csproj') -- hash-password
 & $psql -X -h $HostName -p $Port -d $MaintenanceDatabase -v ON_ERROR_STOP=1 -v "installation_environment=$Environment" -v "install_mode=$Mode" -v "recreate_database=$($RecreateDatabase.IsPresent.ToString().ToLowerInvariant())" -v "maintenance_database=$MaintenanceDatabase" -v "target_database=$Database" -v "database_owner=$OwnerRole" -v "application_role=$ApplicationRole" -v "application_role_password=$appPassword" -v bootstrap_admin=true -v "bootstrap_admin_email=$AdminEmail" -v "bootstrap_admin_password_hash=$hash" -f (Join-Path $root 'database/instalar_plantaopro.psql')
 if($LASTEXITCODE -ne 0){throw 'Instalação SQL falhou.'}
 if($Profile -eq 'demo'){
  if($Environment -cne 'Development'){throw 'Perfil demo somente e permitido em Development; use -Environment Development.'}
  $seedes=@('121_acesso_demo_local.sql','130_operacao_demo_santacasa.sql','140_administrativo360_demo.sql','141_administrativo360_suprimentos_demo.sql')
  foreach($s in $seedes){ & $psql -X -h $HostName -p $Port -d $Database -v ON_ERROR_STOP=1 -f (Join-Path $root ('database/seeds/development/' + $s)); if($LASTEXITCODE -ne 0){throw ('Seed de demonstracao falhou: ' + $s)} }
  Write-Output ("PlantaoPro - perfil demo aplicado ao banco=$Database com "+$seedes.Count+" seeds idempotentes; contas demo documentadas em database/seeds/development/README.md")
 }
 & $psql -X -h $HostName -p $Port -d $Database -v ON_ERROR_STOP=1 -v "database_owner=$OwnerRole" -v "application_role=$ApplicationRole" -f (Join-Path $root 'database/bootstrap/normalize_owner_grants.sql'); if($LASTEXITCODE -ne 0){throw 'Normalizacao de propriedade e privilegios falhou.'}
 $local=Join-Path $root '.local'; New-Item -ItemType Directory -Force $local|Out-Null; $rb=New-Object byte[] 64; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($rb); $jwt=[Convert]::ToBase64String($rb); $envFile=Join-Path $local 'plantaopro.env'
 $connectionString="ConnectionStrings__Default=Host=$HostName;Port=$Port;Database=$Database;Username=$ApplicationRole;"+
  "Password=$appPassword"
 @($connectionString,'Jwt__Issuer=PlantaoPro','Jwt__Audience=PlantaoPro',"Jwt__Key=$jwt","ASPNETCORE_ENVIRONMENT=$Environment")|Set-Content -Encoding utf8 $envFile
 Write-Output "PlantãoPro — instalação concluída; banco=$Database; servidor=$HostName; porta=$Port; usuário=$ApplicationRole; ambiente=$envFile; status=APROVADO"
} finally {$appPassword=$null;$hash=$null;Remove-Item Env:PLANTAOPRO_BOOTSTRAP_PASSWORD -ErrorAction SilentlyContinue; if($script:bootEnvDefault){ Remove-Item Env:PLANTAOPRO_BOOTSTRAP_ENVIRONMENT -ErrorAction SilentlyContinue } }
