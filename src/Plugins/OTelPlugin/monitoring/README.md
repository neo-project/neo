# Neo OpenTelemetry Monitoring Setup

This directory contains monitoring configurations for the Neo OpenTelemetry plugin.

## Contents

- `neo-dashboard.json` - Grafana dashboard for Neo node monitoring
- `prometheus-alerts.yml` - Prometheus alerting rules
- `docker-compose.yml` - Docker compose file for local monitoring stack

## Quick Start

### 1. Configure the Plugin

Ensure your Neo node has the OTelPlugin enabled with proper configuration in `config.json`:

```json
{
  "PluginConfiguration": {
    "OTelPlugin": {
      "Enabled": true,
      "Metrics": {
        "Enabled": true,
        "Exporters": {
          "Prometheus": {
            "Enabled": true,
            "Port": 9090,
            "Path": "/metrics"
          }
        }
      }
    }
  }
}
```

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

1. Configure Alertmanager (see `alertmanager-config.yml.example`)
2. Set up notification channels (email, Slack, PagerDuty, etc.)
3. Restart the monitoring stack

## Dashboard Panels

The Grafana dashboard includes:

- **Blockchain Height** - Current blockchain height and sync status
- **Connected Peers** - Number of connected P2P peers
- **Block Processing Rate** - Blocks processed per second
- **MemPool Size** - Current mempool status (total, verified, unverified)
- **CPU Usage** - Process and system CPU utilization
- **Memory Usage** - Working set and GC heap size
- **Transaction Processing Rate** - Transactions per second
- **Block Processing Time** - p50, p95, p99 latencies

## Alert Rules

### Critical Alerts
- **NeoNodeDown** - Node is unreachable for 2+ minutes
- **NeoBlockchainNotSyncing** - No new blocks for 10+ minutes
- **NeoNoPeers** - No connected peers for 5+ minutes
- **NeoStorageErrors** - Storage system errors detected

### Warning Alerts
- **NeoLowPeerCount** - Less than 3 connected peers
- **NeoHighMemoryUsage** - Memory usage above 4GB
- **NeoHighCPUUsage** - CPU usage above 80%
- **NeoMemPoolFull** - MemPool above 90% capacity
- **NeoSlowBlockProcessing** - Block processing p95 > 1s
- **NeoHighTransactionFailureRate** - Transaction failure rate > 10%

### Info Alerts
- **NeoNodeRestarted** - Node restarted in last 5 minutes
- **NeoBlockchainResyncing** - Node is syncing blockchain
- **NeoHighNetworkTraffic** - Network traffic > 10MB/s

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