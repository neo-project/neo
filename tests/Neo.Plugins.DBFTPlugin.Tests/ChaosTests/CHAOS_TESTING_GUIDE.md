# DBFT Chaos Testing Framework - Complete Guide

This guide explains how to use the comprehensive chaos testing framework to validate DBFT consensus robustness under various failure conditions and attacks.

## Framework Overview

The chaos testing framework provides:
- **Systematic validation** of DBFT consensus resilience 
- **Byzantine attack simulation** with multiple attack vectors
- **Network failure simulation** (partitions, delays, message loss)
- **Performance analysis** under adversarial conditions
- **Reproducible testing** with seed-based randomization

## Quick Start

### Running Basic Robustness Tests

```bash
# Basic validation with minimal chaos
CHAOS_MESSAGE_LOSS=0.05 CHAOS_MAX_LATENCY=500 dotnet test --filter "BasicConsensusWithMinorChaos"

# Single node failure test
dotnet test --filter "Test_FaultTolerance_SingleNodeFailure"

# Network partition test  
dotnet test --filter "Test_NetworkPartition_MajorityCanContinue"
```

### Environment Configuration

```bash
export CHAOS_SEED=123456              # Reproducible randomization
export CHAOS_MESSAGE_LOSS=0.15        # 15% message loss
export CHAOS_MAX_LATENCY=1000         # 1 second max latency
export CHAOS_NODE_FAILURE=0.05        # 5% node failure probability
export CHAOS_BYZANTINE=0.02           # 2% byzantine behavior
export CHAOS_PARTITION=0.05           # 5% partition probability
```

## Core Test Categories

### 1. Byzantine Fault Tolerance Tests

**Purpose**: Validate the fundamental BFT property f < n/3

```csharp
[TestMethod]
public void Test_FaultTolerance_MaximumTolerableFailures()
{
    // With 7 nodes, DBFT can tolerate up to f=2 failures
    InitializeConsensusNodes(7);
    
    // Fail exactly f nodes  
    for (int i = 0; i < 2; i++)
        SimulateNodeFailure(consensusServices[i]);
    
    var success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.70);
    Assert.IsTrue(success, "Should handle maximum tolerable failures");
}
```

**Expected Results**:
- ✅ n=4, f=1: Consensus continues with 1 failure
- ✅ n=7, f=2: Consensus continues with 2 failures  
- ❌ n=4, f=2: Consensus should fail (f+1 failures)

### 2. Byzantine Attack Simulation

**Purpose**: Test resistance to malicious validator behavior

```csharp
[TestMethod] 
public void Test_ByzantineNode_DoubleVoting()
{
    InitializeConsensusNodes(7);
    
    // Make 2 nodes byzantine (maximum tolerable)
    faultInjector.EnableByzantineBehavior(consensusServices[0], ByzantineType.DoubleVoting);
    faultInjector.EnableByzantineBehavior(consensusServices[1], ByzantineType.ConflictingMessages);
    
    var success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.60);
    Assert.IsTrue(success, "Should resist byzantine attacks");
}
```

**Attack Types**:
- **Double Voting**: Node votes for multiple conflicting blocks
- **Conflicting Messages**: Sends different data to different nodes
- **Wrong View**: Messages with incorrect view numbers
- **Protocol Violation**: Ignores consensus rules entirely

### 3. Network Partition Tests

**Purpose**: Validate consistency during network splits

```csharp
[TestMethod]
public void Test_NetworkPartition_Scenarios()
{
    InitializeConsensusNodes(7);
    
    var partition1 = consensusServices.Take(4).ToList(); // Majority
    var partition2 = consensusServices.Skip(4).ToList(); // Minority
    
    faultInjector.CreateNetworkPartition(partition1, partition2);
    
    var success = VerifyConsensusResilience(TimeSpan.FromSeconds(45), 0.60);
    Assert.IsTrue(success, "Majority partition should continue");
}
```

**Scenarios**:
- **Majority vs Minority**: Only majority makes progress
- **Equal Splits**: No progress (prevents split-brain)
- **Healing**: Normal operation after partition resolves

### 4. Message-Level Attacks

**Purpose**: Test robustness to network-level interference

```csharp
[TestMethod]
public void Test_MessageCorruption_And_Loss()
{
    InitializeConsensusNodes(4);
    
    // High message loss and corruption
    config.MessageLossProbability = 0.30;
    config.MessageCorruptionProbability = 0.15;
    
    var success = VerifyConsensusResilience(TimeSpan.FromSeconds(90), 0.50);
    Assert.IsTrue(success, "Should handle severe message interference");
}
```

**Types**:
- **Selective Loss**: Target specific message types (PrepareRequest, Commit, etc.)
- **Corruption**: Modify message contents to test validation
- **Delays**: Introduce artificial latency to test timeouts
- **Reordering**: Deliver messages out of sequence

### 5. Performance Under Attack

**Purpose**: Ensure usability during adversarial conditions

```csharp
[TestMethod]
public void Test_ThroughputUnderChaos()
{
    InitializeConsensusNodes(7);
    
    // Apply combined stress
    config.MessageLossProbability = 0.15;
    config.MaxLatencyMs = 1000;
    
    var startTime = DateTime.UtcNow;
    var success = VerifyConsensusResilience(TimeSpan.FromSeconds(120), 0.65);
    var endTime = DateTime.UtcNow;
    
    var summary = metrics.GetSummary();
    var throughput = summary.ConsensusSuccessCount / (endTime - startTime).TotalSeconds;
    
    Assert.IsTrue(throughput > 0.1, $"Should maintain throughput > 0.1/sec, got {throughput:F3}");
}
```

## Success Criteria and Thresholds

### Robustness Levels

| Scenario | Min Success Rate | Justification |
|----------|------------------|---------------|
| Minor chaos (5% loss) | 90% | Near-optimal conditions |
| Single node failure | 80% | Standard resilience |
| Maximum f failures | 70% | Theoretical limit |
| High message loss (40%) | 40% | Extreme conditions |
| Network partitions | 60% | Majority partition only |
| Byzantine attacks | 60% | Security vs performance |

### Performance Metrics

- **Consensus Rate**: Blocks per second under stress
- **View Change Frequency**: Indicator of stability
- **Recovery Time**: How quickly system adapts to failures
- **Message Overhead**: Network efficiency under attack

## Advanced Scenarios

### 1. Progressive Chaos Testing

```csharp
[TestMethod]
public void Test_ProgressiveChaosIntensity()
{
    InitializeConsensusNodes(7);
    
    for (double intensity = 0.05; intensity <= 0.40; intensity += 0.05)
    {
        config.MessageLossProbability = intensity;
        
        var success = VerifyConsensusResilience(TimeSpan.FromSeconds(30), 0.5);
        
        Console.WriteLine($"Intensity {intensity:P0}: {(success ? "PASS" : "FAIL")}");
        
        if (!success && intensity < 0.30)
        {
            Assert.Fail($"Failed at {intensity:P0} intensity - below expected threshold");
        }
    }
}
```

### 2. Real-World Attack Simulation

```csharp
[TestMethod]
public void Test_CoordinatedAttack()
{
    InitializeConsensusNodes(10); // Larger network
    
    // Simulate coordinated attack: Byzantine nodes + network interference
    faultInjector.EnableByzantineBehavior(consensusServices[0], ByzantineType.DoubleVoting);
    faultInjector.EnableByzantineBehavior(consensusServices[1], ByzantineType.ConflictingMessages);
    faultInjector.EnableByzantineBehavior(consensusServices[2], ByzantineType.WrongViewNumber);
    
    // Add network chaos
    config.MessageLossProbability = 0.20;
    config.MaxLatencyMs = 2000;
    
    // Targeted message delays
    faultInjector.SetMessageTypeDelay(typeof(PrepareRequest), TimeSpan.FromSeconds(3));
    
    var success = VerifyConsensusResilience(TimeSpan.FromMinutes(3), 0.40);
    Assert.IsTrue(success, "Should survive coordinated attack");
}
```

### 3. Recovery Testing

```csharp
[TestMethod]  
public void Test_GracefulDegradationAndRecovery()
{
    InitializeConsensusNodes(7);
    
    // Phase 1: Normal operation
    var phase1Success = VerifyConsensusResilience(TimeSpan.FromSeconds(30), 0.90);
    
    // Phase 2: Introduce failures
    SimulateNodeFailure(consensusServices[0]);
    SimulateNodeFailure(consensusServices[1]);
    
    var phase2Success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.60);
    
    // Phase 3: Recovery
    Task.Delay(5000).ContinueWith(_ => {
        SimulateNodeRecovery(consensusServices[0]);
        SimulateNodeRecovery(consensusServices[1]);
    });
    
    var phase3Success = VerifyConsensusResilience(TimeSpan.FromSeconds(45), 0.80);
    
    Assert.IsTrue(phase1Success && phase2Success && phase3Success, 
        "Should demonstrate graceful degradation and recovery");
}
```

## Interpreting Results

### Success Indicators
- **High success rates** under expected failure levels
- **Graceful degradation** as chaos intensity increases  
- **No safety violations** (conflicting blocks, double spending)
- **Reasonable throughput** maintained during attacks

### Failure Patterns
- **Sudden drops** in success rate may indicate threshold bugs
- **View change storms** suggest instability
- **Inconsistent results** across runs indicate race conditions
- **Resource leaks** show up as degrading performance over time

### Performance Analysis

```csharp
var summary = metrics.GetSummary();

Console.WriteLine($"=== CONSENSUS PERFORMANCE ANALYSIS ===");
Console.WriteLine($"Total Duration: {summary.Duration.TotalSeconds:F1}s");
Console.WriteLine($"Success Rate: {summary.ConsensusSuccessRate:P1}");
Console.WriteLine($"Throughput: {summary.ConsensusSuccessCount / summary.Duration.TotalSeconds:F2} consensus/sec");
Console.WriteLine($"View Changes: {summary.ViewChangeCount}");
Console.WriteLine($"Message Loss: {summary.MessageLossCount}");
Console.WriteLine($"Node Failures: {summary.NodeFailureCount}");
Console.WriteLine($"Average Latency: {summary.AverageLatencyMs:F1}ms");
```

## Integration with CI/CD

### Nightly Testing
```yaml
- name: DBFT Robustness Tests
  run: |
    export CHAOS_SEED=${{ github.run_number }}
    dotnet test --filter "Category=Robustness" --logger "trx;LogFileName=robustness-results.trx"
    
- name: Archive Results
  uses: actions/upload-artifact@v3
  with:
    name: robustness-test-results
    path: "**/robustness-results.trx"
```

### Performance Regression Detection
```bash
# Baseline performance
dotnet test --filter "ThroughputUnderChaos" --logger "console;verbosity=detailed" > baseline.log

# Compare with previous runs
if [ $(grep "throughput:" baseline.log | cut -d: -f2) < 0.05 ]; then
    echo "Performance regression detected!"
    exit 1
fi
```

## Extending the Framework

### Adding New Attack Vectors

1. **Define Attack Type**:
```csharp
public enum ByzantineType 
{
    // ... existing types
    TimingAttack,        // New attack type
    ResourceExhaustion
}
```

2. **Implement Attack Logic**:
```csharp
private ExtensiblePayload CreateTimingAttackMessage(ExtensiblePayload original)
{
    // Implementation of timing-based attack
    return ModifyMessageTiming(original);
}
```

3. **Add Test Scenarios**:
```csharp
[TestMethod]
public void Test_TimingAttackResistance()
{
    // Test implementation
}
```

### Custom Metrics Collection

```csharp
public class CustomMetrics : ChaosMetrics
{
    public void RecordCustomEvent(string eventType, object data)
    {
        // Custom tracking logic
    }
    
    public override void GenerateReport()
    {
        base.GenerateReport();
        // Add custom analysis
    }
}
```

This comprehensive framework ensures that Neo's DBFT consensus maintains its security and performance guarantees under the full spectrum of realistic failure conditions and sophisticated attacks.