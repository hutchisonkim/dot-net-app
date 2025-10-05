FROM ubuntu:20.04

# Allow pinning the runner version at build time
ARG RUNNER_VERSION=2.328.0
ENV RUNNER_VERSION=${RUNNER_VERSION}

ARG IN_DOCKER_BUILD=1
ENV IN_DOCKER_BUILD=${IN_DOCKER_BUILD}
ENV RUNNER_HOME=/home/github-runner
ENV RUNNER_USER=github-runner
ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=UTC

RUN apt-get update && \
    apt-get install -y \
    curl \
    git \
    jq \
    libssl-dev \
    sudo \
    && rm -rf /var/lib/apt/lists/*

# Create a user for the GitHub runner
RUN useradd -m -s /bin/bash $RUNNER_USER

WORKDIR $RUNNER_HOME

COPY scripts/ /usr/local/bin/
RUN chmod +x /usr/local/bin/*.sh

RUN /usr/local/bin/install-deps.sh

ARG DOWNLOAD_RUNNER_AT_BUILD=1
ENV DOWNLOAD_RUNNER_AT_BUILD=${DOWNLOAD_RUNNER_AT_BUILD}

RUN if [ "${DOWNLOAD_RUNNER_AT_BUILD}" = "1" ]; then \
        echo "DOWNLOAD_RUNNER_AT_BUILD=1: invoking /usr/local/bin/download-runner.sh"; \
        RUNNER_USER=${RUNNER_USER} /usr/local/bin/download-runner.sh; \
    else \
        echo "Skipping build-time runner download (DOWNLOAD_RUNNER_AT_BUILD!=1)"; \
    fi

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

# Copy scripts at the end so earlier layers are cached
COPY scripts/ /usr/local/bin/
RUN chmod +x /usr/local/bin/*.sh

# Entry point for ephemeral runner container
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
