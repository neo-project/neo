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
            state_local = max(height - random.randint(0, 12), 0)
            state_validated = max(state_local - random.randint(0, 4), 0)
            state_root_lag = max(height - state_validated, 0)
            state_apply_ms = round(random.uniform(8, 40), 2)
            state_commit_ms = round(random.uniform(3, 20), 2)
            state_health = 0 if state_local == 0 else round(state_validated / state_local, 3)
            state_validations_total = 54000 + random.randint(0, 1000)
            state_validation_errors_total = random.randint(0, 25)
            rpc_active = random.randint(0, 6)
            rpc_methods = ["getblock", "getrawtransaction", "invokefunction", "submitblock"]
            rpc_counts = {method: 25000 + random.randint(0, 2500) for method in rpc_methods}
            rpc_errors = {method: random.randint(0, 60) for method in rpc_methods}
            rpc_duration_sum = {method: round(random.uniform(4000, 12000), 2) for method in rpc_methods}
            rpc_success = {method: max(rpc_counts[method] - rpc_errors[method], 0) for method in rpc_methods}
            superinstruction_plans = random.randint(3, 18)
            
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

# HELP neo_mempool_conflicts_total Mempool conflicts detected
# TYPE neo_mempool_conflicts_total counter
neo_mempool_conflicts_total{{instance="localhost:9099"}} {random.randint(100, 500)}

# HELP neo_mempool_batch_removed_size Transactions removed per batch
# TYPE neo_mempool_batch_removed_size histogram
neo_mempool_batch_removed_size_bucket{{le="1",instance="localhost:9099"}} {random.randint(10, 50)}
neo_mempool_batch_removed_size_bucket{{le="5",instance="localhost:9099"}} {random.randint(60, 120)}
neo_mempool_batch_removed_size_bucket{{le="10",instance="localhost:9099"}} {random.randint(130, 200)}
neo_mempool_batch_removed_size_bucket{{le="25",instance="localhost:9099"}} {random.randint(210, 240)}
neo_mempool_batch_removed_size_bucket{{le="+Inf",instance="localhost:9099"}} {random.randint(250, 280)}
neo_mempool_batch_removed_size_sum{{instance="localhost:9099"}} {random.randint(1000, 2500)}
neo_mempool_batch_removed_size_count{{instance="localhost:9099"}} {random.randint(250, 280)}

# HELP neo_mempool_capacity_ratio Mempool usage ratio
# TYPE neo_mempool_capacity_ratio gauge
neo_mempool_capacity_ratio{{instance="localhost:9099"}} {round(random.uniform(0.1, 0.8), 3)}

# HELP neo_consensus_round Latest consensus block height
# TYPE neo_consensus_round gauge
neo_consensus_round{{instance="localhost:9099"}} {height}

# HELP neo_consensus_view Current consensus view number
# TYPE neo_consensus_view gauge
neo_consensus_view{{instance="localhost:9099"}} {random.randint(0, 3)}

# HELP neo_consensus_state Current primary validator index
# TYPE neo_consensus_state gauge
neo_consensus_state{{instance="localhost:9099"}} {random.randint(0, 6)}

# HELP neo_consensus_time_to_finality Consensus time to finality in milliseconds
# TYPE neo_consensus_time_to_finality gauge
neo_consensus_time_to_finality{{instance="localhost:9099"}} {random.randint(1500, 4500)}

# HELP neo_consensus_view_changes_total Consensus view changes
# TYPE neo_consensus_view_changes_total counter
neo_consensus_view_changes_total{{instance="localhost:9099",reason="Timeout"}} {random.randint(0, 20)}

# HELP neo_consensus_messages_sent_total Consensus messages sent
# TYPE neo_consensus_messages_sent_total counter
neo_consensus_messages_sent_total{{instance="localhost:9099",type="PrepareRequest"}} {random.randint(500, 800)}
neo_consensus_messages_sent_total{{instance="localhost:9099",type="PrepareResponse"}} {random.randint(800, 1200)}
neo_consensus_messages_sent_total{{instance="localhost:9099",type="Commit"}} {random.randint(700, 1100)}
neo_consensus_messages_sent_total{{instance="localhost:9099",type="ChangeView"}} {random.randint(50, 120)}
neo_consensus_messages_sent_total{{instance="localhost:9099",type="RecoveryRequest"}} {random.randint(10, 40)}
neo_consensus_messages_sent_total{{instance="localhost:9099",type="RecoveryMessage"}} {random.randint(10, 40)}

# HELP neo_consensus_messages_received_total Consensus messages received
# TYPE neo_consensus_messages_received_total counter
neo_consensus_messages_received_total{{instance="localhost:9099",type="PrepareRequest"}} {random.randint(500, 800)}
neo_consensus_messages_received_total{{instance="localhost:9099",type="PrepareResponse"}} {random.randint(800, 1200)}
neo_consensus_messages_received_total{{instance="localhost:9099",type="Commit"}} {random.randint(700, 1100)}
neo_consensus_messages_received_total{{instance="localhost:9099",type="ChangeView"}} {random.randint(50, 120)}
neo_consensus_messages_received_total{{instance="localhost:9099",type="RecoveryRequest"}} {random.randint(10, 40)}
neo_consensus_messages_received_total{{instance="localhost:9099",type="RecoveryMessage"}} {random.randint(10, 40)}

# HELP neo_vm_trace_hot_ratio Hot trace hit ratio per script
# TYPE neo_vm_trace_hot_ratio gauge
neo_vm_trace_hot_ratio{{instance="localhost:9099",script="0x4F2B..A1",sequence="PUSH1 PUSH1 ADD MUL DIV",hits="{random.randint(150, 400)}",total_instructions="{random.randint(500, 1200)}",last_seen="{int(time.time())}"}} {round(random.uniform(0.3, 0.8), 3)}
neo_vm_trace_hot_ratio{{instance="localhost:9099",script="0x9CD1..2F",sequence="SYSCALL SWAP ADD RET",hits="{random.randint(80, 200)}",total_instructions="{random.randint(300, 800)}",last_seen="{int(time.time())}"}} {round(random.uniform(0.2, 0.6), 3)}

# HELP neo_vm_trace_hot_hits Hot trace hit counts per script
# TYPE neo_vm_trace_hot_hits gauge
neo_vm_trace_hot_hits{{instance="localhost:9099",script="0x4F2B..A1",sequence="PUSH1 PUSH1 ADD MUL DIV",total_instructions="{random.randint(500, 1200)}",last_seen="{int(time.time())}"}} {random.randint(150, 400)}
neo_vm_trace_hot_hits{{instance="localhost:9099",script="0x9CD1..2F",sequence="SYSCALL SWAP ADD RET",total_instructions="{random.randint(300, 800)}",last_seen="{int(time.time())}"}} {random.randint(80, 200)}

# HELP neo_vm_trace_max_hot_ratio Maximum hot trace hit ratio across scripts
# TYPE neo_vm_trace_max_hot_ratio gauge
neo_vm_trace_max_hot_ratio{{instance="localhost:9099"}} {round(random.uniform(0.3, 0.85), 3)}

# HELP neo_vm_trace_max_hot_hits Maximum hot trace hit count across scripts
# TYPE neo_vm_trace_max_hot_hits gauge
neo_vm_trace_max_hot_hits{{instance="localhost:9099"}} {random.randint(200, 500)}

# HELP neo_vm_trace_profile_count Number of hot trace profiles
# TYPE neo_vm_trace_profile_count gauge
neo_vm_trace_profile_count{{instance="localhost:9099"}} {random.randint(2, 6)}

# HELP neo_vm_superinstruction_plan_count Super-instruction plans derived from profiling
# TYPE neo_vm_superinstruction_plan_count gauge
neo_vm_superinstruction_plan_count{{instance="localhost:9099"}} {superinstruction_plans}

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
