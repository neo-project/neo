# Neo Blockchain Metrics Reference

This document describes all metrics collected by the OpenTelemetry plugin for Neo blockchain nodes.

## Metrics Categories

### 1. Blockchain Core Metrics

#### Block Processing
- **neo.blocks.processed_total** (Counter)
  - Description: Total number of blocks processed
  - Use Case: Track blockchain growth rate, detect processing stalls
  - Alert: If rate drops to 0 for extended period

- **neo.blockchain.height** (Gauge)
  - Description: Current blockchain height
  - Use Case: Monitor sync status, compare with network height
  - Alert: If significantly behind network height

- **neo.block.processing_time** (Histogram)
  - Description: Time taken to process each block (milliseconds)
  - Use Case: Identify performance degradation, bottlenecks
  - Alert: If p95 > 1000ms or increasing trend

#### Transaction Processing
- **neo.transactions.processed_total** (Counter)
  - Description: Total number of transactions processed
  - Use Case: Monitor transaction throughput
  - Labels: type (transfer, invocation, claim, etc.)

- **neo.contracts.invocations_total** (Counter)
  - Description: Total number of smart contract invocations
  - Use Case: Track smart contract usage patterns
  - Labels: contract_hash, method

### 2. Network P2P Metrics

#### Peer Connections
- **neo.p2p.connected_peers** (Gauge)
  - Description: Number of currently connected peers
  - Use Case: Monitor network connectivity health
  - Alert: If < 3 peers (risk of isolation)

- **neo.p2p.peer_connected_total** (Counter)
  - Description: Total peer connections established
  - Use Case: Track connection churn rate

- **neo.p2p.peer_disconnected_total** (Counter)
  - Description: Total peer disconnections
  - Use Case: Identify network instability
  - Labels: reason (timeout, protocol_error, banned, etc.)

#### Network Traffic
- **neo.p2p.bytes_sent_total** (Counter)
  - Description: Total bytes sent to peers
  - Use Case: Monitor bandwidth usage, detect anomalies

- **neo.p2p.bytes_received_total** (Counter)
  - Description: Total bytes received from peers
  - Use Case: Monitor bandwidth usage, detect DDoS

### 3. MemPool Metrics

#### Pool State
- **neo.mempool.size** (Gauge)
  - Description: Total transactions in mempool
  - Use Case: Monitor mempool congestion
  - Alert: If > 80% of capacity

- **neo.mempool.verified_count** (Gauge)
  - Description: Number of verified transactions
  - Use Case: Track verification performance

- **neo.mempool.unverified_count** (Gauge)
  - Description: Number of unverified transactions
  - Use Case: Identify verification bottlenecks
  - Alert: If ratio to verified > 0.5

- **neo.mempool.memory_bytes** (Gauge)
  - Description: Total memory used by mempool transactions
  - Use Case: Monitor memory pressure
  - Alert: If > 1GB

#### Pool Operations
- **neo.mempool.conflicts_total** (Counter)
  - Description: Total transaction conflicts detected
  - Use Case: Identify double-spend attempts or issues

- **neo.mempool.batch_removed_size** (Histogram)
  - Description: Number of transactions removed in batch operations
  - Use Case: Track mempool overflow events

## Critical Metrics for Node Health

### Must-Have Metrics for Production Monitoring

1. **Sync Status**
   - neo.blockchain.height vs network height
   - neo.blocks.processed_total rate
   - Block processing time percentiles

2. **Network Health**
   - Connected peers count
   - Peer churn rate (connections - disconnections)
   - Network bandwidth usage

3. **Transaction Processing**
   - Transaction processing rate
   - MemPool utilization
   - Contract invocation success rate

4. **Resource Usage**
   - MemPool memory usage
   - Storage read/write rates
   - Cache hit rates

## Recommended Dashboards

### 1. Node Health Overview
- Blockchain height and sync status
- Connected peers
- Transaction processing rate
- MemPool size
- Recent alerts

### 2. Performance Dashboard
- Block processing time (p50, p95, p99)
- Transaction verification time
- Network latency to peers
- Storage operation latency

### 3. Network Analysis
- Peer connections/disconnections over time
- Bandwidth usage patterns
- Message types distribution
- Peer geographic distribution

### 4. MemPool Analysis
- MemPool size trends
- Verified vs unverified ratio
- Conflict detection rate
- Memory usage patterns

## Alert Rules

### Critical Alerts
1. **Node Not Syncing**
   ```
   rate(neo.blockchain.height[5m]) == 0 AND neo.blockchain.height < network_height - 10
   ```

2. **Low Peer Count**
   ```
   neo.p2p.connected_peers < 3
   ```

3. **MemPool Overflow**
   ```
   neo.mempool.size / neo.mempool.capacity > 0.9
   ```

4. **High Block Processing Time**
   ```
   histogram_quantile(0.95, neo.block.processing_time) > 2000
   ```

### Warning Alerts
1. **High Peer Churn**
   ```
   rate(neo.p2p.peer_disconnected_total[5m]) > rate(neo.p2p.peer_connected_total[5m])
   ```

2. **MemPool Congestion**
   ```
   neo.mempool.unverified_count / neo.mempool.verified_count > 0.5
   ```

3. **Increasing Block Processing Time**
   ```
   rate(neo.block.processing_time[10m]) > 0
   ```

## Troubleshooting Guide

### Issue: Node Falls Behind Network
Check:
- neo.blockchain.height vs network
- neo.p2p.connected_peers
- neo.block.processing_time
- System resources (CPU, memory, disk I/O)

### Issue: High MemPool Size
Check:
- neo.mempool.verified_count vs unverified_count
- neo.mempool.conflicts_total rate
- neo.mempool.batch_removed_size
- Network bandwidth metrics

### Issue: Frequent Peer Disconnections
Check:
- neo.p2p.peer_disconnected_total by reason
- Network bandwidth usage
- System network errors
- Firewall/NAT configuration

## Future Metrics Considerations

1. **Consensus Metrics** (for consensus nodes)
   - Consensus round duration
   - View changes count
   - Consensus message latency

2. **Storage Metrics**
   - Storage size growth rate
   - State root calculation time
   - MPT node access patterns

3. **VM Execution Metrics**
   - Gas consumption by contract
   - Execution time by operation
   - Stack depth statistics

4. **Security Metrics**
   - Invalid message count by type
   - Banned peer count
   - Protocol violation attempts