# Screenshot troubleshooting and PR display guidance

This repo captures UI "screenshots" from bUnit as standalone HTML files and then references images in Pull Request descriptions/comments.

Recently, some screenshots rendered fine in the PR while others did not. This doc explains the root causes and the changes we made to make screenshots reliable for agents and reviewers.

## Symptoms
- Chess screenshots displayed correctly in the PR and on Pages.
- Pong screenshots intermittently failed to render in the PR. Some links worked, others returned 404 or broke later.
- Agent attempts to fix by pasting different URLs led to inconsistent results (some worked temporarily, then expired).

## Root causes
1) Mixing attachment URL types in PR
   - Working screenshots used the persistent user-attachments pattern: `https://github.com/user-attachments/assets/<id>`.
   - Broken screenshots used the private-user-images pattern with a time-limited JWT: `https://private-user-images.githubusercontent.com/...?.jwt=...`.
   - Those JWT-signed URLs expire and are not suitable for long-lived PR descriptions.

2) Source format was HTML, not a raster image
   - Our bUnit-based tests generate HTML files as "screenshots". They are great for local inspection and the gallery but not directly embeddable as images in PR comments.
   - The PR tried to use external image URLs for display, which weren’t tied to our build artifacts in a durable way.

## Changes implemented
To make screenshots reliable and PR-friendly:

- Added a Playwright-based renderer: `tools/HtmlToPng/`
  - Converts each HTML screenshot (from `tests/Examples.Tests.UI`) into a stable PNG file.
  - Uses a headless Chromium viewport and produces consistent images.

- Wired the renderer into CI (`.github/workflows/test-and-publish.yml`):
  - After UI tests run and HTML screenshots are produced, we build and run the HtmlToPng tool.
  - PNGs are written to `coverage-report/screenshots-png`.
  - Both HTML and PNG outputs are uploaded as artifacts and copied into the Pages site.

- Pages layout unchanged but now includes PNGs alongside HTML so future galleries or PR comments can link to durable PNGs.

## How to reference screenshots in PRs
Prefer one of these approaches:

- Link to the PNG artifact published to GitHub Pages after merge:
  - Example: `https://hutchisonkim.github.io/dot-net-app/screenshots-png/<file>.png`
  - Note: This URL only exists after merge to main. For pre-merge previews, use artifacts or attach the PNGs to the PR via the GitHub UI (drag-drop) which creates `user-attachments/assets/...` links.

- Attach the PNG directly to the PR comment using the GitHub UI. This produces a non-expiring `user-attachments/assets/...` URL which is safe for long-term display.

Avoid using `private-user-images` JWT URLs — they expire and will break later.

## For agents: when adding screenshots
- Do not paste JWT `private-user-images` links into the PR.
- If you must show screenshots before merge:
  - Upload the PNGs in a comment (drag-drop) to get `user-attachments/assets/` links.
  - Or point to the build artifact by name and path; reviewers can download it from the PR checks tab.
- When documenting visuals in the PR description, prefer durable targets:
  - After merge, use Pages URLs for PNGs.
  - Or keep the description generic and add detailed screenshots in comments or the gallery.

## Local usage
- Run UI tests: `dotnet test tests/Examples.Tests.UI`
- Render PNGs locally:
  - `dotnet run --project tools/HtmlToPng/HtmlToPng.csproj -- --input tests/Examples.Tests.UI/bin/Debug/net8.0/screenshots --output coverage-report/screenshots-png`
- Open `coverage-report/screenshots.html` for the interactive gallery (still HTML-based), or browse the PNG folder directly.

## FAQ
- Why not have bUnit emit PNGs directly? bUnit renders components; rasterization needs a browser engine. Playwright is a good fit and already part of this repo for E2E.
- Why not link artifacts directly in PR? Artifact URLs aren’t static public URLs; Pages or `user-attachments` are better for stable references.

## Summary
- Problem: Some PR screenshots broke due to expiring `private-user-images` URLs and relying on HTML “screenshots.”
- Fix: Generate PNGs via Playwright in CI and publish them alongside HTML; document how to reference durable URLs.
- Result: Reviewers and agents get reliable, embeddable PNG screenshots for PRs and the gallery.
