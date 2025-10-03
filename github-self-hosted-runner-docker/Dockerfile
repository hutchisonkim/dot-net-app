FROM ubuntu:20.04

# Allow pinning the runner version at build time
ARG RUNNER_VERSION=2.328.0
ENV RUNNER_VERSION=${RUNNER_VERSION}
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

# Download and configure the GitHub runner
RUN /usr/local/bin/install-deps.sh && \
    /usr/local/bin/download-runner.sh

# Set the entrypoint
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]