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
$LibPaths = @("src\RunnerTasks", "tests\RunnerTasks.Tests")
$LibRepoPath = $OutputRepo

# Map original paths → target folder in new repo
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
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue 
    }
}

function Resolve-ExistingPaths([string[]]$candidates, [string]$root) {
    $existing = @()
    foreach ($pattern in $candidates) {
        $fullPath = Join-Path $root $pattern
        if (Test-Path $fullPath) {
            $existing += $pattern -replace '\\','/'  # relative path
        } else {
            Write-Warning "Path '$fullPath' does not exist; skipping."
        }
    }
    $existing | Sort-Object -Unique
}

function Create-SplitBranch([string]$sourceRoot, [string]$path) {
    $branchName = "split-" + ($path -replace "[\\/]", "-")
    if ($Verbose) { Write-Host "Creating subtree split branch for '$path' → '$branchName'..." }

    Push-Location $sourceRoot
    try {
        git branch -D $branchName 2>$null | Out-Null  # delete if exists
        git subtree split -P $path -b $branchName | Out-Null
    } finally { Pop-Location }
    return $branchName
}

function Extract-To-NewRepo() {
    if ($DryRun) {
        Write-Host "[DryRun] Would create new repo at '$LibRepoPath'"
        foreach ($kv in $RenameMap.GetEnumerator()) {
            Write-Host "[DryRun] Would add subtree '$($kv.Key)' → '$($kv.Value)/'"
        }
        return
    }

    # Remove existing output repo
    if (Test-Path $LibRepoPath) { Safe-RemoveDir $LibRepoPath }
    New-Item -ItemType Directory -Force -Path $LibRepoPath | Out-Null
    Push-Location $LibRepoPath
    try {
        git init | Out-Null

        foreach ($kv in $RenameMap.GetEnumerator()) {
            $sourcePath = $kv.Key
            $targetPrefix = $kv.Value -replace '\\','/'

            $splitBranch = "split-" + ($sourcePath -replace "[\\/]", "-")
            Write-Host "Pulling subtree branch '$splitBranch' into new repo with prefix '$targetPrefix/'..."

            git fetch $SourceRoot $splitBranch | Out-Null
            git merge --allow-unrelated-histories -s ours --no-commit FETCH_HEAD | Out-Null
            git read-tree --prefix=$targetPrefix/ -u FETCH_HEAD | Out-Null
            git commit -m "Add subtree '$sourcePath' → '$targetPrefix/'" | Out-Null
        }
    } finally { Pop-Location }
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
    try { git tag -a "split-runnertasks-before-$(Get-Date -Format 'yyyyMMdd')" -m "Snapshot before extracting RunnerTasks" 2>$null; Write-Host "✅ Created pre-split tag" }
    finally { Pop-Location }
} else {
    Write-Host "[DryRun] Would create pre-split tag"
}

# ---------------------------
# Create split branches
# ---------------------------
foreach ($path in $resolvedLib) {
    Create-SplitBranch -sourceRoot $SourceRoot -path $path
}

# ---------------------------
# Extract into new repo
# ---------------------------
Extract-To-NewRepo

Write-Host "✅ dotnet-gha-runner-tasks extraction complete."
