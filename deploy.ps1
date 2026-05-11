$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$clientDir = "$repoRoot\client"
$apiDir = "$repoRoot\src\Services\Fokus\Fokus.API"

$configPath = "$repoRoot\.credentials\deploy-config.ps1"
if (-not (Test-Path $configPath)) { throw "Missing $configPath — copy from .credentials/deploy-config.example.ps1 and fill in your values" }
. $configPath

Write-Host "Building frontend..." -ForegroundColor Cyan
Push-Location $clientDir
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; throw "Frontend build failed" }
Pop-Location

Write-Host "Publishing API..." -ForegroundColor Cyan
Remove-Item -Recurse -Force "$apiDir\publish", "$apiDir\deploy.zip" -ErrorAction SilentlyContinue
dotnet publish "$apiDir" -c Release -o "$apiDir\publish"
if ($LASTEXITCODE -ne 0) { throw "API publish failed" }

Write-Host "Creating zip..." -ForegroundColor Cyan
Push-Location "$apiDir\publish"
tar -a -cf ../deploy.zip *
Pop-Location

Write-Host "Deploying to Azure..." -ForegroundColor Cyan
az webapp deploy --name $AzureWebAppName --resource-group $AzureResourceGroup --src-path "$apiDir\deploy.zip" --type zip --clean true
if ($LASTEXITCODE -ne 0) { throw "Azure deploy failed" }

Write-Host "Done! $AzureUrl" -ForegroundColor Green
