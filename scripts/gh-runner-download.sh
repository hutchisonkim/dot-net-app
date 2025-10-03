#!/usr/bin/env bash
set -euo pipefail

# Download-only helper for GitHub Actions runner tarball and extract into target directory.
# Usage: gh-runner-download.sh --user ghrunner --dir /home/ghrunner/gh-runner --version 2.328.0

RUNNER_USER="ghrunner"
RUNNER_DIR="/home/${RUNNER_USER}/gh-runner"
RUNNER_VERSION="2.328.0"

while [ $# -gt 0 ]; do
  case "$1" in
    --user) RUNNER_USER="$2"; shift 2;;
    --dir) RUNNER_DIR="$2"; shift 2;;
    --version) RUNNER_VERSION="$2"; shift 2;;
    -h|--help) echo "Usage: $0 [--user USER] [--dir DIR] [--version VERSION]"; exit 0;;
    *) echo "Unknown arg: $1"; exit 1;;
  esac
done

PKG="actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz"
mkdir -p "${RUNNER_DIR}"
if [ ! -f "${RUNNER_DIR}/${PKG}" ]; then
  echo "[gh-runner-download] Downloading runner ${RUNNER_VERSION} to ${RUNNER_DIR}/${PKG}"
  curl -sSLo "${RUNNER_DIR}/${PKG}" -L "https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/${PKG}"
else
  echo "[gh-runner-download] Package already present: ${RUNNER_DIR}/${PKG}"
fi

if [ ! -f "${RUNNER_DIR}/config.sh" ]; then
  echo "[gh-runner-download] Extracting runner into ${RUNNER_DIR}"
  tar xzf "${RUNNER_DIR}/${PKG}" -C "${RUNNER_DIR}"
fi

chown -R "${RUNNER_USER}:${RUNNER_USER}" "${RUNNER_DIR}" || true
echo "[gh-runner-download] Done."
