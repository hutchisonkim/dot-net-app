#!/bin/bash

set -euo pipefail

# NOTE: this script assumes the caller (entrypoint) has already sourced
# /config/runner.env or set the environment variables. Keep this script
# focused on the configure/remove/cleanup logic so it can be invoked from
# entrypoint without duplicating validation or sourcing.
echo "Configuring GitHub runner..."
ACTION_RUNNER_DIR="/actions-runner"
cd "$ACTION_RUNNER_DIR" || exit 1

# If entrypoint created /config/runner.env (it does), source it here so this
# script has the same variable values even if the parent shell didn't export
# them. This ensures GITHUB_TOKEN is available to the removal/config commands
# without relying on another token variable.
if [ -f /config/runner.env ]; then
    # shellcheck disable=SC1090
    source /config/runner.env || true
fi

# Export the common runner-related vars so child commands see them.
export GITHUB_TOKEN GITHUB_URL RUNNER_NAME RUNNER_LABELS RUNNER_WORKDIR

if [ -n "${GITHUB_TOKEN:-}" ]; then
    echo "Attempting to remove existing runner configuration..."
    if [ -x ./config.sh ]; then
        # Run removal as the non-root runner user to avoid config.sh refusing
        # to run under sudo/root. Prefer sudo if available, otherwise use su.
        if command -v sudo >/dev/null 2>&1; then
            sudo -u github-runner bash -lc "cd $ACTION_RUNNER_DIR && ./config.sh remove --token \"$GITHUB_TOKEN\"" || true
        else
            su -s /bin/bash -c "cd $ACTION_RUNNER_DIR && ./config.sh remove --token \"$GITHUB_TOKEN\"" github-runner || true
        fi
    elif [ -x ./config.cmd ]; then
        ./config.cmd remove || true
    else
        echo "No config.sh found, skipping removal. You can manually remove the runner in GitHub settings."
    fi
fi
    



# Build the configure command
CONFIG_CMD="cd $ACTION_RUNNER_DIR && env HOME=/home/github-runner USER=github-runner ./config.sh --url \"$GITHUB_URL\" --token \"$GITHUB_TOKEN\" --name \"$RUNNER_NAME\" --labels \"$RUNNER_LABELS\" --work \"${RUNNER_WORKDIR:-_work}\" --replace"

# Allow running inside container as root if necessary
export RUNNER_ALLOW_RUNASROOT=1

# Run config as the non-root github-runner user when possible, otherwise run with RUNNER_ALLOW_RUNASROOT
if command -v sudo >/dev/null 2>&1; then
    sudo -u github-runner bash -lc "$CONFIG_CMD"
else
    # Try to run as github-runner. If the config command itself fails, do NOT
    # retry as root because that causes a duplicate registration attempt on the
    # server (we observed two POSTs). Fail fast so the caller can decide how to
    # handle retries or forced reconfiguration with proper tokens.
    if su -s /bin/bash -c "$CONFIG_CMD" github-runner; then
        true
    else
        echo "Error: config command as user 'github-runner' failed. Not retrying as root to avoid duplicate registration attempts."
        echo "Please check GITHUB_URL/GITHUB_TOKEN."
        exit 1
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