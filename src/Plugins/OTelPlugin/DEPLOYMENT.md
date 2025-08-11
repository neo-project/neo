# Neo OpenTelemetry Plugin Deployment Guide

This guide provides step-by-step instructions for deploying the Neo OpenTelemetry plugin in production environments.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Startup Verification](#startup-verification)
- [Monitoring Stack Setup](#monitoring-stack-setup)
- [Troubleshooting](#troubleshooting)
- [Production Checklist](#production-checklist)

## Prerequisites

### System Requirements
- **Operating System**: Linux (Ubuntu 20.04+ recommended), Windows Server 2019+, or macOS
- **.NET Runtime**: .NET 9.0 or later
- **Memory**: Minimum 4GB RAM (8GB+ recommended for production)
- **Disk Space**: 100GB+ for blockchain data
- **Network**: Open ports for P2P (20333), RPC (20331), and metrics (9090)

### Software Dependencies
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y wget curl git unzip

# Install .NET 9.0
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0
export PATH=$PATH:$HOME/.dotnet
```

## Installation

### 1. Build the Plugin

```bash
# Clone the Neo repository
git clone https://github.com/neo-project/neo.git
cd neo/src/Plugins/OTelPlugin

# Build the plugin
dotnet build -c Release
```

### 2. Install Neo Node

```bash
# Download Neo CLI
wget https://github.com/neo-project/neo-cli/releases/latest/download/neo-cli-linux-x64.zip
unzip neo-cli-linux-x64.zip -d /opt/neo-cli

# Create plugins directory
mkdir -p /opt/neo-cli/Plugins
```

### 3. Deploy the Plugin

```bash
# Copy plugin files to Neo CLI
cp -r ./bin/Release/net9.0/* /opt/neo-cli/Plugins/OTelPlugin/
cp OTelPlugin.json /opt/neo-cli/Plugins/OTelPlugin/

# Verify installation
ls -la /opt/neo-cli/Plugins/OTelPlugin/
```

## Configuration

### 1. Basic Configuration

Edit `/opt/neo-cli/Plugins/OTelPlugin/OTelPlugin.json`:

```json
{
  "PluginConfiguration": {
    "Enabled": true,
    "ServiceName": "neo-mainnet-node-01",
    "ServiceVersion": "3.8.1",
    "InstanceId": "prod-us-east-1",
    "UnhandledExceptionPolicy": "StopPlugin",
    "Metrics": {
      "Enabled": true,
      "Interval": 10000,
      "PrometheusExporter": {
        "Enabled": true,
        "Port": 9090,
        "Path": "/metrics"
      },
      "ConsoleExporter": {
        "Enabled": false
      }
    },
    "OtlpExporter": {
      "Enabled": false,
      "Endpoint": "http://localhost:4317",
      "Protocol": "grpc",
      "ExportMetrics": true
    },
    "ResourceAttributes": {
      "deployment.environment": "production",
      "service.namespace": "blockchain",
      "cloud.provider": "aws",
      "cloud.region": "us-east-1"
    }
  }
}
```

### 2. Environment-Specific Settings

#### Production
```json
{
  "Metrics": {
    "Interval": 30000,
    "PrometheusExporter": {
      "Port": 9090
    }
  }
}
```

#### Development
```json
{
  "Metrics": {
    "Interval": 5000,
    "ConsoleExporter": {
      "Enabled": true
    }
  }
}
```

## Startup Verification

### 1. Start Neo Node

```bash
cd /opt/neo-cli
nohup dotnet neo-cli.dll > neo.log 2>&1 &

# Wait for node to start (30 seconds)
sleep 30

# Check if process is running
ps aux | grep neo-cli
```

### 2. Verify Plugin Loading

```bash
# Check Neo logs for plugin initialization
grep "OpenTelemetry" neo.log

# Expected output:
# [INFO] OpenTelemetry plugin initialized successfully
# [INFO] OpenTelemetry: Prometheus exporter enabled on port 9090/metrics
```

### 3. Test Metrics Endpoint

```bash
# Test Prometheus endpoint
curl -s http://localhost:9090/metrics | head -20

# Verify Neo metrics are present
curl -s http://localhost:9090/metrics | grep "neo_blockchain_height"

# Expected output:
# neo_blockchain_height 12345678
```

### 4. Use Console Commands

```bash
# Connect to Neo CLI
dotnet neo-cli.dll

# In Neo console:
neo> telemetry status

# Expected output:
# OpenTelemetry Status:
#   Enabled: True
#   Service: neo-mainnet-node-01 v3.8.1
#   Current Block Height: 12345678
#   MemPool Size: 42
#   Connected Peers: 10
#   Metrics:
#     - Blocks Processed Counter: Active
#     - Transactions Processed Counter: Active
#   Exporters:
#     - Prometheus Exporter: Active on port 9090
```

## Monitoring Stack Setup

### 1. Prometheus Configuration

Create `/etc/prometheus/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

alerting:
  alertmanagers:
    - static_configs:
        - targets: ['localhost:9093']

rule_files:
  - "alerting-rules.yml"

scrape_configs:
  - job_name: 'neo-node'
    static_configs:
      - targets: ['localhost:9090']
        labels:
          instance: 'neo-mainnet-01'
          environment: 'production'
```

### 2. Deploy Monitoring Stack with Docker Compose

Create `docker-compose.monitoring.yml`:

```yaml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - ./prometheus/alerting-rules.yml:/etc/prometheus/alerting-rules.yml
      - prometheus_data:/prometheus
    ports:
      - "9091:9090"
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.retention.time=30d'
    restart: unless-stopped

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    volumes:
      - ./grafana-dashboards:/var/lib/grafana/dashboards
      - grafana_data:/var/lib/grafana
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_USERS_ALLOW_SIGN_UP=false
    ports:
      - "3000:3000"
    restart: unless-stopped

  alertmanager:
    image: prom/alertmanager:latest
    container_name: alertmanager
    volumes:
      - ./alertmanager.yml:/etc/alertmanager/alertmanager.yml
      - alertmanager_data:/alertmanager
    ports:
      - "9093:9093"
    restart: unless-stopped

volumes:
  prometheus_data:
  grafana_data:
  alertmanager_data:
```

Start the monitoring stack:

```bash
docker-compose -f docker-compose.monitoring.yml up -d
```

### 3. Import Grafana Dashboards

1. Access Grafana at http://localhost:3000 (admin/admin)
2. Add Prometheus data source (http://prometheus:9090)
3. Import dashboards from `grafana-dashboards/` directory

## Troubleshooting

### Plugin Not Loading

```bash
# Check Neo logs
tail -f neo.log | grep -i error

# Common issues:
# 1. Missing dependencies
dotnet --list-runtimes  # Verify .NET 9.0 is installed

# 2. Invalid configuration
python3 -m json.tool /opt/neo-cli/Plugins/OTelPlugin/OTelPlugin.json

# 3. Permission issues
ls -la /opt/neo-cli/Plugins/OTelPlugin/
chmod -R 755 /opt/neo-cli/Plugins/OTelPlugin/
```

### No Metrics Available

```bash
# Check if port is listening
netstat -tlnp | grep 9090

# Test local connectivity
curl -v http://localhost:9090/metrics

# Check firewall rules
sudo ufw status
sudo ufw allow 9090/tcp  # If needed
```

### High Memory Usage

```bash
# Monitor memory usage
top -p $(pgrep -f neo-cli)

# Adjust configuration
# Reduce metric collection frequency in OTelPlugin.json
# "Interval": 60000  # Increase to 60 seconds
```

### Metrics Not Updating

```bash
# Check blockchain sync status
curl -s http://localhost:9090/metrics | grep neo_blockchain_is_syncing

# Verify events are being processed
tail -f neo.log | grep -i "block"
```

## Production Checklist

### Pre-Deployment
- [ ] Build plugin in Release mode
- [ ] Test on staging environment
- [ ] Backup existing Neo node configuration
- [ ] Document rollback procedure
- [ ] Configure monitoring alerts

### Deployment
- [ ] Copy plugin files to production
- [ ] Update configuration for production
- [ ] Start Neo node with plugin
- [ ] Verify metrics endpoint is accessible
- [ ] Check telemetry status command
- [ ] Confirm metrics in Prometheus
- [ ] Verify Grafana dashboards

### Post-Deployment
- [ ] Monitor memory and CPU usage
- [ ] Check error rates in metrics
- [ ] Verify alert notifications work
- [ ] Document any issues encountered
- [ ] Update runbooks if needed

### Security
- [ ] Restrict Prometheus port access
- [ ] Use HTTPS for OTLP endpoints
- [ ] Rotate any API keys
- [ ] Enable authentication on Grafana
- [ ] Review firewall rules

### Maintenance
- [ ] Schedule regular metric data cleanup
- [ ] Plan for Prometheus storage growth
- [ ] Document backup procedures
- [ ] Create metric retention policies
- [ ] Test disaster recovery

## Support

### Getting Help
- Check plugin status: `telemetry status`
- Review logs: `grep OpenTelemetry neo.log`
- Run verification script: `./verify-installation.sh`

### Common Metrics Queries

```promql
# Block processing rate
rate(neo_blocks_processed_total[5m])

# Transaction throughput
rate(neo_transactions_processed_total[5m])

# Average block processing time
rate(neo_block_processing_time_sum[5m]) / rate(neo_block_processing_time_count[5m])

# Memory usage
process_memory_working_set / 1024 / 1024 / 1024  # GB

# Peer connectivity
neo_p2p_connected_peers
```

### Performance Tuning

1. **Metric Collection Interval**: Adjust based on requirements
   - Production: 30-60 seconds
   - Development: 5-10 seconds

2. **Resource Limits**: Monitor and adjust
   - Memory: Set JVM heap size if needed
   - CPU: Use CPU affinity for dedicated cores

3. **Export Optimization**:
   - Use batch exports for OTLP
   - Increase timeout for slow networks
   - Consider local Prometheus for aggregation

## Conclusion

The Neo OpenTelemetry plugin is now deployed and operational. Monitor the dashboards, review alerts, and adjust configuration as needed for your specific environment.