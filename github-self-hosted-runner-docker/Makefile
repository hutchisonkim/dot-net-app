# Makefile

.PHONY: init build up down logs remove clean

init:
	@echo "Create a .env from .env.example (do NOT commit secrets)."
	@cp -n .env.example .env || true
	@echo "Run 'scripts/populate-env.sh' to safely populate .env with your token."

build:
	docker compose build

up:
	docker compose up -d

down:
	docker compose down

logs:
	docker compose logs -f

remove:
	# Remove runner registration from GitHub (container will call remove-runner.sh)
	docker compose run --rm github-runner /usr/local/bin/remove-runner.sh || true

clean:
	docker compose down --volumes --remove-orphans
	docker system prune -f --volumes