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
    [Parameter(Mandatory=$true)] [string]$Repo,
    [Parameter(Mandatory=$true)] [string]$RegistrationToken,
    [string]$RunnerVersion = '2.328.0',
    [string]$Distro = 'Ubuntu',
    [string]$RunnerUser = 'ghrunner',
    [string]$Labels = 'self-hosted,linux,x64,local',
    [string]$RunnerName,
    [switch]$Replace,
    [switch]$Service,
    [switch]$SkipInstallDistro
)

function Write-Section($Title) { Write-Host "`n=== $Title ===" -ForegroundColor Cyan }

function Assert-Command($Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' not found in PATH."
    }
}

Write-Section "Pre-flight"
Assert-Command wsl

if (-not $Repo.Contains('/')) { throw "Repo must be in 'owner/name' format" }
$RepoUrl = "https://github.com/$Repo"
if (-not $RunnerName) { $RunnerName = "$(hostname)-wsl" }

Write-Host "Repo: $RepoUrl"; Write-Host "Runner name: $RunnerName"; Write-Host "Version: $RunnerVersion";

# 1. Ensure distro exists
$existingDistros = (& wsl.exe -l -q) 2>$null | Where-Object { $_ }
if ($existingDistros -notcontains $Distro) {
    if ($SkipInstallDistro) { throw "Distro '$Distro' not installed and SkipInstallDistro set." }
    Write-Section "Installing WSL Distro $Distro"
    Write-Host "Attempting: wsl --install -d $Distro (Requires Windows 10 22H2 / 11 and elevation)" -ForegroundColor Yellow
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
  & wsl.exe -d $Distro /bin/bash -c 'printf "[boot]\nsystemd=true\n" | sudo tee /etc/wsl.conf >/dev/null' || throw "Failed writing /etc/wsl.conf"
    Write-Host "Shutting down WSL to apply systemd setting" -ForegroundColor Green
    & wsl.exe --shutdown
    Write-Host "Restarting distro to validate systemd" -ForegroundColor Green
}

# 3. Create runner user if missing
Write-Section "Ensuring runner user '$RunnerUser' exists"
& wsl.exe -d $Distro /bin/bash -c "id -u $RunnerUser >/dev/null 2>&1 || sudo useradd -m -s /bin/bash $RunnerUser" || throw "Failed ensuring user"

# 4. Install dependencies (curl, git, etc.)
Write-Section "Installing dependencies (curl git tar ca-certificates lib libs for Playwright)"
& wsl.exe -d $Distro /bin/bash -c "sudo apt-get update && sudo apt-get install -y curl git tar ca-certificates \"libnss3\" \"libatk-bridge2.0-0\" libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgtk-3-0 libdrm2 libasound2 fonts-liberation" || throw "Apt install failed"

# 5. (Optional) Install PowerShell (needed by your workflow for pwsh step)
Write-Section "Ensuring PowerShell (pwsh) installed"
& wsl.exe -d $Distro /bin/bash -c "command -v pwsh >/dev/null 2>&1 || (wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb && sudo dpkg -i packages-microsoft-prod.deb && sudo apt-get update && sudo apt-get install -y powershell)" || throw "Failed installing pwsh"

# 6. Download & configure runner
Write-Section "Configuring GitHub Actions runner"
$runnerDir = "/home/$RunnerUser/actions-runner"
$labelsEscaped = $Labels.Replace('"','')
$configFlags = "--url $RepoUrl --token $RegistrationToken --name $RunnerName --labels $labelsEscaped --unattended"
if ($Replace) { $configFlags += ' --replace' }

$runnerScript = @"
set -euo pipefail
mkdir -p $runnerDir
cd $runnerDir
if [ ! -d .git ]; then :; fi
VERSION=$RunnerVersion
PKG=actions-runner-linux-x64-${RunnerVersion}.tar.gz
if [ ! -f "${PKG}" ]; then
  echo "Downloading runner $RunnerVersion";
  curl -sSLo "$PKG" -L https://github.com/actions/runner/releases/download/v${RunnerVersion}/$PKG
fi
# (Hash validation optional: supply SHA manually if you want)
if [ ! -d _diag ]; then
  tar xzf "$PKG"
fi
# Remove existing config if replacing
if [ -f .runner ] && echo "$configFlags" | grep -q -- '--replace'; then
  echo "Replacing existing runner config";
  ./config.sh remove --token $RegistrationToken || true
fi
./config.sh $configFlags
"@

& wsl.exe -d $Distro /bin/bash -c "echo '$runnerScript' | sudo -u $RunnerUser /bin/bash" || throw "Runner configuration failed"

# 7. Optional: systemd service
if ($Service) {
    Write-Section "Installing systemd service"
    $serviceUnit = @"
[Unit]
Description=GitHub Actions Runner ($RunnerName)
After=network.target

[Service]
User=$RunnerUser
WorkingDirectory=$runnerDir
ExecStart=$runnerDir/run.sh
Restart=always
RestartSec=5
KillSignal=SIGINT
TimeoutStopSec=600

[Install]
WantedBy=multi-user.target
"@
    & wsl.exe -d $Distro /bin/bash -c "echo '$serviceUnit' | sudo tee /etc/systemd/system/github-runner.service >/dev/null" || throw "Failed writing service"
    & wsl.exe -d $Distro /bin/bash -c "sudo systemctl daemon-reload && sudo systemctl enable --now github-runner" || throw "Failed enabling service"
    Write-Host "Service installed and started." -ForegroundColor Green
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
