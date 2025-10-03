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
ACTION_RUNNER_DIR="/actions-runner"
cd "$ACTION_RUNNER_DIR" || exit 1

## If the runner has already been successfully configured, avoid re-running
## configuration on container start. Reconfiguring automatically can lead to
## session conflicts on GitHub (a session may already exist for the same
## runner name). To force reconfiguration/cleanup explicitly set
## FORCE_RECONFIGURE=1 in the environment.
if [ -f "/runner/.runner-configured" ] || [ -f "$ACTION_RUNNER_DIR/.runner-configured" ]; then
    if [ "${FORCE_RECONFIGURE:-0}" = "1" ]; then
        echo "FORCE_RECONFIGURE=1: proceeding to cleanup and reconfigure the runner."
    else
        echo "Runner already configured (marker found) — skipping reconfiguration."
        exit 0
    fi
fi

## Only run cleanup when explicitly requested (FORCE_RECONFIGURE=1). This
## avoids accidental removal of valid configuration files on normal restarts.
if [ "${FORCE_RECONFIGURE:-0}" = "1" ]; then
    echo "FORCE_RECONFIGURE=1: performing cleanup before reconfiguration."

    if [ -n "${RUNNER_DELETE_TOKEN:-}" ]; then
        echo "RUNNER_DELETE_TOKEN provided — attempting server-side remove via config.sh remove"
        if command -v sudo >/dev/null 2>&1; then
            sudo -u github-runner bash -lc "cd $ACTION_RUNNER_DIR && ./config.sh remove --token \"$RUNNER_DELETE_TOKEN\"" || true
        else
            su -s /bin/bash -c "cd $ACTION_RUNNER_DIR && ./config.sh remove --token \"$RUNNER_DELETE_TOKEN\"" github-runner || true
        fi
    else
        echo "No RUNNER_DELETE_TOKEN provided — performing local cleanup of runner state files."
    fi

    echo "Cleaning up leftover runner state files (if any)."
    for f in \
        "$ACTION_RUNNER_DIR/.runner" \
        "$ACTION_RUNNER_DIR/.credentials" \
        "$ACTION_RUNNER_DIR/.credentials_rsaparams" \
        "$ACTION_RUNNER_DIR/.env" \
        "$ACTION_RUNNER_DIR/.runner-configured" \
        "/runner/.runner" \
        "/runner/.credentials" \
        "/runner/.credentials_rsaparams" \
        "/runner/.env" \
        "/runner/.runner-configured"; do
        if [ -e "$f" ]; then
            echo "Removing $f"
            rm -f "$f" || true
        fi
    done

    # Ensure ownership is correct after cleanup
    chown -R github-runner:github-runner "$ACTION_RUNNER_DIR" /runner || true
    echo "Runner cleanup complete. Proceeding to configure a fresh runner."
fi

# Build the configure command
CONFIG_CMD="cd $ACTION_RUNNER_DIR && env HOME=/home/github-runner USER=github-runner ./config.sh --unattended --url \"$GITHUB_URL\" --token \"$GITHUB_TOKEN\" --name \"$RUNNER_NAME\" --labels \"$RUNNER_LABELS\" --work \"${RUNNER_WORKDIR:-_work}\" --replace"

# Allow running inside container as root if necessary
export RUNNER_ALLOW_RUNASROOT=1

# Run config as the non-root github-runner user when possible, otherwise run with RUNNER_ALLOW_RUNASROOT
if command -v sudo >/dev/null 2>&1; then
    sudo -u github-runner bash -lc "$CONFIG_CMD"
else
    # Try to run as github-runner; if that fails fall back to running as root with RUNNER_ALLOW_RUNASROOT
    if su -s /bin/bash -c "$CONFIG_CMD" github-runner; then
        true
    else
        echo "Falling back to running config as root with RUNNER_ALLOW_RUNASROOT=1"
        bash -lc "$CONFIG_CMD"
    fi
fi

echo "GitHub runner configured successfully."
# Create the configured marker and ensure ownership is correct so subsequent
# container restarts know the runner is configured and won't try to reconfigure.
if command -v sudo >/dev/null 2>&1; then
    sudo -u github-runner touch /runner/.runner-configured || true
else
    touch /runner/.runner-configured || true
fi

# Ensure the runner files and marker are owned by the runner user
chown -R github-runner:github-runner "$ACTION_RUNNER_DIR" /runner || true