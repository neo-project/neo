#!/bin/bash
# Script to generate and manage multiple localnet neo-cli nodes
# Usage: ./run-localnet-nodes.sh [start|stop|status|clean] [node_count] [base_port] [base_rpc_port]

set -e

# Configuration
NODE_COUNT=${2:-7}  # 7 nodes
BASE_PORT=${3:-20333}  # Default P2P port, can be overridden
BASE_RPC_PORT=${4:-10330}  # Default RPC port, can be overridden
BASE_DATA_DIR="localnet_nodes"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NEO_CLI_DIR="${NEO_CLI_DIR:-$SCRIPT_DIR/../bin/Neo.CLI/net9.0}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if neo-cli exists
check_neo_cli() {
    log_info "Using NEO_CLI_DIR: $NEO_CLI_DIR"
    if [ ! -f "$NEO_CLI_DIR/neo-cli" ] && [ ! -f "$NEO_CLI_DIR/neo-cli.dll" ]; then
        log_error "neo-cli not found in $NEO_CLI_DIR"
        log_info "Please build the project first: dotnet build"
        log_info "Or set NEO_CLI_DIR environment variable to the correct path"
        exit 1
    fi
}

# An Array of addresses, NOTE: just for test
ADDRESSES=(
    "NSL83LVKbvCpg5gjWC9bfsmERN5kRJSs9d"
    "NRPf2BLaP595UFybH1nwrExJSt5ZGbKnjd"
    "NXRrR4VU3TJyZ6iPBfvoddRKAGynVPvjKm"
    "NfGwaZPHGXLqZ17U7p5hqkZGivArXbXUbL"
    "NjCgqnnJpCsRQwEayWy1cZSWVwQ7eRejRq"
    "NYXoVMFa3ekGUnX4qzk8DTD2hhs5aSh2k4"
    "NVBkHV59hTDxXAGas51eMtWhfxa6VDxf38"
)

# An Array of keys
KEYS=(
    "6PYVdEBZe7Mg4CiikuCXkEpcbwX7WXT72xfHTYd6hJzRWN3iBPDfGis7kV"
    "6PYNZ7WDsjXwn2Mo8T3N7fTw7ZSfY71MXbVeRf1zZjv2baEdjbWNHm5mGQ"
    "6PYXGkpWLLXtyC6cQthCcShioQJupRvyhDrz6xfLyiEa9HeJW4oTb4aJHP"
    "6PYUCCNgCrVrB5vpCbsFwzEA7d2SkCzCTYMyhYw2TL51CaGeie2UWyehzw"
    "6PYQpWR6CGrWDKauPWfVEfmwMKp2xKFod4X1AvV39ud5qhaSkrsFQeCBPy"
    "6PYTm6sJLR1oWX2svdJkzWhkbqTAGurEybDdcCTBa19WNzDuFXURX2NAaE"
    "6PYLnvLHYiVfxzKYMFtaZakWxkyj3WDX86PwBBYrYgkotBms6MWjfA2WHU"
)

# An Array of scripts
SCRIPTS=(
    "DCEChSZdyIWdBeHkKpDWwpqd4VUx6sGCSJdD5qlHgX0qn2ZBVuezJw=="
    "DCECozKyXb9hGPwlv2Tw2DALu2I7eDRDcazwy1ByffMtnbNBVuezJw=="
    "DCECqgIsK8NhTSOvwSFvxD2tkHINHilrTgm37izZvrgNm+pBVuezJw=="
    "DCEDabXhB8SMjperdGnbbr8JAZz7MiPToYxK+iFwQoE9+d5BVuezJw=="
    "DCECnkPTdNxK3KFYu0ZbSthBegdmQaU5UOPLccY0PdJYk9RBVuezJw=="
    "DCEChLsd71mcGde7lMvdiOx+1IXbId6mTIa7kXYi+1ac6cpBVuezJw=="
    "DCECwt/bG9EMosR8IoCV7biuh20TLuae8H4/+okS2TPWhq9BVuezJw=="
)


# Generate configuration for a specific node
generate_node_config() {
    local node_id=$1
    local port=$((BASE_PORT + node_id))
    local rpc_port=$((BASE_RPC_PORT + node_id))
    local data_dir="$BASE_DATA_DIR/node_$node_id"
    local config_file="$data_dir/config.json"
    local wallet_file="$data_dir/wallet.json"

    log_info "Generating config for node $node_id (port: $port, rpc: $rpc_port)"
    
    # Create data directory
    mkdir -p "$data_dir"

    # Generate seed list (all other nodes)
    local seed_list=""
    for i in $(seq 0 $((NODE_COUNT-1))); do
        local seed_port=$((BASE_PORT + i))
        if [ -n "$seed_list" ]; then
            seed_list="$seed_list,"
        fi
        seed_list="$seed_list\"localhost:$seed_port\""
    done
    
    # Create configuration file
    cat > "$config_file" << EOF
{
  "ApplicationConfiguration": {
    "Logger": {
      "Path": "Logs",
      "ConsoleOutput": true,
      "Active": true
    },
    "Storage": {
      "Engine": "LevelDBStore",
      "Path": "Data_LevelDB_Node$node_id"
    },
    "P2P": {
      "Port": $port,
      "EnableCompression": true,
      "MinDesiredConnections": 3,
      "MaxConnections": 10,
      "MaxKnownHashes": 1000,
      "MaxConnectionsPerAddress": 3
    },
    "UnlockWallet": {
      "Path": "wallet.json",
      "Password": "123",
      "IsActive": true
    },
    "Contracts": {
      "NeoNameService": "0x50ac1c37690cc2cfc594472833cf57505d5f46de"
    },
    "Plugins": {
      "DownloadUrl": "https://api.github.com/repos/neo-project/neo/releases"
    }
  },
  "ProtocolConfiguration": {
    "Network": 1234567890,
    "AddressVersion": 53,
    "MillisecondsPerBlock": 15000,
    "MaxTransactionsPerBlock": 5000,
    "MemoryPoolMaxTransactions": 50000,
    "MaxTraceableBlocks": 2102400,
    "Hardforks": {
      "HF_Aspidochelone": 1,
      "HF_Basilisk": 1,
      "HF_Cockatrice": 1,
      "HF_Domovoi": 1,
      "HF_Echidna": 1,
      "HF_Faun": 1
    },
    "InitialGasDistribution": 5200000000000000,
    "ValidatorsCount": 7,
    "StandbyCommittee": [
      "0285265dc8859d05e1e42a90d6c29a9de15531eac182489743e6a947817d2a9f66",
      "02a332b25dbf6118fc25bf64f0d8300bbb623b78344371acf0cb50727df32d9db3",
      "03a1abc97625d45b74e1b862410245338bf64e742984b87ddfaa3e92a4c810450d",
      "02aa022c2bc3614d23afc1216fc43dad90720d1e296b4e09b7ee2cd9beb80d9bea",
      "0369b5e107c48c8e97ab7469db6ebf09019cfb3223d3a18c4afa217042813df9de",
      "029e43d374dc4adca158bb465b4ad8417a076641a53950e3cb71c6343dd25893d4",
      "0284bb1def599c19d7bb94cbdd88ec7ed485db21dea64c86bb917622fb569ce9ca"
    ],
    "SeedList": [
      $seed_list
    ]
  }
}
EOF

    cat > "$wallet_file" << EOF
{
    "name": "node_$node_id",
    "version": "1.0",
    "scrypt": {"n": 2, "r": 1, "p": 1 },
    "accounts": [{
        "address": "${ADDRESSES[$node_id]}",
        "isDefault": true,
        "lock": false,
        "key": "${KEYS[$node_id]}",
        "contract": {
            "script": "${SCRIPTS[$node_id]}",
            "parameters": [{"name": "signature","type": "Signature"}],
            "deployed": false
        }
    }]
}
EOF

    log_success "Generated config for node $node_id"
}

# Update plugin configuration files to use local test network ID
update_plugin_configs() {
    log_info "Updating plugin configurations for local test network..."

    # Find and update all plugin JSON files in the Plugins directory under NEO_CLI_DIR
    find "$NEO_CLI_DIR/Plugins" -name "*.json" -type f 2>/dev/null | while read -r plugin_file; do
        if [ -f "$plugin_file" ]; then
            # Check if the file contains any Network configuration
            if grep -q '"Network":' "$plugin_file"; then
                # Get the current network ID for logging
                current_network=$(grep '"Network":' "$plugin_file" | sed 's/.*"Network": *\([0-9]*\).*/\1/')
                log_info "Updating network ID from $current_network to 1234567890 in: $plugin_file"

                # Replace any network ID with local test network ID
                sed -i.bak 's/"Network": [0-9]*/"Network": 1234567890/g' "$plugin_file"

                # Remove backup file
                rm -f "$plugin_file.bak"
            fi
        fi
    done

    if [ -f "$NEO_CLI_DIR/Plugins/DBFTPlugin/DBFTPlugin.json" ]; then
        # set AutoStart to true
        sed -i.bak 's/"AutoStart": false/"AutoStart": true/g' "$NEO_CLI_DIR/Plugins/DBFTPlugin/DBFTPlugin.json"
        rm -f "$NEO_CLI_DIR/Plugins/DBFTPlugin/DBFTPlugin.json.bak"
    fi

    log_success "Plugin configurations updated for local test network"
}

# Generate all node configurations
generate_configs() {
    local force=${1:-false}

    log_info "Generating configurations for $NODE_COUNT nodes..."

    # Create base directory if it doesn't exist
    mkdir -p "$BASE_DATA_DIR"

    # Generate config for each node only if it doesn't exist or force regenerate
    for i in $(seq 0 $((NODE_COUNT-1))); do
        local data_dir="$BASE_DATA_DIR/node_$i"
        local config_file="$data_dir/config.json"
        local wallet_file="$data_dir/wallet.json"

        if [ "$force" = "true" ] || [ ! -f "$config_file" ] || [ ! -f "$wallet_file" ]; then
            if [ "$force" = "true" ]; then
                log_info "Force regenerating configuration for node $i..."
            fi
            generate_node_config $i
        else
            log_info "Node $i configuration already exists, skipping..."
        fi
    done

    log_success "Generated $NODE_COUNT node configurations"
}

# Start a specific node
start_node() {
    local node_id=$1
    local data_dir="$BASE_DATA_DIR/node_$node_id"
    local config_file="$data_dir/config.json"
    local pid_file="$data_dir/neo.pid"

    if [ -f "$pid_file" ] && kill -0 "$(cat "$pid_file")" 2>/dev/null; then
        log_warning "Node $node_id is already running (PID: $(cat "$pid_file"))"
        return
    fi

    log_info "Starting node $node_id..."
    
    # Ensure data directory exists
    mkdir -p "$data_dir"

    # Change to the data directory
    cd "$data_dir"

    # Start neo-cli in background
    log_info "Starting $NEO_CLI_DIR/neo-cli in $data_dir"
    if [ -f "$NEO_CLI_DIR/neo-cli" ]; then
        nohup "$NEO_CLI_DIR/neo-cli" --background > neo.log 2>&1 &
    else
        log_error "neo-cli executable not found"
        return 1
    fi
    
    local pid=$!
    log_info "node $node_id started with pid $pid"
    echo $pid > neo.pid

    # Wait a moment and check if process is still running
    sleep 1
    if kill -0 $pid 2>/dev/null; then
        log_success "Node $node_id started (PID: $pid)"
    else
        log_error "Failed to start node $node_id"
        rm -f neo.pid
        return 1
    fi
    
    # Return to original directory
    cd - > /dev/null
}

# Start all nodes
start_nodes() {
    log_info "Starting $NODE_COUNT localnet nodes..."

    check_neo_cli

    # Always generate configs to ensure they're up to date
    generate_configs

    # Update plugin configuration files to use local test network ID
    update_plugin_configs

    # Start each node
    for i in $(seq 0 $((NODE_COUNT-1))); do
            # set RpcServer Port to BASE_RPC_PORT + node_id
        if [ -f "$NEO_CLI_DIR/Plugins/RpcServer/RpcServer.json" ]; then
            local rpc_port=$((BASE_RPC_PORT + i))
            sed -i.bak "s/\"Port\": [0-9]*/\"Port\": $rpc_port/g" "$NEO_CLI_DIR/Plugins/RpcServer/RpcServer.json"
            rm -f "$NEO_CLI_DIR/Plugins/RpcServer/RpcServer.json.bak"
        fi

        start_node $i
        sleep 1  # Small delay between starts
    done
    
    log_success "All nodes started!"
    show_status
}

# Stop a specific node
stop_node() {
    local node_id=$1
    local pid_file="$BASE_DATA_DIR/node_$node_id/neo.pid"

    if [ -f "$pid_file" ]; then
        local pid=$(cat "$pid_file")
        if kill -0 $pid 2>/dev/null; then
            log_info "Stopping node $node_id (PID: $pid)..."
            kill $pid
            rm -f "$pid_file"
            log_success "Node $node_id stopped"
        else
            log_warning "Node $node_id was not running"
            rm -f "$pid_file"
        fi
    else
        log_warning "Node $node_id is not running"
    fi
}

# Stop all nodes
stop_nodes() {
    log_info "Stopping all localnet nodes..."

    for i in $(seq 0 $((NODE_COUNT-1))); do
        stop_node $i
    done

    log_success "All nodes stopped!"
}

# Show status of all nodes
show_status() {
    log_info "Localnet nodes status:"
    echo "----------------------------------------------"

    # if RpcServer plugin not installed, don't show RPC port
    show_rpc_port=false
    if [ ! -f "$NEO_CLI_DIR/Plugins/RpcServer/RpcServer.json" ]; then
        show_rpc_port=false
    else
        show_rpc_port=true
    fi

    if [ "$show_rpc_port" = "true" ]; then
        printf "%-8s %-8s %-12s %-8s %-8s\n" "Node" "Status" "PID" "Port" "RPC"
    else
        printf "%-8s %-8s %-12s %-8s\n" "Node" "Status" "PID" "Port"
    fi
    echo "----------------------------------------------"

    for i in $(seq 0 $((NODE_COUNT-1))); do
        local pid_file="$BASE_DATA_DIR/node_$i/neo.pid"
        local port=$((BASE_PORT + i))
        local rpc_port=$((BASE_RPC_PORT + i))
        
        if [ -f "$pid_file" ] && kill -0 "$(cat "$pid_file")" 2>/dev/null; then
            local pid=$(cat "$pid_file")
            if [ "$show_rpc_port" = "false" ]; then
                printf "%-8s %-8s %-12s %-8s\n" "Node$i" "Running" "$pid" "$port"
            else
                printf "%-8s %-8s %-12s %-8s %-8s\n" "Node$i" "Running" "$pid" "$port" "$rpc_port"
            fi
        else
            if [ "$show_rpc_port" = "false" ]; then
                printf "%-8s %-8s %-12s %-8s\n" "Node$i" "Stopped" "-" "$port"
            else
                printf "%-8s %-8s %-12s %-8s %-8s\n" "Node$i" "Stopped" "-" "$port" "$rpc_port"
            fi
        fi
    done
    echo "----------------------------------------------"
}

# Clean up all data
clean_data() {
    log_info "Cleaning up all localnet data..."
    rm -rf "$BASE_DATA_DIR"
    log_success "All localnet data cleaned up"
}

# Show usage
show_usage() {
    echo "Usage: $0 [command] [node_count] [base_port] [base_rpc_port]"
    echo ""
    echo "Commands:"
    echo "  start     Start all localnet nodes (default: 7 nodes)"
    echo "  stop      Stop all localnet nodes"
    echo "  status    Show status of all nodes"
    echo "  clean     Clean up all node data"
    echo "  restart   Stop and start all nodes"
    echo "  regenerate Force regenerate all node configurations"
    echo ""
    echo "Parameters:"
    echo "  node_count    Number of nodes to start (default: 7)"
    echo "  base_port     Starting P2P port (default: 20333)"
    echo "  base_rpc_port Starting RPC port (default: 10330)"
    echo ""
    echo "Environment Variables:"
    echo "  NEO_CLI_DIR    Path to neo-cli directory (default: ../bin/Neo.CLI/net9.0)"
    echo ""
    echo "Examples:"
    echo "  $0 start                    # Start 7 nodes with default ports"
    echo "  $0 start 5                  # Start 5 nodes with default ports"
    echo "  $0 start 7 30000 20000      # Start 7 nodes with P2P ports 30000-30006, RPC ports 20000-20006"
    echo "  $0 status                   # Show status"
    echo "  $0 stop                     # Stop all nodes"
    echo "  $0 regenerate               # Force regenerate all configurations"
    echo "  NEO_CLI_DIR=/path/to/neo-cli $0 start  # Use custom neo-cli path"
}

# Main script logic
case "${1:-start}" in
    "start")
        start_nodes
        ;;
    "stop")
        stop_nodes
        ;;
    "status")
        show_status
        ;;
    "clean")
        clean_data
        ;;
    "restart")
        stop_nodes
        sleep 2
        start_nodes
        ;;
    "regenerate")
        log_info "Force regenerating all node configurations..."
        check_neo_cli
        generate_configs true
        update_plugin_configs
        log_success "All configurations regenerated!"
        ;;
    "help"|"-h"|"--help")
        show_usage
        ;;
    *)
        log_error "Unknown command: $1"
        show_usage
        exit 1
        ;;
esac
