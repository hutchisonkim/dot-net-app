param(
    [string]$WorkflowFile = 'ci-selfhosted-robust.yml',
    [string]$Ref = 'main',
    [int]$PollIntervalSeconds = 5,
    [int]$TimeoutMinutes = 30
)

# Dispatch the workflow
$start = Get-Date
Write-Host "Dispatching workflow '$WorkflowFile' on ref '$Ref' at $start"
$dispatch = gh workflow run $WorkflowFile --ref $Ref 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to dispatch workflow: $dispatch"
    exit 2
}

# Poll for a new run created after we dispatched
$deadline = $start.AddMinutes($TimeoutMinutes)
Write-Host "Waiting up to $TimeoutMinutes minutes for a run to appear..."
$runId = $null
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds $PollIntervalSeconds
    $json = gh run list --workflow=$WorkflowFile --limit 20 --json databaseId,headSha,status,conclusion,createdAt 2>$null
    if (-not $json) { continue }
    $runs = $json | ConvertFrom-Json
    # Find the most recent run with createdAt >= our dispatch time - 10s
    $candidate = $runs | Where-Object { ([datetime]$_.createdAt) -ge $start.AddSeconds(-10) } | Sort-Object {[datetime]$_.createdAt} -Descending | Select-Object -First 1
    if ($candidate) {
        $runId = $candidate.databaseId
        Write-Host "Found run id $runId (status: $($candidate.status), createdAt: $($candidate.createdAt))"
        break
    }
}

if (-not $runId) {
    Write-Error "Timed out waiting for a run to appear."
    exit 3
}

# Poll the run until it's completed, printing status updates
Write-Host "Polling run $runId until completion (poll every $PollIntervalSeconds s)"
while ($true) {
    $infoJson = gh run view $runId --json status,conclusion 2>$null
    if (-not $infoJson) { Start-Sleep -Seconds $PollIntervalSeconds; continue }
    $info = $infoJson | ConvertFrom-Json
    $status = $info.status
    $conclusion = $info.conclusion
    Write-Host "[$(Get-Date -Format o)] status=$status conclusion=$conclusion"
    if ($status -eq 'completed') { break }
    Start-Sleep -Seconds $PollIntervalSeconds
}

Write-Host "Run completed with conclusion: $conclusion. Streaming logs below."
# Stream logs
gh run view $runId --log

# Exit with non-zero if the run failed
if ($conclusion -ne 'success') {
    Write-Error "Workflow run $runId finished with conclusion: $conclusion"
    exit 4
}

Write-Host "Workflow run $runId succeeded."
exit 0
