#!/usr/bin/env bash
set -euo pipefail

# Configure/register runner (assumes runner extracted in target dir)
# Usage: gh-runner-configure.sh --repo owner/repo --token TOKEN --user ghrunner --dir /home/ghrunner/gh-runner --name name --labels labels [--replace]

REPO=""
TOKEN=""
RUNNER_USER="ghrunner"
RUNNER_DIR="/home/${RUNNER_USER}/gh-runner"
RUNNER_NAME=""
RUNNER_LABELS="self-hosted,linux,x64,local"
REPLACE=0

while [ $# -gt 0 ]; do
  case "$1" in
    --repo) REPO="$2"; shift 2;;
    --token) TOKEN="$2"; shift 2;;
    --user) RUNNER_USER="$2"; shift 2;;
    --dir) RUNNER_DIR="$2"; shift 2;;
    --name) RUNNER_NAME="$2"; shift 2;;
    --labels) RUNNER_LABELS="$2"; shift 2;;
    --replace) REPLACE=1; shift;;
    -h|--help) echo "Usage: $0 --repo owner/repo --token TOKEN [--user USER] [--dir DIR] [--name NAME] [--labels L] [--replace]"; exit 0;;
    *) echo "Unknown arg: $1"; exit 1;;
  esac
done

if [ -z "$REPO" ] || [ -z "$TOKEN" ]; then
  echo "--repo and --token are required"; exit 1
fi

if [ -z "$RUNNER_NAME" ]; then
  RUNNER_NAME="$(hostname)-wsl"
fi

cd "$RUNNER_DIR"

if [ $REPLACE -eq 1 ] && [ -f .runner ]; then
  echo "[gh-runner-configure] Removing existing config"
  sudo -u "$RUNNER_USER" ./config.sh remove --token "$TOKEN" || true
fi

FLAGS=( --url "https://github.com/${REPO}" --token "$TOKEN" --name "$RUNNER_NAME" --labels "$RUNNER_LABELS" --unattended )
[ $REPLACE -eq 1 ] && FLAGS+=( --replace )

echo "[gh-runner-configure] Configuring runner $RUNNER_NAME labels=$RUNNER_LABELS"
sudo -u "$RUNNER_USER" ./config.sh "${FLAGS[@]}"

echo "[gh-runner-configure] Done."
