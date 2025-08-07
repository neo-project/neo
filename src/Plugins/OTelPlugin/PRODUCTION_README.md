# OpenTelemetry Plugin for Neo - Production Guide

## Overview
This OpenTelemetry plugin provides production-ready observability for Neo blockchain nodes **without modifying any core classes**. It collects metrics using only publicly exposed properties and existing events, following best practices for clean architecture.

## Architecture Principles
- **Zero Core Modifications**: No changes to LocalNode, MemoryPool, or any core Neo classes
- **Polling-Based Collection**: Metrics are collected via periodic polling of public properties
- **Event-Based Updates**: Uses only existing blockchain events (Committing/Committed)
- **Clean Separation**: Core classes remain unaware of metrics collection
- **Production Ready**: Comprehensive error handling, thread safety, and resource management

## Available Metrics

### ✅ Fully Supported Metrics

#### Blockchain Metrics
- `neo.blocks.processed_total` - Total blocks processed (Counter)
- `neo.transactions.processed_total` - Total transactions processed (Counter)
- `neo.contracts.invocations_total` - Total contract invocations (Counter)
- `neo.block.processing_time` - Block processing time in ms (Histogram)
- `neo.blockchain.height` - Current blockchain height (Gauge)
- `neo.block.processing_rate` - Blocks per second (Gauge)
- `neo.blockchain.is_syncing` - Node sync status (Gauge)
- `neo.transaction.verification_failures_total` - Failed verifications (Counter)

#### MemPool Metrics
- `neo.mempool.size` - Total transactions in mempool (Gauge)
- `neo.mempool.verified_count` - Verified transactions (Gauge)
- `neo.mempool.unverified_count` - Unverified transactions (Gauge)
- `neo.mempool.capacity_ratio` - Usage ratio 0-1 (Gauge)
- `neo.mempool.estimated_bytes` - Estimated memory usage (Gauge)

#### Network Metrics
- `neo.p2p.connected_peers` - Connected peer count (Gauge)
- `neo.p2p.unconnected_peers` - Known unconnected peers (Gauge)

#### System Metrics
- `process.cpu.usage` - Process CPU percentage (Gauge)
- `process.memory.working_set` - Working set memory (Gauge)
- `dotnet.gc.heap_size` - GC heap size (Gauge)
- `process.thread_count` - Thread count (Gauge)
- `neo.node.start_time` - Node start timestamp (Gauge)
- `neo.network.id` - Network identifier (Gauge)

### ❌ Unavailable Metrics (Require Core Support)
These metrics cannot be collected without core modifications:
- Bytes sent/received (requires network layer hooks)
- Message counts by type (requires protocol access)
- Peer connection/disconnection events (no public events)
- Transaction conflict counts (internal operation)
- Batch removal statistics (internal operation)
- Exact memory usage (requires transaction access)

## Configuration

### Basic Configuration
```json
{
  "PluginConfiguration": {
    "Enabled": true,
    "ServiceName": "neo-node",
    "InstanceId": "node-1",
    "UnhandledExceptionPolicy": "StopPlugin",
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
    "OtlpExporter": {
      "Enabled": false,
      "Endpoint": "http://localhost:4317",
      "Protocol": "grpc",
      "ExportMetrics": true
    }
  }
}
```

### Configuration Options

#### Global Settings
- `Enabled` - Enable/disable the plugin (default: true)
- `ServiceName` - Service identifier (default: "neo-node")
- `InstanceId` - Unique instance ID (default: machine name)
- `UnhandledExceptionPolicy` - Error handling: "Ignore", "StopPlugin", "StopNode"

#### Metrics Settings
- `Interval` - Collection interval in milliseconds (default: 10000)
- `PrometheusExporter.Port` - Prometheus HTTP port (default: 9090)
- `PrometheusExporter.Path` - Metrics endpoint path (default: "/metrics")

#### OTLP Exporter
- `Endpoint` - OTLP collector endpoint
- `Protocol` - "grpc" or "http/protobuf"
- `Timeout` - Request timeout in milliseconds

## Deployment

### Prerequisites
- Neo node running with accessible public properties
- .NET 9.0 or later
- OpenTelemetry packages installed

### Installation
1. Copy plugin files to Neo plugins directory
2. Configure `OTelPlugin.json` with your settings
3. Restart Neo node

### Verification
```bash
# Check plugin status
neo> telemetry status

# View current metrics
neo> telemetry metrics

# Test Prometheus endpoint
curl http://localhost:9090/metrics
```

## Monitoring Setup

### Prometheus Configuration
```yaml
scrape_configs:
  - job_name: 'neo-node'
    static_configs:
      - targets: ['localhost:9090']
    scrape_interval: 15s
```

### Grafana Dashboard
Import the provided dashboard JSON for:
- Block processing performance
- MemPool utilization
- Network connectivity
- System resource usage

### Alerting Rules
```yaml
groups:
  - name: neo_alerts
    rules:
      - alert: HighMemPoolUsage
        expr: neo_mempool_capacity_ratio > 0.8
        for: 5m
        annotations:
          summary: "MemPool usage above 80%"
      
      - alert: NodeNotSyncing
        expr: neo_blockchain_is_syncing == 1
        for: 30m
        annotations:
          summary: "Node stuck syncing"
      
      - alert: LowPeerCount
        expr: neo_p2p_connected_peers < 3
        for: 10m
        annotations:
          summary: "Connected peers below threshold"
```

## Performance Considerations

### Resource Usage
- **Memory**: ~10-20 MB for metrics collection
- **CPU**: <1% overhead with default settings
- **Network**: Minimal (only for exporters)

### Optimization Tips
1. Increase collection interval for lower overhead
2. Disable unused exporters
3. Use OTLP with batch export for efficiency
4. Configure appropriate retention in monitoring stack

## Troubleshooting

### Common Issues

#### No Metrics Appearing
- Check plugin is enabled: `telemetry status`
- Verify exporter configuration
- Check firewall rules for Prometheus port
- Review logs for errors

#### High CPU Usage
- Increase collection interval
- Disable console exporter if enabled
- Check for export endpoint issues

#### Memory Growth
- Verify no memory leaks in custom code
- Check metric cardinality
- Review histogram bucket configuration

### Debug Commands
```bash
# Check plugin health
neo> telemetry status

# View real-time metrics
neo> telemetry metrics

# Check Neo system state
neo> show state
```

## Security Considerations

1. **Network Security**
   - Bind Prometheus to localhost only in production
   - Use TLS for OTLP export
   - Implement authentication for metrics endpoint

2. **Resource Limits**
   - Set appropriate timeout values
   - Limit metric cardinality
   - Monitor plugin resource usage

3. **Data Privacy**
   - No sensitive data in metrics
   - No transaction details exposed
   - No private key information

## Migration from Previous Versions

If migrating from a version that modified core classes:
1. Remove all core class modifications
2. Deploy this clean plugin version
3. Update dashboards for removed metrics
4. Adjust alerts for available metrics only

## Support and Maintenance

### Version Compatibility
- Neo: 3.x
- .NET: 9.0+
- OpenTelemetry: 1.6.0+

### Known Limitations
1. Cannot track exact bytes sent/received
2. No per-message-type statistics
3. Memory usage is estimated, not exact
4. No transaction conflict details

### Future Enhancements
Pending core support:
- Network traffic metrics API
- MemPool event system
- Transaction size access
- Message type statistics

## Production Checklist

- [ ] Configure appropriate collection interval
- [ ] Set up monitoring infrastructure (Prometheus/Grafana)
- [ ] Configure alerts for critical metrics
- [ ] Test failover scenarios
- [ ] Document baseline metrics
- [ ] Set up log aggregation
- [ ] Configure backup monitoring
- [ ] Test under load conditions
- [ ] Verify resource consumption
- [ ] Document runbook procedures

## License
MIT License - See LICENSE file for details