#!/usr/bin/env bash
set -euo pipefail

HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/.." && pwd)

ENV_FILE="$ROOT/.env"

echo
if [ -f "$ENV_FILE" ]; then
  echo ".env already exists at $ENV_FILE. Edit it if you need to change values." 
else
  cp "$ROOT/.env.example" "$ENV_FILE"
  echo "Copied .env.example to .env (token intentionally left blank)."
fi

read -r -p "Enter GitHub URL (e.g. https://github.com/owner/repo or https://github.com/org): " GITHUB_URL
read -r -p "Enter runner name (example: my-runner): " RUNNER_NAME
read -r -p "Enter labels (comma-separated, e.g. self-hosted,linux,x64): " RUNNER_LABELS

# Update .env safely but DO NOT write the token to disk
sed -i.bak -E "s~^GITHUB_URL=.*~GITHUB_URL=${GITHUB_URL}~" "$ENV_FILE" || true
sed -i.bak -E "s~^RUNNER_NAME=.*~RUNNER_NAME=${RUNNER_NAME}~" "$ENV_FILE" || true
sed -i.bak -E "s~^RUNNER_LABELS=.*~RUNNER_LABELS=${RUNNER_LABELS}~" "$ENV_FILE" || true

cat <<EOF
.env created/updated at $ENV_FILE. The registration token will NOT be written to disk.
To start the runner without storing the token, either:

# 1) Use the run-with-token helper (recommended):
  ./scripts/run-with-token.sh

# 2) Or export the token into your shell for this session and run compose:
  # Bash example:
  export GITHUB_TOKEN=\"<REGISTRATION_TOKEN>\"
  docker compose up --build

  # PowerShell example:
  $env:GITHUB_TOKEN = '<REGISTRATION_TOKEN>'
  docker compose up --build

EOF
