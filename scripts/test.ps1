[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Running Automated Tests ($Configuration) ===" -ForegroundColor Cyan
dotnet test --configuration $Configuration --logger "console;verbosity=normal"
Write-Host "All tests completed." -ForegroundColor Green
