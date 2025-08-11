# Neo OpenTelemetry Monitoring Deployment Guide

## Complete Monitoring Stack Setup

### Prerequisites
- Docker & Docker Compose installed
- Neo node with OpenTelemetry plugin enabled
- 8GB RAM minimum for monitoring stack
- 50GB disk space for metrics retention

## 1. Deploy Prometheus

### prometheus.yml Configuration
```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    monitor: 'neo-blockchain'
    environment: 'production'

# Alertmanager configuration
alerting:
  alertmanagers:
    - static_configs:
        - targets:
            - alertmanager:9093

# Load alerting rules
rule_files:
  - "/etc/prometheus/rules/*.yml"

# Scrape configurations
scrape_configs:
  - job_name: 'neo-node'
    static_configs:
      - targets: ['neo-node1:9090', 'neo-node2:9090', 'neo-node3:9090']
        labels:
          node_type: 'validator'
      - targets: ['neo-rpc1:9090', 'neo-rpc2:9090']
        labels:
          node_type: 'rpc'
    metric_relabel_configs:
      # Add node_type label to all metrics
      - source_labels: [__address__]
        target_label: instance
      - source_labels: [node_type]
        target_label: node_type
```

### Deploy with Docker Compose
```yaml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - ./alerting-rules.yml:/etc/prometheus/rules/alerting-rules.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--storage.tsdb.retention.time=30d'
      - '--web.enable-lifecycle'
      - '--web.enable-admin-api'
    ports:
      - "9090:9090"
    restart: unless-stopped

volumes:
  prometheus_data:
```

## 2. Deploy Grafana

### Docker Compose for Grafana
```yaml
  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana-dashboards:/etc/grafana/provisioning/dashboards
      - ./datasources.yml:/etc/grafana/provisioning/datasources/datasources.yml
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=changeme
      - GF_INSTALL_PLUGINS=grafana-piechart-panel,grafana-worldmap-panel
      - GF_SERVER_ROOT_URL=https://grafana.neo.example.com
    ports:
      - "3000:3000"
    restart: unless-stopped
    depends_on:
      - prometheus
```

### datasources.yml
```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    jsonData:
      httpMethod: POST
      exemplarTraceIdDestinations:
        - datasourceUid: tempo
          name: trace_id
    isDefault: true
    editable: true
```

### Dashboard Provisioning
```yaml
apiVersion: 1

providers:
  - name: 'Neo Dashboards'
    orgId: 1
    folder: 'Neo Blockchain'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /etc/grafana/provisioning/dashboards
```

## 3. Deploy Alertmanager

### alertmanager.yml Configuration
```yaml
global:
  resolve_timeout: 5m
  slack_api_url: 'YOUR_SLACK_WEBHOOK_URL'
  pagerduty_url: 'https://events.pagerduty.com/v2/enqueue'

route:
  group_by: ['alertname', 'cluster', 'service']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  receiver: 'default'
  
  routes:
    - match:
        severity: critical
      receiver: pagerduty
      continue: true
      
    - match:
        severity: warning
      receiver: slack
      repeat_interval: 4h

receivers:
  - name: 'default'
    slack_configs:
      - channel: '#neo-alerts'
        title: 'Neo Alert'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'
        
  - name: 'pagerduty'
    pagerduty_configs:
      - service_key: 'YOUR_PAGERDUTY_SERVICE_KEY'
        description: '{{ .GroupLabels.alertname }}'
        
  - name: 'slack'
    slack_configs:
      - channel: '#neo-warnings'
        send_resolved: true
        title: 'Neo Warning'
        text: '{{ .CommonAnnotations.description }}'
        actions:
          - type: button
            text: 'Runbook'
            url: '{{ .CommonAnnotations.runbook_url }}'
          - type: button
            text: 'Dashboard'
            url: '{{ .CommonAnnotations.dashboard_url }}'

inhibit_rules:
  - source_match:
      severity: 'critical'
    target_match:
      severity: 'warning'
    equal: ['alertname', 'instance']
```

### Docker Compose for Alertmanager
```yaml
  alertmanager:
    image: prom/alertmanager:latest
    container_name: alertmanager
    volumes:
      - ./alertmanager.yml:/etc/alertmanager/alertmanager.yml
      - alertmanager_data:/alertmanager
    command:
      - '--config.file=/etc/alertmanager/alertmanager.yml'
      - '--storage.path=/alertmanager'
      - '--web.external-url=https://alertmanager.neo.example.com'
    ports:
      - "9093:9093"
    restart: unless-stopped
```

## 4. Complete Docker Compose Stack

```yaml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
      - ./prometheus/alerting-rules.yml:/etc/prometheus/rules/alerting-rules.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--storage.tsdb.retention.time=30d'
      - '--web.enable-lifecycle'
    ports:
      - "9090:9090"
    networks:
      - monitoring
    restart: unless-stopped

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./grafana/datasources.yml:/etc/grafana/provisioning/datasources/datasources.yml
    environment:
      - GF_SECURITY_ADMIN_USER=${GRAFANA_USER:-admin}
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD:-admin}
      - GF_INSTALL_PLUGINS=grafana-piechart-panel,grafana-worldmap-panel
    ports:
      - "3000:3000"
    networks:
      - monitoring
    restart: unless-stopped
    depends_on:
      - prometheus

  alertmanager:
    image: prom/alertmanager:latest
    container_name: alertmanager
    volumes:
      - ./alertmanager/alertmanager.yml:/etc/alertmanager/alertmanager.yml
      - alertmanager_data:/alertmanager
    command:
      - '--config.file=/etc/alertmanager/alertmanager.yml'
      - '--storage.path=/alertmanager'
    ports:
      - "9093:9093"
    networks:
      - monitoring
    restart: unless-stopped

  node-exporter:
    image: prom/node-exporter:latest
    container_name: node-exporter
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.rootfs=/rootfs'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'
    ports:
      - "9100:9100"
    networks:
      - monitoring
    restart: unless-stopped

volumes:
  prometheus_data:
  grafana_data:
  alertmanager_data:

networks:
  monitoring:
    driver: bridge
```

## 5. Deploy the Stack

```bash
# Create directories
mkdir -p monitoring/{prometheus,grafana/dashboards,alertmanager}

# Copy configuration files
cp prometheus.yml monitoring/prometheus/
cp alerting-rules.yml monitoring/prometheus/
cp neo-*.json monitoring/grafana/dashboards/
cp alertmanager.yml monitoring/alertmanager/

# Start the stack
cd monitoring
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f
```

## 6. Configure Neo Node

### Enable OpenTelemetry Plugin
```json
{
  "PluginConfiguration": {
    "OpenTelemetry": {
      "Enabled": true,
      "ServiceName": "neo-node-1",
      "Metrics": {
        "Enabled": true,
        "PrometheusExporter": {
          "Enabled": true,
          "Port": 9090,
          "Path": "/metrics"
        }
      }
    }
  }
}
```

## 7. Verify Installation

### Check Prometheus Targets
1. Navigate to http://localhost:9090/targets
2. All Neo nodes should show as "UP"

### Import Dashboards to Grafana
1. Login to Grafana (http://localhost:3000)
2. Go to Dashboards → Import
3. Upload JSON files or they should auto-load

### Test Alerting
```bash
# Trigger test alert
curl -X POST http://localhost:9093/api/v1/alerts \
  -H "Content-Type: application/json" \
  -d '[{
    "labels": {
      "alertname": "TestAlert",
      "severity": "warning",
      "instance": "test"
    },
    "annotations": {
      "summary": "This is a test alert"
    }
  }]'
```

## 8. Production Considerations

### Security
```bash
# Enable basic auth for Prometheus
htpasswd -c /etc/prometheus/.htpasswd admin

# Update prometheus.yml
web:
  basic_auth_users:
    admin: $2y$10$...
```

### Backup Strategy
```bash
# Backup Prometheus data
docker run --rm -v prometheus_data:/data \
  -v $(pwd):/backup alpine \
  tar czf /backup/prometheus-backup-$(date +%Y%m%d).tar.gz /data

# Backup Grafana dashboards
docker exec grafana grafana-cli admin export-dashboard-json
```

### High Availability
- Deploy multiple Prometheus instances with federation
- Use Thanos for long-term storage
- Configure Grafana with multiple data sources

### Performance Tuning
```yaml
# Prometheus memory settings
storage:
  tsdb:
    retention.time: 15d  # Reduce for less memory
    retention.size: 50GB
    wal-compression: true
```

## 9. Monitoring the Monitoring

### Meta-monitoring Alerts
```yaml
- alert: PrometheusDown
  expr: up{job="prometheus"} == 0
  for: 5m
  annotations:
    summary: "Prometheus is down"

- alert: GrafanaDown
  expr: up{job="grafana"} == 0
  for: 5m
  annotations:
    summary: "Grafana is down"
```

## 10. Useful Commands

```bash
# Reload Prometheus configuration
curl -X POST http://localhost:9090/-/reload

# Check Prometheus configuration
promtool check config prometheus.yml

# Validate alerting rules
promtool check rules alerting-rules.yml

# Export/Import Grafana dashboards
curl -X GET http://admin:admin@localhost:3000/api/dashboards/uid/neo-main

# Silence alerts
amtool silence add alertname="TestAlert" --duration="2h"

# Check alert status
amtool alert query
```

## Support and Troubleshooting

### Common Issues

1. **No metrics appearing**
   - Check Neo node logs for OpenTelemetry errors
   - Verify firewall allows port 9090
   - Check Prometheus targets page

2. **Dashboards show "No Data"**
   - Verify datasource configuration
   - Check time range selection
   - Validate PromQL queries

3. **Alerts not firing**
   - Check alerting rules syntax
   - Verify Alertmanager configuration
   - Check inhibition rules

### Getting Help
- Neo Community Discord: https://discord.gg/neo
- GitHub Issues: https://github.com/neo-project/neo
- Documentation: https://docs.neo.org