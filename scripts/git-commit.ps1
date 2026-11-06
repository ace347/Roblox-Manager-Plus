[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("feat", "fix", "security", "refactor", "perf", "docs", "test", "build", "chore", "ci")]
    [string]$Type,

    [Parameter(Mandatory=$true)]
    [string]$Scope,

    [Parameter(Mandatory=$true)]
    [string]$Description
)

$ErrorActionPreference = "Stop"

# 1. Pre-commit secrets check
Write-Host "Running pre-commit secrets check..." -ForegroundColor Cyan
& "$PSScriptRoot\git-check-secrets.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Secrets check failed. Aborting commit."
    exit 1
}

# 2. Format commit message
$commitMessage = "${Type}(${Scope}): ${Description}"
Write-Host "`nPreparing commit: $commitMessage" -ForegroundColor Yellow

# 3. Stage changes
git add -A

# 4. Check if there are staged changes
$status = git status --porcelain
if (-not $status) {
    Write-Host "No changes to commit. Working tree clean." -ForegroundColor Gray
    exit 0
}

# 5. Commit
git commit -m $commitMessage
if ($LASTEXITCODE -eq 0) {
    Write-Host "Commit created successfully." -ForegroundColor Green
    git log -1 --stat
} else {
    Write-Error "Commit failed."
    exit 1
}
