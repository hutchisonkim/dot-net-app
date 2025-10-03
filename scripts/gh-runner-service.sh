#!/usr/bin/env bash
set -euo pipefail

# Manage systemd service for runner
# Usage: gh-runner-service.sh --user ghrunner --dir /home/ghrunner/gh-runner --name runner-name --action install|start|stop|restart|status --restart-policy on-failure --restart-sec 8

RUNNER_USER="ghrunner"
RUNNER_DIR="/home/${RUNNER_USER}/gh-runner"
RUNNER_NAME="$(hostname)-wsl"
ACTION="status"
RESTART_POLICY="on-failure"
RESTART_SEC=8

while [ $# -gt 0 ]; do
  case "$1" in
    --user) RUNNER_USER="$2"; shift 2;;
    --dir) RUNNER_DIR="$2"; shift 2;;
    --name) RUNNER_NAME="$2"; shift 2;;
    --action) ACTION="$2"; shift 2;;
    --restart-policy) RESTART_POLICY="$2"; shift 2;;
    --restart-sec) RESTART_SEC="$2"; shift 2;;
    -h|--help) echo "Usage: $0 --action install|start|stop|restart|status [--user USER] [--dir DIR] [--name NAME]"; exit 0;;
    *) echo "Unknown arg: $1"; exit 1;;
  esac
done

SERVICE_FILE=/etc/systemd/system/github-runner.service

install_unit(){
  cat > "$SERVICE_FILE" <<EOF
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
Environment=HOME=$RUNNER_DIR
Environment=DOTNET_ROOT=$RUNNER_DIR/.dotnet
Environment=DOTNET_CLI_HOME=$RUNNER_DIR/.dotnet
Environment=NUGET_PACKAGES=$RUNNER_DIR/.nuget/packages
Environment=PLAYWRIGHT_BROWSERS_PATH=$RUNNER_DIR/.cache/ms-playwright
Environment=PATH=$RUNNER_DIR/.dotnet:$RUNNER_DIR/.dotnet/tools:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

[Install]
WantedBy=multi-user.target
EOF
  systemctl daemon-reload
  systemctl enable --now github-runner
}

case "$ACTION" in
  install)
    install_unit
    systemctl status github-runner --no-pager || true
    ;;
  start)
    systemctl enable --now github-runner || true
    systemctl status github-runner --no-pager || true
    ;;
  stop)
    systemctl stop github-runner || true
    ;;
  restart)
    systemctl restart github-runner || true
    systemctl status github-runner --no-pager || true
    ;;
  status)
    systemctl status github-runner --no-pager || true
    journalctl -u github-runner -n 50 --no-pager || true
    ;;
  uninstall)
    systemctl stop github-runner 2>/dev/null || true
    systemctl disable --now github-runner 2>/dev/null || true
    rm -f "$SERVICE_FILE" || true
    systemctl daemon-reload || true
    ;;
  *) echo "Unknown action: $ACTION"; exit 1;;
esac

echo "[gh-runner-service] action=$ACTION done"
