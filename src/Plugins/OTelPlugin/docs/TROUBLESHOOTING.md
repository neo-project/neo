# Neo Node Troubleshooting Guide Using Metrics

This guide helps you diagnose and resolve common issues with Neo blockchain nodes using the OpenTelemetry metrics.

## Quick Health Check

Run these Prometheus queries to get a quick health assessment:

```promql
# Is the node syncing?
rate(neo_blockchain_height[5m]) > 0

# Peer connectivity
neo_p2p_connected_peers >= 3

# MemPool health
neo_mempool_capacity_ratio < 0.8

# Processing performance
histogram_quantile(0.95, rate(neo_block_processing_time_bucket[5m])) < 1000
```

## Common Issues and Solutions

### 1. Node Not Syncing

**Symptoms:**
- `neo_blockchain_height` not increasing
- `neo_block_processing_rate` is 0

**Check these metrics:**
```promql
# Current block height
neo_blockchain_height

# Block processing rate
neo_block_processing_rate

# Connected peers
neo_p2p_connected_peers

# Network activity
rate(neo_p2p_bytes_received_total[5m])
```

**Possible causes and solutions:**

1. **No peers connected**
   - Check firewall settings
   - Verify network configuration
   - Check if seed nodes are reachable

2. **Stuck on specific block**
   - Check `neo_block_processing_time` for spikes
   - Look for errors in logs
   - May need to resync from scratch

3. **Resource constraints**
   - Check system CPU/memory/disk
   - Look at `neo_mempool_memory_bytes`
   - Consider increasing resources

### 2. High MemPool Usage

**Symptoms:**
- `neo_mempool_capacity_ratio` > 0.8
- `neo_mempool_size` near capacity

**Check these metrics:**
```promql
# MemPool utilization
neo_mempool_capacity_ratio

# Verified vs unverified
neo_mempool_verified_count
neo_mempool_unverified_count

# Memory usage
neo_mempool_memory_bytes

# Conflict rate
rate(neo_mempool_conflicts_total[5m])
```

**Solutions:**
1. Check if transactions are being processed:
   ```promql
   rate(neo_transactions_processed_total[5m])
   ```

2. Look for verification bottlenecks:
   ```promql
   neo_mempool_unverified_count / neo_mempool_verified_count
   ```

3. Check for transaction conflicts:
   ```promql
   rate(neo_mempool_conflicts_total[5m])
   ```

### 3. Poor Network Connectivity

**Symptoms:**
- `neo_p2p_connected_peers` < 5
- High peer churn rate

**Check these metrics:**
```promql
# Peer stability
rate(neo_p2p_peer_connected_total[5m])
rate(neo_p2p_peer_disconnected_total[5m])

# Network bandwidth
rate(neo_p2p_bytes_sent_total[5m])
rate(neo_p2p_bytes_received_total[5m])

# Unconnected peers
neo_p2p_unconnected_peers
```

**Solutions:**
1. Check network configuration
2. Verify firewall rules (port 10333 for mainnet)
3. Check bandwidth limits
4. Look for network errors in logs

### 4. Slow Block Processing

**Symptoms:**
- High `neo_block_processing_time` percentiles
- Decreasing `neo_block_processing_rate`

**Check these metrics:**
```promql
# Processing time percentiles
histogram_quantile(0.50, rate(neo_block_processing_time_bucket[5m]))
histogram_quantile(0.95, rate(neo_block_processing_time_bucket[5m]))
histogram_quantile(0.99, rate(neo_block_processing_time_bucket[5m]))

# Transaction types causing delays
sum(rate(neo_transactions_processed_total[5m])) by (type)

# Contract invocation patterns
sum(rate(neo_contracts_invocations_total[5m])) by (contract_hash)
```

**Solutions:**
1. Check disk I/O performance
2. Increase CPU resources
3. Check for complex smart contracts
4. Consider SSD storage if using HDD

### 5. High Error Rates

**Check these metrics:**
```promql
# Transaction failures
rate(neo_transaction_verification_failures_total[5m])

# Protocol errors
rate(neo_errors_protocol_total[5m])

# Network errors
rate(neo_errors_network_total[5m])

# Storage errors
rate(neo_errors_storage_total[5m])
```

**Solutions:**
1. Check logs for specific error messages
2. Verify node software version
3. Check disk space
4. Verify database integrity

## Performance Tuning

### Optimal Metric Values

| Metric | Good | Warning | Critical |
|--------|------|---------|----------|
| `neo_p2p_connected_peers` | > 10 | 5-10 | < 5 |
| `neo_block_processing_time` (p95) | < 500ms | 500-1000ms | > 1000ms |
| `neo_mempool_capacity_ratio` | < 0.5 | 0.5-0.8 | > 0.8 |
| `neo_block_processing_rate` | > 1 block/s | 0.5-1 block/s | < 0.5 block/s |

### Query Examples for Analysis

**Transaction throughput analysis:**
```promql
# Transactions per second by type
sum(rate(neo_transactions_processed_total[5m])) by (type)

# Total TPS
sum(rate(neo_transactions_processed_total[5m]))
```

**Network health analysis:**
```promql
# Peer connection stability (lower is better)
rate(neo_p2p_peer_disconnected_total[10m]) / rate(neo_p2p_peer_connected_total[10m])

# Network bandwidth usage
rate(neo_p2p_bytes_sent_total[5m]) + rate(neo_p2p_bytes_received_total[5m])
```

**MemPool analysis:**
```promql
# MemPool turnover rate
rate(neo_mempool_batch_removed_size_sum[5m])

# Average batch removal size
rate(neo_mempool_batch_removed_size_sum[5m]) / rate(neo_mempool_batch_removed_size_count[5m])
```

## Monitoring Best Practices

1. **Set up alerts** based on the Prometheus rules in `prometheus/neo-alerts.yml`

2. **Create dashboards** using the provided Grafana templates

3. **Regular health checks**:
   - Monitor sync status daily
   - Check peer connectivity
   - Review error rates
   - Track resource usage trends

4. **Baseline your metrics**:
   - Record normal operating ranges
   - Document peak usage patterns
   - Note any recurring issues

5. **Correlate metrics**:
   - High block processing time + high mempool = processing bottleneck
   - Low peers + low bandwidth = network issues
   - High errors + storage errors = disk problems

## Emergency Recovery

If node is completely unresponsive:

1. Check if process is running
2. Review system logs for crashes
3. Check disk space
4. Verify database integrity
5. Consider resyncing from backup or scratch

Remember: Metrics are most useful when you have historical data to compare against. Always monitor trends, not just absolute values.