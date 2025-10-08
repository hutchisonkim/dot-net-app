<#
.SYNOPSIS
Extracts the GitHub.Runner library from a monorepo into its own repository, preserving git history.

.PARAMETER SourceRoot
Root path of the monorepo.

.PARAMETER OutputRepo
Directory for the new repository.

.PARAMETER DryRun
Prints actions without performing them.

.PARAMETER Verbose
Prints detailed progress.
#>

param(
    [string]$SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$OutputRepo = (Join-Path (Split-Path $SourceRoot -Parent) "dotnet-gha-runner-tasks"),
    [switch]$DryRun,
    [switch]$Verbose
)

# ---------------------------
# Library config
# ---------------------------
$LibPaths = @("src\GitHub.GitHub.Runner", "tests\GitHub.GitHub.Runner.Tests")
$RenameMap = @{
    "src/GitHub.GitHub.Runner"         = "src/GitHub.GitHub.Runner"
    "tests/GitHub.GitHub.Runner.Tests" = "tests/GitHub.GitHub.Runner.Tests"
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
        Write-Warning "$path exists — removing."
        Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Resolve-ExistingPaths([string[]]$candidates, [string]$root) {
    $existing = @()
    foreach ($p in $candidates) {
        $fullPath = Join-Path $root $p
        if (Test-Path $fullPath) { $existing += $p -replace '\\','/' } 
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
if ((Test-Path $OutputRepo) -and (-not $DryRun)) {
    $confirmDel = Read-Host "Output repo '$OutputRepo' exists. Proceed and delete it? (y/N)"
    if ($confirmDel.ToLower() -ne "y") { Write-Host "Aborted."; exit 1 }
    Safe-RemoveDir $OutputRepo
}

# ---------------------------
# Resolve library paths
# ---------------------------
$resolvedLib = Resolve-ExistingPaths $LibPaths $SourceRoot
Write-Host "Library paths to extract:"
$resolvedLib | ForEach-Object { Write-Host "  - $_" }

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
        $tagName = "split-runnertasks-before-$(Get-Date -Format 'yyyyMMdd')"
        git tag -a $tagName -m "Snapshot before extracting GitHub.Runner" 2>$null
        Write-Host "✅ Created pre-split tag $tagName"
    } finally { Pop-Location }
} else { Write-Host "[DryRun] Would create pre-split tag" }

# ---------------------------
# Create new empty repo with main branch
# ---------------------------
if (-not $DryRun) {
    New-Item -ItemType Directory -Force -Path $OutputRepo | Out-Null
    Push-Location $OutputRepo
    try {
        git init | Out-Null
        git checkout -b main | Out-Null

        # <-- Add initial commit so subtree add works
        New-Item -ItemType File -Name ".gitkeep" | Out-Null
        git add .gitkeep
        git commit -m "Initial commit" | Out-Null
    } finally { Pop-Location }
} else { Write-Host "[DryRun] Would create new repo at $OutputRepo and checkout main" }

# ---------------------------
# Extract each library path using git subtree
# ---------------------------
foreach ($path in $resolvedLib) {
    $branchName = "split-" + ($path -replace "[\\/]", "-")
    $prefix = $RenameMap[$path]

    if ($DryRun) {
        Write-Host "[DryRun] Would create subtree split branch for '$path' → '$branchName'"
        Write-Host "[DryRun] Would add subtree to '$OutputRepo' under '$prefix/'"
        continue
    }

    # Create subtree split branch in monorepo
    Push-Location $SourceRoot
    try {
        Write-Host "Creating subtree split branch for '$path' → '$branchName'..."
        git branch -D $branchName 2>$null
        git subtree split -P $path -b $branchName | Out-Null
    } finally { Pop-Location }

    # Add subtree to output repo
    Push-Location $OutputRepo
    try {
        Write-Host "Ensuring clean working tree..."
        git reset --hard | Out-Null
        git clean -fdx | Out-Null

        Write-Host "Adding subtree branch '$branchName' to new repo under '$prefix/'..."
        git remote add temp-origin $SourceRoot | Out-Null
        git fetch temp-origin $branchName | Out-Null
        git subtree add --prefix=$prefix temp-origin/$branchName -m "Add $path subtree" | Out-Null
        git remote remove temp-origin | Out-Null
    } finally { Pop-Location }
}

Write-Host "✅ dotnet-gha-runner-tasks extraction complete."
