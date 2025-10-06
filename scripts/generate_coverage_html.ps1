Param()

$summaryPath = Join-Path $PSScriptRoot '..\artifacts\coverage-files\coverage-summary.json'
$outDir = Join-Path $PSScriptRoot '..\coverage-report'
if (-not (Test-Path $summaryPath)) { Write-Host "coverage-summary.json not found at $summaryPath"; exit 1 }
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }


# PowerShell fallback
$json = Get-Content -Raw -Path $summaryPath | ConvertFrom-Json
$files = $json.files
$averages = $json.averages

$total_lines_covered = ($files | Measure-Object -Property linesCovered -Sum).Sum
if (-not $total_lines_covered) { $total_lines_covered = 0 }
$total_lines_valid = ($files | Measure-Object -Property linesValid -Sum).Sum
if (-not $total_lines_valid) { $total_lines_valid = 0 }
$total_lines_uncovered = $total_lines_valid - $total_lines_covered
$total_lines = $total_lines_valid

$total_branches_covered = ($files | Measure-Object -Property branchesCovered -Sum).Sum
if (-not $total_branches_covered) { $total_branches_covered = 0 }
$total_branches_valid = ($files | Measure-Object -Property branchesValid -Sum).Sum
if (-not $total_branches_valid) { $total_branches_valid = 0 }

if ($total_lines_valid -gt 0) { $line_cov = [math]::Round((($total_lines_covered / $total_lines_valid) * 100), 2) } else { $line_cov = $averages.lineRatePercent }
if ($total_branches_valid -gt 0) { $branch_cov = [math]::Round((($total_branches_covered / $total_branches_valid) * 100), 2) } else { $branch_cov = $averages.branchRatePercent }

$total_methods_covered = ($files | Measure-Object -Property methodsCovered -Sum).Sum
if (-not $total_methods_covered) { $total_methods_covered = 0 }
$total_methods_valid = ($files | Measure-Object -Property methodsValid -Sum).Sum
if (-not $total_methods_valid) { $total_methods_valid = 0 }

if ($total_methods_valid -gt 0) { $method_cov = [math]::Round((($total_methods_covered / $total_methods_valid) * 100), 2) } else { $method_cov = $averages.methodRatePercent }

$html = @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <title>Coverage Report</title>
  <style>
    body { font-family: system-ui, Segoe UI, Roboto, Helvetica, Arial; margin: 2rem; color: #222; }
    h1 { margin-bottom: 0.5rem; }
    .metric { margin-bottom: 1.2rem; }
    .label { font-weight: bold; display: inline-block; width: 180px; }
    .bar { height: 18px; background: #ddd; border-radius: 9px; overflow: hidden; width: 300px; display: inline-block; vertical-align: middle; margin-left: 0.5rem; }
    .fill { height: 100%; background: #4caf50; }
    .small { color: #666; font-size: 0.9em; }
    table { border-collapse: collapse; margin-top: 1rem; }
    td, th { padding: 6px 8px; border: 1px solid #ddd; }
  </style>
</head>
<body>
  <h1>Coverage Report</h1>
  <div class="metric">
    <span class="label">Covered lines:</span>
    <span>$total_lines_covered</span>
  </div>
  <div class="metric">
    <span class="label">Uncovered lines:</span>
    <span>$total_lines_uncovered</span>
  </div>
  <div class="metric">
    <span class="label">Coverable lines:</span>
    <span>$total_lines_valid</span>
  </div>
  <div class="metric">
    <span class="label">Total lines:</span>
    <span>$total_lines</span>
  </div>
  <div class="metric">
    <span class="label">Line coverage:</span>
    <span>$line_cov`%</span>
    <div class="bar"><div class="fill" style="width:$line_cov%"></div></div>
  </div>

  <div class="metric">
    <span class="label">Covered branches:</span>
    <span>$total_branches_covered</span>
  </div>
  <div class="metric">
    <span class="label">Total branches:</span>
    <span>$total_branches_valid</span>
  </div>
  <div class="metric">
    <span class="label">Branch coverage:</span>
    <span>$branch_cov`%</span>
    <div class="bar"><div class="fill" style="width:$branch_cov%"></div></div>
  </div>

  <div class="metric">
    <span class="label">Covered methods:</span>
    <span>$total_methods_covered</span>
  </div>
  <div class="metric">
    <span class="label">Total methods:</span>
    <span>$total_methods_valid</span>
  </div>
  <div class="metric">
    <span class="label">Method coverage:</span>
    <span>$method_cov`%</span>
    <div class="bar"><div class="fill" style="width:$method_cov%"></div></div>
  </div>

  <hr />
  <p class="small">Generated from <code>artifacts/coverage-files/coverage-summary.json</code>.</p>
</body>
</html>
"@

Set-Content -Path (Join-Path $outDir 'index.html') -Value $html -Encoding UTF8
Write-Host "Wrote coverage report to $(Join-Path $outDir 'index.html')"
