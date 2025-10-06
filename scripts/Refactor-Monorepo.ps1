<#
.SYNOPSIS
Extracts the RunnerTasks library from the monorepo into its own repo, preserving git history using git subtree.

.PARAMETER SourceRoot
The root path of the monorepo.

.PARAMETER OutputRepo
The directory where the new repo will be created.

.PARAMETER DryRun
If set, prints actions without performing them.

.PARAMETER Verbose
If set, prints detailed progress.
#>

param(
    [string]$SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$OutputRepo = (Split-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path -Parent) + "\dotnet-gha-runner-tasks",
    [switch]$DryRun,
    [switch]$Verbose
)

# ---------------------------
# Library config
# ---------------------------
$LibPaths = @("src\RunnerTasks","tests\RunnerTasks.Tests")
$LibRepoPath = $OutputRepo

# Rename map to determine final location in the new repo
$RenameMap = @{
    "src/RunnerTasks"         = "src/GitHub.RunnerTasks"
    "tests/RunnerTasks.Tests" = "tests/GitHub.RunnerTasks.Tests"
}

# ---------------------------
# Helpers
# ---------------------------
function Test-Tool($name, $args="--version") {
    try { 
        Start-Process -FilePath $name -ArgumentList $args -NoNewWindow -PassThru -Wait `
            -RedirectStandardOutput "$env:TEMP\tool_$name.out" -RedirectStandardError "$env:TEMP\tool_$name.err" | Out-Null
        return $true
    } catch { return $false }
}

function Ensure-Tool($name, $friendly) { 
    if (-not (Test-Tool $name)) { throw "$friendly is required but not found on PATH." } 
}

function Safe-RemoveDir([string]$path) {
    if (Test-Path $path) { 
        Write-Warning "$path exists — removing."; 
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue -Path $path
    }
}

function Resolve-ExistingPaths([string[]]$candidates, [string]$root) {
    $existing = @()
    foreach ($pattern in $candidates) {
        $fullPath = Join-Path $root $pattern
        if (Test-Path $fullPath) { $existing += $pattern -replace '\\','/' }
        else { Write-Warning "Path '$fullPath' does not exist; skipping." }
    }
    return $existing | Sort-Object -Unique
}

# ---------------------------
# Prerequisites
# ---------------------------
Ensure-Tool "git"
Ensure-Tool "dotnet" "dotnet CLI"

# ---------------------------
# Confirm deletion if output repo exists
# ---------------------------
if ((Test-Path $LibRepoPath) -and (-not $DryRun)) {
    $confirmDel = Read-Host "Output repo '$LibRepoPath' exists. Proceed and delete it? (y/N)"
    if ($confirmDel.ToLower() -ne "y") { Write-Host "Aborted."; exit 1 }
}

# ---------------------------
# Resolve library paths
# ---------------------------
$resolvedLib = Resolve-ExistingPaths $LibPaths $SourceRoot
Write-Host "Library paths to extract:"
$resolvedLib | ForEach-Object { Write-Host "  - $_" }

# ---------------------------
# Confirm before destructive action
# ---------------------------
if (-not $DryRun) {
    $confirm = Read-Host "Proceed with extracting dotnet-gha-runner-tasks? (y/N)"
    if ($confirm.ToLower() -ne "y") { Write-Host "Aborted."; exit 1 }
}

# ---------------------------
# Tag monorepo before split
# ---------------------------
if (-not $DryRun) {
    Push-Location $SourceRoot
    try { 
        git tag -a "split-runnertasks-before-$(Get-Date -Format 'yyyyMMdd')" -m "Snapshot before extracting RunnerTasks" 2>$null
        Write-Host "✅ Created pre-split tag"
    } finally { Pop-Location }
} else {
    Write-Host "[DryRun] Would create pre-split tag"
}

# ---------------------------
# Create new repo
# ---------------------------
if (-not $DryRun) {
    Safe-RemoveDir $LibRepoPath
    New-Item -ItemType Directory -Force -Path $LibRepoPath | Out-Null
    Push-Location $LibRepoPath
    try { git init | Out-Null } finally { Pop-Location }
} else {
    Write-Host "[DryRun] Would initialize new repo at $LibRepoPath"
}

# ---------------------------
# Extract each library path with prefix
# ---------------------------
foreach ($kv in $RenameMap.GetEnumerator()) {
    $sourcePath = $kv.Key
    $targetPrefix = $kv.Value

    $tmpBranch = "split-" + ($sourcePath -replace "[\\/]", "-")
    if ($DryRun) {
        Write-Host "[DryRun] Would create subtree split for '$sourcePath' → branch '$tmpBranch'"
        Write-Host "[DryRun] Would pull branch '$tmpBranch' into new repo with prefix '$targetPrefix/'"
        continue
    }

    Push-Location $SourceRoot
    try {
        Write-Host "Creating subtree split branch for '$sourcePath' → '$tmpBranch'..."
        git subtree split -P $sourcePath -b $tmpBranch | Out-Null
    } finally { Pop-Location }

    Push-Location $LibRepoPath
    try {
        Write-Host "Pulling subtree branch '$tmpBranch' into new repo with prefix '$targetPrefix/'..."
        git pull $SourceRoot $tmpBranch --allow-unrelated-histories --prefix "$targetPrefix/" | Out-Null
    } finally { Pop-Location }

    # Delete temporary branch in monorepo
    Push-Location $SourceRoot
    try { git branch -D $tmpBranch | Out-Null } finally { Pop-Location }
}

Write-Host "✅ dotnet-gha-runner-tasks extraction complete."
