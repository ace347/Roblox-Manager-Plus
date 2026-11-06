[CmdletBinding()]
param(
    [string]$Remote = "origin",
    [string]$Branch = ""
)

$ErrorActionPreference = "Stop"

if (-not $Branch) {
    $Branch = git branch --show-current
}

Write-Host "=== Pushing to Remote ===" -ForegroundColor Cyan

# Verify remote exists
$remoteUrl = git remote get-url $Remote 2>$null
if (-not $remoteUrl) {
    Write-Warning "No remote named '$Remote' is configured. Run: git remote add origin <your-repo-url>"
    exit 1
}

Write-Host "Target: $Remote / $Branch" -ForegroundColor Yellow

# Safe push (never force push)
git push -u $Remote $Branch
if ($LASTEXITCODE -eq 0) {
    Write-Host "Pushed successfully to $Remote/$Branch" -ForegroundColor Green
} else {
    Write-Error "Push failed. Local changes preserved. Please check network/credentials."
    exit 1
}
