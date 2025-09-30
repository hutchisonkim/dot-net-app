This folder contains GitHub Actions workflows for the repository.

CI flow overview

- `run-unit-tests.yml` runs unit tests, builds/publishes the API and Client projects into `artifacts/`, and uploads them as the artifact named `build-artifacts`.
- `run-integration-tests.yml` triggers after `run-unit-tests.yml` completes successfully, downloads `build-artifacts` into `./downloaded-artifacts`, and then uses `docker/compose.ci.yml` to build the `api` and `client` images from those published outputs. This avoids rebuilding source inside containers and ensures tests run against the same published artifacts.
- `deploy.yml` triggers after `run-integration-tests.yml` completes successfully and pushes images to a registry. It expects secrets defined in the repository settings.

Artifact paths

- Unit tests upload published outputs to an artifact named `build-artifacts` which contains:
  - `api/` — content produced by `dotnet publish` for the API (contains `DotNetApp.Api.dll`, deps, etc.)
  - `client/wwwroot/` — the published web assets produced by `dotnet publish` for the client (the compose override copies these into nginx html root)

- The integration workflow downloads the artifact to `./downloaded-artifacts/` in the runner and the compose override uses that path as the Docker build context for the prebuilt images.

Secrets and registry configuration

- `deploy.yml` expects the following repository secrets:
  - `REGISTRY_HOST` — e.g. ghcr.io or your private registry host
  - `REGISTRY_USER` — username for registry
  - `REGISTRY_PASS` — password or token for registry

Branch protection recommendation

- Protect `main` and require the following checks before merging:
  - `Run Unit Tests` (required)
  - `Run Integration Tests (Docker Compose)` (required)

This ensures merges only occur if unit tests and integration/E2E flows pass.
