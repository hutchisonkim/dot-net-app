<#
.SYNOPSIS
  Automates preparing a WSL (Ubuntu) environment and installing a GitHub self-hosted Actions runner for this repo.

.DESCRIPTION
  This script will:
    1. Ensure WSL is available and that the specified Ubuntu distribution exists (optionally install if missing).
    2. Enable systemd inside WSL by writing /etc/wsl.conf (if not already enabled) and restarting WSL.
    3. Create (or reuse) a dedicated runner user (default: ghrunner).
    4. Download & configure the GitHub Actions runner (unattended) with provided registration token.
    5. Optionally install as a systemd service so it starts automatically.

  IMPORTANT: The registration token is SHORT-LIVED. Generate a fresh token from:
     GitHub Repo -> Settings -> Actions -> Runners -> New self-hosted runner -> (Copy token)

  You can re-run this script with a new token and -Replace to update an existing runner.

.PARAMETER Repo
  The owner/repo path. Example: hutchisonkim/dot-net-app

.PARAMETER RegistrationToken
  Ephemeral runner registration token (NOT a PAT). Required for initial config / replace.

.PARAMETER RunnerVersion
  Runner release version (tag without leading 'v'). Default: 2.328.0

.PARAMETER Distro
  WSL distribution name. Default: Ubuntu

.PARAMETER RunnerUser
  Linux user to own the runner files. Default: ghrunner

.PARAMETER Labels
  Comma-separated labels to assign to the runner. Default: self-hosted,linux,x64,local

.PARAMETER RunnerName
  Explicit name for the runner (defaults to hostname + '-wsl').

.PARAMETER Replace
  If set, passes --replace to config to overwrite an existing runner with same name.

.PARAMETER Service
  If set, installs a systemd unit to manage the runner (auto-start).

.PARAMETER SkipInstallDistro
  If set, will not attempt "wsl --install" if distro is missing (will fail instead).

.EXAMPLE
  .\scripts\setup-wsl-github-runner.ps1 -Repo hutchisonkim/dot-net-app -RegistrationToken ABC123 -Service

.EXAMPLE (Replace existing runner configuration)
  .\scripts\setup-wsl-github-runner.ps1 -Repo hutchisonkim/dot-net-app -RegistrationToken NEWTOKEN -Replace

.NOTES
  Run this script in an elevated PowerShell session if WSL needs installing or system changes are required.
  After enabling systemd, a wsl --shutdown is performed; any active WSL sessions will terminate.

#>
[CmdletBinding(SupportsShouldProcess=$true)]
param(
  [string]$Repo,
  [string]$RegistrationToken,
    [string]$RunnerVersion = '2.328.0',
    [string]$Distro = 'Ubuntu',
    [string]$RunnerUser = 'ghrunner',
    [string]$Labels = 'self-hosted,linux,x64,local',
    [string]$RunnerName,
    [switch]$Replace,
    [switch]$Service,
  [switch]$SkipInstallDistro,
  [switch]$Remove,
  [switch]$RestartService,
  [switch]$Isolated,
  [string]$RestartPolicy = 'on-failure',
  [int]$RestartSec = 8,
  [switch]$ScriptDebug
)

function Write-Section($Title) { Write-Host "`n=== $Title ===" -ForegroundColor Cyan }

function Assert-Command($Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' not found in PATH."
    }
}

function Invoke-WslCommand {
  param(
    [Parameter(Mandatory)] [string]$Command,
    [string]$Description = '',
    [int]$ExpectedExitCode = 0,
    [switch]$IgnoreFailure,
    [switch]$RunAsRoot
  )
  if ($ScriptDebug) { Write-Host "[DEBUG] WSL: $Description`n        -> $Command" -ForegroundColor DarkGray }
  # Execute command; capture combined output (stderr redirected to stdout for simplicity)
  if ($RunAsRoot) {
    $output = & wsl.exe -d $Distro -u root bash -lc "$Command" 2>&1
  } else {
    $output = & wsl.exe -d $Distro bash -lc "$Command" 2>&1
  }
  $exit = $LASTEXITCODE
  if ($ScriptDebug -and $output) { Write-Host "[DEBUG][OUTPUT] $Description`n$output" -ForegroundColor DarkGreen }
  if (-not $IgnoreFailure -and $exit -ne $ExpectedExitCode) {
    throw "WSL command failed ($Description) exit=$exit. Output:`n$output"
  }
  return @{ ExitCode = $exit; StdOut = $output }
}

Write-Section "Pre-flight"
Assert-Command wsl

# Use a dedicated clean directory for runner binaries separate from any cloned repos
$runnerDir = "/home/$RunnerUser/gh-runner"

# Fast path: restart service (does not require repo/token)
if ($RestartService) {
  Write-Section "Restarting GitHub Runner Service"
  try {
    $svcCheck = Invoke-WslCommand -Command '[ "$(ps -p 1 -o comm= 2>/dev/null)" = systemd ] && test -f /etc/systemd/system/github-runner.service && echo present || echo missing' -Description 'detect service' -RunAsRoot -IgnoreFailure
    if ($svcCheck.StdOut -notmatch 'present') {
      Write-Warning "github-runner.service not found or systemd not active. Install first with -Service."; return
    }
    # Stop service, kill any stray listeners, remove transient session file
  Invoke-WslCommand -Command "systemctl stop github-runner || true; pkill -f Runner.Listener || true; runnerDir=$runnerDir; [ -f \"$runnerDir/.session\" ] && rm -f \"$runnerDir/.session\" || true" -Description 'stop & purge session' -RunAsRoot -IgnoreFailure | Out-Null
    Start-Sleep 3
    Invoke-WslCommand -Command 'systemctl start github-runner' -Description 'start service' -RunAsRoot | Out-Null
    Start-Sleep 3
    $status = Invoke-WslCommand -Command 'systemctl is-active github-runner || true' -Description 'status' -RunAsRoot -IgnoreFailure
    Write-Host "Service status: $($status.StdOut.Trim())" -ForegroundColor Cyan
    $logs = Invoke-WslCommand -Command 'journalctl -u github-runner -n 40 --no-pager 2>/dev/null || true' -Description 'recent logs' -RunAsRoot -IgnoreFailure
    $logText = $logs.StdOut
    Write-Host "---- Last 40 log lines (tail) ----" -ForegroundColor DarkCyan
    Write-Host ($logText.Trim())
    if ($logText -match 'A session for this runner already exists') {
      Write-Warning 'Conflict detected: stale session still reported. Next step: reconfigure with a new runner name to clear server-side session.'
      Write-Host "Suggested: ./scripts/setup-wsl-github-runner.ps1 -Repo <owner/repo> -RegistrationToken <NEW_TOKEN> -Replace -Service -RunnerName <new-name>" -ForegroundColor DarkGray
    } elseif ($logText -match 'Listening for Jobs') {
      Write-Host 'Runner appears healthy (Listening for Jobs).' -ForegroundColor Green
    } else {
      Write-Host 'Runner state inconclusive; inspect full logs if issues persist.' -ForegroundColor Yellow
    }
  } catch {
    Write-Error "Failed to restart service: $_"
  }
  return
}

if (-not $Repo) {
  $Repo = Read-Host "Enter GitHub repository (owner/repo)"
}
if (-not $RegistrationToken) {
  $RegistrationToken = Read-Host "Enter ephemeral runner registration token"
}

if (-not $Repo.Contains('/')) { throw "Repo must be in 'owner/name' format" }
$RepoUrl = "https://github.com/$Repo"
if (-not $RunnerName) { $RunnerName = "$(hostname)-wsl" }

# Append isolated label automatically if requested
if ($Isolated -and $Labels -notmatch '(?i)isolated') { $Labels += ',isolated' }

# Normalize restart policy
$validPolicies = 'always','on-failure'
if ($RestartPolicy -notin $validPolicies) {
  Write-Warning "Invalid -RestartPolicy '$RestartPolicy'. Falling back to 'on-failure'. Valid: $($validPolicies -join ', ')"
  $RestartPolicy = 'on-failure'
}
if ($RestartSec -lt 1 -or $RestartSec -gt 300) { Write-Warning "RestartSec $RestartSec out of range (1-300). Using 8."; $RestartSec = 8 }

Write-Host "Repo: $RepoUrl"; Write-Host "Runner name: $RunnerName"; Write-Host "Version: $RunnerVersion";

if ($Remove) {
  Write-Section "Removal Requested"
  if (-not $RegistrationToken) {
    $RegistrationToken = Read-Host "Enter ephemeral runner registration token (required for removal)"
  }
  Write-Host "Stopping service (if present)" -ForegroundColor Cyan
  # Only attempt systemctl if systemd is PID 1; avoid sudo password prompt by running as root
  Invoke-WslCommand -Command 'if [ "$(ps -p 1 -o comm= 2>/dev/null)" = systemd ] && systemctl list-unit-files | grep -q "github-runner.service"; then systemctl disable --now github-runner 2>/dev/null || true; fi' -Description 'stop service (removal)' -RunAsRoot -IgnoreFailure
  Write-Host "Removing GitHub runner registration (if config exists)" -ForegroundColor Cyan
  Invoke-WslCommand -Command "if [ -x $runnerDir/config.sh ]; then sudo -u $RunnerUser $runnerDir/config.sh remove --token $RegistrationToken || true; fi" -Description 'runner deregister' -RunAsRoot -IgnoreFailure
  Write-Host "Deleting runner directory" -ForegroundColor Cyan
  Invoke-WslCommand -Command "rm -rf $runnerDir" -Description 'delete runner dir' -RunAsRoot -IgnoreFailure
  Write-Host "Removal complete. Verify in GitHub UI that the runner disappeared." -ForegroundColor Green
  return
}

# 1. Ensure distro exists
$existingDistros = (& wsl.exe -l -q) 2>$null | Where-Object { $_ }
if ($existingDistros -notcontains $Distro) {
    if ($SkipInstallDistro) { throw "Distro '$Distro' not installed and SkipInstallDistro set." }
    Write-Section "Installing WSL Distro $Distro"
    Write-Host "Attempting: wsl --install -d $Distro (Requires Windows 10 22H2 / 11 and elevation)" -ForegroundColor Yellow
    if ($ScriptDebug) { Write-Host "[DEBUG] Installing distro via wsl --install -d $Distro" -ForegroundColor DarkGray }
    & wsl.exe --install -d $Distro
    Write-Host "If installation required a reboot, reboot and re-run the script." -ForegroundColor Yellow
    Write-Host "Waiting 10s for distro registration..."
    Start-Sleep 10
}

# 2. Enable systemd if not already
Write-Section "Ensuring systemd enabled"
$systemdEnabled = $false
try {
  $status = & wsl.exe -d $Distro /bin/bash -c 'test -f /etc/wsl.conf && grep -q "^systemd=true" /etc/wsl.conf && echo yes || echo no'
    if ($status.Trim() -eq 'yes') { $systemdEnabled = $true }
} catch {}

if (-not $systemdEnabled) {
    Write-Host "Adding /etc/wsl.conf with systemd=true" -ForegroundColor Green
  # Use single-quoted PowerShell string to avoid PowerShell interpreting [boot] as a type literal
  Invoke-WslCommand -Command 'printf "[boot]\nsystemd=true\n" > /etc/wsl.conf' -Description 'write wsl.conf' -RunAsRoot
    Write-Host "Shutting down WSL to apply systemd setting" -ForegroundColor Green
    & wsl.exe --shutdown
    Write-Host "Restarting distro to validate systemd" -ForegroundColor Green
}

# 3. Create runner user if missing
Write-Section "Ensuring runner user '$RunnerUser' exists"
Invoke-WslCommand -Command "id -u $RunnerUser >/dev/null 2>&1 || useradd -m -s /bin/bash $RunnerUser" -Description 'ensure runner user' -RunAsRoot

# 4. Install dependencies (curl, git, etc.)
Write-Section "Installing dependencies (curl git tar ca-certificates lib libs for Playwright)"
Invoke-WslCommand -Command 'export DEBIAN_FRONTEND=noninteractive; apt-get update; pkgs="curl git tar ca-certificates libnss3 libatk-bridge2.0-0 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgtk-3-0 libdrm2 fonts-liberation"; if apt-cache show libasound2 >/dev/null 2>&1; then pkgs="$pkgs libasound2"; elif apt-cache show libasound2t64 >/dev/null 2>&1; then pkgs="$pkgs libasound2t64"; fi; echo "Installing: $pkgs"; apt-get install -y $pkgs' -Description 'install deps' -RunAsRoot

# 5. (Optional) Install PowerShell (needed by your workflow for pwsh step)
Write-Section "Ensuring PowerShell (pwsh) installed"
Invoke-WslCommand -Command 'command -v pwsh >/dev/null 2>&1 || ( . /etc/os-release && wget -q https://packages.microsoft.com/config/ubuntu/$VERSION_ID/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb && dpkg -i /tmp/packages-microsoft-prod.deb && apt-get update && apt-get install -y powershell )' -Description 'install pwsh' -RunAsRoot

# 6. Download & configure runner
Write-Section "Configuring GitHub Actions runner"
$labelsEscaped = $Labels.Replace('"','')
$configFlags = "--url $RepoUrl --token $RegistrationToken --name $RunnerName --labels $labelsEscaped --unattended"
if ($Replace) { $configFlags += ' --replace' }

# 6a. Ensure directory (root then chown to runner user if newly created)
Invoke-WslCommand -Command "mkdir -p $runnerDir && chown ${RunnerUser}:${RunnerUser} $runnerDir" -Description 'prepare runner dir' -RunAsRoot

# 6b. Download tarball if missing
Write-Section "Downloading runner (can take a while)"
$pkgName = "actions-runner-linux-x64-$RunnerVersion.tar.gz"
$downloadCmd = "cd $runnerDir; if [ ! -f $pkgName ]; then echo 'Downloading runner $RunnerVersion'; curl -sSLo $pkgName -L https://github.com/actions/runner/releases/download/v$RunnerVersion/$pkgName; fi"
Invoke-WslCommand -Command $downloadCmd -Description 'download runner' -RunAsRoot

# 6c. (Checksum verification skipped – simplified script; add verification if required.)

# 6d. Extract if config.sh not present
Invoke-WslCommand -Command "cd $runnerDir; if [ ! -f config.sh ]; then tar xzf $pkgName; chown -R ${RunnerUser}:${RunnerUser} $runnerDir; fi" -Description 'extract runner' -RunAsRoot

# 6e. Remove existing config if replacing
if ($Replace) {
  Invoke-WslCommand -Command "cd $runnerDir; if [ -f .runner ]; then sudo -u $RunnerUser ./config.sh remove --token $RegistrationToken || true; fi" -Description 'remove existing config' -RunAsRoot -IgnoreFailure
}

# 6f. Configure runner
$configureCmd = "cd $runnerDir; sudo -u $RunnerUser ./config.sh $configFlags"
$cfgResult = Invoke-WslCommand -Command $configureCmd -Description 'configure runner' -RunAsRoot -IgnoreFailure
if ($cfgResult.ExitCode -ne 0) {
  if ($cfgResult.StdOut -match 'Http response code: NotFound') {
    throw @"
Runner registration failed with 404 (NotFound).
Likely causes:
  * Expired runner registration token (they expire quickly / single use)
  * Repository path typo (expected $RepoUrl)
  * Actions disabled or insufficient permissions

Resolution:
  1. Generate a NEW registration token: Repo -> Settings -> Actions -> Runners -> New self-hosted runner
  2. Re-run ONLY the configure step (no need to reinstall):
     wsl -d $Distro bash -lc 'cd $runnerDir; sudo -u $RunnerUser ./config.sh $configFlags'
     (Replace the token value inside $configFlags)
  3. If reconfiguration needed (runner name collision), add --replace.
Raw output excerpt:\n$($cfgResult.StdOut | Select-Object -First 20 | Out-String)
"@
  } else {
    throw "Runner configuration failed (exit $($cfgResult.ExitCode)). Output (first lines):`n$($cfgResult.StdOut | Select-Object -First 30 | Out-String)"
  }
}

# 7. Optional: systemd service (idempotent; will update if already exists with different restart policy)
if ($Service) {
    Write-Section "Installing / Updating systemd service (policy=$RestartPolicy, delay=${RestartSec}s)"
  # Ensure user tool/cache directories exist with proper ownership before the service starts
  Invoke-WslCommand -Command "mkdir -p /home/$RunnerUser/.dotnet /home/$RunnerUser/.nuget/packages /home/$RunnerUser/.cache/ms-playwright && chown -R ${RunnerUser}:${RunnerUser} /home/$RunnerUser/.dotnet /home/$RunnerUser/.nuget /home/$RunnerUser/.cache" -Description 'prepare user tool dirs' -RunAsRoot -IgnoreFailure
    $serviceUnit = @"
[Unit]
Description=GitHub Actions Runner ($RunnerName)
After=network.target

[Service]
User=$RunnerUser
WorkingDirectory=$runnerDir
ExecStart=$runnerDir/run.sh
Restart=$RestartPolicy
RestartSec=$RestartSec
KillSignal=SIGINT
TimeoutStopSec=600
Environment=HOME=/home/$RunnerUser
Environment=DOTNET_ROOT=/home/$RunnerUser/.dotnet
Environment=DOTNET_CLI_HOME=/home/$RunnerUser/.dotnet
Environment=NUGET_PACKAGES=/home/$RunnerUser/.nuget/packages
Environment=PLAYWRIGHT_BROWSERS_PATH=/home/$RunnerUser/.cache/ms-playwright
Environment=PATH=/home/$RunnerUser/.dotnet:/home/$RunnerUser/.dotnet/tools:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

[Install]
WantedBy=multi-user.target
"@
  # Only rewrite if content differs to avoid unnecessary restarts
  $tmpPath = [System.IO.Path]::GetTempFileName()
  Set-Content -Path $tmpPath -Value $serviceUnit -NoNewline
  $escaped = $serviceUnit.Replace("'","'\''")
  Invoke-WslCommand -Command "if [ ! -f /etc/systemd/system/github-runner.service ] || ! grep -q 'Restart=$RestartPolicy' /etc/systemd/system/github-runner.service || ! grep -q 'RestartSec=$RestartSec' /etc/systemd/system/github-runner.service; then echo '$escaped' > /etc/systemd/system/github-runner.service; systemctl daemon-reload; fi" -Description 'write/update service unit' -RunAsRoot
  Invoke-WslCommand -Command "systemctl enable github-runner >/dev/null 2>&1 || true; systemctl restart github-runner" -Description 'enable & restart service' -RunAsRoot
    Write-Host "Service ensured with restart policy '$RestartPolicy' (RestartSec=$RestartSec)." -ForegroundColor Green
    Write-Host "Use -RestartService to safely recycle the runner without reconfiguration." -ForegroundColor DarkGray
} else {
    Write-Section "Starting interactive runner (non-service)"
    Write-Host "(You can CTRL+C later; re-run with -Service to daemonize)" -ForegroundColor Yellow
    Write-Host "Launching run.sh in a separate window isn't supported directly; attach with:" -ForegroundColor Yellow
    Write-Host "  wsl -d $Distro sudo -u $RunnerUser bash -c 'cd $runnerDir && ./run.sh'" -ForegroundColor Yellow
}

Write-Section "Done"
Write-Host "Runner registered. In GitHub UI, verify under Settings > Actions > Runners." -ForegroundColor Cyan
if (-not $Service) {
    Write-Host "Remember: runner stops when WSL session stops. Use -Service for persistence." -ForegroundColor Yellow
}
