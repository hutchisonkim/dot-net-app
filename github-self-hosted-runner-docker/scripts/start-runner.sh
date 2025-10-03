#!/bin/bash

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

# Ensure the runner is configured
if [ ! -f /runner/.runner-configured ]; then
  echo "Runner is not configured. Please run the configure script first."
  exit 1
fi

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

# Briefly wait and check the listener process specifically
sleep 2

if pgrep -f Runner.Listener >/dev/null 2>&1; then
  echo "GitHub runner is running."
else
  echo "Failed to start the GitHub runner. Check /runner/_diag for logs."
  exit 1
fi

# Keep the script running to keep the container alive
wait $!