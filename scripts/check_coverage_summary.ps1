# Quick checker for coverage-summary.json
$jPath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent) 'artifacts\coverage-files\coverage-summary.json'
if (-not (Test-Path $jPath)) { Write-Host "coverage-summary.json not found at $jPath"; exit 1 }
$j = Get-Content -Raw -Path $jPath | ConvertFrom-Json
Write-Host "JSON entries: $($j.Count)"
$xmlFiles = Get-ChildItem -Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent) 'artifacts\coverage-files') -Recurse -Filter 'coverage.cobertura.xml' | Select-Object -ExpandProperty FullName
Write-Host "XML files found under artifacts/coverage-files: $($xmlFiles.Count)"
# check dest existence from JSON
$missing = @()
foreach ($e in $j) { if (-not (Test-Path $e.dest)) { $missing += $e.dest } }
if ($missing.Count -eq 0) { Write-Host 'All dest files exist' } else { Write-Host 'Missing dest files:'; $missing | ForEach-Object { Write-Host " - $_" } }
# any non-zero lineRatePercent
$nonZero = $j | Where-Object { $_.lineRatePercent -ne 0 }
Write-Host "Entries with non-zero lineRatePercent: $($nonZero.Count)"
# inspect first few XML root attributes of the first few sources
$toInspect = $j | Select-Object -First 3
foreach ($entry in $toInspect) {
    Write-Host "\nInspecting: $($entry.source)"
    if (-not (Test-Path $entry.source)) { Write-Host '  Source file missing' ; continue }
    try {
        $x = [xml](Get-Content -Path $entry.source -Raw)
        if ($x -and $x.coverage) {
            $attrs = $x.coverage.Attributes
            foreach ($a in $attrs) { Write-Host "  - $($a.Name) = $($a.Value)" }
        } else {
            Write-Host '  No <coverage> root element found'
        }
    } catch {
        Write-Host '  Failed to parse XML:' $_.Exception.Message
    }
}

Write-Host "\nScript exit: OK"