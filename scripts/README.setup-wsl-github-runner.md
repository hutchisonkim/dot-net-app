# WSL Self-Hosted GitHub Actions Runner Setup

This README documents how to use `setup-wsl-github-runner.ps1` to provision a Linux (Ubuntu) self‑hosted GitHub Actions runner inside WSL2 from your Windows machine.

> The script intentionally uses placeholders (e.g. `<owner>/<repo>`, `<REGISTRATION_TOKEN>`). Replace them with your values when running. Do **not** commit real tokens.

---
## Why WSL Ubuntu?
Using an Ubuntu runner matches `ubuntu-latest` used by most workflows, is faster for .NET + Playwright, and avoids Windows-specific browser/package quirks—while still letting you develop on Windows.

---
## Prerequisites
- Windows 11 (or Windows 10 22H2) with WSL enabled (`wsl --version` shows WSL >= 1.1 preferred)
- PowerShell 7 (pwsh) or Windows PowerShell (script targets pwsh but works on Windows PowerShell 5.1+)
- Admin rights (first run may need to install WSL distro / enable systemd)
- A short‑lived **GitHub Actions runner registration token** (NOT a PAT)
  - Generate: Repo → Settings → Actions → Runners → New self-hosted runner → (copy token)

---
## Script Overview
`setup-wsl-github-runner.ps1` will:
1. Ensure the target WSL distro (default: `Ubuntu`) exists (installs if missing unless `-SkipInstallDistro`)
2. Enable systemd inside WSL (`/etc/wsl.conf` + `wsl --shutdown`)
3. Create (or reuse) a dedicated Linux user (default: `ghrunner`)
4. Install dependencies & PowerShell (`pwsh`)
5. Download & configure the GitHub Actions runner (unattended)
6. Optionally install a systemd service (`--Service`) so it auto-starts

Runner directory inside WSL: `/home/<RunnerUser>/actions-runner`

---
## Quick Start
From the repository root in an elevated PowerShell session:

```powershell
pwsh -File .\scripts\setup-wsl-github-runner.ps1 \ 
  -Repo <owner>/<repo> \ 
  -RegistrationToken <REGISTRATION_TOKEN> \ 
  -Service
```

Typical example (DO NOT copy literally—replace placeholders):
```powershell
pwsh -File .\scripts\setup-wsl-github-runner.ps1 -Repo acme/sample-app -RegistrationToken AAAAAAAABBBBBBBBCCCCCCCC -Service
```

### Minimal (interactive, no service)
```powershell
pwsh -File .\scripts\setup-wsl-github-runner.ps1 -Repo <owner>/<repo> -RegistrationToken <REGISTRATION_TOKEN>
```
You then manually start the runner later:
```powershell
wsl -d Ubuntu sudo -u ghrunner bash -c 'cd /home/ghrunner/actions-runner && ./run.sh'
```

---
## Parameters
| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `-Repo` | Yes | — | `<owner>/<repo>` form of your GitHub repository |
| `-RegistrationToken` | Yes | — | Ephemeral token from runner registration UI |
| `-RunnerVersion` | No | `2.328.0` | Actions runner version tag (without leading `v`) |
| `-Distro` | No | `Ubuntu` | WSL distro name (`wsl -l -q` to list) |
| `-RunnerUser` | No | `ghrunner` | Linux user that owns runner |
| `-Labels` | No | `self-hosted,linux,x64,local` | Comma-separated runner labels |
| `-RunnerName` | No | `<hostname>-wsl` | Name shown in GitHub UI |
| `-Replace` | No | Off | Overwrite existing runner config with same name |
| `-Service` | No | Off | Install systemd service for auto-start |
| `-SkipInstallDistro` | No | Off | Fail instead of installing missing distro |

---
## After It Runs
Check in GitHub:
1. Repo → Settings → Actions → Runners
2. You should see your runner with the labels you provided
3. Trigger a workflow (push / PR / workflow_dispatch) to observe usage

### Prefer Fallback Mode
Keep workflow jobs like:
```yaml
runs-on: ubuntu-latest
```
They will keep using GitHub-hosted unless you explicitly request your runner with labels:
```yaml
runs-on: [self-hosted, linux, x64, local]
```
Or use conditional logic inside the workflow if desired.

---
## Managing the Service
If installed with `-Service`:
```powershell
wsl -d Ubuntu systemctl status github-runner
wsl -d Ubuntu journalctl -u github-runner -f    # live logs
wsl -d Ubuntu sudo systemctl restart github-runner
wsl -d Ubuntu sudo systemctl disable --now github-runner
```

To uninstall the service only:
```powershell
wsl -d Ubuntu sudo systemctl disable --now github-runner
wsl -d Ubuntu sudo rm /etc/systemd/system/github-runner.service
wsl -d Ubuntu sudo systemctl daemon-reload
```

---
## Updating the Runner Version
1. Stop service (or CTRL+C interactive run)
2. Download new tarball & reconfigure with `-Replace`:
```powershell
pwsh -File .\scripts\setup-wsl-github-runner.ps1 -Repo <owner>/<repo> -RegistrationToken <NEW_TOKEN> -RunnerVersion <NEW_VERSION> -Replace -Service
```

The script will fetch the new version and re-register.

---
## Regenerating a Token
The registration token is **short-lived**. If expired:
- Return to runner setup page → generate new token → re-run script with `-Replace` if needed.

---
## Removing the Runner Completely
Inside WSL:
```powershell
wsl -d Ubuntu sudo -u ghrunner bash -c '/home/ghrunner/actions-runner/config.sh remove --token <REGISTRATION_TOKEN>'
```
Then delete the directory:
```powershell
wsl -d Ubuntu sudo rm -rf /home/ghrunner/actions-runner
```
(Optional) remove user:
```powershell
wsl -d Ubuntu sudo userdel -r ghrunner
```

---
## Troubleshooting
| Symptom | Cause | Fix |
|---------|-------|-----|
| `systemd not detected` | Distro restarted not yet | Run `wsl --shutdown` then start distro again |
| `Token is no longer valid` | Token expired | Generate a new token & rerun with `-Replace` |
| Job still using hosted runner | Workflow runs-on unchanged | Add labels in YAML or specify `runs-on: [self-hosted, linux, x64, local]` |
| Playwright missing deps | Extra libraries not installed | Ensure libs in script succeeded (re-run dependencies section manually) |
| Service inactive after reboot | systemd not enabled | Confirm `/etc/wsl.conf` has `systemd=true`, then `wsl --shutdown` |

---
## Security Notes
- Treat `<REGISTRATION_TOKEN>` like a secret (don’t commit, don’t paste into logs).
- Do **not** allow untrusted fork PRs to run on self-hosted runners unless you understand the risk (default GitHub protection helps).
- Patch the OS regularly (`sudo apt-get update && sudo apt-get upgrade -y`).
- Use dedicated user (`ghrunner`) with least privileges.

---
## Advanced (Optional)
### Custom Labels
Give the runner a unique label (e.g. `mybox`) then target specific jobs:
```yaml
runs-on: [self-hosted, mybox]
```

### Multiple Runners
Create another directory and repeat with a different `-RunnerName` and token (scale concurrency).

### Ephemeral Pattern
Instead of long-lived service, omit `-Service` and script ephemeral teardown after each run (not implemented here, but can be extended).

---
## Example Full Command (Replace Scenario)
```powershell
pwsh -File .\scripts\setup-wsl-github-runner.ps1 `
  -Repo <owner>/<repo> `
  -RegistrationToken <REGISTRATION_TOKEN> `
  -RunnerVersion 2.328.0 `
  -Labels "self-hosted,linux,x64,local" `
  -RunnerName MyWSLRunner `
  -Replace `
  -Service
```

---
## Next Steps
- Verify runner in GitHub UI
- Trigger CI
- Optionally adjust workflow YAML to leverage labels or conditional self-hosted usage

---
Questions or improvements? Extend the script or open an issue in your repository.
