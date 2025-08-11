#!/bin/bash

# Neo OpenTelemetry Plugin Installation Verification Script
# This script verifies that the OpenTelemetry plugin is properly installed and operational

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_NAME="OTelPlugin"
NEO_CLI_PATH="${NEO_CLI_PATH:-/usr/local/neo-cli}"
PROMETHEUS_PORT="${PROMETHEUS_PORT:-9090}"
OTLP_PORT="${OTLP_PORT:-4317}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "================================================"
echo "Neo OpenTelemetry Plugin Installation Verifier"
echo "================================================"
echo ""

# Function to print colored output
print_status() {
    if [ "$1" == "OK" ]; then
        echo -e "${GREEN}✓${NC} $2"
    elif [ "$1" == "WARN" ]; then
        echo -e "${YELLOW}⚠${NC} $2"
    else
        echo -e "${RED}✗${NC} $2"
    fi
}

# Function to check if a port is open
check_port() {
    local port=$1
    local service=$2
    
    if nc -z localhost "$port" 2>/dev/null; then
        print_status "OK" "$service port $port is accessible"
        return 0
    else
        print_status "WARN" "$service port $port is not accessible (may not be started yet)"
        return 1
    fi
}

# Function to test Prometheus endpoint
test_prometheus() {
    local url="http://localhost:${PROMETHEUS_PORT}/metrics"
    
    echo ""
    echo "Testing Prometheus endpoint..."
    
    if curl -s "$url" > /dev/null 2>&1; then
        # Check for Neo-specific metrics
        local metrics=$(curl -s "$url")
        
        if echo "$metrics" | grep -q "neo_blockchain_height"; then
            print_status "OK" "Prometheus endpoint is working and Neo metrics are exposed"
            
            # Display some key metrics
            echo ""
            echo "Sample metrics:"
            echo "$metrics" | grep "^neo_" | head -5 | sed 's/^/  /'
            return 0
        else
            print_status "WARN" "Prometheus endpoint is accessible but Neo metrics not found yet"
            return 1
        fi
    else
        print_status "ERROR" "Cannot reach Prometheus endpoint at $url"
        return 1
    fi
}

# Function to check OTLP connectivity
test_otlp() {
    echo ""
    echo "Testing OTLP endpoint..."
    
    if check_port "$OTLP_PORT" "OTLP"; then
        print_status "OK" "OTLP port is accessible"
        return 0
    else
        print_status "WARN" "OTLP endpoint not accessible (check collector configuration)"
        return 1
    fi
}

# Function to verify configuration
verify_config() {
    local config_file="$1"
    
    echo ""
    echo "Verifying configuration..."
    
    if [ ! -f "$config_file" ]; then
        print_status "ERROR" "Configuration file not found: $config_file"
        return 1
    fi
    
    # Check if JSON is valid
    if python3 -m json.tool "$config_file" > /dev/null 2>&1; then
        print_status "OK" "Configuration file is valid JSON"
    else
        print_status "ERROR" "Configuration file contains invalid JSON"
        return 1
    fi
    
    # Check if plugin is enabled
    local enabled=$(python3 -c "
import json
with open('$config_file') as f:
    config = json.load(f)
    print(config.get('PluginConfiguration', {}).get('Enabled', False))
")
    
    if [ "$enabled" == "True" ]; then
        print_status "OK" "Plugin is enabled in configuration"
    else
        print_status "ERROR" "Plugin is disabled in configuration"
        return 1
    fi
    
    # Check Prometheus configuration
    local prom_enabled=$(python3 -c "
import json
with open('$config_file') as f:
    config = json.load(f)
    metrics = config.get('PluginConfiguration', {}).get('Metrics', {})
    prom = metrics.get('PrometheusExporter', {})
    print(prom.get('Enabled', False))
")
    
    if [ "$prom_enabled" == "True" ]; then
        print_status "OK" "Prometheus exporter is enabled"
    else
        print_status "WARN" "Prometheus exporter is disabled"
    fi
    
    return 0
}

# Main verification steps
echo "Step 1: Checking plugin files"
echo "------------------------------"

# Check if plugin DLL exists
if [ -f "$SCRIPT_DIR/bin/Debug/net9.0/$PLUGIN_NAME.dll" ] || [ -f "$SCRIPT_DIR/bin/Release/net9.0/$PLUGIN_NAME.dll" ]; then
    print_status "OK" "Plugin DLL found"
else
    print_status "ERROR" "Plugin DLL not found. Please build the plugin first."
    echo "  Run: dotnet build"
    exit 1
fi

# Check if configuration file exists
if [ -f "$SCRIPT_DIR/OTelPlugin.json" ]; then
    print_status "OK" "Configuration file found"
else
    print_status "ERROR" "Configuration file (OTelPlugin.json) not found"
    exit 1
fi

# Verify configuration
verify_config "$SCRIPT_DIR/OTelPlugin.json"

echo ""
echo "Step 2: Checking runtime dependencies"
echo "--------------------------------------"

# Check for .NET runtime
if dotnet --version > /dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version)
    print_status "OK" ".NET runtime found: $DOTNET_VERSION"
else
    print_status "ERROR" ".NET runtime not found"
    exit 1
fi

# Check for required tools
for tool in curl nc python3; do
    if command -v $tool > /dev/null 2>&1; then
        print_status "OK" "$tool is available"
    else
        print_status "WARN" "$tool is not available (needed for testing)"
    fi
done

echo ""
echo "Step 3: Checking Neo node integration"
echo "--------------------------------------"

# Check if Neo CLI directory exists
if [ -d "$NEO_CLI_PATH" ]; then
    print_status "OK" "Neo CLI directory found: $NEO_CLI_PATH"
    
    # Check if plugin is in the correct location
    PLUGIN_DIR="$NEO_CLI_PATH/Plugins/$PLUGIN_NAME"
    if [ -d "$PLUGIN_DIR" ]; then
        print_status "OK" "Plugin installed in Neo CLI Plugins directory"
    else
        print_status "WARN" "Plugin not found in $PLUGIN_DIR"
        echo "  To install: cp -r $SCRIPT_DIR $PLUGIN_DIR"
    fi
else
    print_status "WARN" "Neo CLI directory not found at $NEO_CLI_PATH"
    echo "  Set NEO_CLI_PATH environment variable to your Neo CLI installation"
fi

echo ""
echo "Step 4: Testing endpoints (if Neo is running)"
echo "----------------------------------------------"

# Test Prometheus endpoint
test_prometheus

# Test OTLP endpoint
test_otlp

echo ""
echo "Step 5: Quick start instructions"
echo "---------------------------------"
echo ""
echo "To start using the OpenTelemetry plugin:"
echo ""
echo "1. Build the plugin (if not already done):"
echo "   cd $SCRIPT_DIR"
echo "   dotnet build"
echo ""
echo "2. Copy plugin to Neo CLI (if not already done):"
echo "   cp -r $SCRIPT_DIR $NEO_CLI_PATH/Plugins/$PLUGIN_NAME"
echo ""
echo "3. Start your Neo node:"
echo "   cd $NEO_CLI_PATH"
echo "   dotnet neo-cli.dll"
echo ""
echo "4. Verify metrics are available:"
echo "   curl http://localhost:${PROMETHEUS_PORT}/metrics | grep neo_"
echo ""
echo "5. Check plugin status in Neo CLI:"
echo "   telemetry status"
echo ""
echo "================================================"
echo "Verification complete!"
echo "================================================"