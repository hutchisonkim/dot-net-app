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

    # Try to resolve repo (owner/name) for detailed job/step inspection
    $repo = $null
    try {
        $repo = gh repo view --json name,owner --jq '.owner.login + "/" + .name' 2>$null
    } catch {
        $repo = $env:GITHUB_REPOSITORY
    }

    # Try to print a concise human-friendly current action: Job -> Step
    $currentAction = $null
    if ($repo) {
        $jobsRaw = gh api "repos/$repo/actions/runs/$runId/jobs" --jq '.jobs' 2>$null
        if ($jobsRaw) {
            $jobs = $jobsRaw | ConvertFrom-Json
            # Prefer a job that has any non-completed steps (in_progress/queued or other), otherwise prefer job with status != completed
            $currentJob = $jobs | Where-Object { $_.steps -and ($_.steps | Where-Object { $_.status -ne 'completed' } ) } | Select-Object -First 1
            if (-not $currentJob) { $currentJob = $jobs | Where-Object { $_.status -ne 'completed' } | Select-Object -First 1 }
            if (-not $currentJob) { $currentJob = $jobs | Select-Object -First 1 }

            if ($currentJob) {
                $jobName = $currentJob.name
                $currentStep = $null
                if ($currentJob.steps) {
                    # Choose step in order: in_progress, queued, first not completed
                    $currentStep = $currentJob.steps | Where-Object { $_.status -eq 'in_progress' } | Select-Object -First 1
                    if (-not $currentStep) { $currentStep = $currentJob.steps | Where-Object { $_.status -eq 'queued' } | Select-Object -First 1 }
                    if (-not $currentStep) { $currentStep = $currentJob.steps | Where-Object { $_.status -ne 'completed' } | Select-Object -First 1 }
                }

                if ($currentStep) {
                    $currentAction = "$jobName -> $($currentStep.name) ($($currentStep.status))"
                } else {
                    # fallback to job-level status if no step info
                    $currentAction = "$jobName ($($currentJob.status))"
                }
            }
        }
    }

    if ($currentAction) {
        Write-Host "[$(Get-Date -Format o)] Current action: $currentAction"
    } else {
        Write-Host "[$(Get-Date -Format o)] status=$status conclusion=$conclusion"
    }

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
