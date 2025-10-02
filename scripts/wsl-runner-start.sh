#!/usr/bin/env bash
set -eu
LOG=/tmp/runner-task.log
echo "-- start started $(date +%s)" >> "$LOG"
set -x >> "$LOG" 2>&1
systemctl daemon-reload
systemctl enable --now github-runner || echo "Service not installed - run Setup WSL GitHub Runner first" >> "$LOG"
systemctl status github-runner --no-pager >> "$LOG" 2>&1 || true
journalctl -u github-runner -n 50 --no-pager >> "$LOG" 2>&1 || true
echo "-- start finished $(date +%s)" >> "$LOG"
exit 0
