#!/bin/bash

# Neo OpenTelemetry Monitoring Stack Setup Script

set -e

echo "Setting up Neo blockchain monitoring stack..."

# Create necessary directories
mkdir -p grafana-provisioning/dashboards
mkdir -p grafana-provisioning/datasources

# Copy dashboard
if [ -f "../grafana/neo-complete-dashboard.json" ]; then
    cp ../grafana/neo-complete-dashboard.json grafana-provisioning/dashboards/
    echo "✓ Dashboard copied"
else
    echo "✗ Dashboard file not found"
fi

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "✗ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Start the monitoring stack
echo "Starting monitoring stack..."
docker-compose up -d

# Wait for services to be ready
echo "Waiting for services to start..."
sleep 10

# Check service health
echo -e "\nChecking service status:"
docker-compose ps

# Display access information
echo -e "\n==================================="
echo "Neo Monitoring Stack is ready!"
echo "==================================="
echo "Access the following services:"
echo "  • Grafana:      http://localhost:3000 (admin/admin)"
echo "  • Prometheus:   http://localhost:9090"
echo "  • AlertManager: http://localhost:9093"
echo ""
echo "Configure your Neo node's OTelPlugin settings to export to:"
echo "  • Prometheus endpoint: http://localhost:9184/metrics"
echo ""
echo "To stop the stack: docker-compose down"
echo "To remove all data: docker-compose down -v"
echo "===================================="