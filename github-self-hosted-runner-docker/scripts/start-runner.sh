#!/bin/bash

echo "Starting GitHub runner..."

# Use an atomic lock directory to avoid a race between multiple start attempts.
# We create a lock dir; if it already exists, check the pidfile or running
# processes to decide whether to skip starting. The script keeps the lock while
# it manages the runner process and removes it on exit.
LOCK_DIR="/runner/.runner-lock"
PID_FILE="/runner/.runner.pid"
LOCK_ATTEMPTS=3

acquired=0
for attempt in $(seq 1 "$LOCK_ATTEMPTS"); do
  if mkdir "$LOCK_DIR" 2>/dev/null; then
    # We own the lock. Record our holder PID so cleanup can verify ownership.
    echo "$$" > "$LOCK_DIR/holder.pid" || true
    acquired=1
    break
  fi

  # If a PID file exists, see if the process is alive and looks like Runner.Listener
  if [ -f "$PID_FILE" ]; then
    other_pid=$(cat "$PID_FILE" || true)
    if [ -n "$other_pid" ] && kill -0 "$other_pid" >/dev/null 2>&1; then
      if ps -p "$other_pid" -o cmd= | grep -q "Runner.Listener"; then
        echo "GitHub runner is already running (pid $other_pid); skipping start."
        exit 0
      fi
    fi
  fi

  # As a fallback, check for any Runner.Listener on the system
  if pgrep -f "Runner.Listener" >/dev/null 2>&1; then
    echo "GitHub runner is already running; skipping start."
    exit 0
  fi

  # Possibly a stale lock. Remove and retry a couple times.
  echo "Found stale lock or temporary contention, removing lock and retrying (attempt $attempt/$LOCK_ATTEMPTS)..."
  rm -rf "$LOCK_DIR" || true
  rm -f "$PID_FILE" || true
  sleep 1
done

if [ "$acquired" -ne 1 ]; then
  echo "Failed to acquire start lock after $LOCK_ATTEMPTS attempts; aborting to avoid duplicate starts."
  exit 1
fi

# Ensure we only remove the lock if this script created it. Keep the lock for
# the lifetime of this script (we wait on the runner process later), and remove
# it on exit.
cleanup() {
  if [ -f "$LOCK_DIR/holder.pid" ] && [ "$(cat "$LOCK_DIR/holder.pid")" = "$$" ]; then
    rm -f "$PID_FILE" || true
    rm -f "$LOCK_DIR/holder.pid" || true
    rmdir "$LOCK_DIR" 2>/dev/null || rm -rf "$LOCK_DIR" || true
  fi
}
trap cleanup EXIT

# Run from the actions-runner directory to ensure ./run.sh is found
ACTION_RUNNER_DIR="/actions-runner"
cd "$ACTION_RUNNER_DIR" || { echo "Failed to cd to $ACTION_RUNNER_DIR"; exit 1; }

# Start the runner as the non-root 'github-runner' user if possible
START_CMD="env RUNNER_ALLOW_RUNASROOT=1 $ACTION_RUNNER_DIR/run.sh"
if command -v sudo >/dev/null 2>&1; then
  sudo -u github-runner bash -lc "$START_CMD" &
else
  if su -s /bin/bash -c "$START_CMD &" github-runner; then
    true
  else
    echo "Falling back to running runner as root with RUNNER_ALLOW_RUNASROOT=1"
    export RUNNER_ALLOW_RUNASROOT=1
    $ACTION_RUNNER_DIR/run.sh &
  fi
fi

# Wait for the Runner.Listener process to appear and record its PID so
# subsequent invocations can detect a running runner without races.
# We prefer the process owned by the 'github-runner' user but will accept any
# Runner.Listener if that's not present.
MAX_WAIT=15
found_pid=""
for i in $(seq 1 "$MAX_WAIT"); do
  # Prefer a Runner.Listener owned by the github-runner user
  if pgrep -u github-runner -f "Runner.Listener" >/dev/null 2>&1; then
    found_pid=$(pgrep -u github-runner -f "Runner.Listener" | head -n1)
  elif pgrep -f "Runner.Listener" >/dev/null 2>&1; then
    found_pid=$(pgrep -f "Runner.Listener" | head -n1)
  fi

  if [ -n "$found_pid" ]; then
    echo "$found_pid" > "$PID_FILE" || true
    # Attempt to set ownership so the runner user can read/remove it
    chown github-runner:github-runner "$PID_FILE" 2>/dev/null || true
    echo "Recorded Runner.Listener pid $found_pid to $PID_FILE"
    break
  fi

  sleep 1
done

if [ -z "$found_pid" ]; then
  echo "Failed to detect Runner.Listener after $MAX_WAIT seconds. Check /runner/_diag for logs."
  # continue — we still wait on the backgrounded start command to keep the container alive
else
  echo "GitHub runner is running (pid $found_pid)."
fi

# Keep the script running to keep the container alive. We wait on the last
# background job so the container stays alive the same way it did before.
wait $!