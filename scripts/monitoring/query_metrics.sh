#!/bin/bash
# Simple script to query the Neo node Prometheus metrics endpoint using curl.

# --- Configuration ---
HOST_ADDRESS="127.0.0.1" # Replace with your node's IP address if not running locally
PORT=9100             # Replace with the port configured in your Neo node's config.json

ENDPOINT="http://${HOST_ADDRESS}:${PORT}/metrics"

# --- Script Logic ---
echo "Querying Neo node metrics at: $ENDPOINT"
echo "-------------------------------------------"

# Use curl to fetch the metrics
# -s : Silent mode (don't show progress)
# -f : Fail silently (no output) on HTTP errors, but return non-zero exit code
curl -sf "$ENDPOINT"

# Check the exit status of curl
if [ $? -ne 0 ]; then
    echo
    echo "-------------------------------------------" >&2
    echo "Error: Failed to retrieve metrics from $ENDPOINT." >&2
    echo "Please ensure the Neo node is running and the Prometheus service is enabled and configured correctly." >&2
    exit 1
else
    echo
    echo "-------------------------------------------"
    echo "Metrics retrieved successfully."
fi

exit 0 