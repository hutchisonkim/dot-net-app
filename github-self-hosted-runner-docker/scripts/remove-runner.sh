#!/usr/bin/env bash
set -euo pipefail

# This script tries to remove the configured runner from GitHub (if configured)
RUNNER_DIR="/runner"
if [ -d "$RUNNER_DIR" ] && [ -f "$RUNNER_DIR/.runner-configured" ]; then
  echo "Attempting to remove runner..."
  pushd "$RUNNER_DIR" >/dev/null || true
  if [ -x ./config.sh ]; then
    ./config.sh remove --unattended --token "${GITHUB_TOKEN:-}" || true
  elif [ -x ./config.cmd ]; then
    ./config.cmd remove || true
  else
    echo "No config.sh found, skipping removal. You can manually remove the runner in GitHub settings."
  fi
  popd >/dev/null || true
else
  echo "Runner not found in $RUNNER_DIR. Nothing to remove."
fi
#!/bin/bash

# This script removes the GitHub runner from the GitHub repository.


# Load config from /config/runner.env if present (this is optional)
if [ -f /config/runner.env ]; then
	echo "Sourcing configuration from /config/runner.env"
	# shellcheck disable=SC1090
	source /config/runner.env
fi

# Check if the runner is registered
if [ -z "$RUNNER_NAME" ]; then
  echo "Runner name is not set. Please set the RUNNER_NAME environment variable."
  exit 1
fi

# Remove the runner
echo "Removing runner: $RUNNER_NAME"
./config.sh remove --unattended --name "$RUNNER_NAME" --token "$RUNNER_TOKEN"

# Check if the removal was successful
if [ $? -eq 0 ]; then
  echo "Runner $RUNNER_NAME removed successfully."
else
  echo "Failed to remove runner $RUNNER_NAME."
  exit 1
fi