#!/usr/bin/env bash
set -euo pipefail

# Deregister and remove runner directory
# Usage: gh-runner-remove.sh --user ghrunner --dir /home/ghrunner/gh-runner --token TOKEN --repo owner/repo

RUNNER_USER="ghrunner"
RUNNER_DIR="/home/${RUNNER_USER}/gh-runner"
TOKEN=""
REPO=""

while [ $# -gt 0 ]; do
  case "$1" in
    --user) RUNNER_USER="$2"; shift 2;;
    --dir) RUNNER_DIR="$2"; shift 2;;
    --token) TOKEN="$2"; shift 2;;
    --repo) REPO="$2"; shift 2;;
    -h|--help) echo "Usage: $0 --token TOKEN --repo owner/repo [--user USER] [--dir DIR]"; exit 0;;
    *) echo "Unknown arg: $1"; exit 1;;
  esac
done

if [ -z "$TOKEN" ] || [ -z "$REPO" ]; then
  echo "--token and --repo required"; exit 1
fi

echo "[gh-runner-remove] Stopping service if present"
systemctl stop github-runner 2>/dev/null || true

if [ -x "${RUNNER_DIR}/config.sh" ]; then
  echo "[gh-runner-remove] Deregistering runner from $REPO"
  sudo -u "$RUNNER_USER" bash -c "cd '$RUNNER_DIR' && ./config.sh remove --token '$TOKEN'" || true
fi

echo "[gh-runner-remove] Removing directory $RUNNER_DIR"
rm -rf "$RUNNER_DIR"
systemctl disable github-runner 2>/dev/null || true
echo "[gh-runner-remove] Done."
