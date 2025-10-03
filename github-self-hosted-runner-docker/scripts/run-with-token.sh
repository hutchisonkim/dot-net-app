#!/usr/bin/env bash
set -euo pipefail

ROOT=$(cd "$(dirname "$0")/.." && pwd)

if [ -z "${1:-}" ]; then
  read -r -p "Enter GitHub registration token: " TOKEN
else
  TOKEN="$1"
fi

echo "Starting docker compose with token passed via env (token not persisted to disk)."
GITHUB_TOKEN="$TOKEN" docker compose up --build
