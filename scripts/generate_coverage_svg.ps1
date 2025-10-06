Param()

$summaryPath = Join-Path $PSScriptRoot '..\artifacts\coverage-files\coverage-summary.json'
$outDir = Join-Path $PSScriptRoot '..\coverage-report'

if (-not (Test-Path $summaryPath)) { Write-Host "coverage-summary.json not found at $summaryPath"; exit 1 }
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

$data = Get-Content -Raw -Path $summaryPath | ConvertFrom-Json
$averages = $data.averages
$files = $data.files

# Compute averages if missing
if (-not $averages) {
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

$total_lines_covered = ($files | Measure-Object -Property linesCovered -Sum).Sum
$total_lines_valid   = ($files | Measure-Object -Property linesValid -Sum).Sum
$total_lines_covered = $total_lines_covered -as [int]; $total_lines_valid = $total_lines_valid -as [int]

$total_branches_covered = ($files | Measure-Object -Property branchesCovered -Sum).Sum
$total_branches_valid   = ($files | Measure-Object -Property branchesValid -Sum).Sum
$total_branches_covered = $total_branches_covered -as [int]; $total_branches_valid = $total_branches_valid -as [int]

$total_methods_covered = ($files | Measure-Object -Property methodsCovered -Sum).Sum
$total_methods_valid   = ($files | Measure-Object -Property methodsValid -Sum).Sum
$total_methods_covered = $total_methods_covered -as [int]; $total_methods_valid = $total_methods_valid -as [int]

function Get-Color($p) {
    $n = try { [double]$p } catch { 0 }
    if ($n -ge 80) { return '#4caf50' }        # green
    elseif ($n -ge 50) { return '#fbc02d' }    # yellow
    else { return '#f44336' }                  # red
}

# Dimensions
$w = 700
$h = 200
$pad = 20
$cardW = 200
$cardH = 100
$gap = 20
$barH = 12
$barMaxW = $cardW - 40

# Compute bar widths
$linesW = [math]::Round(($avgLines / 100) * $barMaxW)
$branchesW = [math]::Round(($avgBranches / 100) * $barMaxW)
$methodsW = [math]::Round(($avgMethods / 100) * $barMaxW)

# Build SVG
$svg = @"
<svg xmlns='http://www.w3.org/2000/svg' width='$w' height='$h' viewBox='0 0 $w $h'>
  <style>
    .label { font: 14px system-ui, Arial, sans-serif; fill: #555; }
    .value { font: 18px system-ui, Arial, sans-serif; fill: #222; font-weight: bold; }
    .bar-bg { fill: #e0e0e0; rx:6; ry:6 }
    .bar-fill { rx:6; ry:6 }
    .small { font: 12px system-ui, Arial, sans-serif; fill: #888; }
  </style>

  <!-- Lines Card -->
  <rect x='$pad' y='$pad' width='$cardW' height='$cardH' rx='10' fill='white' stroke='#ddd' stroke-width='1' />
  <text x='$($pad+10)' y='$($pad+25)' class='label'>Lines Covered</text>
  <text x='$($pad+10)' y='$($pad+50)' class='value'>$total_lines_covered / $total_lines_valid</text>
  <rect x='$($pad+10)' y='$($pad+65)' width='$barMaxW' height='$barH' class='bar-bg' />
  <rect x='$($pad+10)' y='$($pad+65)' width='$linesW' height='$barH' fill='$(Get-Color $avgLines)' class='bar-fill' />
  <text x='$($pad+10)' y='$($pad+90)' class='label'>Line Coverage: $avgLines%</text>

  <!-- Branches Card -->
  <rect x='$($pad + $cardW + $gap)' y='$pad' width='$cardW' height='$cardH' rx='10' fill='white' stroke='#ddd' stroke-width='1' />
  <text x='$($pad + $cardW + $gap + 10)' y='$($pad+25)' class='label'>Branches Covered</text>
  <text x='$($pad + $cardW + $gap + 10)' y='$($pad+50)' class='value'>$total_branches_covered / $total_branches_valid</text>
  <rect x='$($pad + $cardW + $gap + 10)' y='$($pad+65)' width='$barMaxW' height='$barH' class='bar-bg' />
  <rect x='$($pad + $cardW + $gap + 10)' y='$($pad+65)' width='$branchesW' height='$barH' fill='$(Get-Color $avgBranches)' class='bar-fill' />
  <text x='$($pad + $cardW + $gap + 10)' y='$($pad+90)' class='label'>Branch Coverage: $avgBranches%</text>

  <!-- Methods Card -->
  <rect x='$($pad + ($cardW + $gap)*2)' y='$pad' width='$cardW' height='$cardH' rx='10' fill='white' stroke='#ddd' stroke-width='1' />
  <text x='$($pad + ($cardW + $gap)*2 + 10)' y='$($pad+25)' class='label'>Methods Covered</text>
  <text x='$($pad + ($cardW + $gap)*2 + 10)' y='$($pad+50)' class='value'>$total_methods_covered / $total_methods_valid</text>
  <rect x='$($pad + ($cardW + $gap)*2 + 10)' y='$($pad+65)' width='$barMaxW' height='$barH' class='bar-bg' />
  <rect x='$($pad + ($cardW + $gap)*2 + 10)' y='$($pad+65)' width='$methodsW' height='$barH' fill='$(Get-Color $avgMethods)' class='bar-fill' />
  <text x='$($pad + ($cardW + $gap)*2 + 10)' y='$($pad+90)' class='label'>Method Coverage: $avgMethods%</text>

  <text x='$pad' y='$($pad + $cardH + 50)' class='small'>Generated from artifacts/coverage-files/coverage-summary.json</text>
</svg>
"@

$outPath = Join-Path $outDir 'coverage-summary.svg'
Set-Content -Path $outPath -Value $svg -Encoding UTF8
Write-Host "Wrote SVG summary to $outPath"
