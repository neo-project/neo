#!/usr/bin/env bash

# Script to start Neo-CLI and setup monitoring with a local Prometheus, Grafana, and Alertmanager stack

# Exit on error
set -e

# --- Default Configuration ---
NEO_METRICS_ADDRESS="127.0.0.1:9101"
PROMETHEUS_LISTEN_ADDRESS="127.0.0.1:9090"
GRAFANA_LISTEN_ADDRESS="127.0.0.1:3000"
ALERTMANAGER_LISTEN_ADDRESS="127.0.0.1:9093"
NEO_DATA_PATH="" # Auto-generate by default
KEEP_EXISTING_NEO_DATA=false

# --- Binary Versions (Keep updated) ---
PROMETHEUS_VERSION="2.53.0"
GRAFANA_VERSION="10.4.2"
ALERTMANAGER_VERSION="0.27.0"

# --- Helper Functions ---

# Basic logging with color support
log_info() { echo -e "\e[36m[INFO]\e[0m $1"; }
log_warn() { echo -e "\e[33m[WARN]\e[0m $1"; }
log_error() { echo -e "\e[31m[ERROR]\e[0m $1"; } >&2
log_success() { echo -e "\e[32m[SUCCESS]\e[0m $1"; }
log_cmd() { echo -e "\e[35m[CMD]\e[0m $1"; }
log_detail() { echo -e "\e[90m  $1\e[0m"; }

display_help() {
    cat << EOF
Usage: $0 [options]

Starts Neo-CLI with a local monitoring stack (Prometheus, Grafana, Alertmanager).
Binaries for the monitoring stack will be downloaded if not found.

Options:
  --neo-metrics-address <addr>         Neo-CLI metrics endpoint (default: $NEO_METRICS_ADDRESS)
                                       Neo-CLI will be started with '--prometheus <addr>'
  --prometheus-listen-address <addr>   Prometheus server listen address (default: $PROMETHEUS_LISTEN_ADDRESS)
  --grafana-listen-address <addr>      Grafana server listen address (default: $GRAFANA_LISTEN_ADDRESS)
  --alertmanager-listen-address <addr> Alertmanager listen address (default: $ALERTMANAGER_LISTEN_ADDRESS)
  --neo-data-path <path>               Path to Neo data directory (default: auto-generated in 'monitoring' folder)
  --keep-existing-neo-data             Keep existing Neo data directory if specified by --neo-data-path
  --help                               Display this help message

Example:
  # Start with default settings
  $0

  # Start using a specific Neo data path
  $0 --neo-data-path /path/to/my/neo-data --keep-existing-neo-data
EOF
}

check_dependency() {
    if ! command -v "$1" &> /dev/null; then
        log_error "Dependency '$1' not found."
        log_warn "Please install '$1'. Example:"
        case "$(uname -s)" in # Use uname -s for broader compatibility
          Linux*)
            if command -v apt-get &> /dev/null; then
              log_warn "  sudo apt-get update && sudo apt-get install $2"
            elif command -v yum &> /dev/null; then
              log_warn "  sudo yum install $2"
            elif command -v dnf &> /dev/null; then
              log_warn "  sudo dnf install $2"
            else
               log_warn "  Use your system's package manager to install $2"
            fi
            ;;
          Darwin*)
            if command -v brew &> /dev/null; then
              log_warn "  brew install $2"
            else
              log_warn "  Install Homebrew (https://brew.sh/) and then run: brew install $2"
            fi
            ;;
          *) log_warn "  Please install $1 using your system's method.";;
        esac
        exit 1
    fi
}

# --- Parse Arguments ---
while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
        --neo-metrics-address) NEO_METRICS_ADDRESS="$2"; shift; shift ;;
        --prometheus-listen-address) PROMETHEUS_LISTEN_ADDRESS="$2"; shift; shift ;;
        --grafana-listen-address) GRAFANA_LISTEN_ADDRESS="$2"; shift; shift ;;
        --alertmanager-listen-address) ALERTMANAGER_LISTEN_ADDRESS="$2"; shift; shift ;;
        --neo-data-path) NEO_DATA_PATH="$2"; shift; shift ;;
        --keep-existing-neo-data) KEEP_EXISTING_NEO_DATA=true; shift ;;
        --help) display_help; exit 0 ;;
        *) log_error "Unknown option: $1"; display_help; exit 1 ;;
    esac
done

# --- Check Dependencies ---
check_dependency "dotnet" "dotnet-sdk"
check_dependency "jq" "jq"

MONITORING_DIR=$(realpath "$SCRIPT_DIR/..")

# --- Expected Local Binary Paths (Relative to monitoring dir) ---
# Users should place binaries here OR ensure they are in PATH
PROMETHEUS_DIR="$MONITORING_DIR/prometheus"
PROMETHEUS_LOCAL_BIN_PATH="$PROMETHEUS_DIR/prometheus"
PROMETHEUS_CONFIG_PATH="$PROMETHEUS_DIR/prometheus.yml"
PROMETHEUS_DATA_PATH="$PROMETHEUS_DIR/data"

GRAFANA_DIR="$MONITORING_DIR/grafana"
GRAFANA_LOCAL_BIN_DIR="$GRAFANA_DIR/bin"
GRAFANA_LOCAL_BIN_PATH="$GRAFANA_LOCAL_BIN_DIR/grafana-server"
GRAFANA_DATA_PATH="$GRAFANA_DIR/data"
GRAFANA_LOGS_PATH="$GRAFANA_DIR/logs"
# Grafana provisioning is read relative to its execution context or config

ALERTMANAGER_DIR="$MONITORING_DIR/alertmanager"
ALERTMANAGER_LOCAL_BIN_PATH="$ALERTMANAGER_DIR/alertmanager"
ALERTMANAGER_CONFIG_PATH="$ALERTMANAGER_DIR/alertmanager.yml"
ALERTMANAGER_DATA_PATH="$ALERTMANAGER_DIR/data"

# --- Remove OS/ARCH detection and Download-related variables ---
# (OS, ARCH, DOWNLOADER, filename templates, URL templates, versions, extract subdirs removed)

# --- Helper Functions ---
# ... (log functions remain)
# ... (display_help remains)
# ... (check_dependency remains, but only for dotnet, jq, tar, etc. NOT curl/wget)


# --- Remove Binary Handling Helper Functions ---
# (cleanup function simplified)
cleanup() {
    log_info "Cleaning up temporary files..."
    # Clean up PID file
    rm -f "$MONITORING_DIR/running_pids.json"
}
trap cleanup EXIT HUP INT QUIT TERM

# (find_binary_in_path removed, replaced by Find-MonitoringBinary)
# (ensure_binary_exists removed)


# --- New Function to Find Monitoring Binary --- 
find_monitoring_binary() {
    local binary_name=$1       # e.g., "prometheus"
    local local_binary_path=$2 # e.g., "monitoring/prometheus/prometheus"

    # 1. Check specific local path first
    if [[ -x "$local_binary_path" ]]; then
        log_success "Found $binary_name locally at $local_binary_path"
        echo "$local_binary_path"
        return 0
    fi

    # 2. If not found locally, check system PATH
    local path_binary
    path_binary=$(command -v "$binary_name")
    if [[ -n "$path_binary" ]]; then
        log_success "Found $binary_name in system PATH at $path_binary"
        echo "$path_binary"
        return 0
    fi

    # Not found anywhere
    log_error "$binary_name not found."
    log_error "Please install $binary_name and either place it in '$local_binary_path' or ensure it's available in your system PATH."
    log_error "Refer to the monitoring/README.md for installation instructions."
    return 1 # Return error code
}

# --- Display Banner ---
log_info "====================================================="
log_info "       Neo with Local Monitoring Setup (Shell)"
log_info "====================================================="


# --- Validate/Locate Binaries ---
log_info "Locating monitoring binaries..."

PROMETHEUS_BIN_PATH=$(find_monitoring_binary "prometheus" "$PROMETHEUS_LOCAL_BIN_PATH")
if [[ $? -ne 0 ]]; then exit 1; fi

GRAFANA_BIN_PATH=$(find_monitoring_binary "grafana-server" "$GRAFANA_LOCAL_BIN_PATH")
if [[ $? -ne 0 ]]; then exit 1; fi

ALERTMANAGER_BIN_PATH=$(find_monitoring_binary "alertmanager" "$ALERTMANAGER_LOCAL_BIN_PATH")
if [[ $? -ne 0 ]]; then exit 1; fi

# --- Validate Neo CLI & Configs ---
if [[ ! -f "$NEO_CLI_DLL_PATH" ]]; then
    log_error "Neo-CLI DLL not found at $NEO_CLI_DLL_PATH."
    log_error "Please build the neo-cli project first (e.g., run 'dotnet build' in '$NEO_REPO_ROOT/src/Neo.CLI')."
    exit 1
fi
if [[ ! -f "$SOURCE_CONFIG_PATH" ]]; then
    log_error "Source Neo-CLI config not found at $SOURCE_CONFIG_PATH."
    exit 1
fi
if [[ ! -f "$PROMETHEUS_CONFIG_PATH" ]]; then
    log_error "Prometheus config not found at $PROMETHEUS_CONFIG_PATH. Please ensure it exists."
    exit 1
fi
if [[ ! -f "$ALERTMANAGER_CONFIG_PATH" ]]; then
    log_error "Alertmanager config not found at $ALERTMANAGER_CONFIG_PATH. Please ensure it exists."
    exit 1
fi

# --- Ensure Grafana Provisioning Dirs Exist ---
GRAFANA_PROVISIONING_DIR="$MONITORING_DIR/grafana/provisioning"
GRAFANA_DATASOURCES_DIR="$GRAFANA_PROVISIONING_DIR/datasources"
GRAFANA_DASHBOARDS_DIR="$GRAFANA_PROVISIONING_DIR/dashboards"
mkdir -p "$GRAFANA_DATASOURCES_DIR"
mkdir -p "$GRAFANA_DASHBOARDS_DIR"


# --- Setup Neo Data Directory ---
if [[ "$KEEP_EXISTING_NEO_DATA" == "true" ]] && [[ -n "$NEO_DATA_PATH" ]] && [[ -d "$NEO_DATA_PATH" ]]; then
    log_warn "Using existing Neo data directory: $NEO_DATA_PATH"
    # Ensure it's an absolute path for config.json
    NEO_DATA_PATH=$(realpath "$NEO_DATA_PATH")
else
    timestamp=$(date +'%Y%m%d_%H%M%S')
    # Create data dir inside monitoring folder for neatness
    NEO_DATA_PATH="$MONITORING_DIR/neo-data_$timestamp"
    log_warn "Creating new Neo data directory: $NEO_DATA_PATH"
    mkdir -p "$NEO_DATA_PATH"
    # Ensure it's an absolute path for config.json
    NEO_DATA_PATH=$(realpath "$NEO_DATA_PATH")
fi
log_info "Absolute Neo data path: $NEO_DATA_PATH"


# --- Kill Existing Processes ---
log_warn "Stopping any potentially conflicting existing processes..."
PROCESSES_TO_KILL=(
    "dotnet.*$NEO_CLI_DLL_PATH" # More specific pattern for neo-cli
    "prometheus.*--config.file=$PROMETHEUS_CONFIG_PATH" # Pattern for our prometheus
    "grafana-server" # Grafana process name (check if path needed)
    "alertmanager.*--config.file=$ALERTMANAGER_CONFIG_PATH" # Pattern for our alertmanager
)

background_pids=()
killed=false
for pattern in "${PROCESSES_TO_KILL[@]}"; do
    log_detail "Checking for processes matching: $pattern"
    # Use pgrep to find PIDs, then kill. -f matches against full command line.
    # Redirect errors as pgrep might return non-zero if nothing is found.
    pids=$(pgrep -f "$pattern" 2>/dev/null || true)
    if [[ -n "$pids" ]]; then
        log_warn "  Attempting to kill PIDs: $pids"
        # Kill the PIDs found
        kill $pids 2>/dev/null || true # Send TERM signal
        sleep 1 # Give time to terminate gracefully
        # Check if still running and force kill
        pids_after_kill=$(pgrep -f "$pattern" 2>/dev/null || true)
         if [[ -n "$pids_after_kill" ]]; then
             log_warn "  Forcing kill on remaining PIDs: $pids_after_kill"
             kill -9 $pids_after_kill 2>/dev/null || true
         fi
        killed=true
    fi
done

if [[ "$killed" == "true" ]]; then
    log_detail "Waiting a bit longer for processes to terminate..."
    sleep 3
else
    log_success "No relevant running processes found to stop."
fi


# --- Configure Neo-CLI (Basic Setup) ---
log_info "Configuring Neo-CLI..."
# Copy the default config to the bin directory
cp "$SOURCE_CONFIG_PATH" "$TARGET_CONFIG_PATH"

# Use jq to modify the config file for data path and logging
jq_script=$(cat <<JQ
  .ApplicationConfiguration.Storage.Path = "$NEO_DATA_PATH/Data_LevelDB_{0}" |
  .ApplicationConfiguration.Logger.Active = true |
  .ApplicationConfiguration.Logger.ConsoleOutput = true |
  .ApplicationConfiguration.Logger.Path = "$NEO_DATA_PATH/Logs" |
  del(.ApplicationConfiguration.Prometheus) # Remove prometheus section if exists
JQ
)

# Create a temporary file for the modified JSON
temp_json=$(mktemp)
if jq "$jq_script" "$TARGET_CONFIG_PATH" > "$temp_json" && mv "$temp_json" "$TARGET_CONFIG_PATH"; then
    log_success "Neo-CLI config updated ($TARGET_CONFIG_PATH): Set Data/Log paths."
else
    log_error "Failed to update Neo-CLI config using jq."
    rm -f "$temp_json"
    exit 1
fi


# --- Start Prometheus Server (background) ---
log_info "Starting Prometheus Server..."
mkdir -p "$PROMETHEUS_DATA_PATH"
# Use the located $PROMETHEUS_BIN_PATH
prometheus_args=(
    "--config.file=$PROMETHEUS_CONFIG_PATH"
    "--storage.tsdb.path=$PROMETHEUS_DATA_PATH"
    "--web.listen-address=$PROMETHEUS_LISTEN_ADDRESS"
    "--web.enable-lifecycle"
    "--log.level=info"
)
log_cmd "$PROMETHEUS_BIN_PATH ${prometheus_args[*]}"
nohup "$PROMETHEUS_BIN_PATH" "${prometheus_args[@]}" > "$PROMETHEUS_DIR/prometheus.log" 2>&1 &
prometheus_pid=$!
background_pids+=("$prometheus_pid") # Store PID
log_detail "Prometheus started in background (PID: $prometheus_pid). Log: $PROMETHEUS_DIR/prometheus.log"
log_info "Prometheus UI: http://$PROMETHEUS_LISTEN_ADDRESS"
sleep 2 # Give Prometheus time to start


# --- Start Alertmanager (background) ---
log_info "Starting Alertmanager..."
mkdir -p "$ALERTMANAGER_DATA_PATH"
# Use the located $ALERTMANAGER_BIN_PATH
alertmanager_args=(
    "--config.file=$ALERTMANAGER_CONFIG_PATH"
    "--storage.path=$ALERTMANAGER_DATA_PATH"
    "--web.listen-address=$ALERTMANAGER_LISTEN_ADDRESS"
    "--log.level=info"
)
log_cmd "$ALERTMANAGER_BIN_PATH ${alertmanager_args[*]}"
nohup "$ALERTMANAGER_BIN_PATH" "${alertmanager_args[@]}" > "$ALERTMANAGER_DIR/alertmanager.log" 2>&1 &
alertmanager_pid=$!
background_pids+=("$alertmanager_pid") # Store PID
log_info "Alertmanager started (PID: $alertmanager_pid). UI: http://$ALERTMANAGER_LISTEN_ADDRESS. Log: $ALERTMANAGER_DIR/alertmanager.log"
sleep 1


# --- Start Grafana (background) ---
log_info "Starting Grafana..."
# Use the located $GRAFANA_BIN_PATH
grafana_exe_dir=$(dirname "$GRAFANA_BIN_PATH")
# Infer homepath: If found locally (e.g., monitoring/grafana/bin/grafana-server), homepath is monitoring/grafana
grafana_home_path=""
if [[ "$GRAFANA_BIN_PATH" == "$MONITORING_DIR"* ]]; then # Check if path starts with monitoring dir
    # Try to go up from 'bin' directory
    potential_home=$(realpath "$grafana_exe_dir/..")
    if [[ -d "$potential_home/conf" ]]; then # Simple check for conf dir existence
        grafana_home_path=$potential_home
        log_detail "Inferred Grafana home path: $grafana_home_path"
    fi
fi

if [[ -z "$grafana_home_path" ]]; then
    log_warn "Grafana found in PATH ($GRAFANA_BIN_PATH). Cannot automatically determine Grafana home path needed for config/provisioning."
    log_warn "Attempting to start Grafana without setting homepath/environment variables."
    log_warn "Ensure Grafana's default/installed configuration points to provisioning files if needed."
fi

# Set environment variables if home path is known, otherwise hope Grafana finds its defaults
if [[ -n "$grafana_home_path" ]]; then
    export GF_PATHS_DATA="$grafana_home_path/data" # Use inferred home path
    export GF_PATHS_LOGS="$grafana_home_path/logs"
    export GF_PATHS_PROVISIONING="$MONITORING_DIR/grafana/provisioning" # Provisioning is always in our monitoring dir
    mkdir -p "$GF_PATHS_DATA" "$GF_PATHS_LOGS"
else
    # Unset vars in case they were inherited, let Grafana use its defaults
    unset GF_PATHS_DATA
    unset GF_PATHS_LOGS
    unset GF_PATHS_PROVISIONING
fi

export GF_SERVER_HTTP_ADDR=$(echo $GRAFANA_LISTEN_ADDRESS | cut -d: -f1)
export GF_SERVER_HTTP_PORT=$(echo $GRAFANA_LISTEN_ADDRESS | cut -d: -f2)
export GF_AUTH_ADMIN_PASSWORD="admin"

grafana_args=()
# Do not pass -homepath, rely on env vars or Grafana finding its own structure

log_cmd "(Env Vars Set/Unset) $GRAFANA_BIN_PATH ${grafana_args[*]}"
nohup "$GRAFANA_BIN_PATH" "${grafana_args[@]}" > "$GRAFANA_DIR/grafana.log" 2>&1 &
grafana_pid=$!
background_pids+=("$grafana_pid") # Store PID
log_info "Grafana started (PID: $grafana_pid). UI: http://$GRAFANA_LISTEN_ADDRESS (Default login: admin/admin). Log: $GRAFANA_DIR/grafana.log"
sleep 5 # Grafana can take a moment


# --- Start Neo-CLI (foreground) ---
log_info "Starting Neo-CLI..."
neo_cli_args=(
    "$NEO_CLI_DLL_PATH"
    "--prometheus" "$NEO_METRICS_ADDRESS" # Pass prometheus endpoint via command line
    "--logfile" "$NEO_DATA_PATH/Logs/cli.log" # Explicit log file path
    # Add verbosity or other desired CLI flags, e.g. --verbose Info
)
log_cmd "dotnet ${neo_cli_args[*]}"
log_detail "Working Directory: $NEO_CLI_BIN_DIR"
log_detail "Data Directory: $NEO_DATA_PATH"
log_detail "Neo Metrics Endpoint: http://$NEO_METRICS_ADDRESS/metrics"

echo ""
log_info "Local Monitoring Stack URLs:"
log_detail " • Prometheus:   http://$PROMETHEUS_LISTEN_ADDRESS"
log_detail " • Grafana:      http://$GRAFANA_LISTEN_ADDRESS (Login: admin/admin)"
log_detail " • Alertmanager: http://$ALERTMANAGER_LISTEN_ADDRESS"

# Store PIDs to a file
echo "{" > "$MONITORING_DIR/running_pids.json"
echo "  \"Prometheus\": $prometheus_pid,"	>> "$MONITORING_DIR/running_pids.json"
echo "  \"Alertmanager\": $alertmanager_pid,"	>> "$MONITORING_DIR/running_pids.json"
echo "  \"Grafana\": $grafana_pid"		>> "$MONITORING_DIR/running_pids.json"
echo "}" >> "$MONITORING_DIR/running_pids.json"
log_detail "Background PIDs saved to: $MONITORING_DIR/running_pids.json"


log_warn "(Use Ctrl+C to stop Neo-CLI; Monitoring components will keep running in background)"
log_warn "To stop background processes later, run: kill $(cat $MONITORING_DIR/running_pids.json | jq -r .Prometheus,.Alertmanager,.Grafana | paste -sd ' ')"
echo ""

# Execute dotnet in the correct directory, wait for it to finish
# Use exec to replace the shell process with dotnet, allows Ctrl+C to work correctly
( cd "$NEO_CLI_BIN_DIR" && exec dotnet "${neo_cli_args[@]}" )
neo_cli_exit_code=$?

log_info "Neo-CLI process exited with code: $neo_cli_exit_code"

log_warn "Neo-CLI has stopped. Local monitoring components (Prometheus, Grafana, Alertmanager) may still be running."
log_warn "Check PIDs: $(cat $MONITORING_DIR/running_pids.json | jq -c .)"

# Cleanup happens via trap
exit $neo_cli_exit_code