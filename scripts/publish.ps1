[CmdletBinding()]
param(
    [string]$OutputPath = "./artifacts/publish",
    [switch]$SingleFile = $false,
    [switch]$SelfContained = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== Publishing Production Windows x64 Build ===" -ForegroundColor Cyan
Write-Host "SingleFile: $SingleFile | SelfContained: $SelfContained" -ForegroundColor Gray

$extraArgs = @()
if ($SingleFile) {
    $extraArgs += "-p:PublishSingleFile=true"
    $extraArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
    if ($SelfContained) {
        $extraArgs += "-p:EnableCompressionInSingleFile=true"
    }
}

dotnet publish ./src/RobloxAccountManager.App/RobloxAccountManager.App.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained $SelfContained `
    --output $OutputPath `
    @extraArgs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Application published to $OutputPath" -ForegroundColor Green

