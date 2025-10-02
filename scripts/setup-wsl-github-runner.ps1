<#
Lightweight delegator for managing the WSL GitHub Actions runner.

Behavior:
- If invoked with -EnableSystemd, the script will enable systemd inside the chosen WSL distro
  by writing /etc/wsl.conf (if necessary) and instructing you to run `wsl --shutdown`.
- Otherwise, this script forwards all remaining arguments to the native Bash manager
  `./scripts/gh-runner.sh` inside the WSL distro. The heavy lifecycle operations live in that
  Bash script to avoid brittle cross-shell quoting and parsing.

Usage examples:
  # Enable systemd in the Ubuntu distro (one-time):
  pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/setup-wsl-github-runner.ps1 -EnableSystemd

  # Forward args to gh-runner.sh (runs inside WSL):
  pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/setup-wsl-github-runner.ps1 -- --repo owner/repo --token TOKEN --service

Notes:
- The wrapper purposely keeps logic small. Edit `scripts/gh-runner.sh` for install/replace/remove/restart behavior.
#>

[CmdletBinding()]
param(
    [switch]$EnableSystemd,
    [string]$Distro = 'Ubuntu',
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$RemainingArgs
)

function Write-Info([string]$m) { Write-Host "[gh-runner.ps1] $m" -ForegroundColor Cyan }
function Write-Err([string]$m) { Write-Host "[gh-runner.ps1] $m" -ForegroundColor Red }

if (-not (Get-Command wsl -ErrorAction SilentlyContinue)) {
    Write-Err "wsl.exe not found in PATH. Please install WSL / enable Windows Subsystem for Linux."
    exit 2
}

if ($EnableSystemd) {
    Write-Info "Enabling systemd in distro '$Distro'..."
    $enableScript = @'
set -e
if [ -f /etc/wsl.conf ]; then
  grep -q "^\[boot\]" /etc/wsl.conf || echo "[boot]" >> /etc/wsl.conf
  grep -q "^systemd\s*=\s*true" /etc/wsl.conf || sed -i "/^\[boot\]/a systemd=true" /etc/wsl.conf || echo "systemd=true" >> /etc/wsl.conf
else
  cat >/etc/wsl.conf <<'WCONF'
[boot]

WCONF
fi
'@

    # Run in the distro as root
    wsl -d $Distro -- bash -lc $enableScript
    $ec = $LASTEXITCODE
    if ($ec -ne 0) { Write-Err "Failed to enable systemd inside distro '$Distro' (exit $ec)"; exit $ec }
    Write-Info "Wrote /etc/wsl.conf with systemd=true. Run 'wsl --shutdown' from Windows to apply changes."
    exit 0
}

if (-not $RemainingArgs -or $RemainingArgs.Length -eq 0) {
    Write-Host "Usage: pwsh -File scripts/setup-wsl-github-runner.ps1 -- [args to pass to ./scripts/gh-runner.sh inside WSL]"
    Write-Host "Or: pwsh -File scripts/setup-wsl-github-runner.ps1 -EnableSystemd"
    exit 0
}

# Join remaining args preserving basic spacing; they will be re-split by bash.
$argList = $RemainingArgs -join ' '

# Try to cd into a repository path inside WSL that matches the checked-out workspace.
# Prefer /workspaces/dot-net-app (used by some dev containers); fallback to /mnt/c/___work/dot-net-app.
$dirs = '/workspaces/dot-net-app /mnt/c/___work/dot-net-app'

# Build a literal bash command; use a single-quoted PowerShell string so $d isn't expanded by PowerShell.
$bash = 'for d in /workspaces/dot-net-app /mnt/c/___work/dot-net-app; do if [ -d "$d" ]; then cd "$d"; break; fi; done; exec sudo bash ./scripts/gh-runner.sh ' + $argList

Write-Info "Forwarding arguments to gh-runner.sh inside distro '$Distro': $argList"

# Execute the command via wsl.exe
& wsl -d $Distro -- bash -lc $bash
$exit = $LASTEXITCODE
if ($exit -ne 0) { Write-Err "gh-runner.sh exited with code $exit" }
exit $exit
