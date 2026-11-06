[CmdletBinding()]
param(
    [string]$Remote = "origin"
)

$ErrorActionPreference = "Stop"

$currentBranch = git branch --show-current
Write-Host "=== Synchronizing branch '$currentBranch' with $Remote ===" -ForegroundColor Cyan

$remoteUrl = git remote get-url $Remote 2>$null
if (-not $remoteUrl) {
    Write-Warning "No remote named '$Remote' is configured."
    exit 1
}

# Fetch safely
Write-Host "Fetching remote changes..." -ForegroundColor Yellow
git fetch $Remote

# Check if there are local uncommitted changes
$uncommitted = git status --porcelain
if ($uncommitted) {
    Write-Warning "You have uncommitted changes. Stash or commit before synchronizing."
    exit 1
}

# Pull with fast-forward only by default to avoid accidental merge chaos
git pull --ff-only $Remote $currentBranch
if ($LASTEXITCODE -eq 0) {
    Write-Host "Branch successfully synchronized." -ForegroundColor Green
} else {
    Write-Error "Sync could not fast-forward. Manual merge or rebase required."
    exit 1
}
