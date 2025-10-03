#!/bin/bash

#!/bin/bash

echo "Starting GitHub runner..."

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