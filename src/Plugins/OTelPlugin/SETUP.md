# OpenTelemetry Setup Guide for Neo Node

This guide will help you set up a complete observability stack for your Neo node using OpenTelemetry.

## Quick Start

### 1. Configure the Plugin

Edit `OTelPlugin.json` to configure your exporters:

```json
{
  "PluginConfiguration": {
    "Enabled": true,
    "ServiceName": "neo-node",
    "OtlpExporter": {
      "Enabled": true,
      "Endpoint": "http://localhost:4317"
    }
  }
}
```

### 2. Start the Observability Stack

Use the provided docker-compose file to start all services:

```bash
cd src/Plugins/OTelPlugin
docker-compose up -d
```

This will start:
- **OpenTelemetry Collector** - Receives and processes telemetry data
- **Prometheus** - Stores metrics (http://localhost:9091)
- **Jaeger** - Stores and visualizes traces (http://localhost:16686)
- **Grafana** - Visualizes metrics (http://localhost:3000)

### 3. Start Your Neo Node

Start your Neo node with the OTelPlugin enabled. The plugin will automatically start sending telemetry data.

## Configuration Options

### Console Export (Development)

For development, enable console exporters to see telemetry data in your terminal:

```json
{
  "Metrics": {
    "ConsoleExporter": {
      "Enabled": true
    }
  },
  "Tracing": {
    "ConsoleExporter": {
      "Enabled": true
    }
  }
}
```

### Prometheus Scraping

To use Prometheus scraping instead of push:

```json
{
  "Metrics": {
    "PrometheusExporter": {
      "Enabled": true,
      "Port": 9090,
      "Path": "/metrics"
    }
  }
}
```

Then configure Prometheus to scrape your Neo node.

### OTLP Export

For production, use OTLP to send data to your observability backend:

```json
{
  "OtlpExporter": {
    "Enabled": true,
    "Endpoint": "https://your-collector.example.com:4317",
    "Headers": "api-key=your-api-key",
    "Protocol": "grpc"
  }
}
```

## Viewing Data

### Metrics in Grafana

1. Access Grafana at http://localhost:3000 (admin/admin)
2. Add Prometheus data source: http://prometheus:9090
3. Import the provided dashboard from `grafana-dashboards/neo-blockchain-dashboard.json`

### Traces in Jaeger

1. Access Jaeger UI at http://localhost:16686
2. Select "neo-node" service
3. View transaction and block processing traces

## Monitoring Recommendations

### Key Metrics to Watch

1. **Blockchain Health**
   - `neo_blockchain_height` - Current block height
   - `neo_blockchain_block_processing_time` - Block processing latency
   - `neo_blockchain_blocks_processed_total` - Total blocks processed

2. **Network Health**
   - `neo_p2p_connected_peers` - Number of connected peers
   - `neo_p2p_messages_received_total` - Network activity

3. **Performance**
   - `neo_mempool_size` - Memory pool size
   - `neo_contracts_execution_time` - Smart contract performance
   - `neo_contracts_gas_consumed_total` - Gas consumption rate

### Setting Up Alerts

Example Prometheus alert rules:

```yaml
groups:
  - name: neo_alerts
    rules:
      - alert: LowPeerCount
        expr: neo_p2p_connected_peers < 3
        for: 5m
        annotations:
          summary: "Low peer count on {{ $labels.instance }}"
          
      - alert: HighMempool
        expr: neo_mempool_size > 1000
        for: 10m
        annotations:
          summary: "High mempool size on {{ $labels.instance }}"
```

## Troubleshooting

### No Data in Grafana

1. Check if Neo node is running
2. Verify OTelPlugin is enabled in configuration
3. Check OpenTelemetry Collector logs: `docker-compose logs otel-collector`
4. Verify Prometheus is scraping: http://localhost:9091/targets

### Connection Errors

1. Ensure all services are running: `docker-compose ps`
2. Check firewall settings
3. Verify endpoint URLs in configuration

### Performance Impact

The telemetry overhead is minimal, but you can reduce it by:
- Lowering sampling ratio for traces
- Increasing metric collection interval
- Disabling unused exporters

## Production Deployment

For production environments:

1. Use persistent volumes for Prometheus and Grafana
2. Configure authentication for all services
3. Use TLS for OTLP connections
4. Set appropriate retention policies
5. Configure backup strategies

## Integration with Cloud Providers

### AWS CloudWatch

Use AWS Distro for OpenTelemetry:
```json
{
  "OtlpExporter": {
    "Endpoint": "https://otel-collector.region.amazonaws.com:4317"
  }
}
```

### Azure Monitor

Configure Application Insights connection string:
```json
{
  "OtlpExporter": {
    "Headers": "Authorization=InstrumentationKey=your-key"
  }
}
```

### Google Cloud

Use Google Cloud Operations:
```json
{
  "OtlpExporter": {
    "Endpoint": "https://opentelemetry.googleapis.com:443"
  }
}
```