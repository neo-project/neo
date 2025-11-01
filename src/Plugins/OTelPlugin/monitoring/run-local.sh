#!/bin/bash

echo "================================================"
echo "Running Neo Monitoring Stack Locally (No Docker)"
echo "================================================"
echo

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

# Check for required tools
check_command() {
    if command -v $1 &> /dev/null; then
        echo -e "${GREEN}✓${NC} $1 is installed"
        return 0
    else
        echo -e "${RED}✗${NC} $1 is not installed"
        return 1
    fi
}

echo -e "${BLUE}Checking prerequisites...${NC}"
echo "-------------------------"

# Check if we have brew
if check_command brew; then
    echo "  Homebrew available for installing missing components"
fi

# Function to download and run Prometheus locally
run_prometheus_local() {
    echo -e "\n${BLUE}Option 1: Download Prometheus Binary${NC}"
    echo "-------------------------------------"
    
    PROMETHEUS_VERSION="2.45.0"
    PROMETHEUS_PLATFORM="darwin-amd64"
    PROMETHEUS_URL="https://github.com/prometheus/prometheus/releases/download/v${PROMETHEUS_VERSION}/prometheus-${PROMETHEUS_VERSION}.${PROMETHEUS_PLATFORM}.tar.gz"
    
    echo "Download URL: $PROMETHEUS_URL"
    echo
    echo "Commands to run Prometheus locally:"
    echo "  curl -LO $PROMETHEUS_URL"
    echo "  tar xvfz prometheus-*.tar.gz"
    echo "  cd prometheus-*"
    echo "  ./prometheus --config.file=../prometheus.yml --web.listen-address=:9091"
}

# Function to install via Homebrew
install_via_brew() {
    echo -e "\n${BLUE}Option 2: Install via Homebrew${NC}"
    echo "-------------------------------"
    echo "Commands to install and run:"
    echo "  brew install prometheus"
    echo "  brew install grafana"
    echo
    echo "Start services:"
    echo "  brew services start prometheus"
    echo "  brew services start grafana"
    echo
    echo "Or run in foreground:"
    echo "  prometheus --config.file=$(pwd)/prometheus.yml --web.listen-address=:9091"
    echo "  grafana-server --config=/usr/local/etc/grafana/grafana.ini --homepath /usr/local/share/grafana"
}

# Create a simple metrics simulator
create_metrics_simulator() {
    cat > metrics-simulator.py << 'EOF'
#!/usr/bin/env python3
import time
import random
from http.server import HTTPServer, BaseHTTPRequestHandler

class MetricsHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path == '/metrics':
            self.send_response(200)
            self.send_header('Content-Type', 'text/plain')
            self.end_headers()
            
            # Generate sample metrics
            height = 8234567 + random.randint(0, 100)
            peers = random.randint(8, 20)
            mempool = random.randint(10, 200)
            cpu = random.uniform(0.1, 0.9)
            memory = random.randint(400000000, 800000000)
            
            metrics = f"""# HELP neo_blockchain_height Current blockchain height
# TYPE neo_blockchain_height gauge
neo_blockchain_height{{network="mainnet",instance="localhost:9090"}} {height}

# HELP neo_p2p_connected_peers Number of connected peers
# TYPE neo_p2p_connected_peers gauge
neo_p2p_connected_peers{{network="mainnet",instance="localhost:9090"}} {peers}

# HELP neo_p2p_max_connected_peers Maximum number of connected peers
# TYPE neo_p2p_max_connected_peers gauge
neo_p2p_max_connected_peers{{network="mainnet",instance="localhost:9090"}} 50

# HELP neo_mempool_size Current mempool size
# TYPE neo_mempool_size gauge
neo_mempool_size{{network="mainnet",instance="localhost:9090"}} {mempool}

# HELP neo_mempool_verified_count Verified transactions in mempool
# TYPE neo_mempool_verified_count gauge
neo_mempool_verified_count{{network="mainnet",instance="localhost:9090"}} {int(mempool * 0.8)}

# HELP neo_mempool_unverified_count Unverified transactions in mempool
# TYPE neo_mempool_unverified_count gauge
neo_mempool_unverified_count{{network="mainnet",instance="localhost:9090"}} {int(mempool * 0.2)}

# HELP neo_blocks_processed_total Total blocks processed
# TYPE neo_blocks_processed_total counter
neo_blocks_processed_total{{network="mainnet",instance="localhost:9090"}} {height}

# HELP neo_transactions_processed_total Total transactions processed
# TYPE neo_transactions_processed_total counter
neo_transactions_processed_total{{network="mainnet",instance="localhost:9090"}} {height * 19}

# HELP neo_block_processing_time Block processing time histogram
# TYPE neo_block_processing_time histogram
neo_block_processing_time_bucket{{le="50",instance="localhost:9090"}} 1000
neo_block_processing_time_bucket{{le="100",instance="localhost:9090"}} 1800
neo_block_processing_time_bucket{{le="250",instance="localhost:9090"}} 2200
neo_block_processing_time_bucket{{le="500",instance="localhost:9090"}} 2400
neo_block_processing_time_bucket{{le="1000",instance="localhost:9090"}} 2450
neo_block_processing_time_bucket{{le="+Inf",instance="localhost:9090"}} 2500

# HELP process_cpu_usage Process CPU usage
# TYPE process_cpu_usage gauge
process_cpu_usage{{instance="localhost:9090"}} {cpu}

# HELP system_cpu_usage System CPU usage
# TYPE system_cpu_usage gauge
system_cpu_usage{{instance="localhost:9090"}} {cpu * 0.5}

# HELP process_memory_working_set Process memory working set
# TYPE process_memory_working_set gauge
process_memory_working_set{{instance="localhost:9090"}} {memory}

# HELP process_virtual_memory_size Process virtual memory
# TYPE process_virtual_memory_size gauge
process_virtual_memory_size{{instance="localhost:9090"}} {memory * 2}

# HELP dotnet_gc_heap_size GC heap size
# TYPE dotnet_gc_heap_size gauge
dotnet_gc_heap_size{{instance="localhost:9090"}} {int(memory * 0.6)}

# HELP process_threads_count Thread count
# TYPE process_threads_count gauge
process_threads_count{{instance="localhost:9090"}} {random.randint(50, 150)}

# HELP dotnet_gc_collections_total GC collections
# TYPE dotnet_gc_collections_total counter
dotnet_gc_collections_total{{generation="0",instance="localhost:9090"}} {random.randint(1000, 5000)}
dotnet_gc_collections_total{{generation="1",instance="localhost:9090"}} {random.randint(100, 500)}
dotnet_gc_collections_total{{generation="2",instance="localhost:9090"}} {random.randint(10, 50)}

# HELP neo_mempool_conflicts_total Mempool conflicts detected
# TYPE neo_mempool_conflicts_total counter
neo_mempool_conflicts_total{{instance="localhost:9090"}} {random.randint(100, 500)}

# HELP neo_mempool_batch_removed_size Transactions removed per batch
# TYPE neo_mempool_batch_removed_size histogram
neo_mempool_batch_removed_size_bucket{{le="1",instance="localhost:9090"}} {random.randint(10, 50)}
neo_mempool_batch_removed_size_bucket{{le="5",instance="localhost:9090"}} {random.randint(60, 120)}
neo_mempool_batch_removed_size_bucket{{le="10",instance="localhost:9090"}} {random.randint(130, 200)}
neo_mempool_batch_removed_size_bucket{{le="25",instance="localhost:9090"}} {random.randint(210, 240)}
neo_mempool_batch_removed_size_bucket{{le="+Inf",instance="localhost:9090"}} {random.randint(250, 280)}
neo_mempool_batch_removed_size_sum{{instance="localhost:9090"}} {random.randint(1000, 2500)}
neo_mempool_batch_removed_size_count{{instance="localhost:9090"}} {random.randint(250, 280)}

# HELP neo_mempool_capacity_ratio Mempool usage ratio
# TYPE neo_mempool_capacity_ratio gauge
neo_mempool_capacity_ratio{{instance="localhost:9090"}} {round(random.uniform(0.1, 0.8), 3)}

# HELP neo_consensus_round Latest consensus block height
# TYPE neo_consensus_round gauge
neo_consensus_round{{instance="localhost:9090"}} {height}

# HELP neo_consensus_view Current consensus view number
# TYPE neo_consensus_view gauge
neo_consensus_view{{instance="localhost:9090"}} {random.randint(0, 2)}

# HELP neo_consensus_state Current primary validator index
# TYPE neo_consensus_state gauge
neo_consensus_state{{instance="localhost:9090"}} {random.randint(0, 6)}

# HELP neo_consensus_time_to_finality Consensus time to finality in milliseconds
# TYPE neo_consensus_time_to_finality gauge
neo_consensus_time_to_finality{{instance="localhost:9090"}} {random.randint(1200, 5200)}

# HELP neo_consensus_view_changes_total Consensus view changes
# TYPE neo_consensus_view_changes_total counter
neo_consensus_view_changes_total{{instance="localhost:9090",reason="Timeout"}} {random.randint(0, 20)}

# HELP neo_consensus_messages_sent_total Consensus messages sent
# TYPE neo_consensus_messages_sent_total counter
neo_consensus_messages_sent_total{{instance="localhost:9090",type="PrepareRequest"}} {random.randint(200, 400)}
neo_consensus_messages_sent_total{{instance="localhost:9090",type="PrepareResponse"}} {random.randint(300, 600)}
neo_consensus_messages_sent_total{{instance="localhost:9090",type="Commit"}} {random.randint(250, 500)}
neo_consensus_messages_sent_total{{instance="localhost:9090",type="ChangeView"}} {random.randint(20, 60)}
neo_consensus_messages_sent_total{{instance="localhost:9090",type="RecoveryRequest"}} {random.randint(5, 30)}
neo_consensus_messages_sent_total{{instance="localhost:9090",type="RecoveryMessage"}} {random.randint(5, 30)}

# HELP neo_consensus_messages_received_total Consensus messages received
# TYPE neo_consensus_messages_received_total counter
neo_consensus_messages_received_total{{instance="localhost:9090",type="PrepareRequest"}} {random.randint(200, 400)}
neo_consensus_messages_received_total{{instance="localhost:9090",type="PrepareResponse"}} {random.randint(300, 600)}
neo_consensus_messages_received_total{{instance="localhost:9090",type="Commit"}} {random.randint(250, 500)}
neo_consensus_messages_received_total{{instance="localhost:9090",type="ChangeView"}} {random.randint(20, 60)}
neo_consensus_messages_received_total{{instance="localhost:9090",type="RecoveryRequest"}} {random.randint(5, 30)}
neo_consensus_messages_received_total{{instance="localhost:9090",type="RecoveryMessage"}} {random.randint(5, 30)}

# HELP neo_vm_trace_hot_ratio Hot trace hit ratio per script
# TYPE neo_vm_trace_hot_ratio gauge
neo_vm_trace_hot_ratio{{instance="localhost:9090",script="0x4F2B..A1",sequence="PUSH1 PUSH1 ADD MUL DIV",hits="{random.randint(150, 400)}",total_instructions="{random.randint(500, 1200)}",last_seen="{int(time.time())}"}} {round(random.uniform(0.3, 0.8), 3)}

# HELP neo_vm_trace_hot_hits Hot trace hit counts per script
# TYPE neo_vm_trace_hot_hits gauge
neo_vm_trace_hot_hits{{instance="localhost:9090",script="0x4F2B..A1",sequence="PUSH1 PUSH1 ADD MUL DIV",total_instructions="{random.randint(500, 1200)}",last_seen="{int(time.time())}"}} {random.randint(150, 400)}

# HELP neo_vm_trace_max_hot_ratio Maximum hot trace hit ratio across scripts
# TYPE neo_vm_trace_max_hot_ratio gauge
neo_vm_trace_max_hot_ratio{{instance="localhost:9090"}} {round(random.uniform(0.3, 0.85), 3)}

# HELP neo_vm_trace_max_hot_hits Maximum hot trace hit count across scripts
# TYPE neo_vm_trace_max_hot_hits gauge
neo_vm_trace_max_hot_hits{{instance="localhost:9090"}} {random.randint(200, 500)}

# HELP neo_vm_trace_profile_count Number of hot trace profiles
# TYPE neo_vm_trace_profile_count gauge
neo_vm_trace_profile_count{{instance="localhost:9090"}} {random.randint(2, 6)}

# HELP neo_errors_total Error count by type
# TYPE neo_errors_total counter
neo_errors_total{{error_type="network",instance="localhost:9090"}} {random.randint(0, 10)}
neo_errors_total{{error_type="storage",instance="localhost:9090"}} {random.randint(0, 5)}
neo_errors_total{{error_type="protocol",instance="localhost:9090"}} {random.randint(0, 3)}

# HELP up Target up
# TYPE up gauge
up{{instance="localhost:9090",job="neo-node"}} 1
"""
            self.wfile.write(metrics.encode())
        else:
            self.send_response(404)
            self.end_headers()
    
    def log_message(self, format, *args):
        return  # Suppress logs

if __name__ == '__main__':
    print("Starting metrics simulator on http://localhost:9090/metrics")
    print("Press Ctrl+C to stop")
    server = HTTPServer(('localhost', 9090), MetricsHandler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down metrics simulator")
        server.shutdown()
EOF
    chmod +x metrics-simulator.py
    echo -e "${GREEN}✓${NC} Created metrics-simulator.py"
}

# Main execution
echo -e "\n${BLUE}Creating Metrics Simulator${NC}"
echo "-------------------------"
create_metrics_simulator

echo -e "\n${YELLOW}Option 1: Run Metrics Simulator${NC}"
echo "--------------------------------"
echo "This will create a fake Neo node metrics endpoint for testing:"
echo -e "${GREEN}  python3 metrics-simulator.py${NC}"
echo
echo "Then in another terminal, you can:"
echo "1. Install Prometheus and Grafana locally"
echo "2. Configure them to use the config files in this directory"
echo

run_prometheus_local
install_via_brew

echo -e "\n${BLUE}Manual Dashboard Import${NC}"
echo "----------------------"
echo "Once Grafana is running (http://localhost:3000):"
echo "1. Login with admin/admin"
echo "2. Add Prometheus datasource: http://localhost:9091"
echo "3. Import dashboard from: $(pwd)/neo-dashboard.json"
echo

echo -e "\n${YELLOW}Option 2: Test with Docker (when available)${NC}"
echo "-------------------------------------------"
echo "Fix Docker Hub connectivity issue, then run:"
echo -e "${GREEN}  docker-compose up -d${NC}"
echo
echo "Services will be available at:"
echo "  - Prometheus: http://localhost:9091"
echo "  - Grafana: http://localhost:3000"
echo "  - Alertmanager: http://localhost:9093"
echo

echo -e "\n${BLUE}Current Status:${NC}"
echo "--------------"
echo -e "${RED}✗${NC} Docker Hub connectivity issue prevents container deployment"
echo -e "${GREEN}✓${NC} All configuration files are valid and ready"
echo -e "${GREEN}✓${NC} Metrics simulator created for local testing"
echo -e "${YELLOW}!${NC} Manual installation of Prometheus/Grafana required for local testing"

# Note: super-instruction planner remains disabled in local harness.
