[CmdletBinding()]
param(
    [string]$TargetDir = "."
)

$ErrorActionPreference = "Stop"

Write-Host "=== Scanning for Sensitive Data & Secrets ===" -ForegroundColor Cyan

$repoRoot = (git -C "$PSScriptRoot\.." rev-parse --show-toplevel).Trim()
if (-not $repoRoot) {
    Write-Error "Could not locate the Git repository root."
    exit 1
}

if ([System.IO.Path]::IsPathRooted($TargetDir)) {
    $targetPath = [System.IO.Path]::GetFullPath($TargetDir)
} else {
    $targetPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $TargetDir))
}

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

$stagedFiles = @(git -C $repoRoot diff --cached --name-only)
$repositoryFiles = @(git -C $repoRoot ls-files --cached --others --exclude-standard)
$targetPrefix = $targetPath.TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar
$filesToScan = @($repositoryFiles | Where-Object {
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $_))
    $fullPath.Equals($targetPath, [System.StringComparison]::OrdinalIgnoreCase) -or
        $fullPath.StartsWith($targetPrefix, [System.StringComparison]::OrdinalIgnoreCase)
}) | Sort-Object -Unique

$violationsFound = $false

foreach ($file in $filesToScan) {
    if (-not $file) { continue }
    if ($file -like "*git-check-secrets.ps1*") { continue }
    $filePath = Join-Path $repoRoot $file
    if (-not (Test-Path $filePath)) { continue }
    if ($file -match '\.(png|jpg|ico|exe|dll|db|wal|shm)$') { continue }

    $contents = @()
    $workingTreeContent = Get-Content $filePath -Raw -ErrorAction SilentlyContinue
    if ($workingTreeContent) { $contents += $workingTreeContent }

    if ($stagedFiles -contains $file) {
        $stagedContent = git -C $repoRoot show ":$file" 2>$null
        if ($stagedContent) { $contents += ($stagedContent -join "`n") }
    }

    if ($contents.Count -eq 0) { continue }

    foreach ($pattern in $secretPatterns) {
        foreach ($content in $contents) {
            if ($content -match $pattern) {
                Write-Error "[SECURITY VIOLATION] Possible secret pattern matched in file '$file' (Pattern: $pattern)"
                $violationsFound = $true
                break
            }
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
