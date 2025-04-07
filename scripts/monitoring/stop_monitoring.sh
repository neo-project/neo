#!/bin/bash
# Script to stop the background monitoring processes (Prometheus, Grafana, Alertmanager)

echo -e "\033[1;33mAttempting to stop monitoring services (Prometheus, Grafana, Alertmanager)...\033[0m"

# --- Setup Paths ---
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" &> /dev/null && pwd)
MONITORING_DIR=$(cd "$SCRIPT_DIR/.." &> /dev/null && pwd) # One level up
PID_FILE_PATH="$MONITORING_DIR/running_pids.json"

PROCESSES_STOPPED=false

# --- Function to attempt killing a process by PID ---
stop_by_pid() {
    local name="$1"
    local pid="$2"

    if [[ -z "$pid" || "$pid" == "null" || "$pid" -le 0 ]]; then
        echo -e "  \033[0;37mSkipping $name (PID not found or invalid in file).\033[0m"
        return
    fi

    # Check if process exists before trying to kill
    if ps -p "$pid" > /dev/null; then
        echo -n -e "  \033[0;37mStopping $name (PID: $pid)... \033[0m"
        # Try graceful termination first, then force if needed after a short wait
        kill "$pid" &> /dev/null
        sleep 0.5
        if ps -p "$pid" > /dev/null; then
            kill -9 "$pid" &> /dev/null
        fi
        # Verify it stopped
        if ! ps -p "$pid" > /dev/null; then
            echo -e "\033[0;32mStopped.\033[0m"
            PROCESSES_STOPPED=true
        else
            echo -e "\033[1;31mFailed to stop.\033[0m"
        fi
    else
        echo -e "  \033[0;37m$name (PID: $pid) was not running.\033[0m"
    fi
}

# --- Method 1: Use PID file if it exists ---
if [[ -f "$PID_FILE_PATH" ]]; then
    echo -e "\033[0;37mFound PID file: $PID_FILE_PATH. Stopping processes by PID...\033[0m"

    # Check if jq is installed
    if ! command -v jq &> /dev/null; then
        echo -e "  \033[1;31mError: 'jq' command not found. Cannot parse PID file.\033[0m"
        echo -e "  \033[1;31mPlease install jq (e.g., 'sudo apt-get install jq' or 'brew install jq').\033[0m"
        echo -e "  \033[0;37mFalling back to stopping processes by name/pattern.\033[0m"
    else
        # Parse PIDs using jq (handle potential nulls)
        GRAFANA_PID=$(jq -r '.Grafana // "0"' "$PID_FILE_PATH")
        ALERTMANAGER_PID=$(jq -r '.Alertmanager // "0"' "$PID_FILE_PATH")
        PROMETHEUS_PID=$(jq -r '.Prometheus // "0"' "$PID_FILE_PATH")

        # Stop processes (optional specific order)
        stop_by_pid "Grafana" "$GRAFANA_PID"
        stop_by_pid "Alertmanager" "$ALERTMANAGER_PID"
        stop_by_pid "Prometheus" "$PROMETHEUS_PID"

        # Clean up PID file after attempting stops
        echo -e "\033[0;37mRemoving PID file: $PID_FILE_PATH\033[0m"
        rm -f "$PID_FILE_PATH"
    fi
else
    echo -e "\033[0;37mPID file not found ($PID_FILE_PATH). Attempting to stop processes by name/pattern...\033[0m"
fi

# --- Method 2: Fallback - Stop by name/pattern (if PID file missing or jq failed or didn't stop all) ---
# Check if we already successfully stopped processes using PIDs from the file
# We might still run this if the pid file existed but some pids were invalid/missing

if [[ "$PROCESSES_STOPPED" = false || ! -f "$PID_FILE_PATH" ]]; then # Condition adjusted for shell
    echo -e "\033[0;37mStopping processes by name/pattern match...\033[0m"

    # Use pkill -f to match the command line. Be careful with patterns.
    # Match patterns that are reasonably specific to avoid killing unrelated processes.
    PATTERNS_TO_KILL=(
        "prometheus.*--config.file=.*prometheus.yml" # Match prometheus started with our config
        "grafana-server.*-homepath=.*monitoring/grafana" # Match grafana if started with homepath
        "grafana-server" # Fallback for grafana if path unknown
        "alertmanager.*--config.file=.*alertmanager.yml" # Match alertmanager with our config
    )

    STOPPED_BY_NAME=false
    for pattern in "${PATTERNS_TO_KILL[@]}"; do
        # Check if any process matches before trying to kill
        if pgrep -f -- "$pattern" > /dev/null; then
            echo -n -e "  \033[0;37mAttempting to stop processes matching pattern: '$pattern'... \033[0m"
            # Try TERM signal first, then KILL
            pkill -f -- "$pattern"
            sleep 0.5
            if pgrep -f -- "$pattern" > /dev/null; then
                 pkill -9 -f -- "$pattern"
            fi
            # Verify
            if ! pgrep -f -- "$pattern" > /dev/null; then
                echo -e "\033[0;32mStopped.\033[0m"
                STOPPED_BY_NAME=true # Mark if any stop succeeded here
            else
                echo -e "\033[1;31mSome processes might still be running.\033[0m"
            fi
        else
             echo -e "  \033[0;37mNo running process found matching pattern: '$pattern'.\033[0m"
        fi
    done

    # Update overall status if any process was stopped by name
    if [[ "$STOPPED_BY_NAME" = true ]]; then
        PROCESSES_STOPPED=true
    fi
fi

# Final status message
if [[ "$PROCESSES_STOPPED" = true ]]; then
    echo -e "\033[1;32mMonitoring services stop sequence initiated.\033[0m"
else
    echo -e "\033[1;32mNo running monitoring services found to stop (based on PID file or name/pattern).\033[0m"
fi

exit 0 