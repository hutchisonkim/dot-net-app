Param()

$summaryPath = Join-Path $PSScriptRoot '..\artifacts\coverage-files\coverage-summary.json'
$outDir = Join-Path $PSScriptRoot '..\coverage-report'
if (-not (Test-Path $summaryPath)) { Write-Host "coverage-summary.json not found at $summaryPath"; exit 1 }
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

$data = Get-Content -Raw -Path $summaryPath | ConvertFrom-Json
$averages = $data.averages

# Fallback: compute averages from files if averages object missing
if (-not $averages) {
    $files = $data.files
    $count = $files.Count
    if ($count -gt 0) {
        $avgLines = ([math]::Round((($files | Measure-Object -Property lineRatePercent -Sum).Sum / $count), 2))
        $avgBranches = ([math]::Round((($files | Measure-Object -Property branchRatePercent -Sum).Sum / $count), 2))
        $avgMethods = ([math]::Round((($files | Measure-Object -Property methodRatePercent -Sum).Sum / $count), 2))
    } else {
        $avgLines = 0; $avgBranches = 0; $avgMethods = 0
    }
} else {
    $avgLines = $averages.lineRatePercent; $avgBranches = $averages.branchRatePercent; $avgMethods = $averages.methodRatePercent
}

function Get-Color($p) {
    if ($p -ge 80) { return '#4caf50' }        # green
    elseif ($p -ge 50) { return '#fbc02d' }    # yellow
    else { return '#f44336' }                  # red
}

$w = 600
$h = 160
$pad = 20
$barH = 18
$labelX = 20
$barX = 160
$barMaxW = $w - $barX - $pad

$linesW = [math]::Round(($avgLines/100) * $barMaxW)
$branchesW = [math]::Round(($avgBranches/100) * $barMaxW)
$methodsW = [math]::Round(($avgMethods/100) * $barMaxW)

$svg = @"
<svg xmlns='http://www.w3.org/2000/svg' width='$w' height='$h' viewBox='0 0 $w $h'>
  <style>
    .label { font: 14px system-ui, Arial, sans-serif; fill: #222; }
    .small { font: 12px system-ui, Arial, sans-serif; fill: #555; }
  </style>
  <rect width='100%' height='100%' fill='#ffffff' />

  <text x='$labelX' y='40' class='label'>Line coverage</text>
  <rect x='$barX' y='28' width='$barMaxW' height='$barH' rx='6' fill='#eee' />
  <rect x='$barX' y='28' width='$linesW' height='$barH' rx='6' fill='$(Get-Color $avgLines)' />
  <text x='$($barX + $barMaxW + 16)' y='42' class='small'>$avgLines`%</text>

  <text x='$labelX' y='82' class='label'>Branch coverage</text>
  <rect x='$barX' y='70' width='$barMaxW' height='$barH' rx='6' fill='#eee' />
  <rect x='$barX' y='70' width='$branchesW' height='$barH' rx='6' fill='$(Get-Color $avgBranches)' />
  <text x='$($barX + $barMaxW + 16)' y='84' class='small'>$avgBranches`%</text>

  <text x='$labelX' y='124' class='label'>Method coverage</text>
  <rect x='$barX' y='112' width='$barMaxW' height='$barH' rx='6' fill='#eee' />
  <rect x='$barX' y='112' width='$methodsW' height='$barH' rx='6' fill='$(Get-Color $avgMethods)' />
  <text x='$($barX + $barMaxW + 16)' y='124' class='small'>$avgMethods`%</text>

  <text x='$pad' y='150' class='small'>Generated from artifacts/coverage-files/coverage-summary.json</text>
</svg>
"@

$outPath = Join-Path $outDir 'coverage-summary.svg'
Set-Content -Path $outPath -Value $svg -Encoding UTF8
Write-Host "Wrote SVG summary to $outPath"
