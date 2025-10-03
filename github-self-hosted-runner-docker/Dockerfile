FROM ubuntu:20.04

# Allow pinning the runner version at build time
ARG RUNNER_VERSION=2.328.0
ENV RUNNER_VERSION=${RUNNER_VERSION}

# During image build we want some scripts to detect they're running inside a build
# and avoid attempting interactive or host-level installs (like Docker). This ARG
# can be overridden at build time if needed. We also export it as an ENV so
# scripts inside the image can read it during RUN steps.
ARG IN_DOCKER_BUILD=1
ENV IN_DOCKER_BUILD=${IN_DOCKER_BUILD}
ENV RUNNER_HOME=/home/github-runner
ENV RUNNER_USER=github-runner
ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=UTC

# Install necessary dependencies
RUN apt-get update && \
    apt-get install -y \
    curl \
    git \
    jq \
    libssl-dev \
    && rm -rf /var/lib/apt/lists/*

# Create a user for the GitHub runner
RUN useradd -m -s /bin/bash $RUNNER_USER

# Set the working directory
WORKDIR $RUNNER_HOME

# Copy scripts into the container
COPY scripts/ /usr/local/bin/

# Make scripts executable
RUN chmod +x /usr/local/bin/*.sh

# Install non-Docker dependencies (this becomes a stable layer cached by Docker)
RUN /usr/local/bin/install-deps.sh

# Optionally download the GitHub runner at image build time so that an empty
# named volume mounted at runtime will be initialized from the image contents
# and subsequent container starts don't need to fetch the runner again.
# Set the build arg DOWNLOAD_RUNNER_AT_BUILD=0 to skip this during docker build.
ARG DOWNLOAD_RUNNER_AT_BUILD=1
ENV DOWNLOAD_RUNNER_AT_BUILD=${DOWNLOAD_RUNNER_AT_BUILD}

# Download and extract the runner into /actions-runner when enabled.
RUN if [ "${DOWNLOAD_RUNNER_AT_BUILD}" = "1" ]; then \
            ARCH=$(uname -m) ; \
            if [ "$ARCH" = "x86_64" ]; then ARCH="x64"; elif [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then ARCH="arm64"; fi ; \
            mkdir -p /actions-runner ; \
            echo "Downloading GitHub Actions runner v${RUNNER_VERSION} for ${ARCH} at build time..." ; \
            curl -fSL -o /actions-runner/actions-runner.tar.gz "https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-${ARCH}-${RUNNER_VERSION}.tar.gz" ; \
            tar xzf /actions-runner/actions-runner.tar.gz -C /actions-runner ; \
            rm -f /actions-runner/actions-runner.tar.gz ; \
            echo "${RUNNER_VERSION}" > /actions-runner/.runner-version ; \
            chown -R ${RUNNER_USER}:${RUNNER_USER} /actions-runner ; \
        else \
            echo "Skipping build-time runner download (DOWNLOAD_RUNNER_AT_BUILD!=1)" ; \
        fi

# Note: the GitHub runner binary is downloaded at container startup by
# /usr/local/bin/download-runner.sh (entrypoint) so that the build remains
# cacheable and the downloaded artifacts can be persisted in a volume on the
# host. Avoid downloading the runner at image build time.

# Set the entrypoint
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]