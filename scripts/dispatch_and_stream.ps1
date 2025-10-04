param(
    [string]$WorkflowFile = 'ci-selfhosted-robust.yml',
    [string]$Ref = 'main',
    [int]$PollIntervalSeconds = 5,
    [int]$TimeoutMinutes = 30,
    [switch]$AlwaysShowSnapshot = $false,
    [switch]$StreamLiveLogs = $false,
    [string]$Repo
)

# --- Resolve repository slug early for all gh commands ---
$repo = $null

if ($Repo) {
    $repo = $Repo
} elseif ($env:GITHUB_REPOSITORY) {
    $repo = $env:GITHUB_REPOSITORY
} else {
    try {
        $repo = gh repo view --json name,owner --jq '.owner.login + "/" + .name' 2>$null
    } catch {}
}

# --- Validate repo resolution ---
if (-not $repo) {
    Write-Error @"
Could not determine repository.
You must either:
  - Run this script inside a git repo linked with GitHub CLI, OR
  - Pass -Repo 'owner/repo' explicitly.
Example:
  .\run-workflow.ps1 -Repo 'my-org/my-repo' -WorkflowFile 'ci.yml'
"@
    exit 1
}

Write-Host "✅ Using repository: $repo"

# --- Dispatch the workflow ---
$start = Get-Date
Write-Host "Dispatching workflow '$WorkflowFile' on ref '$Ref' at $start"
$dispatch = gh workflow run $WorkflowFile --ref $Ref -R $repo 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to dispatch workflow: $dispatch"
    exit 2
}

# --- Poll for a new run created after we dispatched ---
$deadline = $start.AddMinutes($TimeoutMinutes)
Write-Host "Waiting up to $TimeoutMinutes minutes for a run to appear..."
$runId = $null
$runRestId = $null

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds $PollIntervalSeconds
    $json = gh run list --workflow=$WorkflowFile --limit 20 --json databaseId,headSha,status,conclusion,createdAt -R $repo 2>$null
    if (-not $json) { continue }

    $runs = $json | ConvertFrom-Json
    $candidate = $runs | Where-Object { ([datetime]$_.createdAt) -ge $start.AddSeconds(-10) } |
                 Sort-Object {[datetime]$_.createdAt} -Descending | Select-Object -First 1
    if ($candidate) {
        $runId = $candidate.databaseId
        $runUrl = gh run view $runId -R $repo --json url --jq '.url' 2>$null
        if ($runUrl -and ($runUrl -match '/runs/([0-9]+)')) {
            $runRestId = $Matches[1]
        }
        Write-Host "Found run id $runId (REST id: $runRestId, status: $($candidate.status), createdAt: $($candidate.createdAt))"
        break
    }
}

if (-not $runId) {
    Write-Error "Timed out waiting for a run to appear."
    exit 3
}

# --- Poll the run until completion ---
Write-Host "Polling run $runId until completion (poll every $PollIntervalSeconds s)"

function Get-JobsSnapshot {
    param(
        [string]$repo,
        [string]$runId,
        [string]$runRestId
    )

    $lines = @()
    if (-not $repo) { return $lines }

    $jobs = $null

    # Prefer REST API (more accurate mid-run)
    if ($runRestId) {
        $jobsRaw = gh api -H 'Accept: application/vnd.github+json' "repos/$repo/actions/runs/$runRestId/jobs?per_page=100" --jq '.jobs' 2>$null
        if ($jobsRaw) {
            try { $jobs = $jobsRaw | ConvertFrom-Json } catch {}
        }
    }

    # Fallback to GraphQL (gh run view)
    if (-not $jobs -or $jobs.Count -eq 0) {
        $jobsView = gh run view $runId -R $repo --json jobs --jq '.jobs' 2>$null
        if ($jobsView) {
            try { $jobs = $jobsView | ConvertFrom-Json } catch {}
        }
    }

    if (-not $jobs) { return $lines }

    foreach ($job in $jobs) {
        $jobLine = "Job: $($job.name) - $($job.status)"
        if ($job.conclusion) { $jobLine += " (conclusion: $($job.conclusion))" }
        $lines += $jobLine

        # Fetch detailed steps if missing
        if (-not $job.steps -and $job.id) {
            $stepsRaw = gh api -H 'Accept: application/vnd.github+json' "repos/$repo/actions/jobs/$($job.id)" --jq '.steps' 2>$null
            if ($stepsRaw) {
                try { $job.steps = $stepsRaw | ConvertFrom-Json } catch {}
            }
        }

        if ($job.steps) {
            foreach ($step in $job.steps) {
                $stepName = $step.name -replace '\s+', ' '
                if ($step.conclusion) {
                    $lines += "  Step: $stepName - $($step.status) (conclusion: $($step.conclusion))"
                } else {
                    $lines += "  Step: $stepName - $($step.status)"
                }
            }
        }
    }

    return $lines
}

$prevSnapshot = @()
$watchJob = $null

if ($StreamLiveLogs) {
    try {
        Write-Host "Starting live log streaming (gh run watch $runId)"
        $watchJob = Start-Job -Name "gh-watch-$runId" -ScriptBlock {
            param($rid, $rep)
            gh run watch $rid -R $rep --exit-status
        } -ArgumentList $runId, $repo
    } catch {
        Write-Warning "Exception starting live log watcher: $($_.Exception.Message)"
    }
}

while ($true) {
    $infoJson = gh run view $runId -R $repo --json status,conclusion 2>$null
    if (-not $infoJson) { Start-Sleep -Seconds $PollIntervalSeconds; continue }

    $info = $infoJson | ConvertFrom-Json
    $status = $info.status
    $conclusion = $info.conclusion

    if (-not $runRestId) {
        $runUrl = gh run view $runId -R $repo --json url --jq '.url' 2>$null
        if ($runUrl -and ($runUrl -match '/runs/([0-9]+)')) { $runRestId = $Matches[1] }
    }

    if ($watchJob) {
        try { Receive-Job -Job $watchJob -Keep | ForEach-Object { Write-Host "[live] $_" } } catch {}
    }

    $snapshot = Get-JobsSnapshot -repo $repo -runId $runId -runRestId $runRestId

    if ($snapshot.Count -eq 0) {
        Write-Host "[$(Get-Date -Format o)] status=$status conclusion=$conclusion (no job/step info yet)"
    } else {
        $changed = $false
        if ($prevSnapshot.Count -ne $snapshot.Count) { $changed = $true }
        else {
            for ($i = 0; $i -lt $snapshot.Count; $i++) {
                if ($snapshot[$i] -ne $prevSnapshot[$i]) { $changed = $true; break }
            }
        }

        if ($changed -or $AlwaysShowSnapshot) {
            if ($changed) {
                Write-Host "[$(Get-Date -Format o)] Job/step snapshot (changed):"
            } else {
                Write-Host "[$(Get-Date -Format o)] Job/step snapshot:"
            }
            foreach ($line in $snapshot) { Write-Host $line }
            $prevSnapshot = $snapshot
        } else {
            $inProg = $snapshot | Where-Object { $_ -match 'Step: .* - in_progress' } | Select-Object -First 1
            if ($inProg) { Write-Host "[$(Get-Date -Format o)] Current: $inProg" }
            else {
                $queued = $snapshot | Where-Object { $_ -match 'Step: .* - queued' } | Select-Object -First 1
                if ($queued) { Write-Host "[$(Get-Date -Format o)] Current: $queued" }
                else { Write-Host "[$(Get-Date -Format o)] status=$status conclusion=$conclusion" }
            }
        }
    }

    if ($status -eq 'completed') { break }
    Start-Sleep -Seconds $PollIntervalSeconds
}

Write-Host "Run completed with conclusion: $conclusion."

if (-not $StreamLiveLogs) {
    Write-Host "Streaming logs below."
    gh run view $runId -R $repo --log
}

if ($watchJob) {
    try {
        Receive-Job -Job $watchJob -Wait -AutoRemoveJob | ForEach-Object { Write-Host "[live] $_" }
    } catch {}
}

if ($conclusion -ne 'success') {
    Write-Error "Workflow run $runId finished with conclusion: $conclusion"
    exit 4
}

Write-Host "Workflow run $runId succeeded."
exit 0
