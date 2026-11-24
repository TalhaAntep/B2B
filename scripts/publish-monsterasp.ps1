$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $root 'frontend'
$backend = Join-Path $root 'backend/B2BStore.Settings.Api'
$wwwroot = Join-Path $backend 'wwwroot'
$publish = Join-Path $root 'artifacts/monsterasp-publish'

Write-Host '1/4 - Building Vue frontend...'
Push-Location $frontend
try {
    npm ci
    npm run build
} finally { Pop-Location }

Write-Host '2/4 - Copying frontend into ASP.NET Core wwwroot...'
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot -Force | Out-Null
Copy-Item (Join-Path $frontend 'dist/*') $wwwroot -Recurse -Force

Write-Host '3/4 - Publishing ASP.NET Core (.NET 10)...'
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish (Join-Path $backend 'B2BStore.Settings.Api.csproj') -c Release -o $publish

Write-Host '4/4 - Creating upload ZIP...'
$zip = Join-Path $root 'artifacts/B2BStore-MonsterASP-Publish.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip -CompressionLevel Optimal
Write-Host "DONE: $zip"
