# Neo Node Prometheus Monitoring

## Overview

This document describes the Prometheus monitoring integration for the Neo C# node. It allows operators to collect and visualize key performance and health metrics from their node using the Prometheus time-series database and related tools like Grafana.

The monitoring service exposes metrics via an HTTP endpoint (default: `http://127.0.0.1:9100/metrics`).

## Setup

1.  **Enable Prometheus in Configuration**:
    Modify your node's configuration file (e.g., `config.json`) to enable the Prometheus service and configure the host and port. Add or update the `Prometheus` section:

    ```json
    {
      "Prometheus": {
        "Enabled": true,
        "Host": "0.0.0.0", // Or a specific IP address to bind to
        "Port": 9100      // Default Prometheus port
      }
    }
    ```
    *   Set `Enabled` to `true`.
    *   Set `Host` to `0.0.0.0` to listen on all network interfaces or `127.0.0.1` for localhost only.
    *   Set `Port` to the desired port for the metrics endpoint.

2.  **Configure Prometheus Server**:
    Configure your Prometheus server (`prometheus.yml`) to scrape metrics from the Neo node's endpoint. Add a scrape job like this:

    ```yaml
    scrape_configs:
      - job_name: 'neo-node'
        static_configs:
          - targets: ['YOUR_NODE_IP:9100'] # Replace with the actual IP and port
    ```

3.  **Restart Neo Node**:
    Restart your Neo node for the configuration changes to take effect.

4.  **Verify**:
    Check your Prometheus server's web UI under "Status" -> "Targets" to ensure it's successfully scraping the `neo-node` job. You should also be able to query the metrics (e.g., `neo_node_block_height`) in the Prometheus expression browser.

## Available Metrics

The following metrics are collected by the `PrometheusService`:

*(Note: This list reflects the current implementation in `src/Neo/Monitoring/PrometheusService.cs`. System metrics like CPU/Memory might be added depending on compatibility.)*

### P2P Network

*Importance: Monitors the node's connectivity to the Neo network. Low peer count can hinder synchronization and transaction relay. High message rates might indicate heavy network load or potential issues.*

*   `neo_p2p_connections_total` (Gauge): Current number of active P2P connections. (Troubleshooting: If consistently low, check firewall rules, network configuration, and node reachability.)
*   `neo_p2p_messages_received_total` (Counter): Total number of P2P messages received.
    *   Labels: `type` (Message type, e.g., 'Version', 'Block')
*   `neo_p2p_messages_sent_total` (Counter): Total number of P2P messages sent.
    *   Labels: `type` (Message type)

### Mempool

*Importance: Tracks the state of unconfirmed transactions. A rapidly growing mempool size or high rejection rate can signal network congestion, policy misconfiguration, or issues processing transactions.*

*   `neo_mempool_transactions_total` (Gauge): Number of transactions currently in the mempool.
*   `neo_mempool_size_bytes` (Gauge): Total size of transactions currently in the mempool.
*   `neo_mempool_transactions_added_total` (Counter): Total number of transactions successfully added to the mempool.
*   `neo_mempool_transactions_rejected_total` (Counter): Total number of transactions rejected from the mempool.
    *   Labels: `reason` (e.g., 'InsufficientFunds', 'PolicyFail', 'Expired', 'Invalid') (Troubleshooting: High rejection rates for specific reasons might indicate client-side issues, fee problems, or policy conflicts.)

### RPC

*Importance: Monitors the performance and load on the node's RPC interface, crucial for dApps and wallets interacting with the node.*

*   `neo_rpc_requests_total` (Counter): Total number of RPC requests handled.
    *   Labels: `method` (RPC method name), `status` ('success' or 'error') (Troubleshooting: High error rates for specific methods warrant investigation.)
*   `neo_rpc_request_duration_seconds` (Histogram): Histogram of RPC request duration in seconds.
    *   Labels: `method` (RPC method name)
    *   Buckets: Exponential (0.001s to ~32s) (Troubleshooting: High latency, especially p95/p99, suggests performance bottlenecks, potentially CPU, disk I/O, or complex contract calls.)

### Node State

*Importance: Basic indicator of node synchronization status.*

*   `neo_node_block_height` (Gauge): Current validated block height of the node. (Troubleshooting: Compare with network explorers or trusted peers to detect sync issues.)

### Process Metrics (Manually Collected)

*Importance: Provides insight into the resource consumption of the Neo node process itself.*

*   `neo_process_working_set_bytes` (Gauge): Process working set memory usage in bytes (updated periodically). (Troubleshooting: Continuously increasing memory usage may indicate a memory leak. High usage could lead to performance degradation or OOM errors.)
*   `neo_process_cpu_seconds_total` (Gauge): Total process CPU time consumed in seconds since process start (updated periodically). (Troubleshooting: High CPU usage (derived using `rate()`) can indicate heavy load, inefficient operations, or processing bottlenecks.)

### Consensus

*Importance: Monitors the health and progress of the dBFT consensus mechanism.*

*   `neo_consensus_height` (Gauge): Current block height the consensus service is working on. (Should closely track `neo_node_block_height`.)
*   `neo_consensus_view` (Gauge): Current view number in the consensus service. (Troubleshooting: Persistently high or frequently changing views indicate consensus instability, often due to network issues, faulty nodes, or insufficient participation.)
*   `neo_consensus_messages_received_total` (Counter): Total number of consensus messages received.
    *   Labels: `type` (Consensus message type, e.g., 'PrepareRequest', 'Commit')
*   `neo_consensus_block_generation_duration_seconds` (Histogram): Histogram of time taken to generate a block during consensus.
    *   Buckets: Exponential (0.1s to ~51s) (Troubleshooting: High duration suggests potential delays in reaching consensus.)
*   `neo_consensus_new_block_persisted_total` (Counter): Total number of new blocks persisted via consensus.

### Execution / Block Processing

*Importance: Measures the performance of transaction execution within the NeoVM and the overall time taken to process and commit blocks.*

*   `neo_transaction_execution_duration_seconds` (Histogram): Histogram of time taken to execute a transaction in the VM.
    *   Buckets: Exponential (0.001s to ~32s) (Troubleshooting: High execution times might point to complex or inefficient smart contracts.)
*   `neo_block_processing_duration_seconds` (Histogram): Histogram of time taken to process and persist a block (verification, commit, events).
    *   Buckets: Exponential (0.01s to ~327s) (Troubleshooting: High block processing time can delay node synchronization and indicate performance bottlenecks in storage, VM execution, or event processing.)
*   `neo_block_processing_transactions_total` (Gauge): Number of transactions in the last processed block.
*   `neo_block_processing_size_bytes` (Gauge): Size of the last processed block in bytes.
*   `neo_block_gas_generated_total` (Gauge): Total GAS generated in the last processed block (in 10^-8 units, i.e., the integer value).
*   `neo_block_system_fee_total` (Gauge): Total system fee collected in the last processed block (in 10^-8 GAS units).
*   `neo_block_network_fee_total` (Gauge): Total network fee collected in the last processed block (in 10^-8 GAS units).

## Usage Examples

Here are some example PromQL queries you can use in Prometheus or Grafana to monitor your Neo node:

**1. RPC Request Rate (per second, averaged over 5m):**
```promql
sum(rate(neo_rpc_requests_total[5m])) by (method)
```
*This shows the average number of requests per second for each RPC method over the last 5 minutes.*

**2. RPC Request Error Rate (percentage, averaged over 5m):**
```promql
(sum(rate(neo_rpc_requests_total{status="error"}[5m])) by (method)
/
sum(rate(neo_rpc_requests_total[5m])) by (method)) * 100
```
*Calculates the percentage of RPC requests that resulted in an error for each method.*

**3. 95th Percentile RPC Request Duration (over 10m):**
```promql
histogram_quantile(0.95, sum(rate(neo_rpc_request_duration_seconds_bucket[10m])) by (le, method))
```
*Shows the RPC duration (in seconds) below which 95% of requests completed, calculated over the last 10 minutes, broken down by method.*

**4. Mempool Transaction Rate (added vs rejected, per second, over 5m):**
```promql
sum(rate(neo_mempool_transactions_added_total[5m]))

sum(rate(neo_mempool_transactions_rejected_total[5m])) by (reason)
```
*Shows the rate at which transactions are being added to the mempool and the rate at which they are being rejected (per reason).*

**5. Block Processing Time (99th Percentile, over 10m):**
```promql
histogram_quantile(0.99, sum(rate(neo_block_processing_duration_seconds_bucket[10m])) by (le))
```
*Shows the block processing duration (in seconds) below which 99% of blocks were processed.*

**6. Average Transactions per Block (over last 10 blocks):**
```promql
avg_over_time(neo_block_processing_transactions_total[10m])
# Note: This assumes blocks are processed roughly within the 10m window.
# A more accurate but complex query might involve changes() or block height differences.
```
*Provides an estimate of the average number of transactions per block based on the gauge's value over time.*

**7. Process Memory Usage (Working Set):**
```promql
neo_process_working_set_bytes{job="neo-node"}
```
*Shows the current working set memory usage in bytes for the specific node instance.*

**8. Process CPU Usage (Rate, per second, over 5m):**
```promql
rate(neo_process_cpu_seconds_total{job="neo-node"}[5m])
```
*Calculates the rate of CPU time consumption per second, effectively showing CPU usage proportion (e.g., 0.5 means 50% of one core was used on average over the last 5 minutes).*

**9. GAS Generation Rate (per second, over 5m):**
```promql
rate(neo_block_gas_generated_total[5m]) / 100000000
```
*Calculates the approximate rate of GAS generation in full GAS units per second, averaged over 5 minutes. Useful for observing the overall network inflation rate contribution from this node.*

**10. Average System Fee per Block (over 10m):**
```promql
avg_over_time(neo_block_system_fee_total[10m]) / 100000000
```
*Calculates the average system fee (in full GAS units) collected per block over the last 10 minutes.*

**11. Average Network Fee per Block (over 10m):**
```promql
avg_over_time(neo_block_network_fee_total[10m]) / 100000000
```
*Calculates the average network fee (in full GAS units) collected per block over the last 10 minutes. This can indicate network congestion or transaction priority levels.*

**Alerting Example: Low GAS Generation**

```yaml
# Example Alert Rule (prometheus_rules.yml)
# (Assumes blocks are generated somewhat regularly)
groups:
- name: NeoNodeEconomics
  rules:
  - alert: NeoNodeLowGasGeneration
    # Alert if the average GAS generated per block over 10 minutes is below a threshold (e.g., 4 GAS)
    # Adjust the threshold based on expected generation patterns
    expr: avg_over_time(neo_block_gas_generated_total[10m]) < 400000000 
    for: 15m # Alert only if condition persists
    labels:
      severity: warning
    annotations:
      summary: "Neo node {{ $labels.instance }} shows low GAS generation"
      description: "Average GAS generated per block is {{ $value | printf \"%.2f\" }} (expected > 4 GAS). Might indicate issues with block production or transaction processing."
```
*Triggers if the average GAS generated seems unusually low, potentially indicating problems with block finalization or consensus participation.*

**Alerting Example: High Consensus View Changes**

```yaml
# Example Alert Rule (prometheus_rules.yml)
groups:
- name: NeoNodeConsensus
  rules:
  - alert: NeoNodeHighConsensusView
    # Alert if the consensus view number reaches 3 or higher
    expr: neo_consensus_view{job="neo-node"} >= 3
    for: 2m # Alert if view remains high for 2 minutes
    labels:
      severity: critical
    annotations:
      summary: "Neo node {{ $labels.instance }} has high consensus view"
      description: "Node consensus view is {{ $value }} (>= 3). This indicates potential consensus instability or network issues."
```
*Triggers if the consensus protocol requires multiple view changes to agree on a block, signaling potential network latency, partitions, or issues with consensus nodes.*

**Alerting Example: Node Block Height Falling Behind**

*This requires having metrics from an external source (e.g., another trusted node or an explorer) available in your Prometheus setup, scraped under a different job, let's call it `external-monitor`.*

```yaml
# Example Alert Rule (prometheus_rules.yml)
groups:
- name: NeoNodeHealth
  rules:
  - alert: NeoNodeFallingBehind
    expr: (
        max(neo_node_block_height{job="external-monitor"}) - 
        max(neo_node_block_height{job="neo-node"})
      ) > 5 # Alert if the difference is more than 5 blocks
    for: 5m # Alert only if condition persists for 5 minutes
    labels:
      severity: warning
    annotations:
      summary: "Neo node {{ $labels.instance }} is falling behind"
      description: "Node block height {{ $value }} blocks behind external source."
```
*This rule triggers an alert if the monitored Neo node's block height is more than 5 blocks lower than the height reported by the `external-monitor` job for at least 5 minutes.*

*(Further examples can be added for consensus metrics, P2P connections, etc.)* 