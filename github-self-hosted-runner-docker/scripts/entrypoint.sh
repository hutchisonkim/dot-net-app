#!/bin/bash
set -euo pipefail

# Load optional config
if [ -f /config/runner.env ]; then
    echo "Sourcing configuration from /config/runner.env"
    # shellcheck disable=SC1090
    source /config/runner.env
fi

missing=()
if [ -z "${GITHUB_URL:-}" ]; then missing+=(GITHUB_URL); fi
if [ -z "${GITHUB_TOKEN:-}" ]; then missing+=(GITHUB_TOKEN); fi
if [ -z "${RUNNER_LABELS:-}" ]; then missing+=(RUNNER_LABELS); fi

if [ ${#missing[@]} -ne 0 ]; then
    echo "Missing required environment variables: ${missing[*]}"
    exit 1
fi

echo "Starting container initialization..."

RUNNER_DIR="/actions-runner"
RUNNER_USER="${RUNNER_USER:-github-runner}"
chown -R "$RUNNER_USER":"$RUNNER_USER" "$RUNNER_DIR" /runner || true

CONFIG_DIR="/config"
CONFIG_FILE="$CONFIG_DIR/runner.env"
mkdir -p "$CONFIG_DIR"

# Create local config file
cat > "$CONFIG_FILE" <<EOF
GITHUB_URL="${GITHUB_URL:-}"
GITHUB_TOKEN="${GITHUB_TOKEN:-}"
RUNNER_LABELS="${RUNNER_LABELS:-}"
RUNNER_WORKDIR="${RUNNER_WORKDIR:-_work}"
EOF
chmod 600 "$CONFIG_FILE"
chown "$RUNNER_USER":"$RUNNER_USER" "$CONFIG_FILE"

# Configure the runner (ephemeral)
echo "Configuring ephemeral GitHub runner..."
/usr/local/bin/configure-runner.sh

# Cleanup handler (removes runner on container exit)
cleanup() {
    echo "Container stopping; removing runner registration..."
    cd "$RUNNER_DIR"
    if [ -x ./config.sh ]; then
        sudo -u "$RUNNER_USER" bash -lc "./config.sh remove --token \"$GITHUB_TOKEN\"" || true
    fi
}
trap cleanup EXIT

# Start the GitHub runner process
/usr/local/bin/start-runner.sh &

PID=$!
echo "Runner started with PID $PID"

wait $PID
echo "Runner process exited, container shutting down."
