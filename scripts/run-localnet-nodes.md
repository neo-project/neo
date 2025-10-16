# Localnet Node Management Script

This script helps you create and manage multiple Neo localnet nodes for development and testing purposes.

## Features

- **Automatic Configuration Generation**: Creates individual configuration files for each node
- **Port Management**: Automatically assigns unique ports for P2P and RPC communication
- **Node Management**: Start, stop, restart, and monitor multiple nodes
- **Data Isolation**: Each node has its own data directory and storage
- **Process Management**: Tracks running processes with PID files

## Usage

### Basic Commands

The DBFT and RpcServer plugins should be installed.

```bash
# Start 7 nodes (default)
./run-localnet-nodes.sh start

# Check status of all nodes
./run-localnet-nodes.sh status

# Stop all nodes
./run-localnet-nodes.sh stop

# Restart all nodes
./run-localnet-nodes.sh restart

# Clean up all data
./run-localnet-nodes.sh clean
```

### Node Configuration

Each node gets:
- **Unique P2P Port**: Starting from 20333 (Node0: 20333, Node1: 20334, etc.)
- **Unique RPC Port**: Starting from 10330 (Node0: 10330, Node1: 10331, etc.)
- **Isolated Data Directory**: `localnet_nodes/node_X/`
- **Individual Configuration**: `localnet_nodes/node_X/config.json`
- **Process Logs**: `localnet_nodes/node_X/neo.log`

### Network Configuration

- **Network ID**: 1234567890 (localnet)
- **Block Time**: 15 seconds
- **Validators**: 7 validators configured
- **Seed List**: All nodes are configured to connect to each other

## Directory Structure

```
localnet_nodes/
├── node_0/
│   ├── config.json
│   ├── neo.log
│   ├── neo.pid
│   └── Data_LevelDB_Node0/
├── node_1/
│   ├── config.json
│   ├── neo.log
│   ├── neo.pid
│   └── Data_LevelDB_Node1/
└── ...
```

## Prerequisites

1. **Build Neo Project**: Make sure the project is built
   ```bash
   dotnet build
   ```

2. **Neo-CLI Available**: The script looks for `neo-cli` or `neo-cli.dll` in the `bin/` directory

## Troubleshooting

### Node Won't Start
- Check if neo-cli is built: `ls bin/neo-cli*`
- Check logs: `cat localnet_nodes/node_X/neo.log`
- Verify ports are not in use: `netstat -an | grep 20333`

### Port Conflicts
- The script uses ports 20333+ for P2P and 10330+ for RPC
- Make sure these ports are available
- You can modify `BASE_PORT` and `BASE_RPC_PORT` in the script

### Process Management
- Each node runs as a background process
- PID files are stored in each node's directory
- Use `./run-localnet-nodes.sh status` to check running nodes

## Development Tips

1. **Start with 7 Nodes**: For development, 7 nodes is best configuration for testing.
2. **Monitor Logs**: Check individual node logs for debugging
3. **Clean Restart**: Use `clean` command to start fresh
4. **Network Connectivity**: Nodes automatically discover each other via seed list

## Example Workflow

```bash
# 1. Build the project
dotnet build

# 2. Start 3 localnet nodes
./run-localnet-nodes.sh start 3

# 3. Check status
./run-localnet-nodes.sh status

# 4. Monitor a specific node
tail -f localnet_nodes/node_0/neo.log

# 5. Stop all nodes
./run-localnet-nodes.sh stop

# 6. Clean up when done
./run-localnet-nodes.sh clean
```
