echo "Configuring GitHub runner..."
#!/bin/bash

set -euo pipefail

# Source configuration file if present, otherwise rely on environment variables
if [ -f /config/runner.env ]; then
    echo "Sourcing configuration from /config/runner.env"
    # shellcheck disable=SC1090
    source /config/runner.env
else
    echo "Configuration file /config/runner.env not found. Using environment variables if present."
fi

# Validate required environment variables
missing=()
if [ -z "${GITHUB_URL:-}" ]; then missing+=(GITHUB_URL); fi
if [ -z "${GITHUB_TOKEN:-}" ]; then missing+=(GITHUB_TOKEN); fi
if [ -z "${RUNNER_NAME:-}" ]; then missing+=(RUNNER_NAME); fi
if [ -z "${RUNNER_LABELS:-}" ]; then missing+=(RUNNER_LABELS); fi

if [ ${#missing[@]} -ne 0 ]; then
    echo "Missing required environment variables: ${missing[*]}"
    echo "Either create /config/runner.env from runner.env.example or set the variables in the host environment."
    exit 1
fi

echo "Configuring GitHub runner..."
cd /actions-runner || exit 1

# Attempt to remove an old configuration (ignore failures)
./config.sh remove --unattended 2>/dev/null || true

# Run config with the provided values
./config.sh --unattended \
    --url "$GITHUB_URL" \
    --token "$GITHUB_TOKEN" \
    --name "$RUNNER_NAME" \
    --labels "$RUNNER_LABELS" \
    --work "${RUNNER_WORKDIR:-_work}" \
    --replace

echo "GitHub runner configured successfully."
touch /runner/.runner-configured || true