#!/bin/bash

# Set the version of the GitHub runner to download
RUNNER_VERSION="latest"

# Set the architecture based on the system
ARCH=$(uname -m)
if [[ "$ARCH" == "x86_64" ]]; then
    ARCH="x64"
elif [[ "$ARCH" == "arm64" ]]; then
    ARCH="arm64"
else
    echo "Unsupported architecture: $ARCH"
    exit 1
fi

# Set the download URL for the GitHub runner
RUNNER_URL="https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-${ARCH}-${RUNNER_VERSION}.tar.gz"

# Create a directory for the runner
RUNNER_DIR="/actions-runner"
mkdir -p "$RUNNER_DIR"

# Download the runner package
echo "Downloading GitHub runner from $RUNNER_URL..."
curl -o "$RUNNER_DIR/actions-runner.tar.gz" -L "$RUNNER_URL"

# Extract the runner package
echo "Extracting the runner package..."
tar xzf "$RUNNER_DIR/actions-runner.tar.gz" -C "$RUNNER_DIR"

# Clean up the downloaded tar file
rm "$RUNNER_DIR/actions-runner.tar.gz"

echo "GitHub runner downloaded and extracted to $RUNNER_DIR."