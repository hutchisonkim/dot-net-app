#!/bin/bash

set -euo pipefail

# Load config from /config/runner.env if present (this is optional)
if [ -f /config/runner.env ]; then
	echo "Sourcing configuration from /config/runner.env"
	# shellcheck disable=SC1090
	source /config/runner.env
fi

# Validate required environment variables (GITHUB_URL and GITHUB_TOKEN can be supplied via env or file)
missing=()
if [ -z "${GITHUB_URL:-}" ]; then missing+=(GITHUB_URL); fi
if [ -z "${GITHUB_TOKEN:-}" ]; then missing+=(GITHUB_TOKEN); fi
if [ -z "${RUNNER_NAME:-}" ]; then missing+=(RUNNER_NAME); fi
if [ -z "${RUNNER_LABELS:-}" ]; then missing+=(RUNNER_LABELS); fi

if [ ${#missing[@]} -ne 0 ]; then
	echo "Missing required environment variables: ${missing[*]}"
	echo "Either set them in the host environment or create /config/runner.env from runner.env.example."
	exit 1
fi

echo "Starting container initialization..."


# Ensure the runner directories are writable by the runner user
RUNNER_DIR="/actions-runner"
RUNNER_USER="${RUNNER_USER:-github-runner}"
echo "Setting ownership of $RUNNER_DIR and /runner to $RUNNER_USER"
chown -R "$RUNNER_USER":"$RUNNER_USER" "$RUNNER_DIR" /runner || true

# Create /config/runner.env from environment variables if it doesn't exist (keeps token inside container only)
CONFIG_DIR="/config"
CONFIG_FILE="$CONFIG_DIR/runner.env"
mkdir -p "$CONFIG_DIR"
echo "Creating $CONFIG_FILE from environment (not persisted in repo)"
cat > "$CONFIG_FILE" <<EOF
GITHUB_URL="${GITHUB_URL:-}"
GITHUB_TOKEN="${GITHUB_TOKEN:-}"
RUNNER_NAME="${RUNNER_NAME:-}"
RUNNER_LABELS="${RUNNER_LABELS:-}"
RUNNER_WORKDIR="${RUNNER_WORKDIR:-_work}"
EOF
chmod 600 "$CONFIG_FILE" || true
chown "$RUNNER_USER":"$RUNNER_USER" "$CONFIG_FILE" || true

# Configure the runner (removes existing config if any)
/usr/local/bin/configure-runner.sh

# Start the GitHub runner process
/usr/local/bin/start-runner.sh

echo "Container started. Monitoring runner process (no log tailing)."
# Prefer monitoring the runner PID file so we don't pollute the terminal with old logs.
# If a PID file exists, poll the process until it exits. Otherwise block silently.
PID_FILE="/runner/.runner.pid"
if [ -f "$PID_FILE" ]; then
	pid=$(cat "$PID_FILE" || echo "")
	if [ -n "$pid" ] && kill -0 "$pid" >/dev/null 2>&1; then
		echo "Monitoring runner pid $pid"
		# Poll until the process dies; don't print diag logs here.
		while kill -0 "$pid" >/dev/null 2>&1; do
			sleep 5
		done
		echo "Runner process $pid exited. Container will exit."
		exit 0
	else
		echo "PID file present but process $pid not running; exiting.";
		exit 1
	fi
else
	echo "No runner pid file found; sleeping to keep container alive (no logs)."
	# Keep container alive silently; user can `docker compose logs` to inspect logs.
	while true; do sleep 3600; done
fi