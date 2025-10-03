#!/usr/bin/env bash
set -euo pipefail

HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/.." && pwd)

ENV_FILE="$ROOT/.env"

if [ -f "$ENV_FILE" ]; then
  echo ".env already exists at $ENV_FILE. Edit it if you need to change values." 
else
  cp "$ROOT/.env.example" "$ENV_FILE"
  echo "Copied .env.example to .env"
fi

read -r -p "Enter GitHub URL (e.g. https://github.com/owner/repo or https://github.com/org): " GITHUB_URL
read -r -p "Enter runner name (example: my-runner): " RUNNER_NAME
read -r -p "Enter labels (comma-separated, e.g. self-hosted,linux,x64): " RUNNER_LABELS

echo "You will now be prompted for the short-lived registration token. It will be added to .env but not committed." 
read -r -s -p "Enter registration token: " GITHUB_TOKEN
echo

# Update .env safely
sed -i.bak -E "s~^RUNNER_REPOSITORY=.*~RUNNER_REPOSITORY=${GITHUB_URL}~" "$ENV_FILE" || true
sed -i.bak -E "s~^RUNNER_NAME=.*~RUNNER_NAME=${RUNNER_NAME}~" "$ENV_FILE" || true
sed -i.bak -E "s~^RUNNER_LABELS=.*~RUNNER_LABELS=${RUNNER_LABELS}~" "$ENV_FILE" || true
if grep -q '^GITHUB_TOKEN=' "$ENV_FILE" 2>/dev/null; then
  sed -i.bak -E "s~^GITHUB_TOKEN=.*~GITHUB_TOKEN=${GITHUB_TOKEN}~" "$ENV_FILE"
else
  echo "GITHUB_TOKEN=${GITHUB_TOKEN}" >> "$ENV_FILE"
fi

echo ".env populated. KEEP THE TOKEN SECRET. Don't commit .env to git."
