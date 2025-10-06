## DotNetApp

A .NET 8 sample that pairs an ASP.NET Core API with a Blazor WebAssembly client, backed by unit/integration/E2E tests, Docker tooling, and GitHub Actions.

![Test Coverage](https://hutchisonkim.github.io/dot-net-app/coverage-summary.svg)

## Features

- [x] Programmatic Docker orchestration library (`RunnerTasks`) used by tests to manage runners/containers.
- [x] Self-hosted GitHub runner stack (Docker-based) with VS Code tasks to start/stop locally.
- [x] GitHub Actions workflows for robust self-hosted CI, diagnostics, and publishing coverage to GitHub Pages.
- [x] Unit tests (xUnit, bUnit) for client components and services.
- [x] Integration tests for API and client (WebApplicationFactory, shared fixtures, HTTP retry helpers).
- [x] Optional Playwright E2E tests (gated by RUN_E2E) to validate the client loads and runs.
- [x] Code coverage aggregation with HTML and SVG outputs (coverage badge embedded above).

## TODO

- [ ] Split the monorepo into three coordinated repositories and wire CI to consume them:
	- App repo (API + Blazor client)
	- RunnerTasks repo (programmatic Docker orchestration; publish as a NuGet package and reference from tests)
	- Self-hosted runner stack repo (Docker Compose + images; referenced via submodule or multi-checkout in CI)
- [ ] Apply programmatic orchestration end-to-end: replace remaining script/compose invocations with `RunnerTasks` across integration/E2E tests and local dev, and provide a single entry point (CLI or tool) for start/stop/teardown.
- [ ] Increase and enforce test coverage to 100% line/branch/method across API, client, and RunnerTasks; gate in CI using coverlet outputs plus summary thresholds.
- [ ] Enforce branch protections and PR-only merges to protect `main`.




## Français

### À propos

DotNetApp est un exemple pratique utilisant .NET 8 qui combine une API ASP.NET Core et un client Blazor WebAssembly. Le tout est accompagné de tests unitaires, d’intégration et E2E, d’outils Docker et de workflows GitHub Actions prêts à l’emploi.

### Points forts

- [x] Gestion des conteneurs et des runners en Docker via la librairie `RunnerTasks`.
- [x] Environnement de runner GitHub auto-hébergé (Docker) avec tâches VS Code pour démarrer/arrêter localement.
- [x] Workflows GitHub Actions pour CI robuste, diagnostics et publication de la couverture de code sur GitHub Pages.
- [x] Tests unitaires pour les composants et services côté client.
- [x] Tests d’intégration pour l’API et le client avec outils partagés et helpers HTTP.
- [x] Tests E2E optionnels avec Playwright pour valider le chargement et le fonctionnement du client.
- [x] Agrégation de la couverture de code avec sorties HTML et SVG.

### Prochaines étapes

- [ ] Séparer le monorepo en trois dépôts coordonnés et configurer la CI pour les utiliser :
	- Dépôt de l’app (API + client Blazor)
	- Dépôt RunnerTasks (orchestration Docker programmatique; publier en NuGet et référencer dans les tests)
	- Dépôt pour le runner auto-hébergé (Docker Compose + images; utilisé via submodule ou multi-checkout en CI)
- [ ] Déployer l’orchestration programmatique de bout en bout : remplacer les appels restants aux scripts/compose par `RunnerTasks` pour les tests et le développement local, avec un point d’entrée unique (CLI ou outil) pour démarrer/arrêter/nettoyer.
- [ ] Atteindre et maintenir une couverture de tests de 100 % (lignes, branches, méthodes) pour l’API, le client et RunnerTasks, et contrôler la CI avec les rapports coverlet.
- [ ] Appliquer des protections de branches et obliger les merges via PR seulement pour sécuriser `main`.
If you want, I can also make the French section slightly more conversational, like a README a Quebec dev would naturally write, with idiomatic expressions and casual phrasing. This often makes it more approachable for local readers. Do you want me to do that?

