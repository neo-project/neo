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
neo_blockchain_height{{network="mainnet",instance="localhost:9099"}} {height}

# HELP neo_p2p_connected_peers Number of connected peers
# TYPE neo_p2p_connected_peers gauge
neo_p2p_connected_peers{{network="mainnet",instance="localhost:9099"}} {peers}

# HELP neo_p2p_max_connected_peers Maximum number of connected peers
# TYPE neo_p2p_max_connected_peers gauge
neo_p2p_max_connected_peers{{network="mainnet",instance="localhost:9099"}} 50

# HELP neo_mempool_size Current mempool size
# TYPE neo_mempool_size gauge
neo_mempool_size{{network="mainnet",instance="localhost:9099"}} {mempool}

# HELP neo_mempool_verified_count Verified transactions in mempool
# TYPE neo_mempool_verified_count gauge
neo_mempool_verified_count{{network="mainnet",instance="localhost:9099"}} {int(mempool * 0.8)}

# HELP neo_mempool_unverified_count Unverified transactions in mempool
# TYPE neo_mempool_unverified_count gauge
neo_mempool_unverified_count{{network="mainnet",instance="localhost:9099"}} {int(mempool * 0.2)}

# HELP neo_blocks_processed_total Total blocks processed
# TYPE neo_blocks_processed_total counter
neo_blocks_processed_total{{network="mainnet",instance="localhost:9099"}} {height}

# HELP neo_transactions_processed_total Total transactions processed
# TYPE neo_transactions_processed_total counter
neo_transactions_processed_total{{network="mainnet",instance="localhost:9099"}} {height * 19}

# HELP neo_block_processing_time Block processing time histogram
# TYPE neo_block_processing_time histogram
neo_block_processing_time_bucket{{le="50",instance="localhost:9099"}} 1000
neo_block_processing_time_bucket{{le="100",instance="localhost:9099"}} 1800
neo_block_processing_time_bucket{{le="250",instance="localhost:9099"}} 2200
neo_block_processing_time_bucket{{le="500",instance="localhost:9099"}} 2400
neo_block_processing_time_bucket{{le="1000",instance="localhost:9099"}} 2450
neo_block_processing_time_bucket{{le="+Inf",instance="localhost:9099"}} 2500

# HELP process_cpu_usage Process CPU usage
# TYPE process_cpu_usage gauge
process_cpu_usage{{instance="localhost:9099"}} {cpu}

# HELP system_cpu_usage System CPU usage
# TYPE system_cpu_usage gauge
system_cpu_usage{{instance="localhost:9099"}} {cpu * 0.5}

# HELP process_memory_working_set Process memory working set
# TYPE process_memory_working_set gauge
process_memory_working_set{{instance="localhost:9099"}} {memory}

# HELP process_virtual_memory_size Process virtual memory
# TYPE process_virtual_memory_size gauge
process_virtual_memory_size{{instance="localhost:9099"}} {memory * 2}

# HELP dotnet_gc_heap_size GC heap size
# TYPE dotnet_gc_heap_size gauge
dotnet_gc_heap_size{{instance="localhost:9099"}} {int(memory * 0.6)}

# HELP process_threads_count Thread count
# TYPE process_threads_count gauge
process_threads_count{{instance="localhost:9099"}} {random.randint(50, 150)}

# HELP dotnet_gc_collections_total GC collections
# TYPE dotnet_gc_collections_total counter
dotnet_gc_collections_total{{generation="0",instance="localhost:9099"}} {random.randint(1000, 5000)}
dotnet_gc_collections_total{{generation="1",instance="localhost:9099"}} {random.randint(100, 500)}
dotnet_gc_collections_total{{generation="2",instance="localhost:9099"}} {random.randint(10, 50)}

# HELP neo_p2p_messages_received_total P2P messages received
# TYPE neo_p2p_messages_received_total counter
neo_p2p_messages_received_total{{instance="localhost:9099"}} {random.randint(100000, 500000)}

# HELP neo_p2p_messages_sent_total P2P messages sent
# TYPE neo_p2p_messages_sent_total counter
neo_p2p_messages_sent_total{{instance="localhost:9099"}} {random.randint(100000, 500000)}

# HELP neo_p2p_failed_messages_total Failed P2P messages
# TYPE neo_p2p_failed_messages_total counter
neo_p2p_failed_messages_total{{instance="localhost:9099"}} {random.randint(10, 100)}

# HELP neo_errors_total Error count by type
# TYPE neo_errors_total counter
neo_errors_total{{error_type="network",instance="localhost:9099"}} {random.randint(0, 10)}
neo_errors_total{{error_type="storage",instance="localhost:9099"}} {random.randint(0, 5)}
neo_errors_total{{error_type="protocol",instance="localhost:9099"}} {random.randint(0, 3)}

# HELP up Target up
# TYPE up gauge
up{{instance="localhost:9099",job="neo-node"}} 1
"""
            self.wfile.write(metrics.encode())
        else:
            self.send_response(404)
            self.end_headers()
    
    def log_message(self, format, *args):
        return  # Suppress logs

if __name__ == '__main__':
    print("Starting metrics simulator on http://localhost:9099/metrics")
    print("Press Ctrl+C to stop")
    server = HTTPServer(('localhost', 9099), MetricsHandler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down metrics simulator")
        server.shutdown()
