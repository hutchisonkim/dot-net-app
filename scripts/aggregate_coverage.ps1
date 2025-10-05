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
 $coverageFiles = Get-ChildItem -Path $root -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
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
  try {
    $xml = [xml](Get-Content -Path $file -Raw)
    if ($xml -and $xml.coverage) {
      # attribute is usually named 'line-rate' in cobertura
      $attr = $xml.coverage.'@line-rate'
      if ($attr) { $lineRatePercent = [math]::Round([double]$attr * 100, 2) }
    }
  } catch {
    # ignore parse errors
  }

  $summary += [pscustomobject]@{
    source = $file
    dest = $destPath
    lineRatePercent = if ($lineRatePercent -ne $null) { $lineRatePercent } else { 'n/a' }
  }
}

# Save a JSON summary to the artifacts folder
$summaryPath = Join-Path $reportDir 'coverage-summary.json'
$summary | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host "Copied $($coverageFiles.Count) coverage file(s) to: $reportDir"
Write-Host "Summary written to: $summaryPath"

$ciExample = @'
Next steps (examples):
- Azure Pipelines: use PublishCodeCoverageResults@1 to publish cobertura results. Example:

- task: PublishCodeCoverageResults@1
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '$(System.DefaultWorkingDirectory)/artifacts/coverage-files/**/coverage.cobertura.xml'
    reportDirectory: '$(Build.ArtifactStagingDirectory)/coverage-report'

- GitHub Actions: upload artifacts and process them in subsequent jobs (no vendor HTML required).
'@

Write-Host $ciExample
