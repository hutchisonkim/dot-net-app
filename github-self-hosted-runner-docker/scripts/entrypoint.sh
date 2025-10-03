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

# If a persisted runner version matches the requested version, skip download to speed up startup
# NOTE: we install non-Docker dependencies at image build time so the container
# startup does not perform APT downloads. See Dockerfile which runs
# /usr/local/bin/install-deps.sh during build to make the runtime fast and
# cacheable.
RUNNER_DIR="/actions-runner"
VERSION_MARKER="$RUNNER_DIR/.runner-version"
if [ -f "$VERSION_MARKER" ]; then
	INSTALLED_VERSION=$(cat "$VERSION_MARKER" || true)
else
	INSTALLED_VERSION=""
fi

if [ "$INSTALLED_VERSION" = "${RUNNER_VERSION:-}" ] && [ -f "$RUNNER_DIR/run.sh" ]; then
	echo "Detected existing runner version $INSTALLED_VERSION in $RUNNER_DIR — skipping download."
else
	# Download the GitHub runner binaries
	/usr/local/bin/download-runner.sh
fi

# Ensure the runner directories are writable by the runner user
RUNNER_USER="${RUNNER_USER:-github-runner}"
echo "Setting ownership of $RUNNER_DIR and /runner to $RUNNER_USER"
chown -R "$RUNNER_USER":"$RUNNER_USER" "$RUNNER_DIR" /runner || true

# Create /config/runner.env from environment variables if it doesn't exist (keeps token inside container only)
CONFIG_DIR="/config"
CONFIG_FILE="$CONFIG_DIR/runner.env"
mkdir -p "$CONFIG_DIR"
if [ ! -f "$CONFIG_FILE" ]; then
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
else
	echo "$CONFIG_FILE already exists"
fi

# Configure the GitHub runner (this will use GITHUB_URL and GITHUB_TOKEN)
# If the runner is already configured, skip configuration unless explicitly forced.
# Additionally, detect stale markers coming from image/container caching by
# validating that essential runner files exist. If the marker exists but
# critical files are missing, force reconfiguration to avoid using a stale
# configuration state.
if [ -f "/runner/.runner-configured" ] || [ -f "$RUNNER_DIR/.runner-configured" ]; then
	if [ "${FORCE_RECONFIGURE:-0}" = "1" ]; then
		echo "FORCE_RECONFIGURE=1: running configure-runner.sh to reconfigure the runner."
		/usr/local/bin/configure-runner.sh
	else
		# Validate presence of essential runner files. If missing, treat the
		# configuration marker as stale and force reconfiguration.
		stale_marker=0
		# run.sh must exist in the actions-runner directory
		if [ ! -f "$RUNNER_DIR/run.sh" ]; then
			echo "Warning: $RUNNER_DIR/run.sh not found — configuration marker may be stale."
			stale_marker=1
		fi
		# Either .credentials or .env should exist to indicate a configured runner
		if [ ! -f "$RUNNER_DIR/.credentials" ] && [ ! -f "$RUNNER_DIR/.env" ]; then
			echo "Warning: no $RUNNER_DIR/.credentials or $RUNNER_DIR/.env found — configuration marker may be stale."
			stale_marker=1
		fi

		if [ "$stale_marker" -eq 1 ]; then
			echo "Detected stale configuration marker; forcing reconfiguration."
			# Force configure-runner.sh for this invocation only.
			FORCE_RECONFIGURE=1 /usr/local/bin/configure-runner.sh
		else
			echo "Detected existing configuration marker; skipping configuration step."
		fi
	fi
else
	/usr/local/bin/configure-runner.sh
fi

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