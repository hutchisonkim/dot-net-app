#!/bin/bash

# Use RUNNER_VERSION from environment (set by Docker build ARG or env)
RUNNER_VERSION="${RUNNER_VERSION:-latest}"

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

# Create a directory for the runner (this can be mounted to a volume to persist between runs)
RUNNER_DIR="/actions-runner"
mkdir -p "$RUNNER_DIR"

# Version marker file used to record the currently installed runner version
VERSION_MARKER="$RUNNER_DIR/.runner-version"

echo "Using RUNNER_VERSION=$RUNNER_VERSION, runner dir=$RUNNER_DIR"

if [ -f "$VERSION_MARKER" ]; then
    INSTALLED_VERSION=$(cat "$VERSION_MARKER" || true)
else
    INSTALLED_VERSION=""
fi

# If the version marker matches and run.sh exists, we're done
if [ "$INSTALLED_VERSION" = "$RUNNER_VERSION" ] && [ -f "$RUNNER_DIR/run.sh" ]; then
    echo "Runner version $RUNNER_VERSION already present in $RUNNER_DIR — skipping download."
    exit 0
fi

# If we don't have a version marker but runner files already exist (for example
# because the image was built with the runner), create the marker and skip.
if [ -z "$INSTALLED_VERSION" ] && [ -f "$RUNNER_DIR/run.sh" ]; then
    echo "Found existing runner files in $RUNNER_DIR but no version marker. Creating marker for version $RUNNER_VERSION and skipping download."
    echo "$RUNNER_VERSION" > "$VERSION_MARKER"
    # try to set ownership if possible
    if command -v chown >/dev/null 2>&1; then
        chown -R "${RUNNER_USER:-github-runner}":"${RUNNER_USER:-github-runner}" "$RUNNER_DIR" || true
    fi
    exit 0
fi

echo "Downloading GitHub runner from $RUNNER_URL..."
curl -fSL -o "$RUNNER_DIR/actions-runner.tar.gz" "$RUNNER_URL"

echo "Extracting the runner package..."
tar xzf "$RUNNER_DIR/actions-runner.tar.gz" -C "$RUNNER_DIR"

# Clean up the downloaded tar file
rm -f "$RUNNER_DIR/actions-runner.tar.gz"

# Persist the installed version so subsequent container restarts can skip download
echo "$RUNNER_VERSION" > "$VERSION_MARKER"

# Try to chown the directory so the non-root runner user can use it
if command -v chown >/dev/null 2>&1; then
    chown -R "${RUNNER_USER:-github-runner}":"${RUNNER_USER:-github-runner}" "$RUNNER_DIR" || true
fi

echo "GitHub runner downloaded and extracted to $RUNNER_DIR (version $RUNNER_VERSION)."