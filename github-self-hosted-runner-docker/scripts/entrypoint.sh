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

# Install necessary dependencies (keeps the container self-contained)
/usr/local/bin/install-deps.sh

# Download the GitHub runner binaries
/usr/local/bin/download-runner.sh

# Configure the GitHub runner (this will use GITHUB_URL and GITHUB_TOKEN)
/usr/local/bin/configure-runner.sh

# Start the GitHub runner process
/usr/local/bin/start-runner.sh

echo "Container started. Tailing runner logs..."
tail -f /runner/_diag/*.log /dev/null || tail -f /dev/null