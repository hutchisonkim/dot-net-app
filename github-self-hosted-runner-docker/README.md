# GitHub Self-Hosted Runner (docker)

Tip: Known issue — subsequent runners may not get configured when container image/volume caches contain stale state. If a runner unexpectedly skips configuration, remove the runner volumes or wipe `/runner` and `/actions-runner` in the container before restarting.

This folder contains a small Docker setup to run a GitHub Actions self-hosted runner in a container.

Quick start
- Copy or edit `.env` to set `GITHUB_URL`, `GITHUB_TOKEN` (registration token), `RUNNER_NAME`, and other vars.
- Build & run:
	- `docker-compose up -d`
- Stop:
	- `docker-compose down`

If you need to reconfigure a runner (e.g., after wiping state):
- Obtain a registration token for the repo/org (via UI or API).
- Run inside the running container:
	- `GITHUB_TOKEN='<REG_TOKEN>' /usr/local/bin/configure-runner.sh`
	- then ` /usr/local/bin/start-runner.sh`

Notes
- `GITHUB_TOKEN` used by `config.sh` must be a registration token scoped to the repository or organization you set as `GITHUB_URL`.
- For management API checks (listing/removing runners) use a separate PAT in `GITHUB_API_TOKEN`.

