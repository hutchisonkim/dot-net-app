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

# Rename map for clarity in the new repo
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
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $path
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

function Apply-Renames([string]$repoPath) {
    if ($DryRun) {
        Write-Host "[DryRun] Would apply renames in: $repoPath"
        foreach ($kv in $RenameMap.GetEnumerator()) {
            Write-Host "[DryRun] Move contents of '$($kv.Key)' → '$($kv.Value)'"
        }
        return
    }

    Push-Location $repoPath
    try {
        foreach ($kv in $RenameMap.GetEnumerator()) {
            $oldFolder = (Split-Path $kv.Key -Leaf)
            $oldFull = Join-Path $repoPath $oldFolder
            $newFull = Join-Path $repoPath $kv.Value

            if (Test-Path $oldFull) {
                $parent = Split-Path $newFull -Parent
                if ($parent -and -not (Test-Path $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }

                Get-ChildItem -Path $oldFull -Force | Where-Object { $_.Name -notin @('bin','obj') } | 
                    Move-Item -Destination $newFull -Force

                Remove-Item $oldFull -Recurse -Force
            } else {
                Write-Warning "Path '$oldFull' does not exist; skipping."
            }
        }
        git add . | Out-Null
        try { git commit -m "Refactor: move library files into proper src/tests folders" | Out-Null } catch {}
    } finally { Pop-Location }
}

function Extract-With-Subtree([string]$sourceRoot, [string]$repoPath, [string[]]$paths) {
    $branchName = "split-runnertasks"

    if ($DryRun) {
        Write-Host "[DryRun] Would create combined subtree branch '$branchName' from paths:"
        $paths | ForEach-Object { Write-Host "  - $_" }
        Write-Host "[DryRun] Would create new repo at '$repoPath'"
        return
    }

    Push-Location $sourceRoot
    try {
        # Delete existing temp branch if present
        git branch -D $branchName 2>$null

        $tempBranches = @()
        foreach ($path in $paths) {
            $tmpBranch = "split-temp-" + ($path -replace "[\\/]", "-")
            Write-Host "Creating subtree split for '$path' → '$tmpBranch'..."
            git subtree split -P $path -b $tmpBranch | Out-Null
            $tempBranches += $tmpBranch
        }

        # Create orphan branch and merge all temp branches
        git checkout --orphan $branchName | Out-Null
        git reset --hard | Out-Null

        foreach ($tb in $tempBranches) {
            git merge --allow-unrelated-histories $tb -m "Merge $tb into $branchName" | Out-Null
        }

        foreach ($tb in $tempBranches) { git branch -D $tb | Out-Null }
    } finally { Pop-Location }

    # Create new repo and pull combined branch
    if (Test-Path $repoPath) { Safe-RemoveDir $repoPath }
    New-Item -ItemType Directory -Force -Path $repoPath | Out-Null
    Push-Location $repoPath
    try {
        git init | Out-Null
        git pull $sourceRoot $branchName
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
# Execute extraction
# ---------------------------
Extract-With-Subtree -sourceRoot $SourceRoot -repoPath $LibRepoPath -paths $resolvedLib
Apply-Renames -repoPath $LibRepoPath

Write-Host "✅ dotnet-gha-runner-tasks extraction complete."
