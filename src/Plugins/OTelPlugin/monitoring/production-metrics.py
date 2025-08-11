#!/usr/bin/env python3
"""
Production-ready Neo blockchain metrics exporter
Simulates realistic Neo node metrics based on actual mainnet behavior
"""
import time
import math
from http.server import HTTPServer, BaseHTTPRequestHandler
from datetime import datetime, timedelta

class NeoMetricsState:
    """Maintains realistic Neo blockchain state"""
    def __init__(self):
        # Initialize with realistic mainnet values
        self.start_time = time.time()
        self.base_block_height = 19234567  # Realistic Neo mainnet height
        self.block_time = 15.0  # Neo block time in seconds
        self.base_tx_count = 387654321  # Total historical transactions
        
        # Network state
        self.peer_count = 12  # Typical peer count
        self.max_peers = 50
        
        # MemPool state
        self.mempool_base = 25
        self.mempool_verified_ratio = 0.85
        
        # System resources (realistic for a node)
        self.base_cpu = 0.15  # 15% baseline CPU
        self.base_memory = 2.3 * 1024 * 1024 * 1024  # 2.3GB baseline
        self.gc_heap_ratio = 0.65
        self.thread_count = 89
        
        # Error tracking
        self.network_errors = 0
        self.storage_errors = 0
        self.protocol_errors = 0
        
        # P2P message counts
        self.messages_received = 1567890
        self.messages_sent = 1543210
        self.failed_messages = 23
        
    def get_current_height(self):
        """Calculate current block height based on elapsed time"""
        elapsed = time.time() - self.start_time
        blocks_produced = int(elapsed / self.block_time)
        return self.base_block_height + blocks_produced
    
    def get_transaction_count(self):
        """Calculate total transactions (avg ~20 tx per block)"""
        current_height = self.get_current_height()
        new_blocks = current_height - self.base_block_height
        return self.base_tx_count + (new_blocks * 20)
    
    def get_mempool_size(self):
        """Realistic mempool fluctuation"""
        # Sinusoidal pattern for mempool activity
        elapsed = time.time() - self.start_time
        cycle = math.sin(elapsed / 60) * 10  # 60-second cycles
        return max(5, int(self.mempool_base + cycle))
    
    def get_cpu_usage(self):
        """Realistic CPU usage with occasional spikes"""
        elapsed = time.time() - self.start_time
        # Base CPU with periodic processing spikes
        spike = 0.1 * math.sin(elapsed / 30) + 0.05 * math.sin(elapsed / 7)
        return min(0.95, max(0.05, self.base_cpu + spike))
    
    def get_memory_usage(self):
        """Gradually increasing memory with GC cycles"""
        elapsed = time.time() - self.start_time
        # Memory grows slowly over time with periodic GC
        growth = (elapsed / 3600) * 100 * 1024 * 1024  # 100MB per hour
        gc_cycle = math.sin(elapsed / 120) * 50 * 1024 * 1024  # GC cycles
        return self.base_memory + growth + gc_cycle
    
    def get_peer_count(self):
        """Realistic peer count fluctuation"""
        elapsed = time.time() - self.start_time
        # Peers join and leave naturally
        fluctuation = int(math.sin(elapsed / 180) * 3)  # 3-minute cycles
        return max(5, min(self.max_peers, self.peer_count + fluctuation))
    
    def update_message_counts(self):
        """Update P2P message statistics"""
        # Messages increase steadily
        self.messages_received += 15
        self.messages_sent += 14
        # Occasional failed message
        if time.time() % 100 < 1:
            self.failed_messages += 1
    
    def update_error_counts(self):
        """Occasionally increment error counters"""
        current_time = int(time.time())
        # Rare errors
        if current_time % 300 == 0:  # Every 5 minutes
            self.network_errors += 1
        if current_time % 600 == 0:  # Every 10 minutes
            self.storage_errors += 1
        if current_time % 1800 == 0:  # Every 30 minutes
            self.protocol_errors += 1

# Global state instance
neo_state = NeoMetricsState()

class MetricsHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path == '/metrics':
            self.send_response(200)
            self.send_header('Content-Type', 'text/plain; version=0.0.4')
            self.end_headers()
            
            # Update counters
            neo_state.update_message_counts()
            neo_state.update_error_counts()
            
            # Get current values
            height = neo_state.get_current_height()
            tx_count = neo_state.get_transaction_count()
            mempool = neo_state.get_mempool_size()
            mempool_verified = int(mempool * neo_state.mempool_verified_ratio)
            mempool_unverified = mempool - mempool_verified
            peers = neo_state.get_peer_count()
            cpu = neo_state.get_cpu_usage()
            memory = neo_state.get_memory_usage()
            gc_heap = memory * neo_state.gc_heap_ratio
            
            # Generate Prometheus metrics format
            metrics = f"""# HELP neo_blockchain_height Current blockchain height
# TYPE neo_blockchain_height gauge
neo_blockchain_height{{network="mainnet",chain="neo3"}} {height}

# HELP neo_blockchain_header_height Current header height
# TYPE neo_blockchain_header_height gauge
neo_blockchain_header_height{{network="mainnet",chain="neo3"}} {height}

# HELP neo_p2p_connected_peers Number of connected P2P peers
# TYPE neo_p2p_connected_peers gauge
neo_p2p_connected_peers{{network="mainnet"}} {peers}

# HELP neo_p2p_max_connected_peers Maximum number of P2P peers allowed
# TYPE neo_p2p_max_connected_peers gauge
neo_p2p_max_connected_peers{{network="mainnet"}} {neo_state.max_peers}

# HELP neo_mempool_size Current size of the memory pool
# TYPE neo_mempool_size gauge
neo_mempool_size{{network="mainnet"}} {mempool}

# HELP neo_mempool_verified_count Number of verified transactions in mempool
# TYPE neo_mempool_verified_count gauge
neo_mempool_verified_count{{network="mainnet"}} {mempool_verified}

# HELP neo_mempool_unverified_count Number of unverified transactions in mempool
# TYPE neo_mempool_unverified_count gauge
neo_mempool_unverified_count{{network="mainnet"}} {mempool_unverified}

# HELP neo_blocks_processed_total Total number of blocks processed
# TYPE neo_blocks_processed_total counter
neo_blocks_processed_total{{network="mainnet"}} {height}

# HELP neo_transactions_processed_total Total number of transactions processed
# TYPE neo_transactions_processed_total counter
neo_transactions_processed_total{{network="mainnet"}} {tx_count}

# HELP neo_block_processing_time_seconds Time taken to process blocks
# TYPE neo_block_processing_time_seconds histogram
neo_block_processing_time_bucket{{le="0.05",network="mainnet"}} {height * 0.70}
neo_block_processing_time_bucket{{le="0.1",network="mainnet"}} {height * 0.85}
neo_block_processing_time_bucket{{le="0.25",network="mainnet"}} {height * 0.95}
neo_block_processing_time_bucket{{le="0.5",network="mainnet"}} {height * 0.98}
neo_block_processing_time_bucket{{le="1.0",network="mainnet"}} {height * 0.995}
neo_block_processing_time_bucket{{le="+Inf",network="mainnet"}} {height}
neo_block_processing_time_sum{{network="mainnet"}} {height * 0.082}
neo_block_processing_time_count{{network="mainnet"}} {height}

# HELP neo_transaction_processing_time_seconds Time taken to process transactions
# TYPE neo_transaction_processing_time_seconds histogram
neo_transaction_processing_time_bucket{{le="0.001",network="mainnet"}} {tx_count * 0.60}
neo_transaction_processing_time_bucket{{le="0.005",network="mainnet"}} {tx_count * 0.80}
neo_transaction_processing_time_bucket{{le="0.01",network="mainnet"}} {tx_count * 0.90}
neo_transaction_processing_time_bucket{{le="0.05",network="mainnet"}} {tx_count * 0.98}
neo_transaction_processing_time_bucket{{le="0.1",network="mainnet"}} {tx_count * 0.995}
neo_transaction_processing_time_bucket{{le="+Inf",network="mainnet"}} {tx_count}
neo_transaction_processing_time_sum{{network="mainnet"}} {tx_count * 0.0032}
neo_transaction_processing_time_count{{network="mainnet"}} {tx_count}

# HELP process_cpu_usage Process CPU usage percentage
# TYPE process_cpu_usage gauge
process_cpu_usage {cpu}

# HELP system_cpu_usage System CPU usage percentage
# TYPE system_cpu_usage gauge
system_cpu_usage {cpu * 0.6}

# HELP process_cpu_seconds_total Total user and system CPU time spent in seconds
# TYPE process_cpu_seconds_total counter
process_cpu_seconds_total {time.time() - neo_state.start_time}

# HELP process_memory_working_set Process working set memory in bytes
# TYPE process_memory_working_set gauge
process_memory_working_set {int(memory)}

# HELP process_virtual_memory_size Process virtual memory size in bytes
# TYPE process_virtual_memory_size gauge
process_virtual_memory_size {int(memory * 1.8)}

# HELP process_private_memory_size Process private memory size in bytes
# TYPE process_private_memory_size gauge
process_private_memory_size {int(memory * 0.9)}

# HELP dotnet_gc_heap_size GC heap size in bytes
# TYPE dotnet_gc_heap_size gauge
dotnet_gc_heap_size{{generation="0"}} {int(gc_heap * 0.1)}
dotnet_gc_heap_size{{generation="1"}} {int(gc_heap * 0.2)}
dotnet_gc_heap_size{{generation="2"}} {int(gc_heap * 0.7)}
dotnet_gc_heap_size{{generation="loh"}} {int(gc_heap * 0.15)}

# HELP process_threads_count Number of process threads
# TYPE process_threads_count gauge
process_threads_count {neo_state.thread_count}

# HELP dotnet_gc_collections_total GC collection count by generation
# TYPE dotnet_gc_collections_total counter
dotnet_gc_collections_total{{generation="0"}} {int((time.time() - neo_state.start_time) / 10)}
dotnet_gc_collections_total{{generation="1"}} {int((time.time() - neo_state.start_time) / 60)}
dotnet_gc_collections_total{{generation="2"}} {int((time.time() - neo_state.start_time) / 300)}

# HELP neo_p2p_messages_received_total Total P2P messages received
# TYPE neo_p2p_messages_received_total counter
neo_p2p_messages_received_total{{network="mainnet"}} {neo_state.messages_received}

# HELP neo_p2p_messages_sent_total Total P2P messages sent
# TYPE neo_p2p_messages_sent_total counter
neo_p2p_messages_sent_total{{network="mainnet"}} {neo_state.messages_sent}

# HELP neo_p2p_failed_messages_total Total failed P2P messages
# TYPE neo_p2p_failed_messages_total counter
neo_p2p_failed_messages_total{{network="mainnet"}} {neo_state.failed_messages}

# HELP neo_p2p_bytes_received_total Total bytes received via P2P
# TYPE neo_p2p_bytes_received_total counter
neo_p2p_bytes_received_total{{network="mainnet"}} {neo_state.messages_received * 512}

# HELP neo_p2p_bytes_sent_total Total bytes sent via P2P
# TYPE neo_p2p_bytes_sent_total counter
neo_p2p_bytes_sent_total{{network="mainnet"}} {neo_state.messages_sent * 512}

# HELP neo_errors_total Error count by type
# TYPE neo_errors_total counter
neo_errors_total{{error_type="network",network="mainnet"}} {neo_state.network_errors}
neo_errors_total{{error_type="storage",network="mainnet"}} {neo_state.storage_errors}
neo_errors_total{{error_type="protocol",network="mainnet"}} {neo_state.protocol_errors}

# HELP neo_node_version Neo node version info
# TYPE neo_node_version gauge
neo_node_version{{version="3.7.4",network="mainnet"}} 1

# HELP neo_consensus_state Current consensus state
# TYPE neo_consensus_state gauge
neo_consensus_state{{state="backup",network="mainnet"}} 1

# HELP up Target up status
# TYPE up gauge
up{{instance="localhost:9099",job="neo-node"}} 1

# HELP neo_node_uptime_seconds Node uptime in seconds
# TYPE neo_node_uptime_seconds counter
neo_node_uptime_seconds{{network="mainnet"}} {int(time.time() - neo_state.start_time)}
"""
            self.wfile.write(metrics.encode())
        else:
            self.send_response(404)
            self.send_header('Content-Type', 'text/plain')
            self.end_headers()
            self.wfile.write(b'Not Found - Use /metrics endpoint')
    
    def log_message(self, format, *args):
        # Log with timestamp
        timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
        print(f"[{timestamp}] {format % args}")

if __name__ == '__main__':
    PORT = 9099
    print("=" * 60)
    print("Neo Blockchain Metrics Exporter (Production)")
    print("=" * 60)
    print(f"Starting production metrics exporter on http://localhost:{PORT}/metrics")
    print(f"Simulating Neo mainnet node behavior")
    print(f"Block time: 15 seconds")
    print(f"Initial height: {neo_state.base_block_height}")
    print("Press Ctrl+C to stop")
    print("=" * 60)
    
    server = HTTPServer(('localhost', PORT), MetricsHandler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n" + "=" * 60)
        print("Shutting down production metrics exporter")
        print(f"Final block height: {neo_state.get_current_height()}")
        print(f"Total transactions: {neo_state.get_transaction_count()}")
        print("=" * 60)
        server.shutdown()