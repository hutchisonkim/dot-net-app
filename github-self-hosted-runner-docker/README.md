# GitHub Self-Hosted Runner Docker

This project provides a Docker-based setup for a self-hosted GitHub runner. It simplifies the process of configuring and managing a GitHub Actions runner in an isolated environment using Docker containers.

## Project Structure

The project consists of the following files and directories:

- **Dockerfile**: Defines the Docker image for the self-hosted GitHub runner, including necessary dependencies and environment setup.
- **docker-compose.yml**: Used to define and run multi-container Docker applications, specifying services, networks, and volumes for the runner setup.
- **.env.example**: A template for environment variables needed for the Docker containers, with placeholders for configuration values.
- **Makefile**: Contains directives for building the Docker image, starting containers, and managing the runner lifecycle.
- **scripts/**: A directory containing various scripts for installing dependencies, downloading the runner, configuring it, starting the runner, and removing it.
  - **install-deps.sh**: Installs necessary dependencies for the self-hosted runner environment.
  - **download-runner.sh**: Downloads the GitHub runner binaries.
  - **configure-runner.sh**: Configures the GitHub runner with authentication tokens and labels.
  - **start-runner.sh**: Starts the GitHub runner process within the container.
  - **remove-runner.sh**: Removes the GitHub runner from the GitHub repository.
  - **entrypoint.sh**: The entry point for the Docker container, orchestrating the execution of other scripts.
- **config/**: Contains configuration files for the runner environment variables.
  - **runner.env.example**: An example configuration file for the runner environment variables.

## Getting Started

To set up the self-hosted GitHub runner using Docker, follow these steps:

1. **Clone the Repository**:
   ```bash
   git clone <repository-url>
   cd github-self-hosted-runner-docker
   ```

2. **Configure Environment Variables**:
   Copy the `.env.example` file to `.env` and update the values as needed:
   ```bash
   cp .env.example .env
   ```

3. **Build the Docker Image**:
   Use the Makefile to build the Docker image:
   ```bash
   make build
   ```

4. **Start the Runner**:
   Start the self-hosted runner using Docker Compose:
   ```bash
   make up
   ```

5. **Access the Runner**:
   The runner will be available for use in your GitHub repository. Ensure that it is properly configured and running.

## Usage

- To stop the runner, use:
  ```bash
  make down
  ```

- To remove the runner, execute:
  ```bash
  make remove
  ```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License. See the LICENSE file for more details.