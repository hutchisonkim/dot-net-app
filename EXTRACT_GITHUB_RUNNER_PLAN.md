# Extract GitHub Runner into its own repository

Goal: move the GitHub runner implementation out of this monorepo into a dedicated repository (recommended name: `hutchisonkim/github-runner`), preserving commit history for the runner files, and then remove those files from this repository.

This document is a concrete, step-by-step plan. It gives a preferred method (using `git filter-repo`) and a fallback (`git subtree split`) and lists validation, rollback, and post-migration tasks. It follows Microsoft (MSDN) guidance for repository migration and history-preserving operations where applicable.

## Scope

- Preserve history and move the following paths into a new repo:
  - `src/GitHub.Runner.Docker`
  - `tools/GitHub.Runner.Docker.Cli`
  - `.vscode/tasks.json`
- Remove those files/directories from the current repository after the extraction (in a follow-up commit).
- Create the new repository at `https://github.com/hutchisonkim/github-runner` (recommended name). If that repo name is acceptable, we will use it; otherwise pick a similar descriptive name.

## Assumptions

- You have push access to `hutchisonkim/github-runner` (or will create it during the process).
- Developers have a reasonably recent Git (and ideally `git-filter-repo`) installed. If `git-filter-repo` is not available, we'll use `git subtree` as a fallback.
- CI and other automation that reference these paths will be updated after the migration.

If any assumption is false, see the "Alternatives & prechecks" section.

## Contract (what success looks like)

- Inputs: this repository at branch `main` (or current HEAD) with the runner files present.
- Outputs:
  - A new repo `github-runner` containing only the extracted runner code and history for the listed paths.
  - The current repo with the runner files removed (in a clean commit) and its history intact.
  - A migration branch in both repos with the migration commit(s) for review.
- Error modes:
  - `git-filter-repo` missing — fallback to `git subtree`.
  - Conflicting file paths in new repo remote — fail before push and require manual resolution.

## Safety & MSDN guidance notes

- Prefer tools that avoid rewriting unrelated history for other contributors. `git-filter-repo` is endorsed by Git maintainers and is more robust and faster than `git filter-branch` (MSDN guidance supports using supported Git tools and documenting rewrite operations).
- Always work on feature branches and do not rewrite `main` directly. Create branches and keep the original repository intact until the migration is validated.
- Preserve tags for the moved paths only if needed — tags are global in a repo and must be carefully handled.

## Prechecks (do these before making changes)

1. Confirm the files exist in the repository root:
   - `src/GitHub.Runner.Docker`
   - `tools/GitHub.Runner.Docker.Cli`
   - `.vscode/tasks.json`
2. Ensure working tree is clean locally: stash or commit unrelated changes.
3. Make sure you have a clean branch for the migration:

```
git checkout -b refactor-extract-github-runner
```

4. Install `git-filter-repo` if not present (preferred):

Windows (recommended via pip in your developer environment):

```
python -m pip install --upgrade git-filter-repo
```

If you cannot install `git-filter-repo`, ensure `git` supports `subtree` and you are comfortable using `git subtree split` as the fallback.

## Preferred Method: git-filter-repo (recommended)

Why: preserves history for the specified paths only; it's fast and considered the modern approach.

High-level steps:

1. Create a temporary clone of the repository (mirror clone recommended to preserve refs):

```
git clone --no-local --no-hardlinks --mirror "$(pwd)" repo-mirror.git
cd repo-mirror.git
```

2. Run `git filter-repo` to extract the specified paths into a new repository. The command below rewrites the mirror to only include those paths (two separate runs are shown: one for both top-level paths together and a small move to place files at root if desired).

Example (keep directory layout):

```
git filter-repo --path src/GitHub.Runner.Docker --path tools/GitHub.Runner.Docker.Cli --path .vscode/tasks.json --invert-paths
```

Note: The example above uses `--invert-paths` only if you want to remove everything except those paths. Instead use the non-inverted form to KEEP them:

```
git filter-repo --path src/GitHub.Runner.Docker --path tools/GitHub.Runner.Docker.Cli --path .vscode/tasks.json
```

After the filter, the repository will contain only commits that touched the specified paths. Inspect the repository and run quick checks.

3. Optionally move files to top-level structure in the new repository (if you want `src/GitHub.Runner.Docker` contents to be at the repo root rather than nested): use `--path-rename`:

```
git filter-repo --path-rename src/GitHub.Runner.Docker/:
git filter-repo --path-rename tools/GitHub.Runner.Docker.Cli/:tools/  # optional
```

4. Create the new GitHub repository (or use the web UI). Then add the remote and push:

```
git remote add origin https://github.com/hutchisonkim/github-runner.git
git push -u origin --all
git push origin --tags
```

5. Validate in the new repo: check log, files present, commit counts, and a sample commit's diff to ensure history preserved.

6. In the original repository, create a branch and remove the files (see "Cleaning the original repo" below). Keep the branch for review and testing before merging.

Notes and pitfalls:

- `git-filter-repo` rewrites history — do not run it in the existing repo's clone unless you are prepared to force-push rewritten history. That's why we recommend a mirror clone and pushing into a new remote.
- Verify that any subpaths which are compiled into the solution (csproj references, package references) will still be valid after extraction — likely you'll need to adjust solution files and CI.

## Fallback Method: git subtree split

Why: easier if `git-filter-repo` is not available; simpler workflow but produces a new branch you can push to a new remote. It preserves history for the split subtree in a branch.

Steps:

1. From a fresh local clone (or worktree), run:

```
git checkout -b split-github-runner main
git subtree split --prefix=src/GitHub.Runner.Docker -P runner-src -b gh-runner-src
git subtree split --prefix=tools/GitHub.Runner.Docker.Cli -P runner-cli -b gh-runner-cli
```

This produces branch `gh-runner-src` with history for that prefix. Repeat for other prefixes.

2. Create a new empty repository on GitHub and push one of the subtree branches as its main branch:

```
git remote add new-origin https://github.com/hutchisonkim/github-runner.git
git push new-origin gh-runner-src:main
```

3. In the new repo, reintroduce the other piece (`gh-runner-cli`) by merging or by pulling and rearranging files; subtree split does not automatically combine multiple directories into one branch — you may need to rebase or merge.

Notes:

- `git subtree` is simpler but less flexible when combining multiple directories into a single new repository while preserving a linear history that mixes commits from both directories. You may need to import both histories and reconcile.

## Cleaning the original repository (after new repo validated)

1. In the original repository, create a branch for the removal and tests:

```
git checkout -b remove-github-runner
```

2. Remove the directories/files and commit:

```
git rm -r src/GitHub.Runner.Docker tools/GitHub.Runner.Docker.Cli .vscode/tasks.json
git commit -m "chore: remove GitHub runner code (migrated to hutchisonkim/github-runner)"
```

3. Run build and tests to ensure nothing else breaks. If the monorepo depends on those projects, update solution files and CI to reference the new repo (via submodule, package, or other mechanisms).

4. Push the branch and open a PR for review. Keep the original `main` unchanged until PR is merged and validated.

## Validation / smoke tests

- In the extracted repository:
  - `git log` should show meaningful commits that touch runner files.
  - Spot-check a few commits to ensure diffs are correct.
  - Run `dotnet build` (if solution present) and unit tests where applicable.
- In the original repo (on the `remove-github-runner` branch):
  - Run the repository build, unit tests, integration tests that should not depend on the moved code.
  - Ensure CI pipeline still runs and does not reference deleted paths (or adjust pipeline to call new repo where necessary).

## Rollback plan

- If the new repo push fails or is invalid, do not remove files from the original repo. Keep the original branch intact.
- If you accidentally rewrite public history, you may need to coordinate with contributors and force-push a recovery branch. That's why we use a mirror clone for `git-filter-repo` operations and avoid force-pushing `main`.

## Post-migration tasks

- Update solution files, CI, README badges, and any references to the old paths.
- Decide how the new repo will be consumed:
  - Git submodule
  - Git subtree pull
  - Package (NuGet) if applicable
  - Separate pipeline that builds/releases the runner independently
- Add CONTRIBUTING.md and a basic README in the new repo following MSDN and GitHub guidelines.

## Checklist (quick)

- [ ] Create new GitHub repo `hutchisonkim/github-runner`
- [ ] Mirror clone current repo for safe history rewrite
- [ ] Install `git-filter-repo` (preferred)
- [ ] Produce new repo with filtered history and push
- [ ] Validate extracted repo contents and history
- [ ] Create `remove-github-runner` branch in original repo and remove files
- [ ] Run build/tests in monorepo and extracted repo
- [ ] Merge removal PR once validated

## Notes / recommendations

- Prefer `git-filter-repo` for a clean history that contains only the runner-related commits. Document the chosen commands in the migration PR so reviewers can reproduce.
- Consider exporting only the commits that touched the specified directories; if you want to include related commits that changed CI or docs, include those paths explicitly.
- Recreate or migrate any relevant issues, wikis, or CI secrets manually — they do not move automatically with git history.

---

If this plan looks good, I'll create the branch `refactor-extract-github-runner`, add this file, commit it, and push the branch to `origin` so you can review the plan and start the migration.
