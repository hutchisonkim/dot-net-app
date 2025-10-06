# Quick checker for coverage-summary.json
 $jPath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent) 'artifacts\coverage-files\coverage-summary.json'
 if (-not (Test-Path $jPath)) { Write-Host "coverage-summary.json not found at $jPath"; exit 1 }
 $jObj = Get-Content -Raw -Path $jPath | ConvertFrom-Json
 # new shape: { files: [...], averages: { ... } }
 $j = $jObj.files
 Write-Host "JSON entries: $($j.Count)"
 $xmlFiles = Get-ChildItem -Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent) 'artifacts\coverage-files') -Recurse -Filter 'coverage.cobertura.xml' | Select-Object -ExpandProperty FullName
Write-Host "XML files found under artifacts/coverage-files: $($xmlFiles.Count)"
# check dest existence from JSON
$missing = @()
foreach ($e in $j) { if (-not (Test-Path $e.dest)) { $missing += $e.dest } }
if ($missing.Count -eq 0) { Write-Host 'All dest files exist' } else { Write-Host 'Missing dest files:'; $missing | ForEach-Object { Write-Host " - $_" } }
# any non-zero lineRatePercent
# any non-zero line/branch/method
$nonZero = $j | Where-Object { $_.lineRatePercent -ne 0 }
Write-Host "Entries with non-zero lineRatePercent: $($nonZero.Count)"
$nonZeroBranches = $j | Where-Object { $_.branchRatePercent -ne 0 }
Write-Host "Entries with non-zero branchRatePercent: $($nonZeroBranches.Count)"
$nonZeroMethods = $j | Where-Object { $_.methodRatePercent -ne 0 }
Write-Host "Entries with non-zero methodRatePercent: $($nonZeroMethods.Count)"

# print averages if present
if ($jObj.averages) {
    Write-Host "Averages: lines=$($jObj.averages.lineRatePercent) branches=$($jObj.averages.branchRatePercent) methods=$($jObj.averages.methodRatePercent)"
}
# inspect first few XML root attributes of the first few sources
$toInspect = $j | Select-Object -First 3
foreach ($entry in $toInspect) {
    Write-Host "\nInspecting: $($entry.source)"
    Write-Host "  Summary JSON: lineRatePercent=$($entry.lineRatePercent) branchRatePercent=$($entry.branchRatePercent) methodRatePercent=$($entry.methodRatePercent)"
    if (-not (Test-Path $entry.source)) { Write-Host '  Source file missing' ; continue }
    try {
        $x = [xml](Get-Content -Path $entry.source -Raw)
        if ($x -and $x.coverage) {
            $attrs = $x.coverage.Attributes
            foreach ($a in $attrs) { Write-Host "  - $($a.Name) = $($a.Value)" }
            # compute method coverage directly from the XML for cross-check
            $methods = @($x.SelectNodes('//method'))
            if ($methods -and $methods.Count -gt 0) {
                $total = $methods.Count
                $covered = 0
                foreach ($m in $methods) {
                    $mAttr = $null
                    try { $mAttr = $m.GetAttribute('line-rate') } catch { }
                    if (-not $mAttr) { $mAttr = $m.'@line-rate' }
                    try { if ($mAttr -and ([double]$mAttr) -gt 0) { $covered++ } } catch { }
                }
                $computed = [math]::Round((($covered / $total) * 100), 2)
                Write-Host "  - methods: $covered / $total covered -> computedMethodRatePercent=$computed"
            } else {
                Write-Host '  - methods: none found'
            }
        } else {
            Write-Host '  No <coverage> root element found'
        }
    } catch {
        Write-Host '  Failed to parse XML:' $_.Exception.Message
    }
}

Write-Host "\nScript exit: OK"