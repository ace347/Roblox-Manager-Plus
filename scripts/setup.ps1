[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "=== Setting up Secure Roblox Account Manager Environment ===" -ForegroundColor Cyan

# 1. Check Git
try {
    $gitVersion = git --version
    Write-Host "[OK] $gitVersion" -ForegroundColor Green
} catch {
    Write-Warning "[MISSING] Git is not installed or not in PATH."
}

# 2. Check .NET SDK
try {
    $dotnetSdks = dotnet --list-sdks
    if ($dotnetSdks) {
        Write-Host "[OK] .NET SDK(s) detected:`n$dotnetSdks" -ForegroundColor Green
    } else {
        Write-Warning "[ACTION REQUIRED] .NET SDK not detected. Please install .NET 10 SDK via: winget install Microsoft.DotNet.SDK.10"
    }
} catch {
    Write-Warning "[MISSING] dotnet CLI not found."
}

Write-Host "`nEnvironment verification finished." -ForegroundColor Cyan
