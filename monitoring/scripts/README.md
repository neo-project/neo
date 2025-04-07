# Neo Monitoring Scripts

This directory contains scripts to run Neo-CLI with a local monitoring stack (Prometheus, Grafana, Alertmanager).

## Enabling Prometheus Metrics in Neo-CLI

The `start-neo-with-prometheus.ps1` script automatically enables Prometheus metrics exposure in Neo-CLI via command-line arguments when starting the node.

For reference, Prometheus monitoring can also be enabled directly in the Neo node configuration file (`src/Neo.CLI/config.json`) by adding or modifying the `Prometheus` section within `ApplicationConfiguration`:

```json
{
  "ApplicationConfiguration": {
    // ... other settings ...
    "Prometheus": {
      "Enabled": true,
      "Host": "0.0.0.0", // Or 127.0.0.1
      "Port": 9101      // Port for metrics endpoint (adjust if needed)
    },
    // ... other settings ...
  }
  // ... other sections ...
}
```

However, the `start-neo-with-prometheus.ps1` script will use the `--prometheus <host>:<port>` command-line argument (defaulting to `127.0.0.1:9101`), which takes precedence over the configuration file settings.

See the main `docs/monitoring.md` for comprehensive details on available metrics, PromQL examples, and alerting rules.

## Installation Prerequisites

Before running the start script, you **must** install the following components and ensure their executables (`.exe` files) are either:

a) Placed in their respective subdirectories within the `monitoring` folder (e.g., `monitoring/prometheus/prometheus.exe`, `monitoring/grafana/bin/grafana-server.exe`, `monitoring/alertmanager/alertmanager.exe`).
b) Available in your system's PATH environment variable.

*   **Prometheus:** [https://prometheus.io/download/](https://prometheus.io/download/)
*   **Grafana:** [https://grafana.com/grafana/download/](https://grafana.com/grafana/download/)
*   **Alertmanager:** [https://prometheus.io/download/#alertmanager](https://prometheus.io/download/#alertmanager)

Refer to the main `monitoring/README.md` for more detailed setup instructions.

## Scripts

*   **`start-neo-with-prometheus.ps1`**: (PowerShell) Starts Neo-CLI along with the local monitoring stack (Prometheus, Grafana, Alertmanager). It handles finding binaries, managing configuration files (Prometheus, Alertmanager, Neo-CLI), managing data directories, and starting the processes. Neo-CLI runs in the foreground, while monitoring components run in the background.
*   **`stop_monitoring.ps1`**: (PowerShell) Stops the background monitoring processes (Prometheus, Grafana, Alertmanager) that were started by `start-neo-with-prometheus.ps1`. It uses a PID file created by the start script if available, otherwise it attempts to find and stop the processes by name.
*   `query_metrics.ps1`: (PowerShell) Utility script to query metrics directly from a running Neo-CLI Prometheus endpoint (e.g., `http://127.0.0.1:9101/metrics`).
*   `query_metrics.sh`: (Bash) Utility script to query metrics directly from a running Neo-CLI Prometheus endpoint.

## Starting and Stopping the Setup

1.  **Build Neo-CLI:** Ensure you have built the `Neo.CLI` project (e.g., using `dotnet build` in the `src/Neo.CLI` directory).
2.  **Install Binaries:** Make sure Prometheus, Grafana, and Alertmanager are installed as described in Prerequisites.
3.  **Run Start Script:** Open PowerShell, navigate to the `monitoring/scripts` directory, and run:
    ```powershell
    .\start-neo-with-prometheus.ps1
    ```
    *(Optional arguments can be used to customize ports and data paths, see `.\start-neo-with-prometheus.ps1 -help`)*
4.  **Use Neo-CLI:** Interact with Neo-CLI in the console as needed.
5.  **Stop Neo-CLI:** Press `Ctrl+C` in the Neo-CLI console when finished.
6.  **Run Stop Script:** To stop the background monitoring services, run:
    ```powershell
    .\stop_monitoring.ps1
    ```

## Accessing Services (Default Ports)

*   Neo CLI RPC: http://localhost:10332 (Default, check `src/Neo.CLI/config.json`)
*   Neo CLI Metrics: http://127.0.0.1:9101/metrics
*   Prometheus UI: http://127.0.0.1:9090
*   Grafana UI: http://127.0.0.1:3000 (Login: admin/admin)
*   Alertmanager UI: http://127.0.0.1:9093
