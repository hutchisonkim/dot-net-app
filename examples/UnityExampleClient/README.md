Unity Example Client
====================

This folder contains a minimal example of how to consume the shared platform-agnostic `ApiClient` inside a Unity project.

Why a separate shared library?
------------------------------
The `DotNetApp.Client.Shared` project targets `netstandard2.1` so it can be referenced by Unity (2022+), Blazor WebAssembly, console apps, etc. It exposes the `ApiClient` implementing `IHealthStatusProvider` without any Blazor-specific dependencies.

Getting Started in Unity
------------------------
1. Build the solution (from the repository root):
   - `dotnet build -c Release`
2. Copy the following compiled DLLs into your Unity project's `Assets/Plugins` (create if missing):
   - `src/DotNetApp.Core/bin/Release/net8.0/DotNetApp.Core.dll` (or a netstandard build if you add one)
   - `src/DotNetApp.Client.Shared/bin/Release/netstandard2.1/DotNetApp.Client.Shared.dll`
   - Any dependency assemblies from `bin` folders for `Microsoft.Extensions.*` used (Configuration.Abstractions, DependencyInjection.Abstractions).
3. Inside Unity create a C# script (e.g., `HealthStatusBehaviour.cs`) with the contents from `SampleHealthStatusBehaviour.cs` in this folder.
4. Set the `ApiBaseAddress` (e.g., via a serialized field) to the running API server (e.g., `http://localhost:5000/`). Ensure CORS is enabled (already allowed by the API for local dev).

Sample Script
-------------
See `SampleHealthStatusBehaviour.cs` for a MonoBehaviour that periodically queries the API health endpoint using the shared client.

Production Considerations
-------------------------
- For larger Unity integrations, consider wrapping DI using a lightweight container or using the `Microsoft.Extensions.DependencyInjection` package directly at game startup.
- Retry, exponential backoff, and error handling have been omitted for brevity.

Unity WebGL Docker Pipeline
---------------------------
This repo includes a scaffolded Docker-based WebGL build pipeline (`docker/Dockerfile.unitywebgl`). It uses GameCI `unityci/editor` images.

Current docker-compose service: `unity-example-client` builds with arg:
```
UNITY_IMAGE=unityci/editor:6000.2.5f1-base-3
```
This is a base image (no WebGL module). To produce an actual WebGL build, switch to a webgl-tagged image that includes the module, e.g.:
```
UNITY_IMAGE=unityci/editor:6000.2.5f1-webgl-3
```
Then ensure the project and `BuildScript.BuildWebGL` exist.

To enable an actual build:
1. Place a Unity project at `examples/UnityExampleClient/UnityProject`.
2. Add an Editor C# script containing a static method `BuildScript.BuildWebGL` that invokes `BuildPipeline.BuildPlayer` to output to `WebGLBuild`.
3. Run:
   - `docker compose build unity-example-client`
   - `docker compose up -d unity-example-client`
4. Navigate to `http://localhost:8082`.

Fallback Behavior:
If the project or build method is missing, the Dockerfile writes a placeholder `index.html` so the container still serves content.

Changing Unity Version:
Edit build args under the service in `docker/docker-compose.yml`. Ensure the tag exists on Docker Hub (see GameCI docs).

Caching & Performance:
The Nginx config (`docker/nginx.unity.conf`) applies long-lived caching for `.data`, `.wasm`, `.js`, `.symbols` asset types and a no-cache policy for `index.html`.

License
-------
This example inherits the repository license.
