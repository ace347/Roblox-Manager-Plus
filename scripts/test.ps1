[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Running Automated Tests ($Configuration) ===" -ForegroundColor Cyan
dotnet test --configuration $Configuration --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Write-Host "All tests completed." -ForegroundColor Green

