# Neo OpenTelemetry Plugin Examples

This directory contains example configurations and queries for monitoring Neo blockchain nodes using the OpenTelemetry plugin.

## Contents

### 1. `prometheus-queries.md`
A comprehensive collection of Prometheus queries for monitoring Neo nodes, including:
- Basic health metrics
- Performance monitoring
- Network statistics
- MemPool analysis
- System resource tracking
- Alert conditions

### 2. `neo-node-dashboard.json`
A ready-to-import Grafana dashboard that provides:
- Real-time blockchain height and sync status
- Peer connection monitoring
- Block processing performance metrics
- Transaction processing rates by type
- Network bandwidth usage
- MemPool utilization

To import:
1. Open Grafana
2. Go to Dashboards → Import
3. Upload the JSON file or paste its contents
4. Select your Prometheus datasource
5. Click Import

### 3. `prometheus-alerts.yml`
Prometheus alerting rules for Neo node monitoring with three severity levels:
- **Critical**: Node down, no peers, sync issues
- **Warning**: Low peers, high resource usage, slow processing
- **Info**: Transaction failures, conflicts, performance degradation

To use:
1. Add to your Prometheus configuration:
   ```yaml
   rule_files:
     - 'prometheus-alerts.yml'
   ```
2. Reload Prometheus configuration

## Sample Metrics Output

When the OpenTelemetry plugin is running, you can access metrics at `http://localhost:9090/metrics`. Here's what you'll see:

```
# Blockchain Metrics
neo_blockchain_height 2500000
neo_blocks_processed_total 1234567
neo_block_processing_time_bucket{le="100"} 4900
neo_blockchain_is_syncing 0

# Network Metrics
neo_p2p_connected_peers 10
neo_p2p_bytes_sent_total 1073741824
neo_p2p_bytes_received_total 2147483648

# MemPool Metrics
neo_mempool_size 150
neo_mempool_verified_count 140
neo_mempool_capacity_ratio 0.15

# System Metrics
process_cpu_usage 15.3
process_memory_working_set 536870912
dotnet_gc_heap_size 268435456
```

## Quick Start Guide

1. **Enable the Plugin**
   - Ensure OTelPlugin is in your Neo-CLI plugins directory
   - Configure `OTelPlugin.json` with your desired settings

2. **Set Up Prometheus**
   ```yaml
   scrape_configs:
     - job_name: 'neo-node'
       static_configs:
         - targets: ['localhost:9090']
   ```

3. **Import Grafana Dashboard**
   - Use the provided `neo-node-dashboard.json`

4. **Configure Alerts**
   - Add `prometheus-alerts.yml` to your Prometheus configuration

5. **Monitor Your Node**
   - Access Grafana dashboards
   - Set up alert notifications
   - Use queries from `prometheus-queries.md` for custom analysis

## Metric Categories

### Core Blockchain Metrics
- `neo_blockchain_height`: Current block height
- `neo_blocks_processed_total`: Total blocks processed
- `neo_block_processing_time`: Block processing duration histogram
- `neo_transactions_processed_total`: Transactions by type
- `neo_contracts_invocations_total`: Smart contract calls

### Network Metrics
- `neo_p2p_connected_peers`: Active peer connections
- `neo_p2p_bytes_sent/received_total`: Network traffic
- `neo_p2p_peer_connected/disconnected_total`: Peer churn

### MemPool Metrics
- `neo_mempool_size`: Total transactions in mempool
- `neo_mempool_verified/unverified_count`: Transaction states
- `neo_mempool_memory_bytes`: Memory usage
- `neo_mempool_capacity_ratio`: Utilization percentage

### System Metrics
- `process_cpu_usage`: CPU utilization
- `process_memory_working_set`: Memory usage
- `dotnet_gc_heap_size`: .NET garbage collection
- `neo_node_start_time`: Node uptime tracking

## Best Practices

1. **Retention**: Configure appropriate data retention in Prometheus
2. **Sampling**: Adjust metric collection intervals based on needs
3. **Alerting**: Customize alert thresholds for your environment
4. **Dashboards**: Create role-specific dashboards (ops, dev, business)
5. **Export**: Consider OTLP export for centralized monitoring

## Troubleshooting

If metrics are not appearing:
1. Check if the plugin is loaded: Look for "OpenTelemetry plugin initialized successfully" in logs
2. Verify Prometheus endpoint: `curl http://localhost:9090/metrics`
3. Check plugin configuration: Ensure `Enabled: true` in `OTelPlugin.json`
4. Review Neo-CLI logs for any errors

## Support

For issues or questions:
- Check the main plugin documentation
- Review Neo-CLI logs
- Verify network connectivity to Prometheus endpoint