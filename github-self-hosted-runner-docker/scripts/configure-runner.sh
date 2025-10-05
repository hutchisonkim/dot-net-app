#!/bin/bash
set -euo pipefail

echo "Configuring GitHub runner (ephemeral mode)..."

ACTION_RUNNER_DIR="/actions-runner"
cd "$ACTION_RUNNER_DIR" || exit 1

if [ -f /config/runner.env ]; then
    # shellcheck disable=SC1090
    source /config/runner.env || true
fi

export GITHUB_TOKEN GITHUB_URL GITHUB_REPOSITORY RUNNER_LABELS RUNNER_WORKDIR

# Generate a unique name for the ephemeral runner
RUNNER_NAME="${RUNNER_NAME:-runner-$(hostname)-$(openssl rand -hex 3)}"
echo "Using runner name: $RUNNER_NAME"

export RUNNER_ALLOW_RUNASROOT=1

CONFIG_CMD="cd $ACTION_RUNNER_DIR && env HOME=/home/github-runner USER=github-runner ./config.sh \
    --url \"${GITHUB_URL%/}/$GITHUB_REPOSITORY\" \
    --token \"$GITHUB_TOKEN\" \
    --name \"$RUNNER_NAME\" \
    --labels \"$RUNNER_LABELS\" \
    --work \"${RUNNER_WORKDIR:-_work}\" \
    --ephemeral"

echo "---- DEBUG: Runner Configuration ----"
echo "GITHUB_URL: ${GITHUB_URL}"
echo "GITHUB_REPOSITORY: ${GITHUB_REPOSITORY}"
echo "GITHUB_TOKEN: ${GITHUB_TOKEN}"
echo "RUNNER_NAME: ${RUNNER_NAME}"
echo "RUNNER_LABELS: ${RUNNER_LABELS}"
echo "RUNNER_WORKDIR:-work: ${RUNNER_WORKDIR:-_work}"
echo "CONFIG_CMD: $CONFIG_CMD"
echo "--------------------------------------"

if command -v sudo >/dev/null 2>&1; then
    sudo -u github-runner bash -lc "$CONFIG_CMD"
else
    su -s /bin/bash -c "$CONFIG_CMD" github-runner
fi

echo "Ephemeral runner configured successfully: $RUNNER_NAME"
