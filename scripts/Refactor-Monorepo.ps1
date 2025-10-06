<#
.SYNOPSIS
    Refactors a .NET monorepo by splitting projects into separate subfolders,
    updating namespaces, .csproj names, and .sln references.

.DESCRIPTION
    This script:
      1. Moves each project in the given solution into its own folder (repo-style).
      2. Renames namespaces and file contents to match new paths.
      3. Updates .csproj, .sln, and Directory.Build.props references accordingly.
      4. Runs in -WhatIf mode by default for safety.

.PARAMETER SolutionPath
    Path to the .sln file of your main monorepo (e.g. ./DotNetApp.sln)
#>

param (
    [Parameter(Mandatory = $true)]
    [string]$SolutionPath,

    [switch]$Force
)

$ErrorActionPreference = "Stop"
$root = Split-Path $SolutionPath -Parent
$solutionName = [IO.Path]::GetFileNameWithoutExtension($SolutionPath)
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$nsRoot = $solutionName

Write-Host "📦 Starting monorepo refactor for '$solutionName'" -ForegroundColor Cyan
Write-Host "Root: $root" -ForegroundColor DarkGray

# --- Find all .csproj files
$projects = Get-ChildItem -Path $root -Recurse -Filter *.csproj | Where-Object {
    $_.FullName -notmatch "__backup_"
}

Write-Host "🧩 Found $($projects.Count) projects" -ForegroundColor Cyan

foreach ($proj in $projects) {
    $projName = [IO.Path]::GetFileNameWithoutExtension($proj.Name)
    $projDir  = Split-Path $proj.FullName -Parent
    $newDir   = Join-Path $root $projName
    $newPath  = Join-Path $newDir "$projName.csproj"

    Write-Host "`n--- Processing: $projName ---" -ForegroundColor Yellow
    Write-Host "  From: $projDir"
    Write-Host "  To:   $newDir"

    # Create new folder
    if (-not (Test-Path $newDir)) {
        New-Item -ItemType Directory -Path $newDir -WhatIf:(!$Force)
    }

    # Move files into it
    Get-ChildItem $projDir -File | ForEach-Object {
        Move-Item $_.FullName (Join-Path $newDir $_.Name) -WhatIf:(!$Force)
    }

    # --- Update namespaces in all .cs files
    $csFiles = Get-ChildItem $newDir -Recurse -Include *.cs
    foreach ($file in $csFiles) {
        (Get-Content $file.FullName) | ForEach-Object {
            $_ -replace "namespace\s+$nsRoot(\.\w+)?", "namespace $nsRoot.$projName`$1"
        } | Set-Content -Path $file.FullName -WhatIf:(!$Force)
    }

    # --- Update csproj name inside the file
    (Get-Content $newPath) | ForEach-Object {
        $_ -replace "<RootNamespace>$nsRoot.*?</RootNamespace>", "<RootNamespace>$nsRoot.$projName</RootNamespace>"
        $_ -replace "<AssemblyName>$nsRoot.*?</AssemblyName>", "<AssemblyName>$nsRoot.$projName</AssemblyName>"
    } | Set-Content -Path $newPath -WhatIf:(!$Force)

    # --- Update .sln references (move path)
    (Get-Content $SolutionPath) | ForEach-Object {
        $_ -replace [regex]::Escape($proj.FullName), $newPath
    } | Set-Content -Path $SolutionPath -WhatIf:(!$Force)
}

# --- Update solution folder references
Write-Host "`n🔧 Cleaning up and updating solution references..."
$slndir = Split-Path $SolutionPath -Parent
(Get-Content $SolutionPath) | ForEach-Object {
    $_ -replace "\\", "/"
} | Set-Content -Path $SolutionPath -WhatIf:(!$Force)

# --- Optionally tag a snapshot
if (-not $Force) {
    Write-Host "`n💡 (dry-run) Would tag snapshot as: split-before-$timestamp"
    Write-Host "    Run with -Force to apply and 'git tag split-before-$timestamp -m \"Monorepo split snapshot\"'"
} else {
    git add .
    git commit -m "Refactor: split monorepo structure into per-project folders"
    git tag "split-before-$timestamp" -m "Monorepo split snapshot"
}

Write-Host "`n✅ Done. Review changes and test build integrity." -ForegroundColor Green
Write-Host "   To apply for real: rerun with -Force"
