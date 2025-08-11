# Neo Blockchain SLI/SLO Definitions

## Service Level Indicators (SLIs) and Objectives (SLOs)

### 1. Node Availability
**SLI**: Percentage of time the Neo node is operational and responding
```promql
# SLI Query
(1 - (sum(rate(up{job="neo-node"}[5m]) == 0) / count(up{job="neo-node"}))) * 100
```

**SLO Targets**:
- **99.9%** monthly availability (43.2 minutes downtime allowed)
- **99.95%** monthly for consensus nodes (21.6 minutes downtime allowed)

**Error Budget**: 
- Standard nodes: 43.2 minutes/month
- Consensus nodes: 21.6 minutes/month

---

### 2. Block Processing Latency
**SLI**: 99th percentile block processing time
```promql
# SLI Query
histogram_quantile(0.99, rate(neo_block_processing_time_bucket[5m]))
```

**SLO Targets**:
- **P50 < 100ms** - 50% of blocks processed under 100ms
- **P95 < 500ms** - 95% of blocks processed under 500ms
- **P99 < 2000ms** - 99% of blocks processed under 2s

**Error Budget**:
- 1% of blocks can exceed 2000ms processing time

---

### 3. Blockchain Sync Performance
**SLI**: Time to sync with network when behind
```promql
# SLI Query - Sync rate when behind
rate(neo_blockchain_height[5m]) 
  * on(instance) 
  (neo_blockchain_is_syncing == 1)
```

**SLO Targets**:
- **Sync rate > 10 blocks/second** when catching up
- **Stay within 10 blocks** of network height when synced
- **Resync within 1 hour** after extended downtime

---

### 4. P2P Network Connectivity
**SLI**: Number of healthy peer connections
```promql
# SLI Query
neo_p2p_connected_peers / 
  (neo_p2p_connected_peers + neo_p2p_unconnected_peers) * 100
```

**SLO Targets**:
- **Minimum 5 peers** connected at all times
- **> 20% peer connectivity** rate
- **< 5 minutes** to establish minimum peers after restart

---

### 5. MemPool Performance
**SLI**: MemPool capacity utilization and processing
```promql
# SLI Query - Capacity
neo_mempool_capacity_ratio * 100

# SLI Query - Processing rate
rate(neo_mempool_verified_count[5m])
```

**SLO Targets**:
- **< 80% capacity** utilization sustained
- **> 100 tx/second** verification rate
- **< 10 seconds** average time in mempool

---

### 6. Transaction Processing Success Rate
**SLI**: Percentage of successfully verified transactions
```promql
# SLI Query
(1 - (
  rate(neo_transaction_verification_failures_total[5m]) /
  rate(neo_transactions_processed_total[5m])
)) * 100
```

**SLO Targets**:
- **> 99.5%** transaction verification success rate
- **< 0.5%** verification failure rate

---

### 7. System Resource Utilization
**SLI**: CPU and Memory usage efficiency
```promql
# SLI Query - CPU
process_cpu_usage

# SLI Query - Memory
process_memory_working_set / (8 * 1024 * 1024 * 1024) * 100  # Assuming 8GB target
```

**SLO Targets**:
- **CPU < 70%** sustained usage
- **Memory < 4GB** for standard operations
- **Memory < 8GB** during sync operations

---

### 8. Error Rate
**SLI**: Rate of errors across all components
```promql
# SLI Query
sum(rate(neo_protocol_errors_total[5m])) +
sum(rate(neo_network_errors_total[5m])) +
sum(rate(neo_storage_errors_total[5m]))
```

**SLO Targets**:
- **< 0.1 errors/second** combined error rate
- **< 1% requests** resulting in errors

---

### 9. Consensus Performance (Consensus Nodes Only)
**SLI**: Consensus round completion time
```promql
# SLI Query
neo_consensus_time_to_finality
```

**SLO Targets**:
- **< 15 seconds** consensus finality
- **> 99%** consensus participation when online
- **< 1%** consensus message failures

---

### 10. Storage Performance
**SLI**: Storage operation latency
```promql
# SLI Query - Read latency
histogram_quantile(0.99, rate(neo_storage_read_latency_bucket[5m]))

# SLI Query - Write latency  
histogram_quantile(0.99, rate(neo_storage_write_latency_bucket[5m]))
```

**SLO Targets**:
- **Read P99 < 10ms**
- **Write P99 < 50ms**
- **< 100GB** storage growth per month

---

## Error Budget Policy

### Budget Consumption Triggers
1. **50% consumed**: Review and analysis required
2. **75% consumed**: Freeze on non-critical changes
3. **100% consumed**: All hands on deck, only critical fixes

### Budget Reset
- Monthly reset on the 1st at 00:00 UTC
- Quarterly review of SLO targets

### Exemptions
- Planned maintenance windows (pre-announced)
- Network-wide issues beyond node control
- Force majeure events

---

## Monitoring Implementation

### Dashboard Requirements
Each SLI must have:
1. Real-time gauge/graph
2. Historical trend (30 days)
3. Error budget burn rate
4. Alert status indicator

### Alert Prioritization
- **Page (Critical)**: SLO violation imminent (< 10% budget remaining)
- **Alert (Warning)**: High burn rate (> 2x normal)
- **Notify (Info)**: Abnormal but within bounds

### Recording Rules
```yaml
# Error budget remaining
- record: slo:error_budget_remaining:ratio
  expr: |
    1 - (
      increase(slo_violations_total[30d]) /
      increase(slo_requests_total[30d])
    )

# Burn rate
- record: slo:burn_rate:1h
  expr: |
    rate(slo_violations_total[1h]) * 3600 * 24 * 30 /
    (slo_requests_total * 0.001)  # For 99.9% SLO
```

---

## Reporting

### Weekly Report Contents
1. SLO compliance percentage
2. Error budget consumption
3. Top 3 issues impacting SLOs
4. Remediation actions taken

### Monthly Review
1. SLO achievement vs targets
2. Incident post-mortems
3. SLO target adjustments
4. Infrastructure improvements

### Stakeholder Communication
- **Green**: All SLOs met (> 99% compliance)
- **Yellow**: Minor violations (95-99% compliance)
- **Red**: Major violations (< 95% compliance)

---

## Continuous Improvement

### SLO Evolution Process
1. **Baseline**: Establish current performance
2. **Target**: Set achievable but challenging goals
3. **Monitor**: Track performance continuously
4. **Adjust**: Refine targets based on data
5. **Improve**: Implement changes to meet targets

### Review Cadence
- **Weekly**: Burn rate and trends
- **Monthly**: SLO compliance and adjustments
- **Quarterly**: Strategic review and planning

---

## Tools and Automation

### Required Tools
1. **Prometheus**: Metrics collection and alerting
2. **Grafana**: Visualization and dashboards
3. **PagerDuty/Opsgenie**: Alert routing and escalation
4. **Slack**: Team notifications

### Automation Goals
1. Auto-remediation for common issues
2. Predictive alerting based on trends
3. Capacity planning recommendations
4. Cost optimization suggestions