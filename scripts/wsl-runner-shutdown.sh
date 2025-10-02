#!/usr/bin/env bash
set -eu
LOG=/tmp/runner-task.log
echo "-- shutdown started $(date +%s)" >> "$LOG"
set -x >> "$LOG" 2>&1
systemctl stop github-runner 2>/dev/null || true
pkill -f Runner.Listener 2>/dev/null || true
rm -f /home/ghrunner/gh-runner/.session 2>/dev/null || true
systemctl disable --now github-runner 2>/dev/null || true
echo "-- stopped service, listing /home/ghrunner/gh-runner" >> "$LOG"
ls -la /home/ghrunner/gh-runner >> "$LOG" 2>&1 || true
if [ -f /home/ghrunner/gh-runner/.session ]; then echo ".session exists" >> "$LOG"; else echo ".session missing" >> "$LOG"; fi
systemctl status github-runner --no-pager >> "$LOG" 2>&1 || true
echo "-- shutdown finished $(date +%s)" >> "$LOG"
exit 0
