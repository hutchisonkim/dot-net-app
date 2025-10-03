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
		# If the user provided a management API token and repository, verify
		# the runner is online server-side via the GitHub API. We will not
		# trust local marker files; absence of API credentials or an API
		# response indicating the runner is not online will cause a
		# reconfiguration to ensure the runner registers correctly.
		check_server_registration() {
			# Return codes:
			# 0 = runner found and status == online
			# 1 = runner missing or not online (including API errors or missing creds)
			if [ -z "${GITHUB_API_TOKEN:-}" ] || [ -z "${GITHUB_REPOSITORY:-}" ]; then
				return 1
			fi

			# Determine API base (handle GitHub Enterprise Server)
			if echo "$GITHUB_URL" | grep -qi "github.com"; then
				API_BASE="https://api.github.com"
			else
				API_BASE="${GITHUB_URL%/}/api/v3"
			fi

			owner=$(echo "$GITHUB_REPOSITORY" | cut -d'/' -f1)
			repo=$(echo "$GITHUB_REPOSITORY" | cut -d'/' -f2-)

			# Query runners for the repository
			resp=$(curl -sS -w "\n%{http_code}" -H "Authorization: token $GITHUB_API_TOKEN" -H "Accept: application/vnd.github+json" "$API_BASE/repos/$owner/$repo/actions/runners?per_page=100" ) || true
			http_status=$(echo "$resp" | tail -n1)
			body=$(echo "$resp" | sed '$d')

			if [ -z "$http_status" ] || [ "$http_status" -ge 400 ]; then
				echo "Warning: GitHub API check failed with HTTP status ${http_status:-unknown}."
				return 1
			fi

			# Extract the runner status for the given name
			if command -v jq >/dev/null 2>&1; then
				status=$(echo "$body" | jq -r --arg name "$RUNNER_NAME" '.runners[] | select(.name==$name) | .status' | head -n1 || true)
			else
				# Fallback parsing; may be less reliable but sufficient to
				# determine 'online' vs other statuses.
				# Find the entry for the runner name and then look for the
				# nearest status field following it.
				status=$(echo "$body" | awk -v name="\"name\": \"$RUNNER_NAME\"" 'index($0, name){found=1} found && /"status"/ {gsub(/.*"status"\s*:\s*"|".*/,"",$0); print $0; exit}' || true)
			fi

			if [ "$status" = "online" ]; then
				return 0
			fi

			return 1
		}

		# Decide reconfiguration based solely on server-side online check.
		need_reconfig=1
		if [ -n "${GITHUB_API_TOKEN:-}" ] && [ -n "${GITHUB_REPOSITORY:-}" ]; then
			echo "Verifying runner registration on GitHub via API..."
			if check_server_registration; then
				echo "Runner '$RUNNER_NAME' is registered and online on GitHub — skipping configuration."
				need_reconfig=0
			else
				echo "Runner '$RUNNER_NAME' is not registered/online on GitHub — will reconfigure."
				need_reconfig=1
			fi
		else
			echo "No GITHUB_API_TOKEN/GITHUB_REPOSITORY provided — treating local marker as stale and forcing reconfiguration."
			need_reconfig=1
		fi

		if [ "$need_reconfig" -eq 1 ]; then
			echo "Forcing reconfiguration."
			FORCE_RECONFIGURE=1 /usr/local/bin/configure-runner.sh
		else
			echo "Detected existing configuration marker and verified online — skipping configuration step."
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