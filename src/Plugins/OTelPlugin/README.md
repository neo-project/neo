# Neo OpenTelemetry Plugin

A production-ready OpenTelemetry plugin for Neo blockchain nodes that provides comprehensive observability through metrics collection and export.

## Features

- **Real-time Blockchain Metrics**: Tracks blocks, transactions, and contract invocations
- **Multiple Export Options**: Prometheus, OTLP, and Console exporters
- **Thread-Safe Implementation**: Designed for production use with proper synchronization
- **Event-Driven Architecture**: Integrates with Neo blockchain events for accurate metrics
- **Consensus Telemetry**: Tracks dBFT view changes, finality, and message flow for production validators
- **State Service Visibility**: Monitors state root lag, validation cadence, and snapshot health
- **RPC Observability**: Captures request throughput, latency percentiles, and error rates
- **Hot Trace Profiling**: Captures hot opcode sequences per contract for targeted JIT tuning
- **Operational Health Signals**: Readiness and health scoring backed by automated telemetry checks
- **Cross-Platform System Metrics**: Portable CPU and memory telemetry for Windows and Linux
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

### MemPool Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `neo.mempool.size` | Gauge | Current number of transactions in mempool | - |
| `neo.mempool.verified_count` | Gauge | Number of verified transactions | - |
| `neo.mempool.unverified_count` | Gauge | Number of unverified transactions | - |
| `neo.mempool.memory_bytes` | Gauge | Estimated memory footprint of transactions in mempool | - |
| `neo.mempool.conflicts_total` | Counter | Transactions evicted because of conflicts | - |
| `neo.mempool.batch_removed_size` | Histogram | Batch size of removals triggered by mempool events | - |
| `neo.mempool.capacity_ratio` | Gauge | Ratio of current mempool usage to configured capacity | - |

### System Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `process.cpu.usage` | Gauge | Process CPU usage percentage | - |
| `system.cpu.usage` | Gauge | System CPU usage percentage | - |
| `process.memory.working_set` | Gauge | Process working set memory (bytes) | - |
| `process.memory.virtual` | Gauge | Process virtual memory (bytes) | - |
| `dotnet.gc.heap_size` | Gauge | .NET GC heap size (bytes) | - |
| `process.thread_count` | Gauge | Number of process threads | - |
| `process.file_descriptors` | Gauge | Open file descriptors / handles | - |
| `neo.node.disk_free_bytes` | Gauge | Free disk space for the chain data volume | - |
| `neo.node.chain_db_size_bytes` | Gauge | Approximate on-disk size of the chain database | - |
| `neo.node.start_time` | Gauge | Node start time (Unix timestamp) | - |
| `neo.node.health_score` | Gauge | Telemetry health (-1=unhealthy, 0=degraded, 1=healthy) | - |
| `neo.node.readiness` | Gauge | Node readiness for serving traffic (1=ready, 0=not ready) | - |
| `neo.node.last_activity` | Gauge | Unix timestamp of the last persisted block | - |
| `neo.network.id` | Gauge | Neo network magic identifier | - |


### Consensus Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `neo.consensus.round` | Gauge | Latest block height observed by consensus | - |
| `neo.consensus.view` | Gauge | Current consensus view number | - |
| `neo.consensus.state` | Gauge | Current primary validator index | - |
| `neo.consensus.view_changes_total` | Counter | View changes grouped by reason | `reason` |
| `neo.consensus.messages_sent_total` | Counter | Consensus messages sent | `type` |
| `neo.consensus.messages_received_total` | Counter | Consensus messages received | `type` |
| `neo.consensus.time_to_finality` | Gauge | Time from proposal to block persistence (ms) | - |

### VM Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `neo.vm.instruction_rate` | Gauge | Average VM instruction dispatch rate (ops/s) | - |
| `neo.vm.instruction_latency_ms` | Gauge | Average VM instruction dispatch latency (ms) | - |
| `neo.vm.evaluation_stack_depth` | Gauge | Current evaluation stack depth | - |
| `neo.vm.invocation_stack_depth` | Gauge | Current invocation stack depth | - |
| `neo.vm.result_stack_depth` | Gauge | Current result stack depth | - |
| `neo.vm.reference_sweeps_rate` | Gauge | Reference sweep operations per second | - |
| `neo.vm.trace.hot_ratio` | Gauge | Hit ratio of the hottest opcode window (per script, labelled) | `script`, `sequence`, `hits`, `total_instructions`, `last_seen` |
| `neo.vm.trace.hot_hits` | Gauge | Hit count of the hottest opcode window (per script, labelled) | `script`, `sequence`, `total_instructions`, `last_seen` |
| `neo.vm.trace.max_hot_ratio` | Gauge | Maximum hot-trace hit ratio across scripts | - |
| `neo.vm.trace.max_hot_hits` | Gauge | Maximum hot-trace hit count across scripts | - |
| `neo.vm.trace.profile_count` | Gauge | Number of trace profiles persisted | - |
| `neo.vm.superinstruction.plan_count` | Gauge | Number of super-instruction plans derived from profiling | - |

### RPC Metrics
| Metric Name | Type | Description | Labels |
|------------|------|-------------|---------|
| `neo.rpc.active_requests` | Gauge | In-flight RPC requests per instance | - |
| `neo.rpc.requests_total` | Counter | Total RPC requests processed | `method` |
| `neo.rpc.request_errors_total` | Counter | RPC failures grouped by method and code | `method`, `code` |
| `neo.rpc.request_duration_ms` | Histogram | RPC request latencies (milliseconds) | `method`, `result` |

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
    "Categories": {
      "Blockchain": true,
      "Mempool": true,
      "Network": true,
      "System": true,
      "Consensus": true,
      "State": true,
      "Vm": true,
      "Rpc": true
    },
    "PrometheusExporter": {
      "Enabled": true,
      "Port": 9090,
      "Path": "/metrics"
    }
  }
}
```

### Metric Categories

Set `Metrics.Categories` flags to disable an entire telemetry area without code changes. All switches default to `true`.

- `Blockchain` – block/transaction counters, processing latency, planner rate gauges
- `Mempool` – pool occupancy, capacity ratio, conflict tracking
- `Network` – connected and unconnected peer gauges
- `System` – process health, readiness, disk capacity, file descriptors
- `Consensus` – dBFT round/view/state gauges with message and finality tracking
- `State` – state root heights, lag, snapshot durations, validation counters
- `Vm` – EventCounter feeds, stack depth gauges, trace profiler aggregates, super-instruction plans
- `Rpc` – request concurrency, throughput, error rate, latency histograms

Example override:

```json
{
  "Metrics": {
    "Categories": {
      "Vm": false,
      "Rpc": true
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
- `telemetry plans [count]` - Print the top super-instruction planner suggestions captured from runtime telemetry (defaults to 10 entries).

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
   - Conflicting transactions: `increase(neo_mempool_conflicts_total[15m])`
   - Average block time: `rate(neo_block_processing_time_sum[5m]) / rate(neo_block_processing_time_count[5m])`

### Grafana Dashboards

Pre-configured dashboards are available in the `monitoring/` directory:
- `neo-dashboard.json` - Comprehensive overview with system metrics, network status, and blockchain data
- `neo-dashboard.html` - Lightweight dashboard preview that mirrors the JSON layout
- `professional-dashboard.html` - Production-ready multi-pane monitoring view
- `real-dashboard.html` - Alternative layout focused on operational runbooks

### Trace Profiling Output

Trace profiling produces hot opcode sequences per script. Profiles persist to `Plugins/OTelPlugin/profiles/vm-trace-profiles.json` and expose:

- `scriptHash` – 20-byte Neo script hash for the contract/script
- `hotSequence` – Most frequently observed 6-opcode window
- `hitCount` / `totalInstructions` – Frequency data to feed super-instruction tuning

The planner also emits `Plugins/OTelPlugin/profiles/vm-superinstructions.json`, bundling the most valuable sequences (with hit ratios and counts) so they can be ingested directly by your super-instruction/JIT pipeline. Pair these artifacts with the `neo_vm_superinstruction_plan_count` gauge and the "VM Optimization Playbook" panel to close the loop from profiling to deployment.

### Converting Planner Output into C# Stubs

Use the helper in `tools/generate_superinstructions.py` to convert the planner JSON into a partial C# helper:

```bash
python tools/generate_superinstructions.py \
  --input Plugins/OTelPlugin/profiles/vm-superinstructions.json \
  --output src/Neo.VM/JumpTable.SuperInstructions.generated.cs \
  --min-ratio 0.10 --max-count 24
```

The generated file exposes a static list of sequences with hit ratios/counts, ready to feed into your JumpTable or JIT optimisation pipeline.

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
