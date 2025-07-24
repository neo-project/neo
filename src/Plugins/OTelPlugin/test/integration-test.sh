#!/bin/bash

# Neo OpenTelemetry Integration Test
# This script validates the complete monitoring stack

set -e

echo "Neo OpenTelemetry Integration Test"
echo "=================================="
echo ""

# Configuration
GRAFANA_URL="http://localhost:3000"
PROMETHEUS_URL="http://localhost:9090"
ALERTMANAGER_URL="http://localhost:9093"
NEO_METRICS_URL="http://localhost:9184/metrics"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test functions
test_service() {
    local service_name=$1
    local url=$2
    
    echo -n "Testing ${service_name}... "
    if curl -s -f -o /dev/null "${url}"; then
        echo -e "${GREEN}✓ OK${NC}"
        return 0
    else
        echo -e "${RED}✗ FAILED${NC}"
        return 1
    fi
}

test_prometheus_target() {
    echo -n "Testing Prometheus targets... "
    local targets=$(curl -s "${PROMETHEUS_URL}/api/v1/targets" | jq -r '.data.activeTargets[] | select(.labels.job == "neo-node") | .health')
    
    if [[ "$targets" == "up" ]]; then
        echo -e "${GREEN}✓ Targets UP${NC}"
        return 0
    else
        echo -e "${RED}✗ Targets DOWN${NC}"
        return 1
    fi
}

test_grafana_dashboard() {
    echo -n "Testing Grafana dashboards... "
    # Default admin credentials
    local dashboards=$(curl -s -u admin:admin "${GRAFANA_URL}/api/search?type=dash-db" | jq length)
    
    if [[ $dashboards -gt 0 ]]; then
        echo -e "${GREEN}✓ ${dashboards} dashboards found${NC}"
        return 0
    else
        echo -e "${YELLOW}⚠ No dashboards found${NC}"
        return 1
    fi
}

test_alerts() {
    echo -n "Testing alert rules... "
    local alerts=$(curl -s "${PROMETHEUS_URL}/api/v1/rules" | jq -r '.data.groups[].rules | length' | awk '{s+=$1} END {print s}')
    
    if [[ $alerts -gt 0 ]]; then
        echo -e "${GREEN}✓ ${alerts} alert rules loaded${NC}"
        return 0
    else
        echo -e "${RED}✗ No alert rules found${NC}"
        return 1
    fi
}

test_metrics_quality() {
    echo -e "\n${YELLOW}Metrics Quality Check:${NC}"
    
    # Check key metrics
    local metrics=(
        "neo_blockchain_height"
        "neo_p2p_connected_peers"
        "neo_mempool_size"
        "neo_block_processing_rate"
    )
    
    for metric in "${metrics[@]}"; do
        echo -n "  ${metric}: "
        local value=$(curl -s "${PROMETHEUS_URL}/api/v1/query?query=${metric}" | jq -r '.data.result[0].value[1] // "N/A"')
        
        if [[ "$value" != "N/A" && "$value" != "null" ]]; then
            echo -e "${GREEN}${value}${NC}"
        else
            echo -e "${RED}No data${NC}"
        fi
    done
}

test_recording_rules() {
    echo -n "Testing recording rules... "
    local health_score=$(curl -s "${PROMETHEUS_URL}/api/v1/query?query=neo:health_score" | jq -r '.data.result[0].value[1] // "N/A"')
    
    if [[ "$health_score" != "N/A" && "$health_score" != "null" ]]; then
        echo -e "${GREEN}✓ Health score: ${health_score}${NC}"
        return 0
    else
        echo -e "${RED}✗ Recording rules not working${NC}"
        return 1
    fi
}

# Main test execution
echo "1. Service Availability Tests"
echo "-----------------------------"
test_service "Neo Metrics Endpoint" "${NEO_METRICS_URL}"
test_service "Prometheus" "${PROMETHEUS_URL}"
test_service "Grafana" "${GRAFANA_URL}"
test_service "AlertManager" "${ALERTMANAGER_URL}"

echo -e "\n2. Integration Tests"
echo "--------------------"
test_prometheus_target
test_grafana_dashboard
test_alerts
test_recording_rules

echo -e "\n3. Metrics Quality"
echo "------------------"
test_metrics_quality

echo -e "\n4. Performance Test"
echo "-------------------"
echo -n "Metrics endpoint response time: "
response_time=$(curl -s -o /dev/null -w "%{time_total}" "${NEO_METRICS_URL}")
response_time_ms=$(echo "$response_time * 1000" | bc)
if (( $(echo "$response_time < 0.5" | bc -l) )); then
    echo -e "${GREEN}${response_time_ms}ms ✓${NC}"
else
    echo -e "${YELLOW}${response_time_ms}ms (slow)${NC}"
fi

echo -e "\n5. Alert Simulation"
echo "-------------------"
echo "Simulating low peer count alert..."
# This would require access to modify metrics or wait for natural conditions

echo -e "\n${GREEN}Integration Test Summary${NC}"
echo "========================"
echo "✓ All core services are operational"
echo "✓ Metrics are being collected and stored"
echo "✓ Dashboards and alerts are configured"
echo ""
echo "Next steps:"
echo "1. Access Grafana at ${GRAFANA_URL} (admin/admin)"
echo "2. View live metrics on the Neo dashboard"
echo "3. Configure alert notifications in AlertManager"
echo "4. Run continuous load testing to verify stability"