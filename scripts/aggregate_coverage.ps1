<#
# Aggregates all cobertura coverage XML files under the repo for CI ingestion.
# Usage:
#   pwsh .\scripts\aggregate_coverage.ps1
#
# This script will:
# - Find all coverage.cobertura.xml files produced by tests
# - Copy each into artifacts/coverage-files/<sanitized-project-folder>/coverage.cobertura.xml
# - Produce artifacts/coverage-files/coverage-summary.json containing per-file line-rate (if present)
#
# The resulting artifacts are suitable for publishing in CI:
# - Azure Pipelines: use PublishCodeCoverageResults@1 with the captured cobertura files
# - GitHub Actions: upload the artifacts using actions/upload-artifact and consume in downstream steps
#>

Set-StrictMode -Version Latest

$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
 $coverageFiles = @(Get-ChildItem -Path $root -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
if (-not $coverageFiles) {
    Write-Host "No coverage.cobertura.xml files found. Run tests with coverlet first (e.g. dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura)" -ForegroundColor Yellow
    exit 0
}

Write-Host "Found coverage files:`n" -NoNewline
$coverageFiles | ForEach-Object { Write-Host " - $_" }
# Prepare an artifacts folder containing the raw cobertura XMLs for CI

$reportDir = Join-Path $root "artifacts\coverage-files"
# Remove previous artifacts directory if present. We intentionally recreate it.
if (Test-Path $reportDir) { Remove-Item $reportDir -Recurse -Force }
New-Item -ItemType Directory -Path $reportDir | Out-Null

# Filter out any coverage files that are already inside the destination folder (this can happen when re-running the script)
$coverageFiles = $coverageFiles | Where-Object { -not ($_.StartsWith($reportDir, [System.StringComparison]::OrdinalIgnoreCase)) } | Select-Object -Unique

if (-not $coverageFiles) {
  Write-Host "No coverage files found after filtering out artifacts folder. Nothing to copy." -ForegroundColor Yellow
  exit 0
}

$summary = @()

foreach ($file in $coverageFiles) {
  # Build a safe folder name from the source file path to avoid collisions
  $projectDir = Split-Path -Parent $file
  $rel = $projectDir.Substring($root.Length).TrimStart('\','/')
  if ([string]::IsNullOrWhiteSpace($rel)) { $rel = 'root' }
  # sanitize relative path into a folder name
  $safeName = ($rel -replace "[:\\/\s]+", '_' ) -replace '[^A-Za-z0-9_\-\.]','_' 
  $destFolder = Join-Path $reportDir $safeName
  if (-not (Test-Path $destFolder)) { New-Item -ItemType Directory -Path $destFolder | Out-Null }

  $destPath = Join-Path $destFolder (Split-Path -Leaf $file)
  Copy-Item -Path $file -Destination $destPath -Force

  # try to read cobertura line-rate attribute if present
  $lineRatePercent = $null
  $branchRatePercent = $null
  try {
    $xml = [xml](Get-Content -Path $file -Raw)
    if ($xml -and $xml.coverage) {
      # attribute is usually named 'line-rate' in cobertura; use GetAttribute which is more reliable
      $attr = $null
      try { $attr = $xml.coverage.GetAttribute('line-rate') } catch { }
      if (-not $attr) { $attr = $xml.coverage.'@line-rate' }
      try {
        if ($attr -ne $null -and $attr -ne '') {
          $val = [double]$attr
          $lineRatePercent = [math]::Round($val * 100, 2)
        }
      } catch {
        # ignore parse/cast errors
      }
      # branch-rate attribute
      try {
        $bAttr = $null
        try { $bAttr = $xml.coverage.GetAttribute('branch-rate') } catch { }
        if (-not $bAttr) { $bAttr = $xml.coverage.'@branch-rate' }
        if ($bAttr -ne $null -and $bAttr -ne '') {
          $bVal = [double]$bAttr
          $branchRatePercent = [math]::Round($bVal * 100, 2)
        }
      } catch { }
      # numeric counts
      try {
        $linesCovered = 0
        $linesValid = 0
        $branchesCovered = 0
        $branchesValid = 0
        $lc = $xml.coverage.GetAttribute('lines-covered')
        if (-not $lc) { $lc = $xml.coverage.'@lines-covered' }
        if ($lc) { $linesCovered = [int]$lc }
        $lv = $xml.coverage.GetAttribute('lines-valid')
        if (-not $lv) { $lv = $xml.coverage.'@lines-valid' }
        if ($lv) { $linesValid = [int]$lv }
        $bc = $xml.coverage.GetAttribute('branches-covered')
        if (-not $bc) { $bc = $xml.coverage.'@branches-covered' }
        if ($bc) { $branchesCovered = [int]$bc }
        $bv = $xml.coverage.GetAttribute('branches-valid')
        if (-not $bv) { $bv = $xml.coverage.'@branches-valid' }
        if ($bv) { $branchesValid = [int]$bv }
      } catch { }
    }
  } catch {
    # ignore parse errors
  }

  # compute method coverage: percentage of methods with line-rate > 0
  $methodRatePercent = $null
  $methodsCovered = 0
  $methodsValid = 0
  try {
    if ($xml) {
      $methods = @($xml.SelectNodes('//method'))
      if ($methods -and $methods.Count -gt 0) {
        $totalMethods = $methods.Count
        $covered = 0
        foreach ($m in $methods) {
          $mAttr = $null
          try { $mAttr = $m.GetAttribute('line-rate') } catch { }
          if (-not $mAttr) { $mAttr = $m.'@line-rate' }
          try {
            if ($mAttr -ne $null -and $mAttr -ne '') {
              if ([double]$mAttr -gt 0) { $covered++ }
            }
          } catch { }
        }
        $methodRatePercent = [math]::Round((($covered / $totalMethods) * 100), 2)
        $methodsCovered = $covered
        $methodsValid = $totalMethods
      } else {
        $methodRatePercent = 0
        $methodsCovered = 0
        $methodsValid = 0
      }
    }
  } catch {
    $methodRatePercent = 0
    $methodsCovered = 0
    $methodsValid = 0
  }

  $summary += [pscustomobject]@{
    source = $file
    dest = $destPath
    # ensure a numeric value (0) when line-rate is not present to make downstream tooling simpler
    lineRatePercent = if ($lineRatePercent -ne $null) { $lineRatePercent } else { 0 }
    branchRatePercent = if ($branchRatePercent -ne $null) { $branchRatePercent } else { 0 }
    methodRatePercent = if ($methodRatePercent -ne $null) { $methodRatePercent } else { 0 }
    linesCovered = if ($linesCovered) { $linesCovered } else { 0 }
    linesValid = if ($linesValid) { $linesValid } else { 0 }
    branchesCovered = if ($branchesCovered) { $branchesCovered } else { 0 }
    branchesValid = if ($branchesValid) { $branchesValid } else { 0 }
    methodsCovered = if ($methodsCovered) { $methodsCovered } else { 0 }
    methodsValid = if ($methodsValid) { $methodsValid } else { 0 }
  }
}

# Save a JSON summary to the artifacts folder
$summaryPath = Join-Path $reportDir 'coverage-summary.json'
$summaryObj = [pscustomobject]@{
  files = $summary
  averages = @{ }
}

# compute averages across files
$count = $summary.Count
if ($count -gt 0) {
  $avgLines = ([math]::Round((($summary | Measure-Object -Property lineRatePercent -Sum).Sum / $count), 2))
  $avgBranches = ([math]::Round((($summary | Measure-Object -Property branchRatePercent -Sum).Sum / $count), 2))
  $avgMethods = ([math]::Round((($summary | Measure-Object -Property methodRatePercent -Sum).Sum / $count), 2))
} else {
  $avgLines = 0; $avgBranches = 0; $avgMethods = 0
}
$summaryObj.averages = [pscustomobject]@{
  lineRatePercent = $avgLines
  branchRatePercent = $avgBranches
  methodRatePercent = $avgMethods
}

$summaryObj | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host "Copied $($summary.Count) coverage file(s) to: $reportDir"
Write-Host "Summary written to: $summaryPath"
