[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Building Solution ($Configuration) ===" -ForegroundColor Cyan
dotnet build --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Write-Host "Build finished successfully." -ForegroundColor Green

