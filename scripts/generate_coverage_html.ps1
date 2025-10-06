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

# build per-file table rows
$rows = ""
foreach ($f in $files) {
  $name = if ($f.dest) { Split-Path $f.dest -Leaf } else { Split-Path $f.source -Leaf }
  $flinesCovered = $f.linesCovered
  $flinesValid = $f.linesValid
  $flinesUncovered = $flinesValid - $flinesCovered
  $flinePerc = $f.lineRatePercent
  $fbranchesCovered = $f.branchesCovered
  $fbranchesValid = $f.branchesValid
  $fbranchPerc = $f.branchRatePercent
  $fmethodsCovered = $f.methodsCovered
  $fmethodsValid = $f.methodsValid
  $fmethodPerc = $f.methodRatePercent
  $rows += "<tr>"
  $rows += "<td><code>$name</code></td>"
  $rows += "<td class=right>$flinesCovered</td>"
  $rows += "<td class=right>$flinesUncovered</td>"
  $rows += "<td class=right>$flinesValid</td>"
  $rows += "<td class=right>$flinePerc`%</td>"
  $rows += "<td class=right>$fbranchesCovered</td>"
  $rows += "<td class=right>$fbranchesValid</td>"
  $rows += "<td class=right>$fbranchPerc`%</td>"
  $rows += "<td class=right>$fmethodsCovered</td>"
  $rows += "<td class=right>$fmethodsValid</td>"
  $rows += "<td class=right>$fmethodPerc`%</td>"
  $rows += "</tr>\n"
}

$html = @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <title>Coverage Report</title>
  <style>
    body {
      font-family: system-ui, Segoe UI, Roboto, Helvetica, Arial;
      margin: 2rem;
      color: #222;
      background: #fafafa;
    }
    h1 {
      margin-bottom: 1.2rem;
      border-bottom: 2px solid #ddd;
      padding-bottom: 0.3rem;
    }
    h2 {
      margin-top: 2.5rem;
      margin-bottom: 1rem;
    }
    .summary {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }
    .card {
      background: white;
      border-radius: 10px;
      box-shadow: 0 2px 5px rgba(0,0,0,0.1);
      padding: 1rem 1.2rem;
    }
    .label {
      font-size: 0.9rem;
      color: #555;
    }
    .value {
      font-size: 1.3rem;
      font-weight: bold;
    }
    .bar {
      height: 10px;
      background: #e0e0e0;
      border-radius: 5px;
      overflow: hidden;
      margin-top: 0.5rem;
    }
    .fill {
      height: 100%;
      transition: width 0.6s ease;
    }
    .fill.green { background: #4caf50; }
    .fill.yellow { background: #fbc02d; }
    .fill.red { background: #f44336; }

    table {
      border-collapse: collapse;
      width: 100%;
      background: white;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
    }
    th, td {
      padding: 8px 10px;
      border-bottom: 1px solid #eee;
      text-align: right;
    }
    th {
      background: #f0f0f0;
      text-align: left;
    }
    td code {
      color: #0366d6;
      font-weight: 500;
    }
    tr:hover {
      background: #f9f9f9;
    }
    .small {
      color: #666;
      font-size: 0.9em;
      margin-top: 1.5rem;
    }
  </style>
</head>
<body>
  <h1>Coverage Report</h1>

  <div class="summary">
    <div class="card">
      <div class="label">Lines Covered</div>
      <div class="value">$total_lines_covered / $total_lines_valid</div>
      <div class="bar"><div class="fill $(if($line_cov -ge 80){'green'}elseif($line_cov -ge 50){'yellow'}else{'red'})" style="width:$line_cov%"></div></div>
      <div class="label">Line Coverage: $line_cov`%</div>
    </div>

    <div class="card">
      <div class="label">Branches Covered</div>
      <div class="value">$total_branches_covered / $total_branches_valid</div>
      <div class="bar"><div class="fill $(if($branch_cov -ge 80){'green'}elseif($branch_cov -ge 50){'yellow'}else{'red'})" style="width:$branch_cov%"></div></div>
      <div class="label">Branch Coverage: $branch_cov`%</div>
    </div>

    <div class="card">
      <div class="label">Methods Covered</div>
      <div class="value">$total_methods_covered / $total_methods_valid</div>
      <div class="bar"><div class="fill $(if($method_cov -ge 80){'green'}elseif($method_cov -ge 50){'yellow'}else{'red'})" style="width:$method_cov%"></div></div>
      <div class="label">Method Coverage: $method_cov`%</div>
    </div>
  </div>

  <p class="small">Generated from <code>artifacts/coverage-files/coverage-summary.json</code>.</p>
</body>
</html>
"@


Set-Content -Path (Join-Path $outDir 'index.html') -Value $html -Encoding UTF8
Write-Host "Wrote coverage report to $(Join-Path $outDir 'index.html')"
