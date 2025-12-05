# Neo N3 Telemetry Plugin

A comprehensive telemetry and metrics collection plugin for Neo N3 full nodes, providing Prometheus-compatible metrics for monitoring node health, performance, and operational status.

## Features

- **Blockchain Metrics**: Block height, sync status, block processing time, transaction counts
- **Network Metrics**: Peer connections, message statistics, bandwidth usage
- **Mempool Metrics**: Transaction pool size, utilization, add/remove rates
- **System Metrics**: CPU usage, memory consumption, GC statistics, thread pool
- **Plugin Metrics**: Loaded plugins count and status
- **Prometheus Export**: Native Prometheus metrics endpoint

## Installation

1. Build the plugin:
```bash
dotnet build src/Plugins/TelemetryPlugin/TelemetryPlugin.csproj
```

2. Copy the output to your Neo node's `Plugins/TelemetryPlugin/` directory:
```bash
cp -r bin/Debug/net10.0/* /path/to/neo-node/Plugins/TelemetryPlugin/
```

3. Configure the plugin by editing `config.json` in the plugin directory.

## Configuration

Create or edit `config.json` in the `Plugins/TelemetryPlugin/` directory:

```json
{
  "PluginConfiguration": {
    "Enabled": true,
    "ExceptionPolicy": "StopPlugin",
    "PrometheusPort": 9100,
    "PrometheusHost": "localhost",
    "PrometheusPath": "/metrics",
    "SystemMetricsIntervalMs": 5000,
    "CollectBlockchainMetrics": true,
    "CollectNetworkMetrics": true,
    "CollectMempoolMetrics": true,
    "CollectSystemMetrics": true,
    "CollectConsensusMetrics": true,
    "NodeId": "my-neo-node",
    "NetworkName": "mainnet"
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable the telemetry plugin |
| `ExceptionPolicy` | string | `StopPlugin` | Exception handling policy (`StopNode`, `StopPlugin`, `Ignore`) |
| `PrometheusPort` | int | `9100` | Port for the Prometheus metrics endpoint |
| `PrometheusHost` | string | `localhost` | Host address for the metrics endpoint |
| `PrometheusPath` | string | `/metrics` | URL path for the metrics endpoint |
| `SystemMetricsIntervalMs` | int | `5000` | Interval for collecting system metrics (ms) |
| `CollectBlockchainMetrics` | bool | `true` | Enable blockchain metrics collection |
| `CollectNetworkMetrics` | bool | `true` | Enable network metrics collection |
| `CollectMempoolMetrics` | bool | `true` | Enable mempool metrics collection |
| `CollectSystemMetrics` | bool | `true` | Enable system resource metrics collection |
| `CollectConsensusMetrics` | bool | `true` | Enable consensus metrics collection |
| `NodeId` | string | hostname | Unique identifier for this node |
| `NetworkName` | string | auto-detect | Network name label (mainnet, testnet, etc.) |

## Metrics Reference

### Blockchain Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `neo_blockchain_height` | Gauge | Current block height |
| `neo_blockchain_header_height` | Gauge | Current header height |
| `neo_blockchain_blocks_persisted_total` | Counter | Total blocks persisted |
| `neo_blockchain_block_persist_duration_milliseconds` | Histogram | Block persist duration |
| `neo_blockchain_block_transactions` | Gauge | Transactions in last block |
| `neo_blockchain_transactions_processed_total` | Counter | Total transactions processed |
| `neo_blockchain_sync_status` | Gauge | Sync status (1=synced, 0=syncing) |
| `neo_blockchain_blocks_behind` | Gauge | Blocks behind network |
| `neo_blockchain_time_since_last_block_seconds` | Gauge | Time since last block |

### Network Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `neo_network_peers_connected` | Gauge | Connected peer count |
| `neo_network_peers_unconnected` | Gauge | Unconnected peer count |
| `neo_network_peer_connections_total` | Counter | Total peer connections |
| `neo_network_peer_disconnections_total` | Counter | Total peer disconnections |
| `neo_network_messages_received_total` | Counter | Messages received by type |
| `neo_network_messages_sent_total` | Counter | Messages sent by type |
| `neo_network_bytes_received_total` | Counter | Total bytes received |
| `neo_network_bytes_sent_total` | Counter | Total bytes sent |

### Mempool Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `neo_mempool_transactions` | Gauge | Current mempool size |
| `neo_mempool_verified_transactions` | Gauge | Verified transaction count |
| `neo_mempool_unverified_transactions` | Gauge | Unverified transaction count |
| `neo_mempool_capacity` | Gauge | Mempool capacity |
| `neo_mempool_utilization_ratio` | Gauge | Mempool utilization (0-1) |
| `neo_mempool_transactions_added_total` | Counter | Total transactions added |
| `neo_mempool_transactions_removed_total` | Counter | Total transactions removed |

### System Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `neo_system_cpu_usage_ratio` | Gauge | CPU usage ratio (0-1) |
| `neo_system_memory_usage_bytes` | Gauge | Memory usage by type |
| `neo_system_gc_collection_count` | Gauge | GC collections by generation |
| `neo_system_threadpool_worker_threads` | Gauge | Active worker threads |
| `neo_system_threadpool_completion_port_threads` | Gauge | Active completion port threads |
| `neo_system_process_uptime_seconds` | Gauge | Process uptime |

### Node Info Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `neo_node_info` | Gauge | Node information (version labels) |
| `neo_node_start_time_seconds` | Gauge | Node start timestamp |
| `neo_plugins_loaded` | Gauge | Number of loaded plugins |
| `neo_plugin_status` | Gauge | Individual plugin status |

## Prometheus Integration

### Scrape Configuration

Add to your `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'neo-node'
    static_configs:
      - targets: ['localhost:9100']
    scrape_interval: 15s
```

### Example Queries

```promql
# Block height
neo_blockchain_height{node_id="my-node"}

# Sync progress
1 - (neo_blockchain_blocks_behind / neo_blockchain_header_height)

# Mempool utilization
neo_mempool_utilization_ratio{node_id="my-node"}

# Transaction rate (per minute)
rate(neo_blockchain_transactions_processed_total[1m])

# Connected peers
neo_network_peers_connected{node_id="my-node"}

# Memory usage
neo_system_memory_usage_bytes{type="working_set"}
```

## Grafana Dashboard

Import the provided Grafana dashboard JSON for a pre-configured monitoring view:

1. Open Grafana
2. Go to Dashboards → Import
3. Upload `grafana-dashboard.json` or paste the JSON content
4. Select your Prometheus data source
5. Click Import

## Alerting Examples

### Prometheus Alerting Rules

```yaml
groups:
  - name: neo-node-alerts
    rules:
      - alert: NeoNodeOutOfSync
        expr: neo_blockchain_blocks_behind > 10
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Neo node is out of sync"
          description: "Node {{ $labels.node_id }} is {{ $value }} blocks behind"

      - alert: NeoNodeNoPeers
        expr: neo_network_peers_connected == 0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Neo node has no peers"
          description: "Node {{ $labels.node_id }} has no connected peers"

      - alert: NeoMempoolFull
        expr: neo_mempool_utilization_ratio > 0.9
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Neo mempool is nearly full"
          description: "Mempool utilization is {{ $value | humanizePercentage }}"
```

## Troubleshooting

### Metrics endpoint not accessible

1. Check if the plugin is enabled in `config.json`
2. Verify the port is not in use: `netstat -tlnp | grep 9100`
3. Check firewall rules allow the port
4. Review node logs for plugin startup errors

### Missing metrics

1. Ensure the corresponding `Collect*Metrics` option is enabled
2. Check that the node has fully started
3. Verify the metric collection interval is appropriate

### High resource usage

1. Increase `SystemMetricsIntervalMs` to reduce collection frequency
2. Disable unnecessary metric categories
3. Consider using a dedicated metrics aggregation service

## License

This plugin is part of the Neo project and is distributed under the MIT license.
