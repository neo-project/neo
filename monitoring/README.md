# Neo N3 Local Monitoring Setup

## 1. Overview

This directory provides the configuration and scripts necessary to run a local monitoring stack for a Neo N3 node (`neo-cli`). It uses standard open-source tools:

-   **Prometheus**: Collects metrics from `neo-cli` over HTTP.
-   **Grafana**: Visualizes the collected metrics through dashboards. Includes a pre-configured Neo Node Dashboard.
-   **Alertmanager**: Manages alerts triggered by Prometheus based on defined rules (basic configuration included).

The goal is to provide developers and node operators with insights into the performance and status of their local `neo-cli` instance.

## 2. Prerequisites

Before running the setup scripts, ensure you have the following installed and configured:

1.  **Neo N3 `neo-cli` Built**: You need a successful build of the `neo-cli` project. The scripts assume the output is in the standard location (e.g., `[repo_root]/bin/Neo.CLI/net9.0`).
2.  **.NET SDK**: Required by `neo-cli` (`dotnet` command must be available).
3.  **Monitoring Tools Installed**: You must install Prometheus, Grafana, and Alertmanager binaries yourself.
    *   **Download Links**:
        *   Prometheus: <https://prometheus.io/download/>
        *   Grafana: <https://grafana.com/grafana/download>
        *   Alertmanager: <https://prometheus.io/download/#alertmanager>
    *   **Installation & Path**: After downloading and extracting/installing these tools, ensure their main executables (`prometheus`/`prometheus.exe`, `grafana-server`/`grafana-server.exe`, `alertmanager`/`alertmanager.exe`) are findable by the startup scripts. This means they must be either:
        *   Placed in the designated subdirectories within *this* `monitoring` folder:
            *   `prometheus.exe` -> `monitoring/prometheus/`
            *   `grafana-server.exe` -> `monitoring/grafana/bin/`
            *   `alertmanager.exe` -> `monitoring/alertmanager/`
        *   **OR** accessible via your system's `PATH` environment variable. (Remember to restart your terminal after modifying the PATH).
4.  **Script Dependencies**:
    *   **PowerShell (Windows)**: If using `start-neo-with-prometheus.ps1`.
    *   **Bash (Linux/macOS)**: If using `start-neo-with-prometheus.sh`.
        *   `jq`: For modifying `neo-cli`'s `config.json`.
        *   `realpath`: For resolving paths.
        *   `pgrep`, `kill`: For stopping previous processes.

## 3. Directory Structure Explained

```
monitoring/
├── README.md                 # This guide
|
├── alertmanager/             # Alertmanager configuration and data
│   ├── alertmanager.exe      # <-- Alertmanager Binary (User Installed)
│   ├── alertmanager.yml      # <-- Alertmanager Configuration File
│   └── data/                 # Alertmanager runtime data (e.g., alert states)
|
├── grafana/                  # Grafana configuration and data
│   ├── bin/
│   │   └── grafana-server.exe # <-- Grafana Binary (User Installed)
│   ├── conf/                 # Grafana's default configuration files
│   ├── data/                 # Grafana database (users, dashboards), logs, plugins
│   ├── dashboards/           # Custom dashboards loaded via provisioning
│   │   └── neo-node-dashboard.json # <-- Pre-configured Neo Dashboard
│   │   └── README.md           # Dashboard description
│   ├── logs/                 # Grafana's log files
│   └── provisioning/         # Grafana automatic provisioning
│       ├── dashboards/       # Tells Grafana where to find dashboards
│       │   └── default.yml   # <-- Dashboard Provider Configuration
│       └── datasources/      # Tells Grafana how to connect to datasources
│           └── datasources.yml # <-- Prometheus Datasource Configuration
|
├── neo-data_<timestamp>/     # Neo-CLI blockchain data (created by script)
│   ├── Logs/                 # Neo-CLI log files
│   └── Data_LevelDB_*/       # Neo-CLI database files
|
├── prometheus/               # Prometheus configuration and data
│   ├── prometheus.exe        # <-- Prometheus Binary (User Installed)
│   ├── prometheus.yml        # <-- Prometheus Configuration File
│   └── data/                 # Prometheus time-series database (TSDB)
|
├── scripts/                  # Helper scripts
│   ├── start-neo-with-prometheus.ps1 # Startup script (Windows)
│   ├── start-neo-with-prometheus.sh  # Startup script (Linux/macOS)
│   └── debug-prometheus.ps1      # Debugging script (Windows)
└── running_pids.json         # Temporary file storing PIDs of background processes
```

## 4. Configuration Files Explained

Several YAML (`.yml`) and JSON (`.json`) files configure the monitoring stack:

### `prometheus/prometheus.yml`

This file tells Prometheus what to do.

```yaml
global:
  scrape_interval: 15s     # How often to fetch metrics
  evaluation_interval: 15s # How often to evaluate alerting rules

alerting:
  alertmanagers:
    - static_configs:
      - targets:
         - localhost:9093  # Tells Prometheus where Alertmanager is running

scrape_configs:
  # Job to scrape metrics from Neo CLI
  - job_name: 'neo'
    static_configs:
      - targets: ['127.0.0.1:9101'] # Address where neo-cli exposes metrics (/metrics endpoint)
                                     # Must match the --prometheus arg passed by the script
    metrics_path: /metrics         # Standard endpoint path for Prometheus metrics

  # Job to scrape metrics from Prometheus itself
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090'] # Address where Prometheus runs

  # Job to scrape metrics from Alertmanager
  - job_name: 'alertmanager'
    static_configs:
      - targets: ['localhost:9093'] # Address where Alertmanager runs
```

-   **`global`**: Sets default timings.
-   **`alerting`**: Configures how Prometheus communicates with Alertmanager.
-   **`scrape_configs`**: Defines *jobs* for scraping metrics. Each job specifies *targets* (endpoints) to fetch metrics from.
    -   The `neo` job scrapes your `neo-cli` instance.
    -   The `prometheus` and `alertmanager` jobs scrape the monitoring tools themselves.

### `alertmanager/alertmanager.yml`

This configures how Alertmanager handles incoming alerts from Prometheus.

```yaml
global:
  resolve_timeout: 5m # How long to wait before declaring an alert resolved

route:
  group_by: ['alertname']    # Group alerts by name before sending notifications
  group_wait: 10s           # How long to buffer alerts for grouping
  group_interval: 10s       # How long to wait before sending notifications for a group
  repeat_interval: 1h       # How long to wait before re-sending resolved alerts
  receiver: 'null'          # Default receiver if no specific route matches

receivers:
- name: 'null'             # A basic receiver that does nothing (discards alerts)
  # Add other receivers here (e.g., Slack, PagerDuty, Email)
  # Example Webhook:
  # webhook_configs:
  # - url: 'http://127.0.0.1:5001/' # URL to send alert notifications to
```

-   **`global`**: Default settings.
-   **`route`**: Defines the routing tree for alerts. The basic route sends all alerts to the `null` receiver.
-   **`receivers`**: Defines *how* notifications are sent. The `null` receiver effectively silences alerts. You would replace or add receivers here to integrate with Slack, PagerDuty, email, etc.

### `grafana/provisioning/datasources/datasources.yml`

This file automatically adds Prometheus as a data source in Grafana.

```yaml
apiVersion: 1
datasources:
  - name: Prometheus         # Name of the datasource in Grafana
    type: prometheus         # Type of datasource
    url: http://localhost:9090 # Address where Prometheus is running
    access: proxy            # How Grafana connects (proxy recommended)
    isDefault: true          # Make this the default datasource for new panels
    editable: true           # Allow editing in Grafana UI
```

### `grafana/provisioning/dashboards/default.yml`

This file tells Grafana to automatically load dashboards from a specific folder.

```yaml
apiVersion: 1
providers:
- name: 'default'            # Provider name
  orgId: 1                 # Organization ID
  folder: 'Neo'              # Folder name within Grafana where dashboards will appear
  type: file
  disableDeletion: false     # Allow deleting dashboards from UI
  editable: true             # Allow editing dashboards from UI
  options:
    path: /etc/grafana/provisioning/dashboards # Path *inside* Grafana where it looks for dashboards
                                               # Note: This path is relative to Grafana's config/runtime, not the host OS.
                                               # The provided 'neo-node-dashboard.json' should be loaded from here.
```
*Correction Note: The `path` in the actual YML file points to `monitoring/grafana/dashboards`. The comment above describes how Grafana interprets such paths internally based on its homepath or configuration.* The setup intends for Grafana (when started correctly) to find dashboards in the `monitoring/grafana/dashboards` directory on the host system.

### `grafana/dashboards/neo-node-dashboard.json`

This is the actual dashboard definition file in JSON format. It contains panels pre-configured to query Prometheus for `neo-cli` metrics (RPC counts, block height, mempool size, P2P connections, etc.) and display them visually.

## 5. Running the Local Stack

1.  **Fulfill Prerequisites**: Ensure `neo-cli` is built and Prometheus, Grafana, and Alertmanager are installed and findable (see Section 2).
2.  **Navigate to Scripts**: Open your terminal/PowerShell and navigate into the `scripts/monitoring` directory.
3.  **Run the Start Script**:
    *   **Windows (PowerShell)**:
        ```powershell
        # If needed, allow script execution (Run PowerShell as Admin):
        # Set-ExecutionPolicy RemoteSigned -Scope CurrentUser

        .\start-neo-with-prometheus.ps1
        ```
    *   **Linux / macOS (Bash)**:
        ```bash
        chmod +x start-neo-with-prometheus.sh
        ./start-neo-with-prometheus.sh
        ```

**What the script does:**

1.  Locates the `prometheus`, `grafana-server`, and `alertmanager` binaries (checking local paths first, then system PATH).
2.  Stops potentially conflicting previous instances of `neo-cli` and the monitoring tools.
3.  Creates a timestamped data directory (`monitoring/neo-data_<timestamp>`) for `neo-cli`.
4.  Copies the template `config.json` to `neo-cli`'s binary directory and modifies it to use the new data/log paths.
5.  Starts `prometheus`, `alertmanager`, and `grafana-server` in the background using their respective configuration files.
6.  Starts `neo-cli` in the foreground, passing the `--prometheus` argument (e.g., `--prometheus 127.0.0.1:9101`) so it exposes metrics.

## 6. Accessing Services

Once the script is running `neo-cli`:

-   **Prometheus UI**: `http://localhost:9090`
    -   Explore metrics: Go to `Graph`, enter metric names (e.g., `neo_rpc_requests_total`, `neo_block_height`).
    -   Check targets: Go to `Status` -> `Targets` to see if Prometheus is successfully scraping `neo-cli`.
-   **Grafana UI**: `http://localhost:3000`
    -   **Login**: `admin` / `admin` (change the password on first login).
    -   **Dashboard**: Navigate to `Dashboards` -> `Browse` -> `Neo` folder -> `Neo Node Dashboard`.
-   **Alertmanager UI**: `http://localhost:9093`
    -   View active alerts (if any were defined and firing).
-   **Neo Metrics Endpoint**: `http://localhost:9101/metrics` (or the address specified via `-NeoMetricsAddress` / `--neo-metrics-address`)
    -   View the raw metrics exposed by `neo-cli`.

## 7. Stopping Services

1.  **Stop `neo-cli`**: Press `Ctrl+C` in the terminal where it is running.
2.  **Stop Background Monitoring Stack**: `prometheus`, `grafana-server`, and `alertmanager` continue running.
    *   Navigate to the `scripts/monitoring` directory if you are not already there.
    *   Run the appropriate stop script:
        *   **Windows (PowerShell)**:
            ```powershell
            .\stop_monitoring.ps1
            ```
        *   **Linux/macOS (Bash)**:
            ```bash
            chmod +x stop_monitoring.sh
            ./stop_monitoring.sh
            ```
    *   The stop script will attempt to use the `monitoring/running_pids.json` file (if it exists) or fallback to stopping processes by name/pattern.

## 8. Customization

-   **Ports**: Use script parameters (e.g., `-PrometheusListenAddress`, `--grafana-listen-address`) to change listening ports. Update `prometheus.yml` accordingly if needed (e.g., the `alerting` section or scrape targets if they depend on each other).
-   **Neo Data**: Use `-NeoDataPath`/`--neo-data-path` and `-KeepExistingNeoData`/`--keep-existing-neo-data` to control the `neo-cli` data directory.
-   **Alerting Rules**: Define alerting rules in Prometheus (e.g., create a `*.rules.yml` file and reference it in `prometheus.yml` under `rule_files`).
-   **Alerting Notifications**: Configure receivers (Slack, etc.) in `alertmanager/alertmanager.yml`.
-   **Dashboards**: Add/modify dashboards in `grafana/dashboards/` (JSON files). They should be picked up automatically by Grafana on startup.

## 9. Debugging

-   Carefully read the console output of the start/stop scripts for error messages.
-   Check the log files:
    *   `monitoring/prometheus/prometheus.log`
    *   `monitoring/grafana/grafana.log`
    *   `monitoring/alertmanager/alertmanager.log`
    *   `monitoring/neo-data_<timestamp>/Logs/cli.log` (Neo-CLI logs)
-   **Prometheus UI**: Check `Status` -> `Targets`. Are the `neo`, `prometheus`, `alertmanager` jobs 'UP'? If not, check addresses, ports, and if the target process is running.
-   Use the query utility scripts:
    *   **Windows**: `.\scripts\monitoring\query_metrics.ps1`
    *   **Linux/macOS**: `./scripts/monitoring/query_metrics.sh`
