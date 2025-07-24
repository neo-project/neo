#!/bin/bash

# Neo OpenTelemetry Metrics Validation Script

set -e

PROMETHEUS_URL="${PROMETHEUS_URL:-http://localhost:9090}"
NEO_METRICS_URL="${NEO_METRICS_URL:-http://localhost:9184/metrics}"

echo "Neo OpenTelemetry Metrics Validation"
echo "===================================="

# Function to check if a metric exists
check_metric() {
    local metric=$1
    local description=$2
    
    if curl -s "${NEO_METRICS_URL}" | grep -q "^${metric}"; then
        echo "✓ ${metric} - ${description}"
        return 0
    else
        echo "✗ ${metric} - ${description}"
        return 1
    fi
}

# Function to query Prometheus
query_prometheus() {
    local query=$1
    curl -s "${PROMETHEUS_URL}/api/v1/query?query=${query}" | jq -r '.data.result[0].value[1]' 2>/dev/null || echo "N/A"
}

echo -e "\n1. Checking Neo metrics endpoint..."
if curl -s -f "${NEO_METRICS_URL}" > /dev/null; then
    echo "✓ Metrics endpoint is accessible"
else
    echo "✗ Metrics endpoint is not accessible at ${NEO_METRICS_URL}"
    echo "  Make sure the Neo node is running with OpenTelemetry plugin enabled"
    exit 1
fi

echo -e "\n2. Validating core metrics..."
FAILED=0

# Blockchain metrics
check_metric "neo_blocks_processed_total" "Total blocks processed" || ((FAILED++))
check_metric "neo_blockchain_height" "Current blockchain height" || ((FAILED++))
check_metric "neo_block_processing_time" "Block processing time histogram" || ((FAILED++))
check_metric "neo_block_processing_rate" "Block processing rate" || ((FAILED++))

# Transaction metrics
check_metric "neo_transactions_processed_total" "Total transactions processed" || ((FAILED++))
check_metric "neo_contracts_invocations_total" "Contract invocations" || ((FAILED++))
check_metric "neo_transaction_verification_failures_total" "Transaction failures" || ((FAILED++))

# Network metrics
check_metric "neo_p2p_connected_peers" "Connected peers count" || ((FAILED++))
check_metric "neo_p2p_unconnected_peers" "Unconnected peers count" || ((FAILED++))
check_metric "neo_p2p_peer_connected_total" "Total peer connections" || ((FAILED++))
check_metric "neo_p2p_peer_disconnected_total" "Total peer disconnections" || ((FAILED++))
check_metric "neo_p2p_bytes_sent_total" "Bytes sent" || ((FAILED++))
check_metric "neo_p2p_bytes_received_total" "Bytes received" || ((FAILED++))

# MemPool metrics
check_metric "neo_mempool_size" "MemPool size" || ((FAILED++))
check_metric "neo_mempool_verified_count" "Verified transactions" || ((FAILED++))
check_metric "neo_mempool_unverified_count" "Unverified transactions" || ((FAILED++))
check_metric "neo_mempool_capacity_ratio" "Capacity ratio" || ((FAILED++))
check_metric "neo_mempool_memory_bytes" "Memory usage" || ((FAILED++))
check_metric "neo_mempool_conflicts_total" "Conflicts detected" || ((FAILED++))

# Error metrics
check_metric "neo_errors_protocol_total" "Protocol errors" || ((FAILED++))
check_metric "neo_errors_network_total" "Network errors" || ((FAILED++))
check_metric "neo_errors_storage_total" "Storage errors" || ((FAILED++))

echo -e "\n3. Checking Prometheus connectivity..."
if curl -s -f "${PROMETHEUS_URL}/api/v1/query?query=up" > /dev/null; then
    echo "✓ Prometheus is accessible"
    
    echo -e "\n4. Querying live metrics from Prometheus..."
    echo "  Blockchain height: $(query_prometheus 'neo_blockchain_height')"
    echo "  Connected peers: $(query_prometheus 'neo_p2p_connected_peers')"
    echo "  MemPool size: $(query_prometheus 'neo_mempool_size')"
    echo "  Block processing rate: $(query_prometheus 'neo_block_processing_rate')"
    echo "  MemPool capacity ratio: $(query_prometheus 'neo_mempool_capacity_ratio')"
    
    echo -e "\n5. Checking recording rules..."
    echo "  Health score: $(query_prometheus 'neo:health_score')"
    echo "  Transaction rate (5m): $(query_prometheus 'sum(neo:transactions:total_rate_5m)')"
    echo "  Error rate (5m): $(query_prometheus 'neo:errors:total_rate_5m')"
else
    echo "✗ Prometheus is not accessible at ${PROMETHEUS_URL}"
fi

echo -e "\n6. Summary"
echo "=========="
if [ $FAILED -eq 0 ]; then
    echo "✓ All metrics validated successfully!"
    exit 0
else
    echo "✗ ${FAILED} metrics failed validation"
    echo "  Please check your OpenTelemetry plugin configuration"
    exit 1
fi