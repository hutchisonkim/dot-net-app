#!/bin/bash
set -euo pipefail

# Make apt non-interactive (avoid tzdata/timezone prompts during Docker builds)
export DEBIAN_FRONTEND=noninteractive
export TZ=UTC

# Preseed timezone selection to UTC to prevent interactive prompt from tzdata
if command -v debconf-set-selections >/dev/null 2>&1; then
    echo "tzdata tzdata/Areas select Etc" | debconf-set-selections || true
    echo "tzdata tzdata/Zones/Etc select UTC" | debconf-set-selections || true
fi

# Update package list and install necessary dependencies
apt-get update
apt-get install -y --no-install-recommends \
        tzdata \
        curl \
        jq \
        git \
        unzip \
        wget \
        software-properties-common \
        ca-certificates \
        gnupg \
        lsb-release \
        && apt-get clean \
        && rm -rf /var/lib/apt/lists/*

### Do NOT attempt to install Docker or Docker Compose inside the image
### Installing Docker during a docker build (for example via get.docker.com)
### will try to detect the host environment and can prompt or fail (WSL, etc.).
### Instead: run Docker/Docker Compose on the host (Docker Desktop) and use
### this image only for the GitHub runner process.

if [ -f /.dockerenv ] || [ "${IN_DOCKER_BUILD:-}" = "1" ]; then
    echo "Detected container build environment; skipping Docker/Docker Compose installation."
else
    echo "Host environment: Docker may be installed on the host. To run nested Docker you should configure Docker on the host or use a DinD image intentionally. Skipping installation here."
fi

echo "All non-Docker dependencies installed successfully."