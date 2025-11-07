# Neo OpenTelemetry Monitoring Setup

This directory contains monitoring configurations for the Neo OpenTelemetry plugin.

## Contents

- `neo-dashboard.json` - Grafana dashboard for Neo node monitoring
- `prometheus-alerts.yml` - Prometheus alerting rules
- `docker-compose.yml` - Docker compose file for local monitoring stack

## Quick Start

### 1. Configure the Plugin

Ensure your Neo node has the OpenTelemetry plugin enabled via `Plugins/OTelPlugin/OTelPlugin.json`:

```json
{
  "PluginConfiguration": {
    "Enabled": true,
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
}
```

Set any category to `false` to remove the corresponding panels/alerts from the stack (e.g., disable `Vm` for lightweight nodes).

### 2. Start Monitoring Stack

Use the provided Docker Compose file to start Prometheus and Grafana:

```bash
cd src/Plugins/OTelPlugin/monitoring
docker-compose up -d
```

### 3. Access Services

- **Prometheus**: http://localhost:9091
- **Grafana**: http://localhost:3000 (admin/admin)
- **Neo Metrics**: http://localhost:9090/metrics

### 4. Import Dashboard

1. Open Grafana at http://localhost:3000
2. Go to Dashboards → Import
3. Upload `neo-dashboard.json`
4. Select Prometheus as the data source
5. Click Import

### 5. Configure Alerts

Alerts are automatically loaded when Prometheus starts. To receive notifications:

1. Configure Alertmanager (see `alertmanager.yml.example`)
2. Set up notification channels (email, Slack, PagerDuty, etc.)
3. Restart the monitoring stack

## Dashboard Panels

The primary Grafana dashboard ships with dedicated rows for:

- **Node & System** – Height, sync status, peer counts, CPU, memory, descriptors, disk usage
- **Consensus** – Round/view gauges, message counters, finality latency
- **State Service** – Root height, validation lag, snapshot durations, health ratio
- **RPC** – Active requests, throughput/error rates, p95 latency, optimisation playbook
- **VM Performance** – Instruction rate/latency, stack depth, hot trace ratios, super-instruction plans
- **Mempool & Transactions** – Size breakdown, conflicts, verification failures

Import `neo-dashboard.json` (or the provisioning bundle) to get the full layout.

## Alert Rules

### Critical Alerts
- **NeoNodeDown** – Node is unreachable for 2+ minutes
- **NeoBlockchainNotSyncing** – No new blocks for 10+ minutes
- **NeoNoPeers** – No connected peers for 5+ minutes
- **NeoStorageErrors** – Storage system errors detected

### Warning Alerts
- **NeoLowPeerCount** – Less than 3 connected peers
- **NeoHighMemoryUsage** – Memory usage above 4 GB
- **NeoHighCPUUsage** – CPU usage above 80 %
- **NeoMemPoolFull** – MemPool above 90 % capacity
- **NeoSlowBlockProcessing** – Block processing p95 > 1 s
- **NeoHighTransactionFailureRate** – Transaction failure rate > 10 %
- **NeoStateLagging** – State root lag exceeds 12 blocks
- **NeoStateHealthDegraded** – State snapshot health below 85 %
- **NeoStateValidationStalled** – No state validation progress for 15 min
- **NeoDiskCapacityLow** – Less than 50 GB free on the data volume
- **NeoFileDescriptorsHigh** – Process file descriptor count above 2000
- **NeoRpcErrorRateHigh** – RPC error rate above 5 %
- **NeoRpcLatencyHigh** – RPC p95 latency above 500 ms
- **NeoRpcBacklog** – More than 25 in-flight RPC requests

### Info Alerts
- **NeoNodeRestarted** – Node restarted in the last 5 minutes
- **NeoBlockchainResyncing** – Node is currently syncing
- **NeoPeerChurn** – Significant peer delta within 5 minutes
- **NeoConsensusViewFlapping** – View changes exceeded normal cadence

## Customization

### Modifying Alerts

Edit `prometheus-alerts.yml` and restart Prometheus:

```bash
docker-compose restart prometheus
```

### Adding Custom Metrics

The plugin exposes all metrics at the `/metrics` endpoint. To add custom panels:

1. Explore available metrics in Prometheus
2. Create new panels in Grafana
3. Export and save the dashboard

### Scaling

For production deployments:

1. Use external Prometheus/Grafana instances
2. Configure remote storage for Prometheus
3. Set up high availability with multiple replicas
4. Use Prometheus federation for multi-node monitoring
5. Configure appropriate retention policies

## Troubleshooting

### No Data in Dashboard

1. Check if Neo node is running: `curl http://localhost:9090/metrics`
2. Verify Prometheus can scrape metrics: Check Targets page in Prometheus
3. Ensure correct data source in Grafana

### Alerts Not Firing

1. Check alert rules in Prometheus: Status → Rules
2. Verify alert conditions are met
3. Check Alertmanager configuration

### High Memory Usage

The plugin collects metrics every 10 seconds by default. To reduce overhead:

1. Increase collection interval in plugin config
2. Reduce metric cardinality
3. Adjust Prometheus scrape interval

## Support

For issues or questions:
- Check the [plugin documentation](../README.md)
- Report issues on [GitHub](https://github.com/neo-project/neo)
- Join the Neo Discord community
