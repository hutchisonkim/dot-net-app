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
$LibRepoName = "dotnet-gha-runner-tasks"
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
        Remove-Item -Recurse -Force $path -ErrorAction SilentlyContinue 
    }
}

function Resolve-ExistingPaths([string[]]$candidates, [string]$root) {
    $existing = @()
    foreach ($pattern in $candidates) {
        $fullPath = Join-Path $root $pattern
        if (Test-Path $fullPath) {
            $existing += $pattern -replace '\\','/'  # return relative path from root
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
            Write-Host "[DryRun] Would rename '$($kv.Key)' → '$($kv.Value)'"
        }
        return
    }

    if (-not (Test-Path $repoPath)) {
        Write-Warning "Repo path '$repoPath' does not exist; skipping rename step."
        return
    }

    Push-Location $repoPath
    try {
        foreach ($kv in $RenameMap.GetEnumerator()) {
            $old = $kv.Key -replace "\\","/"
            $new = $kv.Value -replace "\\","/"
            if (Test-Path $old) {
                $newDir = Split-Path $new -Parent
                if ($newDir -and -not (Test-Path $newDir)) { New-Item -ItemType Directory -Force -Path $newDir | Out-Null }
                git mv -f $old $new | Out-Null
            } else {
                Write-Warning "Path '$old' does not exist; skipping rename."
            }
        }
        git add . | Out-Null
        try { git commit -m "Refactor: renamed folders for clarity" | Out-Null } catch { }
    } finally { Pop-Location }
}

function Extract-With-Subtree([string]$sourceRoot, [string]$repoPath, [string[]]$paths) {
    $branchName = "split-runnertasks"

    if ($DryRun) {
        Write-Host "[DryRun] Would create subtree branch '$branchName' for paths:"
        $paths | ForEach-Object { Write-Host "  - $_" }
        Write-Host "[DryRun] Would create new repo at '$repoPath'"
        Write-Host "[DryRun] Would pull subtree branch into new repo"
        return
    }

    Push-Location $sourceRoot
    try {
        Write-Host "Creating subtree branch '$branchName'..."
        # Merge all library paths into one temporary branch
        $tempBranches = @()
        foreach ($path in $paths) {
            $tmpBranch = "split-temp-" + ($path -replace "[\\/]", "-")
            git subtree split -P $path -b $tmpBranch | Out-Null
            $tempBranches += $tmpBranch
        }

        git checkout --orphan $branchName | Out-Null
        git reset --hard | Out-Null

        foreach ($tb in $tempBranches) {
            git merge --allow-unrelated-histories $tb -m "Merge $tb into $branchName" | Out-Null
        }

        foreach ($tb in $tempBranches) { git branch -D $tb | Out-Null }
    } finally { Pop-Location }

    # Create new repo
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
