[CmdletBinding()]
param(
    [string]$OutputPath = "./artifacts/publish"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Publishing Production Windows x64 Build ===" -ForegroundColor Cyan
dotnet publish ./src/RobloxAccountManager.App/RobloxAccountManager.App.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output $OutputPath

Write-Host "Application published to $OutputPath" -ForegroundColor Green

