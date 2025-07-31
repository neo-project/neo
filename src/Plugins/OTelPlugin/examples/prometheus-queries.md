# Prometheus Query Examples for Neo Blockchain Monitoring

This document provides example Prometheus queries for monitoring a Neo blockchain node using the OpenTelemetry plugin.

## Basic Health Queries

### 1. Current Blockchain Height
```promql
neo_blockchain_height
```

### 2. Block Processing Rate (blocks per second)
```promql
neo_block_processing_rate
```

### 3. Node Sync Status
```promql
neo_blockchain_is_syncing
```

### 4. Connected Peers
```promql
neo_p2p_connected_peers
```

## Performance Queries

### 5. Average Block Processing Time (last 5 minutes)
```promql
rate(neo_block_processing_time_sum[5m]) / rate(neo_block_processing_time_count[5m])
```

### 6. Block Processing Time Percentiles
```promql
# 95th percentile
histogram_quantile(0.95, rate(neo_block_processing_time_bucket[5m]))

# 99th percentile
histogram_quantile(0.99, rate(neo_block_processing_time_bucket[5m]))
```

### 7. Transaction Processing Rate
```promql
sum(rate(neo_transactions_processed_total[5m])) by (type)
```

## Network Metrics

### 8. Network Bandwidth Usage
```promql
# Bytes received per second
rate(neo_p2p_bytes_received_total[1m])

# Bytes sent per second
rate(neo_p2p_bytes_sent_total[1m])
```

### 9. Peer Churn Rate
```promql
# Connections per minute
rate(neo_p2p_peer_connected_total[1m]) * 60

# Disconnections per minute
rate(neo_p2p_peer_disconnected_total[1m]) * 60
```

## MemPool Monitoring

### 10. MemPool Utilization
```promql
neo_mempool_capacity_ratio * 100
```

### 11. MemPool Transaction Backlog
```promql
neo_mempool_verified_count + neo_mempool_unverified_count
```

### 12. MemPool Memory Usage (MB)
```promql
neo_mempool_memory_bytes / 1024 / 1024
```

## System Resources

### 13. CPU Usage
```promql
process_cpu_usage
```

### 14. Memory Usage (GB)
```promql
process_memory_working_set / 1024 / 1024 / 1024
```

### 15. GC Pressure
```promql
rate(dotnet_gc_heap_size[5m])
```

## Alert Examples

### 16. High MemPool Usage Alert
```promql
neo_mempool_capacity_ratio > 0.8
```

### 17. Low Peer Count Alert
```promql
neo_p2p_connected_peers < 3
```

### 18. Block Processing Slowdown Alert
```promql
histogram_quantile(0.95, rate(neo_block_processing_time_bucket[5m])) > 1000
```

### 19. Node Falling Behind Alert
```promql
neo_blockchain_is_syncing == 1 and up > 300
```

### 20. High CPU Usage Alert
```promql
process_cpu_usage > 80
```

## Dashboard Queries

### 21. Block Processing Success Rate
```promql
(
  sum(rate(neo_blocks_processed_total[5m])) / 
  (sum(rate(neo_blocks_processed_total[5m])) + sum(rate(neo_transaction_verification_failures_total[5m])))
) * 100
```

### 22. Contract Invocation Rate
```promql
rate(neo_contracts_invocations_total[5m])
```

### 23. Network Health Score (custom metric)
```promql
(
  (neo_p2p_connected_peers / 10) * 0.3 +
  (1 - neo_mempool_capacity_ratio) * 0.3 +
  (1 - neo_blockchain_is_syncing) * 0.4
) * 100
```

## Usage Tips

1. **Time Ranges**: Adjust `[5m]` to different intervals based on your monitoring needs
2. **Aggregations**: Use `sum()`, `avg()`, `max()`, `min()` for multi-instance deployments
3. **Labels**: Filter by labels like `{instance="node1"}` for specific nodes
4. **Recording Rules**: Create recording rules for frequently used complex queries

## Grafana Dashboard Variables

```promql
# Instance selector
label_values(neo_blockchain_height, instance)

# Network selector
label_values(neo_network_id, network)
```