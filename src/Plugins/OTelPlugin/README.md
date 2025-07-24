# Neo OpenTelemetry Plugin

A production-ready OpenTelemetry plugin for Neo blockchain nodes that provides comprehensive observability through metrics collection and export.

## Features

- **Real-time Blockchain Metrics**: Tracks blocks, transactions, and contract invocations
- **Multiple Export Options**: Prometheus, OTLP, and Console exporters
- **Thread-Safe Implementation**: Designed for production use with proper synchronization
- **Event-Driven Architecture**: Integrates with Neo blockchain events for accurate metrics
- **Configuration Validation**: Validates all settings with clear error messages
- **Resource Management**: Proper disposal patterns and error recovery

## Quick Start

1. Copy the plugin to your Neo node's `Plugins` directory
2. Start your Neo node (Prometheus metrics are enabled by default)
3. Access metrics at `http://localhost:9090/metrics`
4. Use `telemetry status` command to check plugin status

## Metrics Collected

### Blockchain Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `neo.blocks.processed_total` | Counter | Total number of blocks processed | - |
| `neo.transactions.processed_total` | Counter | Total number of transactions processed | - |
| `neo.contracts.invocations_total` | Counter | Total number of contract invocations | - |
| `neo.block.processing_time` | Histogram | Time taken to process a block (ms) | - |
| `neo.blockchain.height` | Gauge | Current blockchain height | - |
| `neo.blockchain.is_syncing` | Gauge | Whether node is syncing (1=yes, 0=no) | - |

### Network Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `neo.p2p.connected_peers` | Gauge | Number of connected P2P peers | - |
| `neo.p2p.unconnected_peers` | Gauge | Number of known but unconnected peers | - |
| `neo.p2p.bytes_sent_total` | Counter | Total bytes sent to peers | - |
| `neo.p2p.bytes_received_total` | Counter | Total bytes received from peers | - |

### MemPool Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `neo.mempool.size` | Gauge | Current number of transactions in mempool | - |
| `neo.mempool.verified_count` | Gauge | Number of verified transactions | - |
| `neo.mempool.unverified_count` | Gauge | Number of unverified transactions | - |
| `neo.mempool.memory_bytes` | Gauge | Total memory used by mempool | - |

### System Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `process_cpu_usage` | Gauge | Process CPU usage percentage | - |
| `system_cpu_usage` | Gauge | System CPU usage percentage | - |
| `process_memory_working_set` | Gauge | Process working set memory (bytes) | - |
| `process_memory_virtual` | Gauge | Process virtual memory (bytes) | - |
| `dotnet_gc_heap_size` | Gauge | .NET GC heap size (bytes) | - |
| `process_thread_count` | Gauge | Number of process threads | - |
| `neo_node_start_time` | Gauge | Node start time (Unix timestamp) | - |
| `neo_network_id` | Gauge | Network ID (0=TestNet, 1=MainNet) | - |

## Configuration

Configure the plugin via `OTelPlugin.json`. All settings are validated on startup.

### Basic Configuration

```json
{
  "PluginConfiguration": {
    "Enabled": true,
    "ServiceName": "neo-node",
    "ServiceVersion": "3.8.1",
    "InstanceId": "node-1"
  }
}
```

### Prometheus Configuration

```json
{
  "Metrics": {
    "Enabled": true,
    "PrometheusExporter": {
      "Enabled": true,
      "Port": 9090,
      "Path": "/metrics"
    }
  }
}
```

### OTLP Configuration

```json
{
  "OtlpExporter": {
    "Enabled": true,
    "Endpoint": "http://localhost:4317",
    "Protocol": "grpc",
    "Headers": "api-key=your-key",
    "Timeout": 10000,
    "ExportMetrics": true
  }
}
```

### Resource Attributes

Add custom attributes to identify your node:

```json
{
  "ResourceAttributes": {
    "deployment.environment": "production",
    "service.namespace": "blockchain",
    "node.type": "full",
    "datacenter": "us-east-1"
  }
}
```

## Console Commands

- `telemetry status` - Display current telemetry status including:
  - Plugin enabled state
  - Current blockchain height
  - MemPool size
  - Connected peers count
  - Active metrics and exporters

## Monitoring Setup

### Prometheus

1. Configure Prometheus to scrape your Neo node:

```yaml
scrape_configs:
  - job_name: 'neo-node'
    static_configs:
      - targets: ['localhost:9090']
    scrape_interval: 15s
```

2. Example Prometheus queries:
   - Block processing rate: `rate(neo_blocks_processed_total[5m])`
   - Transaction throughput: `rate(neo_transactions_processed_total[5m])`
   - Average block time: `rate(neo_block_processing_time_sum[5m]) / rate(neo_block_processing_time_count[5m])`

### Grafana Dashboards

Pre-configured dashboards are available in the `grafana/` directory:
- `neo-node-overview-dashboard.json` - Comprehensive overview with system metrics, network status, and blockchain data
- `neo-complete-dashboard.json` - Detailed metrics dashboard with performance analysis  
- `neo-node-health-dashboard.json` - Focused health monitoring dashboard

The overview dashboard includes:
- **Node Information**: Block height, peer count, network type, sync status, uptime
- **System Resources**: CPU usage, memory consumption, disk usage, thread count
- **Network Activity**: Bandwidth usage, peer connections over time
- **Blockchain Activity**: Block height progress, processing times, transaction statistics

Import these dashboards into your Grafana instance for instant visualization.

### OTLP Collector

Configure your OpenTelemetry Collector:

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317

processors:
  batch:

exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"

service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

## Security Considerations

- **Endpoint Validation**: Only HTTP/HTTPS endpoints are allowed for OTLP export
- **Header Sanitization**: Headers are sanitized to prevent injection attacks
- **Port Validation**: Prometheus port must be between 1-65535
- **Resource Limits**: Metrics are collected with proper synchronization to prevent resource exhaustion

## Troubleshooting

### Plugin Not Loading
- Check Neo logs for configuration errors
- Verify `OTelPlugin.json` is valid JSON
- Ensure all required dependencies are present

### No Metrics Exported
- Use `telemetry status` to check if metrics are active
- Verify exporter configuration (ports, endpoints)
- Check firewall settings for Prometheus port

### High Memory Usage
- Reduce metric collection frequency in configuration
- Disable unused exporters
- Check for metric cardinality issues

## Performance Impact

The plugin is designed for minimal performance impact:
- Metrics are collected during existing blockchain events
- Thread-safe implementation prevents contention
- Efficient metric recording using OpenTelemetry SDK
- Typical overhead: <1% CPU, <50MB memory

## Development

To extend the plugin:

1. Add new metrics in `InitializeMetrics()`
2. Update metric values in appropriate event handlers
3. Follow OpenTelemetry semantic conventions for naming
4. Add unit tests for new functionality

### Building from Source

```bash
cd src/Plugins/OTelPlugin
dotnet build
```

### Running Tests

```bash
cd tests/Neo.Plugins.OTelPlugin.Tests
dotnet test
```

## Requirements

- Neo N3 node (v3.8.0+)
- .NET 9.0 runtime
- Network access for exporters
- 50MB free memory
- Port 9090 available (for Prometheus)

## License

This plugin is part of the Neo project and is distributed under the MIT license.