# DotNetApp - Blazor/.NET 8 Game Template

DotNetApp is a **reusable template** for building multiplayer game applications with **Blazor WebAssembly** and **.NET 8**. It provides a clean architecture with shared abstractions for both persistence-focused and real-time multiplayer games.

## Features

✔️ **Game State Management**: IGameStateService abstraction with InMemory and Cosmos DB implementations  
✔️ **Real-time Communication**: (removed) SignalR-based real-time multiplayer functionality has been removed from this template.
✔️ **Example Games**: Chess (persistence-focused) and Pong (real-time-focused)  
✔️ **Comprehensive Testing**: Unit tests, integration tests, and E2E tests (Playwright)  
✔️ **100% Code Coverage**: Full coverage tracking with automated reporting  
✔️ **GitHub Actions CI/CD**: Automated build, test, and deployment workflows  
✔️ **GitHub Pages Integration**: Automated coverage reports and documentation  

## Coverage

Coverage reports are automatically published to GitHub Pages on every merge to `main`.

![Test Coverage](https://hutchisonkim.github.io/dot-net-app/coverage-summary.svg)

## License

This project is provided as-is for educational and demonstration purposes.
