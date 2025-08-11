# Neo Blockchain Operations Runbooks

## Table of Contents
1. [Node Down](#node-down)
2. [Sync Stalled](#sync-stalled)
3. [No Peers Connected](#no-peers-connected)
4. [High CPU Usage](#high-cpu-usage)
5. [High Memory Usage](#high-memory-usage)
6. [MemPool Full](#mempool-full)
7. [High Error Rate](#high-error-rate)
8. [Consensus Issues](#consensus-issues)

---

## Node Down
**Alert**: `NeoNodeDown`
**Severity**: CRITICAL
**Team**: blockchain-ops

### Symptoms
- Node not responding to health checks
- Metrics collection stopped
- No block processing activity

### Diagnosis Steps
1. **Check node process**:
   ```bash
   ps aux | grep neo-cli
   systemctl status neo-node
   ```

2. **Check logs**:
   ```bash
   tail -n 100 /var/log/neo/node.log
   journalctl -u neo-node -n 100
   ```

3. **Check system resources**:
   ```bash
   df -h
   free -m
   top -b -n 1
   ```

4. **Check network connectivity**:
   ```bash
   ping -c 4 seed1.neo.org
   netstat -tuln | grep 10333
   ```

### Resolution Steps

#### Immediate Actions
1. **Restart the node**:
   ```bash
   systemctl restart neo-node
   # OR
   supervisorctl restart neo-node
   ```

2. **Monitor startup**:
   ```bash
   tail -f /var/log/neo/node.log
   ```

3. **Verify metrics are flowing**:
   ```bash
   curl -s http://localhost:9090/metrics | grep neo_blockchain_height
   ```

#### If restart fails:
1. **Check disk space**:
   ```bash
   # Clear logs if needed
   find /var/log -type f -name "*.log" -mtime +7 -delete
   
   # Clear temp files
   rm -rf /tmp/neo-*
   ```

2. **Check for port conflicts**:
   ```bash
   lsof -i :10333
   lsof -i :9090
   ```

3. **Verify configuration**:
   ```bash
   neo-cli --config-check
   cat /etc/neo/config.json | jq .
   ```

4. **Force recovery**:
   ```bash
   # Backup current state
   cp -r /var/lib/neo /var/lib/neo.backup.$(date +%Y%m%d)
   
   # Clear lock files
   rm -f /var/lib/neo/*.lock
   
   # Start with recovery mode
   neo-cli --recover
   ```

### Escalation
- After 15 minutes: Page on-call engineer
- After 30 minutes: Escalate to team lead
- After 1 hour: Consider failover to backup node

---

## Sync Stalled
**Alert**: `BlockchainSyncStalled`
**Severity**: CRITICAL
**Team**: blockchain-ops

### Symptoms
- Block height not increasing
- Node marked as syncing but no progress
- Peer connections active but no data flow

### Diagnosis Steps
1. **Check sync status**:
   ```bash
   neo-cli show state
   neo-cli show height
   ```

2. **Check peer quality**:
   ```bash
   neo-cli show peers
   netstat -an | grep ESTABLISHED | grep 10333
   ```

3. **Check resource bottlenecks**:
   ```bash
   iostat -x 1 10
   sar -n DEV 1 10
   ```

### Resolution Steps

#### Immediate Actions
1. **Reset peer connections**:
   ```bash
   # Disconnect all peers
   neo-cli disconnect all
   
   # Wait 30 seconds
   sleep 30
   
   # Reconnect to seed nodes
   neo-cli connect seed1.neo.org:10333
   neo-cli connect seed2.neo.org:10333
   ```

2. **Clear bad peers**:
   ```bash
   # Show banned peers
   neo-cli show banned
   
   # Clear ban list
   neo-cli clear banned
   ```

3. **Force resync from checkpoint**:
   ```bash
   # Get latest checkpoint
   CHECKPOINT=$(curl -s https://neo.org/checkpoints/latest.json | jq -r .height)
   
   # Resync from checkpoint
   neo-cli resync --from $CHECKPOINT
   ```

#### Advanced Recovery
1. **Database repair**:
   ```bash
   # Stop node
   systemctl stop neo-node
   
   # Repair database
   neo-cli db repair
   
   # Verify integrity
   neo-cli db verify
   
   # Restart
   systemctl start neo-node
   ```

2. **Full resync** (last resort):
   ```bash
   # Backup current state
   mv /var/lib/neo/chain /var/lib/neo/chain.old
   
   # Download snapshot
   wget https://neo.org/snapshots/latest.tar.gz
   tar -xzf latest.tar.gz -C /var/lib/neo/
   
   # Restart and sync
   systemctl restart neo-node
   ```

---

## No Peers Connected
**Alert**: `NoPeersConnected`
**Severity**: CRITICAL
**Team**: network-ops

### Symptoms
- Zero peer connections
- Network isolation
- No block or transaction propagation

### Diagnosis Steps
1. **Check network interfaces**:
   ```bash
   ip addr show
   ip route show
   ```

2. **Check firewall rules**:
   ```bash
   iptables -L -n
   ufw status verbose
   ```

3. **Test connectivity**:
   ```bash
   nc -zv seed1.neo.org 10333
   telnet seed1.neo.org 10333
   ```

### Resolution Steps

#### Immediate Actions
1. **Check firewall**:
   ```bash
   # Allow Neo P2P port
   ufw allow 10333/tcp
   iptables -A INPUT -p tcp --dport 10333 -j ACCEPT
   ```

2. **Manual peer connection**:
   ```bash
   # Connect to known good peers
   neo-cli connect seed1.neo.org:10333
   neo-cli connect seed2.neo.org:10333
   neo-cli connect 1.2.3.4:10333  # Known good peer IP
   ```

3. **Check DNS resolution**:
   ```bash
   nslookup seed1.neo.org
   dig seed1.neo.org
   ```

#### Network Troubleshooting
1. **Reset network stack**:
   ```bash
   # Flush routing table
   ip route flush cache
   
   # Restart network service
   systemctl restart networking
   ```

2. **Check NAT/Port forwarding**:
   ```bash
   # If behind NAT, ensure port forwarding
   curl -s https://api.ipify.org  # Get external IP
   ```

---

## High CPU Usage
**Alert**: `HighCPUUsage`
**Severity**: WARNING
**Team**: platform-ops

### Symptoms
- CPU usage > 80% sustained
- Slow response times
- High system load

### Diagnosis Steps
1. **Identify CPU consumers**:
   ```bash
   top -b -n 1 | head -20
   ps aux --sort=-pcpu | head -10
   ```

2. **Check thread activity**:
   ```bash
   top -H -p $(pgrep neo-cli)
   strace -c -p $(pgrep neo-cli) -f
   ```

3. **Profile the application**:
   ```bash
   perf top -p $(pgrep neo-cli)
   ```

### Resolution Steps

#### Immediate Mitigation
1. **Reduce load**:
   ```bash
   # Limit peer connections
   neo-cli set max-peers 10
   
   # Reduce mempool size
   neo-cli set mempool-size 5000
   ```

2. **CPU affinity** (if multi-core):
   ```bash
   taskset -cp 0-3 $(pgrep neo-cli)
   ```

3. **Nice priority**:
   ```bash
   renice -n 10 -p $(pgrep neo-cli)
   ```

#### Long-term Solutions
1. **Optimize configuration**:
   ```json
   {
     "ApplicationConfiguration": {
       "MaxTransactionsPerBlock": 500,
       "MemPoolSize": 5000,
       "MaxPeers": 20
     }
   }
   ```

2. **Scale horizontally**: Deploy additional nodes

---

## High Memory Usage
**Alert**: `HighMemoryUsage`
**Severity**: WARNING
**Team**: platform-ops

### Symptoms
- Memory usage > 4GB
- Potential OOM killer activation
- Swap usage increasing

### Diagnosis Steps
1. **Memory analysis**:
   ```bash
   free -h
   vmstat 1 10
   cat /proc/$(pgrep neo-cli)/status | grep -i vm
   ```

2. **Check for memory leaks**:
   ```bash
   pmap -x $(pgrep neo-cli)
   ```

### Resolution Steps

#### Immediate Actions
1. **Clear caches**:
   ```bash
   # Clear system caches
   sync && echo 3 > /proc/sys/vm/drop_caches
   
   # Restart node to clear application memory
   systemctl restart neo-node
   ```

2. **Adjust memory limits**:
   ```bash
   # Set memory limit in systemd
   systemctl edit neo-node
   # Add: MemoryLimit=4G
   ```

3. **Reduce memory usage**:
   ```bash
   # Reduce cache sizes
   neo-cli set cache-size 1000
   neo-cli set mempool-size 5000
   ```

---

## MemPool Full
**Alert**: `MemPoolNearlyFull`
**Severity**: WARNING
**Team**: blockchain-ops

### Symptoms
- MemPool at >80% capacity
- Transaction processing delays
- Increased transaction fees

### Diagnosis Steps
1. **Check mempool status**:
   ```bash
   neo-cli show mempool
   neo-cli show mempool --verbose
   ```

2. **Analyze transaction types**:
   ```bash
   neo-cli show mempool --group-by-type
   ```

### Resolution Steps

#### Immediate Actions
1. **Clear low-fee transactions**:
   ```bash
   # Remove transactions with fee < threshold
   neo-cli mempool clear --fee-threshold 0.001
   ```

2. **Increase mempool size** (temporary):
   ```bash
   neo-cli set mempool-size 100000
   ```

3. **Prioritize high-fee transactions**:
   ```bash
   neo-cli set mempool-priority fee
   ```

---

## High Error Rate
**Alert**: `HighErrorRate`
**Severity**: WARNING
**Team**: blockchain-ops

### Symptoms
- Elevated error counts in logs
- Failed transaction verifications
- Protocol or network errors

### Diagnosis Steps
1. **Analyze error patterns**:
   ```bash
   grep ERROR /var/log/neo/node.log | tail -100
   grep ERROR /var/log/neo/node.log | awk '{print $5}' | sort | uniq -c
   ```

2. **Check specific error types**:
   ```bash
   curl -s localhost:9090/metrics | grep errors_total
   ```

### Resolution Steps

#### Error Type Specific Actions

**Protocol Errors**:
```bash
# Update protocol configuration
neo-cli update-protocol

# Verify protocol version
neo-cli show protocol
```

**Network Errors**:
```bash
# Reset network connections
neo-cli disconnect all
sleep 30
neo-cli connect seed1.neo.org:10333
```

**Storage Errors**:
```bash
# Check disk health
smartctl -a /dev/sda
fsck -n /dev/sda1

# Repair database
neo-cli db repair
```

---

## Consensus Issues
**Alert**: `ConsensusNodeOffline` or `ConsensusDelayed`
**Severity**: CRITICAL
**Team**: consensus-ops

### Symptoms
- Consensus node not participating
- High time to finality
- Consensus round failures

### Diagnosis Steps
1. **Check consensus status**:
   ```bash
   neo-cli show consensus
   neo-cli show consensus --detailed
   ```

2. **Verify keys and permissions**:
   ```bash
   neo-cli wallet verify
   ls -la /var/lib/neo/consensus/
   ```

### Resolution Steps

#### Immediate Actions
1. **Restart consensus service**:
   ```bash
   systemctl restart neo-consensus
   ```

2. **Verify consensus configuration**:
   ```bash
   cat /etc/neo/consensus.json | jq .
   ```

3. **Check time synchronization**:
   ```bash
   timedatectl status
   chronyc sources
   ```

4. **Force resync time**:
   ```bash
   systemctl stop ntp
   ntpdate -s time.nist.gov
   systemctl start ntp
   ```

---

## Emergency Contacts

### Escalation Chain
1. **Level 1**: On-call Engineer (0-15 min)
   - Phone: +1-xxx-xxx-xxxx
   - Slack: #neo-oncall

2. **Level 2**: Team Lead (15-30 min)
   - Phone: +1-xxx-xxx-xxxx
   - Slack: #neo-escalation

3. **Level 3**: Infrastructure Director (30+ min)
   - Phone: +1-xxx-xxx-xxxx
   - Email: emergency@neo.org

### External Resources
- Neo Core Team: support@neo.org
- Community Discord: https://discord.gg/neo
- Status Page: https://status.neo.org

---

## Post-Incident Procedures

1. **Document the incident**:
   - Timeline of events
   - Actions taken
   - Resolution steps
   - Time to resolution

2. **Update runbooks** with new findings

3. **Schedule post-mortem** within 48 hours

4. **Share learnings** with the team

---

## Automation Scripts

### Health Check Script
```bash
#!/bin/bash
# neo-health-check.sh

HEIGHT=$(neo-cli show height | grep "Current" | awk '{print $3}')
PEERS=$(neo-cli show peers | grep "Connected" | wc -l)
CPU=$(top -b -n1 | grep "neo-cli" | awk '{print $9}')

if [ "$PEERS" -lt 3 ]; then
  echo "WARNING: Low peer count: $PEERS"
  neo-cli connect seed1.neo.org:10333
fi

if [ "$HEIGHT" -eq "$LAST_HEIGHT" ]; then
  echo "ERROR: Block height stuck at $HEIGHT"
  systemctl restart neo-node
fi

echo "Height: $HEIGHT, Peers: $PEERS, CPU: $CPU%"
```

### Auto-recovery Script
```bash
#!/bin/bash
# neo-auto-recover.sh

MAX_RETRIES=3
RETRY=0

while [ $RETRY -lt $MAX_RETRIES ]; do
  if systemctl is-active neo-node > /dev/null; then
    echo "Node is running"
    exit 0
  fi
  
  echo "Attempting to start node (attempt $((RETRY+1))/$MAX_RETRIES)"
  systemctl start neo-node
  sleep 30
  RETRY=$((RETRY+1))
done

echo "Failed to start node after $MAX_RETRIES attempts"
# Send alert
curl -X POST https://alerts.example.com/neo-node-down -d "host=$(hostname)"
exit 1
```