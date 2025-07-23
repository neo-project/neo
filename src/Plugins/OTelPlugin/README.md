# Neo OpenTelemetry Plugin

This plugin provides OpenTelemetry observability for Neo blockchain nodes, enabling comprehensive monitoring through metrics, tracing, and logging.

## Features

- **Metrics**: Export blockchain metrics to various backends (Prometheus, OTLP, Console)
- **Tracing**: Track transaction and block processing flows (coming soon)
- **Logging**: Integrate Neo logs with OpenTelemetry (coming soon)

## Configuration

The plugin is configured via `OTelPlugin.json`:

```json
{
  "PluginConfiguration": {
    "Enabled": true,
    "ServiceName": "neo-node",
    "ServiceVersion": "3.8.1",
    "InstanceId": "",
    "Metrics": {
      "Enabled": true,
      "Interval": 10000,
      "PrometheusExporter": {
        "Enabled": true,
        "Port": 9090,
        "Path": "/metrics"
      },
      "ConsoleExporter": {
        "Enabled": false
      }
    },
    "Tracing": {
      "Enabled": true,
      "SamplingRatio": 0.1,
      "ConsoleExporter": {
        "Enabled": false
      }
    },
    "Logging": {
      "Enabled": true,
      "IncludeScopes": true,
      "IncludeFormattedMessage": true,
      "ConsoleExporter": {
        "Enabled": false
      }
    },
    "OtlpExporter": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317",
      "Protocol": "grpc",
      "Headers": "",
      "Timeout": 10000,
      "ExportMetrics": true,
      "ExportTraces": true,
      "ExportLogs": true
    },
    "ResourceAttributes": {
      "deployment.environment": "production",
      "service.namespace": "blockchain"
    }
  }
}
```

## Usage

1. Place the plugin in your Neo node's `Plugins` directory
2. Configure the exporters as needed in `OTelPlugin.json`
3. Start your Neo node

### Prometheus Metrics

If Prometheus exporter is enabled, metrics will be available at:
```
http://localhost:9090/metrics
```

### OTLP Export

For OTLP export, ensure your OpenTelemetry collector is running and accessible at the configured endpoint.

## Console Commands

- `telemetry status` - Show current telemetry status

## Metrics (Coming Soon)

The plugin will expose the following metrics:

### Blockchain Metrics
- `neo.blockchain.height` - Current blockchain height
- `neo.blockchain.block_processing_time` - Time to process blocks
- `neo.blockchain.blocks_processed_total` - Total blocks processed
- `neo.blockchain.transactions_processed_total` - Total transactions processed
- `neo.blockchain.transactions_per_block` - Transactions per block distribution

### MemoryPool Metrics
- `neo.mempool.size` - Current mempool size
- `neo.mempool.capacity` - Mempool capacity
- `neo.mempool.transactions_added_total` - Transactions added to mempool
- `neo.mempool.transactions_removed_total` - Transactions removed from mempool
- `neo.mempool.transaction_verification_time` - Transaction verification time

### Network Metrics
- `neo.p2p.connected_peers` - Number of connected peers
- `neo.p2p.messages_received_total` - Total messages received
- `neo.p2p.messages_sent_total` - Total messages sent
- `neo.p2p.bytes_received_total` - Total bytes received
- `neo.p2p.bytes_sent_total` - Total bytes sent

### Smart Contract Metrics
- `neo.contracts.invocations_total` - Total contract invocations
- `neo.contracts.execution_time` - Contract execution time
- `neo.contracts.gas_consumed_total` - Total gas consumed
- `neo.contracts.faults_total` - Contract execution faults

## Development

To extend the plugin with additional metrics or features:

1. Add new metrics in the `OnSystemLoaded` method
2. Subscribe to Neo events to update metrics
3. Follow OpenTelemetry best practices for metric naming and labels

## Requirements

- Neo N3 node
- .NET 9.0 or later
- OpenTelemetry collector (for OTLP export)
- Prometheus server (for Prometheus scraping)