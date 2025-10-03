#!/bin/bash

# Exit immediately if a command exits with a non-zero status
set -e

# Load environment variables from the runner configuration file
if [ -f /config/runner.env ]; then
    source /config/runner.env
else
    echo "Configuration file /config/runner.env not found. Please create it based on runner.env.example."
    exit 1
fi

# Check if the required environment variables are set
if [ -z "$GITHUB_TOKEN" ] || [ -z "$RUNNER_NAME" ] || [ -z "$RUNNER_LABELS" ]; then
    echo "GITHUB_TOKEN, RUNNER_NAME, and RUNNER_LABELS must be set in the runner.env file."
    exit 1
fi

# Configure the GitHub runner
echo "Configuring GitHub runner..."
./bin/Runner configure \
    --url "$GITHUB_URL" \
    --token "$GITHUB_TOKEN" \
    --name "$RUNNER_NAME" \
    --labels "$RUNNER_LABELS" \
    --work "$RUNNER_WORKDIR" \
    --replace \
    --unattended

echo "GitHub runner configured successfully."