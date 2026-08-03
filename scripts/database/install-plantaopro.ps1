[CmdletBinding()]
param([ValidateSet('CLEAN','UPGRADE','REPAIR','RECREATE_DEVELOPMENT')][string]$Mode='UPGRADE',[string]$Environment='Development',[string]$HostName='localhost',[int]$Port=5432,[string]$MaintenanceDatabase='postgres',[string]$Database='plantaopro',[string]$OwnerRole='plantaopro_owner',[string]$ApplicationRole='plantaopro_app',[string]$AdminEmail='admin.global@plantaopro.local',[switch]$RecreateDatabase)
$ErrorActionPreference='Stop'; $PSNativeCommandUseErrorActionPreference=$true
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$psql=(Get-Command psql -ErrorAction SilentlyContinue); if(-not $psql){$candidates=Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin\psql.exe' -ErrorAction SilentlyContinue|Sort-Object FullName -Descending; $psql=$candidates|Select-Object -First 1}; if(-not $psql){throw 'psql.exe 16+ não encontrado.'}
$dotnet=Get-Command dotnet -ErrorAction SilentlyContinue; if(-not $dotnet){throw 'dotnet não encontrado.'}
if([int]((& $psql --version)-replace '^.*?([0-9]+)\..*$','$1') -lt 16){throw 'psql 16+ é obrigatório.'}
function Convert-Secure([Security.SecureString]$value){$ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($value);try{[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)}}
$appSecure=Read-Host 'Senha da role da aplicação' -AsSecureString; $adminSecure=Read-Host 'Senha inicial do superadministrador' -AsSecureString
$appPassword=Convert-Secure $appSecure; $env:PLANTAOPRO_BOOTSTRAP_PASSWORD=Convert-Secure $adminSecure
try {
 $hash=& $dotnet run --project (Join-Path $root 'backend/PlantaoPro.Tools.Bootstrap/PlantaoPro.Tools.Bootstrap.csproj') -- hash-password
 & $psql -X -h $HostName -p $Port -d $MaintenanceDatabase -v ON_ERROR_STOP=1 -v "installation_environment=$Environment" -v "install_mode=$Mode" -v "recreate_database=$($RecreateDatabase.IsPresent.ToString().ToLowerInvariant())" -v "maintenance_database=$MaintenanceDatabase" -v "target_database=$Database" -v "database_owner=$OwnerRole" -v "application_role=$ApplicationRole" -v "application_role_password=$appPassword" -v bootstrap_admin=true -v "bootstrap_admin_email=$AdminEmail" -v "bootstrap_admin_password_hash=$hash" -f (Join-Path $root 'database/instalar_plantaopro.psql')
 if($LASTEXITCODE -ne 0){throw 'Instalação SQL falhou.'}
 $local=Join-Path $root '.local'; New-Item -ItemType Directory -Force $local|Out-Null; $jwt=[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64)); $envFile=Join-Path $local 'plantaopro.env'
 @("ConnectionStrings__Default=Host=$HostName;Port=$Port;Database=$Database;Username=$ApplicationRole;Password=$appPassword",'Jwt__Issuer=PlantaoPro','Jwt__Audience=PlantaoPro',"Jwt__Key=$jwt","ASPNETCORE_ENVIRONMENT=$Environment")|Set-Content -Encoding utf8 $envFile
 Write-Output "PlantãoPro — instalação concluída; banco=$Database; servidor=$HostName; porta=$Port; usuário=$ApplicationRole; ambiente=$envFile; status=APROVADO"
} finally {$appPassword=$null;$hash=$null;Remove-Item Env:PLANTAOPRO_BOOTSTRAP_PASSWORD -ErrorAction SilentlyContinue}
