[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "=== Git Repository Status & Health ===" -ForegroundColor Cyan

# 1. Current Branch
$branch = git branch --show-current
Write-Host "Branch: $branch" -ForegroundColor Yellow

# 2. Remote check
$remotes = git remote -v
if ($remotes) {
    Write-Host "Configured Remotes:`n$remotes" -ForegroundColor Gray
} else {
    Write-Host "Remote: [NONE CONFIGURED]" -ForegroundColor DarkYellow
}

# 3. Last commit
$lastCommit = git log -1 --oneline 2>$null
if ($lastCommit) {
    Write-Host "Latest Commit: $lastCommit" -ForegroundColor Green
} else {
    Write-Host "Latest Commit: None" -ForegroundColor Gray
}

# 4. Status summary
Write-Host "`nWorking Directory State:" -ForegroundColor Cyan
git status --short
