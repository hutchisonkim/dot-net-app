#!/usr/bin/env bash
set -euo pipefail

# gh-runner.sh
# Lightweight GitHub Actions self-hosted runner lifecycle manager (intended for WSL Ubuntu)
# Replaces most of the complicated PowerShell logic with native Bash to avoid quoting / escaping issues.
#
# Features:
#   Install / configure runner
#   Replace (reconfigure) existing runner
#   Remove (deregister + delete directory)
#   Restart service (update systemd unit with restart policy + env)
#   Update labels / runner name
#
# Usage examples:
#   sudo ./gh-runner.sh --repo owner/repo --token <REG_TOKEN> --service
#   sudo ./gh-runner.sh --repo owner/repo --token <NEW_TOKEN> --replace --service --name my-runner
#   sudo ./gh-runner.sh --restart --restart-policy on-failure --restart-sec 8
#   sudo ./gh-runner.sh --remove --repo owner/repo --token <REG_TOKEN>
#
# NOTE: Registration token is ephemeral (expires fast / single use).
# Run as root if you need to create user or install the systemd service.

VERSION_DEFAULT="2.328.0"
RUNNER_USER="ghrunner"
RUNNER_VERSION="$VERSION_DEFAULT"
RUNNER_LABELS="self-hosted,linux,x64,local"
RUNNER_NAME=""
RUNNER_DIR_BASE="/home/${RUNNER_USER}"
RUNNER_DIR=""
REPO=""
TOKEN=""
REPLACE=0
SERVICE=0
REMOVE=0
RESTART=0
ISOLATED=0
RESTART_POLICY="on-failure"
RESTART_SEC=8
DEBUG=0

log(){ printf "[gh-runner] %s\n" "$*"; }
err(){ printf "[gh-runner][ERROR] %s\n" "$*" >&2; }
dbg(){ if [ "$DEBUG" -eq 1 ]; then printf "[gh-runner][DEBUG] %s\n" "$*"; fi }

usage(){ cat <<USAGE
GitHub Actions self-hosted runner manager

Required for install/replace: --repo owner/repo --token <REG_TOKEN>

Flags:
  --repo owner/repo          Repository (owner/name)
  --token TOKEN              Ephemeral registration token
  --version V                Runner version (default ${VERSION_DEFAULT})
  --labels LIST              Comma-separated labels (default self-hosted,linux,x64,local)
  --name NAME                Runner name (default <hostname>-wsl)
  --user USER                Runner UNIX user (default ghrunner)
  --replace                  Replace existing runner with same name
  --service                  Install/update systemd service
  --remove                   Deregister and delete the runner directory
  --restart                  Restart service (no reconfigure)
  --isolated                 Append 'isolated' label (if not present)
  --restart-policy P         always|on-failure (default on-failure)
  --restart-sec N            RestartSec delay (default 8)
  --debug                    Verbose debug output
  -h|--help                  Show help

Examples:
  sudo ./gh-runner.sh --repo foo/bar --token ABC --service
  sudo ./gh-runner.sh --repo foo/bar --token NEW --replace --service --name dev-runner
  sudo ./gh-runner.sh --restart --restart-policy on-failure --restart-sec 10
  sudo ./gh-runner.sh --remove --repo foo/bar --token ABC
USAGE
}

need(){ command -v "$1" >/dev/null 2>&1 || { err "Missing required command: $1"; exit 1; }; }

# Parse args
while [ $# -gt 0 ]; do
  case "$1" in
    --repo) REPO="$2"; shift 2;;
    --token) TOKEN="$2"; shift 2;;
    --version) RUNNER_VERSION="$2"; shift 2;;
    --labels) RUNNER_LABELS="$2"; shift 2;;
    --name) RUNNER_NAME="$2"; shift 2;;
    --user) RUNNER_USER="$2"; shift 2;;
    --replace) REPLACE=1; shift;;
    --service) SERVICE=1; shift;;
    --remove) REMOVE=1; shift;;
    --restart) RESTART=1; shift;;
    --isolated) ISOLATED=1; shift;;
    --restart-policy) RESTART_POLICY="$2"; shift 2;;
    --restart-sec) RESTART_SEC="$2"; shift 2;;
    --debug) DEBUG=1; shift;;
    -h|--help) usage; exit 0;;
    *) err "Unknown arg: $1"; usage; exit 1;;
  esac
done

# Derived paths (after user might change RUNNER_USER)
RUNNER_DIR_BASE="/home/${RUNNER_USER}"
RUNNER_DIR="${RUNNER_DIR_BASE}/gh-runner"

if [ "$ISOLATED" -eq 1 ] && ! echo "$RUNNER_LABELS" | grep -qi 'isolated'; then
  RUNNER_LABELS="${RUNNER_LABELS},isolated"
fi

if [ -z "$RUNNER_NAME" ]; then
  RUNNER_NAME="$(hostname)-wsl"
fi

if [ "$RESTART_POLICY" != "always" ] && [ "$RESTART_POLICY" != "on-failure" ]; then
  err "Invalid --restart-policy $RESTART_POLICY (must be always|on-failure)"; exit 1
fi
if ! echo "$RESTART_SEC" | grep -Eq '^[0-9]+$'; then err "--restart-sec must be numeric"; exit 1; fi
if [ "$RESTART_SEC" -lt 1 ] || [ "$RESTART_SEC" -gt 300 ]; then err "--restart-sec out of range (1-300)"; exit 1; fi

need curl; need tar; need grep; need awk; need sed

if [ "$REMOVE" -eq 1 ]; then
  [ -z "$REPO" ] && { err "--repo required for --remove"; exit 1; }
  [ -z "$TOKEN" ] && { err "--token required for --remove"; exit 1; }
  log "Stopping service if present"; systemctl stop github-runner 2>/dev/null || true
  if [ -x "${RUNNER_DIR}/config.sh" ]; then
    log "Deregistering runner from $REPO" || true
    sudo -u "$RUNNER_USER" bash -c "cd '$RUNNER_DIR' && ./config.sh remove --token '$TOKEN'" || true
  fi
  log "Removing directory $RUNNER_DIR"; rm -rf "$RUNNER_DIR"
  log "Disable service"; systemctl disable github-runner 2>/dev/null || true
  log "Done (remove)."; exit 0
fi

if [ "$RESTART" -eq 1 ]; then
  log "Restart requested (policy=$RESTART_POLICY sec=$RESTART_SEC)"
  if [ ! -f /etc/systemd/system/github-runner.service ]; then
    err "Service file missing; install first with --service"; exit 1
  fi
  CURRENT_UNIT=$(cat /etc/systemd/system/github-runner.service || true)
  UPDATE=0
  echo "$CURRENT_UNIT" | grep -q "Restart=$RESTART_POLICY" || UPDATE=1
  echo "$CURRENT_UNIT" | grep -q "RestartSec=$RESTART_SEC" || UPDATE=1
  echo "$CURRENT_UNIT" | grep -q 'Environment=HOME=' || UPDATE=1
  if [ $UPDATE -eq 1 ]; then
    log "Updating service unit"
    cat > /etc/systemd/system/github-runner.service <<EOF
[Unit]
Description=GitHub Actions Runner ($RUNNER_NAME)
After=network.target

[Service]
User=$RUNNER_USER
WorkingDirectory=$RUNNER_DIR
ExecStart=$RUNNER_DIR/run.sh
Restart=$RESTART_POLICY
RestartSec=$RESTART_SEC
KillSignal=SIGINT
TimeoutStopSec=600
Environment=HOME=$RUNNER_DIR_BASE
Environment=DOTNET_ROOT=$RUNNER_DIR_BASE/.dotnet
Environment=DOTNET_CLI_HOME=$RUNNER_DIR_BASE/.dotnet
Environment=NUGET_PACKAGES=$RUNNER_DIR_BASE/.nuget/packages
Environment=PLAYWRIGHT_BROWSERS_PATH=$RUNNER_DIR_BASE/.cache/ms-playwright
Environment=PATH=$RUNNER_DIR_BASE/.dotnet:$RUNNER_DIR_BASE/.dotnet/tools:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

[Install]
WantedBy=multi-user.target
EOF
    systemctl daemon-reload
  else
    log "Service unit already up to date"
  fi
  systemctl stop github-runner 2>/dev/null || true
  pkill -f Runner.Listener 2>/dev/null || true
  rm -f "$RUNNER_DIR/.session" 2>/dev/null || true
  systemctl start github-runner
  systemctl is-active --quiet github-runner && log "Service active" || { err "Service failed to start"; exit 1; }
  journalctl -u github-runner -n 25 --no-pager || true
  exit 0
fi

# Install / Replace path
[ -z "$REPO" ] && { err "--repo required (install/replace)"; usage; exit 1; }
[ -z "$TOKEN" ] && { err "--token required (install/replace)"; usage; exit 1; }

if ! id -u "$RUNNER_USER" >/dev/null 2>&1; then
  log "Creating user $RUNNER_USER"
  useradd -m -s /bin/bash "$RUNNER_USER"
fi

mkdir -p "$RUNNER_DIR" && chown "$RUNNER_USER:$RUNNER_USER" "$RUNNER_DIR"
cd "$RUNNER_DIR"

PKG="actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz"
if [ ! -f "$PKG" ]; then
  log "Downloading runner v${RUNNER_VERSION}";
  curl -sSLo "$PKG" -L "https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/${PKG}"
fi

if [ ! -f config.sh ]; then
  log "Extracting runner";
  tar xzf "$PKG";
  chown -R "$RUNNER_USER:$RUNNER_USER" "$RUNNER_DIR";
fi

if [ $REPLACE -eq 1 ] && [ -f .runner ]; then
  log "Removing existing configuration (replace)";
  sudo -u "$RUNNER_USER" ./config.sh remove --token "$TOKEN" || true
fi

FLAGS=( --url "https://github.com/${REPO}" --token "$TOKEN" --name "$RUNNER_NAME" --labels "$RUNNER_LABELS" --unattended )
[ $REPLACE -eq 1 ] && FLAGS+=( --replace )

log "Configuring runner name=$RUNNER_NAME labels=$RUNNER_LABELS"
sudo -u "$RUNNER_USER" ./config.sh "${FLAGS[@]}"

if [ $SERVICE -eq 1 ]; then
  log "Ensuring service (policy=$RESTART_POLICY sec=$RESTART_SEC)"
  cat > /etc/systemd/system/github-runner.service <<EOF
[Unit]
Description=GitHub Actions Runner ($RUNNER_NAME)
After=network.target

[Service]
User=$RUNNER_USER
WorkingDirectory=$RUNNER_DIR
ExecStart=$RUNNER_DIR/run.sh
Restart=$RESTART_POLICY
RestartSec=$RESTART_SEC
KillSignal=SIGINT
TimeoutStopSec=600
Environment=HOME=$RUNNER_DIR_BASE
Environment=DOTNET_ROOT=$RUNNER_DIR_BASE/.dotnet
Environment=DOTNET_CLI_HOME=$RUNNER_DIR_BASE/.dotnet
Environment=NUGET_PACKAGES=$RUNNER_DIR_BASE/.nuget/packages
Environment=PLAYWRIGHT_BROWSERS_PATH=$RUNNER_DIR_BASE/.cache/ms-playwright
Environment=PATH=$RUNNER_DIR_BASE/.dotnet:$RUNNER_DIR_BASE/.dotnet/tools:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

[Install]
WantedBy=multi-user.target
EOF
  systemctl daemon-reload
  systemctl enable --now github-runner
  journalctl -u github-runner -n 25 --no-pager || true
else
  log "Runner configured (non-service). Start manually with: sudo -u $RUNNER_USER bash -c 'cd $RUNNER_DIR && ./run.sh'"
fi

log "Done."