<#
.SYNOPSIS
Extracts the RunnerTasks library from the monorepo into its own repo, preserving git history, renaming paths, and creating a solution.

.PARAMETER SourceRoot
The root path of the monorepo.

.PARAMETER OutputRepo
The target git URL or local path for the new library repo.

.PARAMETER DryRun
If set, prints actions without performing them.
#>

param(
    [string]$SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$OutputRepo = "$SourceRoot/../dotnet-gha-runner-tasks",
    [switch]$DryRun
)

# ---------------------------
# Library config
# ---------------------------
$LibPaths = @("src/RunnerTasks", "tests/RunnerTasks.Tests")
$PathRename = @{
    "src/RunnerTasks/"         = "src/GitHub.RunnerTasks/"
    "tests/RunnerTasks.Tests/" = "tests/GitHub.RunnerTasks.Tests/"
}
$RepoName = "dotnet-gha-runner-tasks"

# ---------------------------
# Helpers
# ---------------------------
function Ensure-Tool($name, $friendly) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "$friendly is required but not found on PATH."
    }
}

function Run-IfNotDry([scriptblock]$action, [string]$msg) {
    if ($DryRun) { Write-Host "[DryRun] $msg" }
    else { & $action }
}

# ---------------------------
# Prerequisites
# ---------------------------
Ensure-Tool "git" "git"
Ensure-Tool "dotnet" "dotnet"

# ---------------------------
# Confirm destructive action
# ---------------------------
if (-not $DryRun) {
    $confirm = Read-Host "This will extract RunnerTasks into a new repo. Proceed? (y/N)"
    if ($confirm.ToLower() -ne "y") { Write-Host "Aborted."; exit 1 }
}

# ---------------------------
# Tag monorepo before split
# ---------------------------
Run-IfNotDry { 
    Push-Location $SourceRoot
    try { git tag -a "split-runnertasks-before-$(Get-Date -Format 'yyyyMMdd')" -m "Snapshot before extracting RunnerTasks" } finally { Pop-Location }
} "Would create pre-split tag in monorepo"

# ---------------------------
# Create temporary clone
# ---------------------------
$tempDir = Join-Path $env:TEMP ("runnertasks-split-" + [guid]::NewGuid().ToString())
Run-IfNotDry { git clone --no-local --quiet $SourceRoot $tempDir } "Would clone monorepo to $tempDir"

# ---------------------------
# Filter repository
# ---------------------------
Push-Location $tempDir
try {
    $filterArgs = @("--force")
    foreach ($p in $LibPaths) { $filterArgs += @("--path", ($p -replace "\\","/")) }
    foreach ($kv in $PathRename.GetEnumerator()) { $filterArgs += @("--path-rename",$kv.Key + "=" + $kv.Value) }

    Run-IfNotDry { git filter-repo @filterArgs } "Would run git filter-repo for paths $($LibPaths -join ', ')"
    Run-IfNotDry { git checkout -B main; git remote remove origin 2>$null } "Would set main branch and remove remote"

    # Initialize solution if missing
    $csprojs = Get-ChildItem -Recurse -Filter *.csproj -ErrorAction SilentlyContinue
    if ($csprojs -and -not (Get-ChildItem -Filter *.sln -ErrorAction SilentlyContinue)) {
        Run-IfNotDry { dotnet new sln -n $RepoName } "Would create solution file $RepoName.sln"
        foreach ($p in $csprojs) { 
            Run-IfNotDry { dotnet sln add $p.FullName } "Would add $($p.FullName) to solution"
        }
    }

    # Commit if needed
    Run-IfNotDry { git add .; git commit -m "Initial import of $RepoName" } "Would commit changes"

    # Push to new repo if specified as remote
    if ($OutputRepo -match "^(git@|https://)") {
        Run-IfNotDry { git remote add origin $OutputRepo; git push -u origin main } "Would push to remote $OutputRepo"
    } else {
        # Local path
        Run-IfNotDry { Move-Item -Force $tempDir $OutputRepo } "Would move filtered repo to $OutputRepo"
    }

} finally { Pop-Location }
if (-not $DryRun -and (Test-Path $tempDir)) { Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue }

Write-Host "✅ dotnet-gha-runner-tasks extraction complete."
