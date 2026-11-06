[CmdletBinding()]
param(
    [string]$TargetDir = "."
)

$ErrorActionPreference = "Stop"

Write-Host "=== Scanning for Sensitive Data & Secrets ===" -ForegroundColor Cyan

# Avoid false positive matching of the scanner script itself
$tokenName = '_ROBLO' + 'SECURITY'
$secretPatterns = @(
    $tokenName,
    'password\s*[:=]\s*["''][^"'']+["'']',
    'bearer\s+[a-zA-Z0-9_\-\.]{20,}',
    'ghp_[a-zA-Z0-9]{36}',
    'github_pat_[a-zA-Z0-9_]{82}',
    '-----BEGIN (RSA|EC|OPENSSH|PRIVATE) KEY-----'
)

$stagedFiles = git diff --cached --name-only
if (-not $stagedFiles) {
    # Check untracked and modified if nothing staged
    $stagedFiles = git status --porcelain | ForEach-Object { 
        if ($_.Length -ge 3) { $_.Substring(3).Trim() } 
    }
}

$violationsFound = $false

foreach ($file in $stagedFiles) {
    if (-not $file) { continue }
    if ($file -like "*git-check-secrets.ps1*") { continue }
    if (-not (Test-Path $file)) { continue }
    if ($file -match '\.(png|jpg|ico|exe|dll|db|wal|shm)$') { continue }

    $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    foreach ($pattern in $secretPatterns) {
        if ($content -match $pattern) {
            Write-Error "[SECURITY VIOLATION] Possible secret pattern matched in file '$file' (Pattern: $pattern)"
            $violationsFound = $true
        }
    }
}

if ($violationsFound) {
    Write-Host "`nScan failed: Sensitive data detected. Resolve issues before staging/committing." -ForegroundColor Red
    exit 1
} else {
    Write-Host "[OK] No plaintext secrets or security tokens detected." -ForegroundColor Green
    exit 0
}
