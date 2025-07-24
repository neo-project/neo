# Neo Blockchain Monitoring Setup Guide

This guide provides complete instructions for setting up production-ready monitoring for Neo blockchain nodes using the OpenTelemetry plugin.

## Quick Start

### 1. Enable OpenTelemetry Plugin

Add to your Neo node configuration:

```json
{
  "PluginConfiguration": {
    "OpenTelemetry": {
      "Enabled": true,
      "ServiceName": "neo-mainnet-node",
      "InstanceId": "node-1",
      "Metrics": {
        "Enabled": true,
        "ConsoleExporter": {
          "Enabled": false
        },
        "PrometheusExporter": {
          "Enabled": true,
          "Port": 9184,
          "Path": "/metrics"
        }
      }
    }
  }
}
```

### 2. Deploy Monitoring Stack

```bash
cd src/Plugins/OTelPlugin/docker
./setup.sh
```

This starts:
- **Prometheus**: Metrics collection and storage
- **Grafana**: Visualization dashboards
- **AlertManager**: Alert routing and notifications

### 3. Access Services

- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **AlertManager**: http://localhost:9093

## Architecture

```
┌─────────────┐     ┌─────────────────┐     ┌───────────┐
│  Neo Node   │────►│   Prometheus    │────►│  Grafana  │
│ OTelPlugin  │     │  (Metrics DB)   │     │ (Visuals) │
└─────────────┘     └─────────────────┘     └───────────┘
                             │
                             ▼
                      ┌─────────────┐
                      │AlertManager │
                      │  (Alerts)   │
                      └─────────────┘
```

## Metrics Overview

### Core Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `neo_blockchain_height` | Gauge | Current blockchain height |
| `neo_block_processing_rate` | Gauge | Blocks processed per second |
| `neo_block_processing_time` | Histogram | Time to process blocks (ms) |
| `neo_transactions_processed_total` | Counter | Total transactions by type |
| `neo_p2p_connected_peers` | Gauge | Number of connected peers |
| `neo_mempool_capacity_ratio` | Gauge | MemPool usage (0-1) |

### Complete Metrics List

See [METRICS.md](docs/METRICS.md) for the full metrics reference.

## Dashboards

### Pre-built Dashboards

1. **Node Health Overview**
   - Overall health score
   - Sync status
   - Peer connectivity
   - MemPool utilization

2. **Blockchain Performance**
   - Block processing time (p50, p95, p99)
   - Transaction throughput
   - Contract invocations

3. **Network Analysis**
   - Peer connection stability
   - Bandwidth usage
   - Message type distribution

4. **Error Tracking**
   - Error rates by type
   - Active alerts
   - Failure analysis

### Importing Custom Dashboards

1. Open Grafana (http://localhost:3000)
2. Navigate to Dashboards → Import
3. Upload JSON file from `grafana/` directory
4. Select Prometheus datasource
5. Click Import

## Alerts

### Critical Alerts

- **Node Not Syncing**: No blocks processed for 5 minutes
- **Low Peer Count**: Less than 3 connected peers
- **MemPool Overflow**: Over 90% capacity
- **High Block Processing Time**: p95 > 2 seconds

### Configuring Alerts

1. **Email Alerts**:
   ```yaml
   # Edit docker/alertmanager.yml
   global:
     smtp_smarthost: 'smtp.gmail.com:587'
     smtp_auth_username: 'your-email@gmail.com'
     smtp_auth_password: 'your-app-password'
   ```

2. **Slack Alerts**:
   ```yaml
   receivers:
     - name: 'warning-alerts'
       slack_configs:
         - api_url: 'YOUR_SLACK_WEBHOOK_URL'
           channel: '#neo-alerts'
   ```

3. **PagerDuty Integration**:
   ```yaml
   receivers:
     - name: 'critical-alerts'
       pagerduty_configs:
         - service_key: 'YOUR_PAGERDUTY_KEY'
   ```

## Production Deployment

### 1. Prometheus Configuration

For production, modify `prometheus/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    environment: 'production'
    region: 'us-east-1'

scrape_configs:
  - job_name: 'neo-cluster'
    static_configs:
      - targets:
        - 'neo-node-1.internal:9184'
        - 'neo-node-2.internal:9184'
        - 'neo-node-3.internal:9184'
```

### 2. High Availability Setup

For HA monitoring:

1. **Multiple Prometheus Instances**:
   ```yaml
   # prometheus-1.yml and prometheus-2.yml
   global:
     external_labels:
       replica: '1'  # or '2'
   ```

2. **Thanos for Long-term Storage**:
   ```yaml
   # Add to docker-compose.yml
   thanos-sidecar:
     image: quay.io/thanos/thanos:v0.32.0
     command:
       - sidecar
       - --prometheus.url=http://prometheus:9090
   ```

### 3. Security

1. **Enable Authentication**:
   ```yaml
   # Grafana
   GF_SECURITY_ADMIN_PASSWORD: 'strong-password'
   GF_AUTH_ANONYMOUS_ENABLED: false
   ```

2. **TLS/SSL**:
   - Use reverse proxy (nginx/traefik)
   - Configure TLS certificates
   - Restrict network access

### 4. Resource Requirements

| Component | CPU | Memory | Storage |
|-----------|-----|--------|---------|
| Prometheus | 2 cores | 4GB | 100GB SSD |
| Grafana | 1 core | 2GB | 10GB |
| AlertManager | 0.5 core | 512MB | 1GB |

## Troubleshooting

### 1. No Metrics Appearing

```bash
# Check if metrics endpoint is accessible
curl http://localhost:9184/metrics

# Validate metrics
cd test
./validate-metrics.sh
```

### 2. Prometheus Not Scraping

Check Prometheus targets: http://localhost:9090/targets

Common issues:
- Firewall blocking port 9184
- Wrong target configuration
- Neo node not running

### 3. Alerts Not Firing

1. Check alert rules:
   ```
   http://localhost:9090/alerts
   ```

2. Verify AlertManager config:
   ```bash
   docker-compose exec alertmanager amtool config show
   ```

### 4. Dashboard Shows "No Data"

1. Check datasource configuration
2. Verify time range selection
3. Ensure metrics are being collected

## Maintenance

### Backup

1. **Prometheus Data**:
   ```bash
   docker-compose exec prometheus tar -czf /tmp/prometheus-backup.tar.gz /prometheus
   docker cp neo-prometheus:/tmp/prometheus-backup.tar.gz ./
   ```

2. **Grafana Dashboards**:
   - Export via UI: Settings → Dashboards → Export

### Updates

1. Update Docker images:
   ```bash
   docker-compose pull
   docker-compose up -d
   ```

2. Update alert rules:
   ```bash
   docker-compose restart prometheus
   ```

## Advanced Configuration

### Custom Recording Rules

Add to `prometheus/recording-rules.yml`:

```yaml
- record: neo:custom:metric
  expr: |
    your_complex_query_here
```

### OTLP Export (Alternative to Prometheus)

Configure in Neo node:

```json
{
  "OtlpExporter": {
    "Enabled": true,
    "Endpoint": "http://otel-collector:4317",
    "Protocol": "grpc",
    "ExportMetrics": true
  }
}
```

### Monitoring Multiple Networks

Use labels to distinguish networks:

```yaml
scrape_configs:
  - job_name: 'neo-mainnet'
    static_configs:
      - targets: ['mainnet-node:9184']
        labels:
          network: 'mainnet'
  
  - job_name: 'neo-testnet'
    static_configs:
      - targets: ['testnet-node:9184']
        labels:
          network: 'testnet'
```

## Support

- **Documentation**: See `docs/` directory
- **Metrics Reference**: [METRICS.md](docs/METRICS.md)
- **Troubleshooting Guide**: [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)
- **GitHub Issues**: Report bugs and feature requests

## Best Practices

1. **Monitor the Monitors**: Set up alerts for Prometheus/Grafana health
2. **Regular Reviews**: Check dashboard usage and optimize queries
3. **Capacity Planning**: Monitor storage growth and plan accordingly
4. **Test Alerts**: Regularly test alert routing and notifications
5. **Document Changes**: Keep runbooks updated with any modifications