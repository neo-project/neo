#!/bin/bash

echo "======================================"
echo "Neo Monitoring Configuration Validator"
echo "======================================"
echo

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check if file exists
check_file() {
    if [ -f "$1" ]; then
        echo -e "${GREEN}✓${NC} $1 exists"
        return 0
    else
        echo -e "${RED}✗${NC} $1 missing"
        return 1
    fi
}

# Function to validate JSON
validate_json() {
    if python3 -m json.tool "$1" > /dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} $1 is valid JSON"
        return 0
    else
        echo -e "${RED}✗${NC} $1 has invalid JSON"
        return 1
    fi
}

# Function to validate YAML
validate_yaml() {
    if python3 -c "import yaml; yaml.safe_load(open('$1'))" 2>/dev/null; then
        echo -e "${GREEN}✓${NC} $1 is valid YAML"
        return 0
    else
        echo -e "${RED}✗${NC} $1 has invalid YAML"
        return 1
    fi
}

echo "1. Checking required files..."
echo "------------------------------"
check_file "docker-compose.yml"
check_file "prometheus.yml"
check_file "prometheus-alerts.yml"
check_file "alertmanager.yml"
check_file "neo-dashboard.json"
check_file "grafana-provisioning/dashboards/dashboard.yml"
check_file "grafana-provisioning/dashboards/neo-dashboard.json"
check_file "grafana-provisioning/datasources/prometheus.yml"
echo

echo "2. Validating configuration syntax..."
echo "-------------------------------------"
validate_yaml "docker-compose.yml"
validate_yaml "prometheus.yml"
validate_yaml "prometheus-alerts.yml"
validate_yaml "alertmanager.yml"
validate_yaml "grafana-provisioning/dashboards/dashboard.yml"
validate_yaml "grafana-provisioning/datasources/prometheus.yml"
validate_json "neo-dashboard.json"
validate_json "grafana-provisioning/dashboards/neo-dashboard.json"
echo

echo "3. Checking Prometheus configuration..."
echo "---------------------------------------"
# Check if prometheus.yml has the correct structure
if grep -q "global:" prometheus.yml && grep -q "scrape_configs:" prometheus.yml; then
    echo -e "${GREEN}✓${NC} Prometheus config has required sections"
else
    echo -e "${RED}✗${NC} Prometheus config missing required sections"
fi

# Check if neo-node job is configured
if grep -q "job_name: 'neo-node'" prometheus.yml; then
    echo -e "${GREEN}✓${NC} Neo node scrape job configured"
else
    echo -e "${RED}✗${NC} Neo node scrape job not found"
fi

# Check if alerts are configured
if grep -q "groups:" prometheus-alerts.yml; then
    echo -e "${GREEN}✓${NC} Alert rules configured"
    ALERT_COUNT=$(grep -c "alert:" prometheus-alerts.yml)
    echo -e "  Found ${ALERT_COUNT} alert rules"
else
    echo -e "${RED}✗${NC} No alert rules found"
fi
echo

echo "4. Checking Grafana dashboard..."
echo "--------------------------------"
# Check dashboard has panels
if grep -q '"panels"' grafana-provisioning/dashboards/neo-dashboard.json; then
    PANEL_COUNT=$(grep -c '"id":' grafana-provisioning/dashboards/neo-dashboard.json)
    echo -e "${GREEN}✓${NC} Dashboard has ${PANEL_COUNT} panels"
else
    echo -e "${RED}✗${NC} Dashboard has no panels"
fi

# Check dashboard has variables
if grep -q '"templating"' grafana-provisioning/dashboards/neo-dashboard.json; then
    echo -e "${GREEN}✓${NC} Dashboard has template variables"
else
    echo -e "${YELLOW}!${NC} Dashboard has no template variables"
fi

# Check dashboard title
DASHBOARD_TITLE=$(grep '"title":' grafana-provisioning/dashboards/neo-dashboard.json | head -1 | cut -d'"' -f4)
echo -e "${GREEN}✓${NC} Dashboard title: $DASHBOARD_TITLE"
echo

echo "5. Checking Docker Compose setup..."
echo "-----------------------------------"
# Check if all services are defined
for service in prometheus grafana alertmanager; do
    if grep -q "  $service:" docker-compose.yml; then
        echo -e "${GREEN}✓${NC} Service '$service' is defined"
    else
        echo -e "${RED}✗${NC} Service '$service' is missing"
    fi
done

# Check if volumes are mounted correctly
if grep -q "./prometheus.yml:/etc/prometheus/prometheus.yml" docker-compose.yml; then
    echo -e "${GREEN}✓${NC} Prometheus config volume mounted"
else
    echo -e "${RED}✗${NC} Prometheus config volume not mounted"
fi

if grep -q "./grafana-provisioning:/etc/grafana/provisioning" docker-compose.yml; then
    echo -e "${GREEN}✓${NC} Grafana provisioning volume mounted"
else
    echo -e "${RED}✗${NC} Grafana provisioning volume not mounted"
fi
echo

echo "======================================"
echo "Validation Complete!"
echo "======================================"
echo
echo "To start the monitoring stack (when Docker Hub is accessible):"
echo "  docker-compose up -d"
echo
echo "Services will be available at:"
echo "  - Prometheus: http://localhost:9091"
echo "  - Grafana: http://localhost:3000 (admin/admin)"
echo "  - Alertmanager: http://localhost:9093"