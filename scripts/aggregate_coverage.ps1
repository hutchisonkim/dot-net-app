<#
Aggregates all cobertura coverage XML files under the repo and produces an HTML report using ReportGenerator.
Usage:
  pwsh .\scripts\aggregate_coverage.ps1

The script will:
- Find coverage.cobertura.xml files
- Install dotnet tool 'dotnet-reportgenerator-globaltool' if not present
- Run reportgenerator to create artifacts/coverage-report/index.html
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

# Ensure reportgenerator dotnet tool is installed
$toolName = 'dotnet-reportgenerator-globaltool'
$toolCheck = & dotnet tool list -g 2>$null | Select-String $toolName
if (-not $toolCheck) {
    Write-Host "Installing $toolName as a global tool (user-level)..."
    & dotnet tool install -g $toolName | Out-Null
}

$reportDir = Join-Path $root "artifacts\coverage-report"
if (Test-Path $reportDir) { Remove-Item $reportDir -Recurse -Force }
New-Item -ItemType Directory -Path $reportDir | Out-Null

# Build the -reports argument as a semicolon-separated list
$reportsArg = ($coverageFiles -join ";")

Write-Host "Running ReportGenerator..."
& reportgenerator -reports:$reportsArg -targetdir:$reportDir -reporttypes:HtmlInline_AzurePipelines | Out-Null

Write-Host "Coverage report created at: $reportDir\index.html"
