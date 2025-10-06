<#
.SYNOPSIS
Extracts the RunnerTasks library from the monorepo into its own repo, preserving git history.

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
$LibPaths   = @("src\RunnerTasks", "tests\RunnerTasks.Tests")
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

function Resolve-ExistingPaths([string[]]$candidates, [string]$root) {
    $existing = @()
    foreach ($pattern in $candidates) {
        $fullPattern = Join-Path $root $pattern
        $paths = Get-ChildItem -Path $fullPattern -ErrorAction SilentlyContinue -Recurse:$false
        foreach ($p in $paths) { 
            $existing += $p.FullName.Substring($root.Length).TrimStart('\','/').Replace('\','/') 
        }
    }
    $existing | Sort-Object -Unique
}

function Safe-RemoveDir([string]$path) {
    if (Test-Path $path) { Write-Warning "$path exists — removing."; Remove-Item -Recurse -Force $path -ErrorAction SilentlyContinue }
}

function New-FilteredRepo([string]$sourceRoot, [string]$repoPath, [string[]]$pathsToKeep, [string]$repoName) {
    $tempDir = Join-Path $env:TEMP ("runnertasks-split-" + [guid]::NewGuid().ToString())

    if ($DryRun) {
        Write-Host "[DryRun] Would create temp clone at $tempDir"
        Write-Host "[DryRun] Would run git filter-repo for paths:"
        $pathsToKeep | ForEach-Object { Write-Host "  - $_" }
        Write-Host "[DryRun] Would move filtered repo to $repoPath"
        return
    }

    Push-Location $sourceRoot
    try {
        $rootGit = git rev-parse --show-toplevel 2>$null
        if (-not $rootGit) { throw "SourceRoot must be inside a Git repo." }
    } finally { Pop-Location }

    git clone --no-local --quiet "$rootGit" "$tempDir" | Out-Null
    Push-Location $tempDir
    try {
        $args = @("--force")
        foreach ($p in $pathsToKeep) { $args += @("--path",$p) }

        git filter-repo @args
        git checkout -B main | Out-Null
        git remote remove origin 2>$null

        if (Test-Path $repoPath) { Safe-RemoveDir $repoPath }
        New-Item -ItemType Directory -Force -Path $repoPath | Out-Null

        # Move everything except .git
        Get-ChildItem -Force | Where-Object { $_.Name -ne ".git" } | Move-Item -Destination $repoPath -Force

    } finally { 
        Pop-Location
        Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue 
    }
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
            $old = $kv.Key
            $new = $kv.Value
            if (Test-Path $old) {
                $newDir = Split-Path $new -Parent
                if ($newDir -and -not (Test-Path $newDir)) { New-Item -ItemType Directory -Force -Path $newDir | Out-Null }
                git mv -f $old $new | Out-Null
            } else {
                Write-Warning "Path '$old' does not exist after filter-repo; skipping rename."
            }
        }
        git add . | Out-Null
        try { git commit -m "Refactor: renamed folders for clarity" | Out-Null } catch { }
    } finally { Pop-Location }
}

# ---------------------------
# Prerequisites
# ---------------------------
Ensure-Tool "git" "git"
if (-not (Test-Tool "git-filter-repo" "--help")) { throw "git-filter-repo is required but not found on PATH." }

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
Write-Host "Using git filter-repo to preserve history..."
New-FilteredRepo -sourceRoot $SourceRoot -repoPath $LibRepoPath -pathsToKeep $resolvedLib -repoName $LibRepoName
Apply-Renames -repoPath $LibRepoPath

Write-Host "✅ dotnet-gha-runner-tasks extraction complete."
