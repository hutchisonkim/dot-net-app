#!/bin/bash

# Start the GitHub runner process
echo "Starting GitHub runner..."

# Ensure the runner is configured
if [ ! -f /runner/.runner-configured ]; then
  echo "Runner is not configured. Please run the configure script first."
  exit 1
fi

# Start the runner
./run.sh &

# Wait for the runner to be ready
sleep 5

# Check if the runner is running
if ps aux | grep -v grep | grep 'runner'; then
  echo "GitHub runner is running."
else
  echo "Failed to start the GitHub runner."
  exit 1
fi

# Keep the script running to keep the container alive
wait $!