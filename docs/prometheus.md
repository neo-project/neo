# Neo Prometheus Monitoring

## Overview

Neo N3 includes built-in support for Prometheus metrics, enabling comprehensive monitoring of node performance, blockchain statistics, and system health. This document explains how to configure and use Prometheus with Neo nodes in both local and Docker environments.

## API Reference

### Metrics Endpoint

Neo exposes a Prometheus-compatible metrics endpoint at `/metrics` on the configured port. The metrics are organized into several categories:

#### Core Blockchain Metrics
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_blockchain_block_height` | Gauge | Current validated block height of the node |
| `neo_blockchain_sync_status` | Gauge | Sync progress from 0 (syncing) to 1 (synced) |
| `neo_blockchain_chain_tip_lag` | Gauge | Blocks behind the network chain tip |

#### Node Performance Metrics
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_node_memory_working_set_bytes` | Gauge | Process working set memory in bytes |
| `neo_node_cpu_seconds_total` | Gauge | Total process CPU time consumed in seconds since process start |
| `neo_node_api_requests_total` | Counter | Total number of RPC/API requests handled (labels: method, status) |
| `neo_node_api_request_duration_seconds` | Histogram | Histogram of RPC/API request duration in seconds (label: method) |

#### Network Metrics
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_network_peers_count` | Gauge | Current number of active P2P connections |
| `neo_network_p2p_messages_received_total` | Counter | Total number of P2P messages received (label: type) |
| `neo_network_p2p_messages_sent_total` | Counter | Total number of P2P messages sent (label: type) |

#### Transaction Pool (Mempool) Metrics
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_mempool_size_transactions` | Gauge | Number of transactions currently in the mempool |
| `neo_mempool_transactions_added_total` | Counter | Total number of transactions successfully added to the mempool |
| `neo_mempool_transactions_rejected_total` | Counter | Total number of transactions rejected from the mempool (label: reason) |

#### Consensus Metrics (dBFT Specific)
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_consensus_current_height` | Gauge | Current block height the consensus service is working on |
| `neo_consensus_current_view` | Gauge | Current view number in the consensus service |
| `neo_consensus_p2p_messages_received_total` | Counter | Total number of consensus messages received (label: type) |
| `neo_consensus_block_generation_duration_seconds` | Histogram | Histogram of time taken to generate a block during consensus |
| `neo_consensus_new_block_persisted_total` | Counter | Total number of new blocks persisted via consensus |

#### Validator Metrics
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_validator_active` | Gauge | Indicates if the node is currently an active consensus validator (1 if active, 0 otherwise) |
| `neo_validator_missed_blocks_total` | Counter | Total number of block proposals missed by this node when it was the primary validator |

#### Execution & Block Processing Metrics
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_transaction_execution_duration_seconds` | Histogram | Histogram of time taken to execute a transaction in the VM |
| `neo_block_processing_duration_seconds` | Histogram | Histogram of time taken to process and persist a block |
| `neo_block_processing_transactions_total` | Gauge | Number of transactions in the last processed block |
| `neo_block_processing_size_bytes` | Gauge | Size of the last processed block in bytes |

#### N3 Economics Metrics
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_block_gas_generated_total` | Gauge | GAS generated in the last processed block (in 10^-8 units) |
| `neo_block_system_fee_total` | Gauge | System fee collected in the last processed block (in 10^-8 units) |
| `neo_block_network_fee_total` | Gauge | Network fee collected in the last processed block (in 10^-8 units) |

#### Security Metrics
| Metric Name | Type | Description |
|-------------|------|-------------|
| `neo_failed_authentication_attempts_total` | Counter | Total number of failed authentication attempts (label: service) |
| `neo_invalid_p2p_message_count_total` | Counter | Total number of invalid P2P messages received (label: reason) |
| `neo_unexpected_shutdowns_total` | Counter | Total number of unexpected node shutdowns detected (label: reason) |

## Configuration

### Local Usage

To enable Prometheus metrics on a local Neo node:

1. **Enable metrics in the Neo node**:

   ```bash
   ./neo-cli --prometheus <listen-address>:<port>
   ```

   Example:
   ```bash
   ./neo-cli --prometheus 127.0.0.1:9101
   ```

2. **Configure Prometheus** to scrape the Neo metrics:

   Create or modify your `prometheus.yml`:

   ```yaml
   scrape_configs:
     - job_name: 'neo'
       scrape_interval: 15s
       static_configs:
         - targets: ['localhost:9101']
   ```

3. **Start Prometheus** with your configuration:

   ```bash
   prometheus --config.file=prometheus.yml
   ```

4. **Access Prometheus UI** at `http://localhost:9090`

### Docker Usage

The Neo repository includes a complete Docker-based monitoring stack in the `/monitoring` directory.

1. **Start the monitoring stack**:

   ```bash
   cd monitoring
   docker-compose up -d
   ```

   This starts:
   - `neo-cli` with Prometheus metrics enabled
   - Prometheus for metrics collection
   - Grafana for visualization
   - Alertmanager for alerts

2. **Access endpoints**:
   - Neo Metrics: http://localhost:9101/metrics
   - Prometheus: http://localhost:9090
   - Grafana: http://localhost:3001 (default credentials: admin/admin)
   - Alertmanager: http://localhost:9093

3. **Custom configuration**:
   - Prometheus: Modify `monitoring/prometheus/prometheus.yml`
   - Alertmanager: Modify `monitoring/docker/alertmanager/alertmanager.yml`
   - Grafana dashboards: Add to `monitoring/grafana/dashboards/`

## Usage Examples

### Querying Metrics in Prometheus

1. Open the Prometheus web interface at http://localhost:9090
2. Use PromQL to query Neo metrics:
   - Blockchain height: `neo_blockchain_block_height`
   - Connected peers over time: `neo_network_peers_count[1h]`
   - Memory pool size: `neo_mempool_size_transactions`
   - CPU usage: `neo_node_cpu_seconds_total`
   - API request duration percentiles: `histogram_quantile(0.95, sum(rate(neo_node_api_request_duration_seconds_bucket[5m])) by (le, method))`

### Creating Custom Alerts

1. Edit `monitoring/prometheus/neo_alerts.yml`:

   ```yaml
   groups:
   - name: neo_alerts
     rules:
     - alert: LowPeerCount
       expr: neo_network_peers_count < 3
       for: 5m
       labels:
         severity: warning
       annotations:
         summary: "Low peer count"
         description: "Node has less than 3 peers for over 5 minutes"
   ```

2. Restart Prometheus to apply changes:
   ```bash
   docker-compose restart prometheus
   ```

## Troubleshooting

- **Cannot access metrics endpoint**: Ensure the node is started with `--prometheus` flag and the port is exposed/mapped correctly if using Docker
- **No data in Prometheus**: Check the Prometheus targets page at http://localhost:9090/targets to verify the Neo endpoint is being scraped
- **Docker container exits immediately**: Add `stdin_open: true` and `tty: true` to the Neo service in `docker-compose.yml`
- **Permission errors in Docker logs**: Ensure your host path mappings are correct and the container has appropriate permissions
