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

## Download and extract the runner into /actions-runner when enabled.
# Use the download-runner.sh helper so the logic is centralized and easier
# to maintain. The script is copied earlier to /usr/local/bin during the
# build and will respect RUNNER_VERSION and run as root during build; it will
# try to chown the files to ${RUNNER_USER} when possible.
RUN if [ "${DOWNLOAD_RUNNER_AT_BUILD}" = "1" ]; then \
        echo "DOWNLOAD_RUNNER_AT_BUILD=1: invoking /usr/local/bin/download-runner.sh"; \
        RUNNER_USER=${RUNNER_USER} /usr/local/bin/download-runner.sh; \
    else \
        echo "Skipping build-time runner download (DOWNLOAD_RUNNER_AT_BUILD!=1)"; \
    fi

# Note: the GitHub runner binary is downloaded at container startup by
# /usr/local/bin/download-runner.sh (entrypoint) so that the build remains
# cacheable and the downloaded artifacts can be persisted in a volume on the
# host. Avoid downloading the runner at image build time.

# Optional: install .NET SDK here as a late layer so earlier layers remain cached
# Controlled by build arg INSTALL_DOTNET (set to 1 to install at build time)
ARG INSTALL_DOTNET=1
ENV INSTALL_DOTNET=${INSTALL_DOTNET}

RUN if [ "${INSTALL_DOTNET}" = "1" ]; then \
            echo "Installing .NET SDK 8.0 at build time"; \
            wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb && \
            dpkg -i /tmp/packages-microsoft-prod.deb && \
            apt-get update && \
            apt-get install -y --no-install-recommends dotnet-sdk-8.0 ca-certificates && \
            rm -f /tmp/packages-microsoft-prod.deb && \
            apt-get clean && rm -rf /var/lib/apt/lists/*; \
        else \
            echo "Skipping .NET SDK install at build time (INSTALL_DOTNET!=1)"; \
        fi

# Set the entrypoint
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]