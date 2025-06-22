# DBFT Chaos Testing Framework

This chaos testing framework provides comprehensive resilience testing for the Neo DBFT consensus mechanism by injecting various failure scenarios and network conditions.

## Overview

The chaos tests simulate real-world failure conditions including:
- Message loss and corruption
- Node failures and recoveries
- Network partitions and asymmetric failures
- Variable latency and jitter
- Byzantine node behavior
- Combined failure scenarios

## Test Structure

```
ChaosTests/
├── Framework/
│   ├── ChaosTestBase.cs         # Base class for all chaos tests
│   ├── FaultInjector.cs         # Fault injection mechanisms
│   └── NetworkChaosSimulator.cs # Network failure simulation
├── Scenarios/
│   ├── UT_MessageLossChaos.cs   # Message loss scenarios
│   ├── UT_NodeFailureChaos.cs   # Node failure scenarios
│   ├── UT_NetworkPartitionChaos.cs # Network partition scenarios
│   └── UT_CombinedChaos.cs      # Combined chaos scenarios
├── Utilities/
│   └── ChaosMetrics.cs          # Metrics collection and reporting
└── ChaosTestRunner.cs           # Test execution coordinator
```

## Running Chaos Tests

### Run all chaos tests:
```bash
dotnet test --filter TestCategory=ChaosTests
```

### Run quick chaos tests (reduced duration):
```bash
dotnet test --filter TestCategory=QuickChaos
```

### Run specific chaos scenario:
```bash
dotnet test --filter FullyQualifiedName~MessageLossChaos
```

## Configuration

### Environment Variables

- `CHAOS_SEED`: Set a specific seed for reproducible tests
- `CHAOS_MESSAGE_LOSS`: Override message loss probability (0.0-1.0)
- `CHAOS_NODE_FAILURE`: Override node failure probability (0.0-1.0)
- `CHAOS_MAX_LATENCY`: Override maximum network latency (milliseconds)
- `CHAOS_QUICK_MODE`: Enable quick mode with reduced test duration

### Chaos Configuration

Default configuration in `ChaosTestBase.cs`:
```csharp
new ChaosConfiguration
{
    MessageLossProbability = 0.1,      // 10% message loss
    NodeFailureProbability = 0.05,     // 5% node failure chance
    MaxLatencyMs = 2000,               // Max 2 second latency
    MinLatencyMs = 50,                 // Min 50ms latency
    ByzantineProbability = 0.02,       // 2% Byzantine behavior
    MessageCorruptionProbability = 0.01, // 1% message corruption
    NetworkPartitionProbability = 0.05,  // 5% partition chance
    ViewChangeDelayMs = 5000           // 5 second view change delay
}
```

## Test Scenarios

### 1. Message Loss Chaos
- Random message loss with configurable rates
- Burst message loss patterns
- Selective message type loss
- Progressive network degradation

### 2. Node Failure Chaos
- Single node random failures
- Cascading node failures
- Byzantine node behavior
- Node restart with state recovery

### 3. Network Partition Chaos
- Random network partitions
- Asymmetric network failures
- Network latency and jitter
- Dynamic topology changes

### 4. Combined Chaos
- Realistic combined scenarios
- Extreme resilience testing
- Adaptive chaos response
- Phased chaos intensity

## Metrics and Reporting

Each test generates a comprehensive report including:
- Message loss statistics
- Node failure/recovery counts
- Network partition events
- Consensus success/failure rates
- Latency percentiles (p50, p95, p99)
- View change frequency

Example output:
```
=== CHAOS TEST METRICS REPORT ===
Test Duration: 120.5 seconds

## Fault Injection Summary
  MessageLoss: 1523
  NodeFailure: 12
  NodeRecovery: 10
  NetworkPartition: 3
  ViewChange: 18

## Network Latency Statistics
  Min: 52ms
  Max: 1987ms
  Average: 234.7ms
  Median: 187.3ms
  P95: 743.2ms
  P99: 1456.8ms

## Consensus Performance
  Total Rounds: 98
  Successful: 87 (88.78%)
  Failed: 11 (11.22%)
  View Changes: 18
```

## Extending the Framework

### Adding New Chaos Scenarios

1. Create a new test class inheriting from `ChaosTestBase`
2. Implement test methods with chaos injection
3. Use the provided fault injection tools
4. Collect metrics using `ChaosMetrics`

Example:
```csharp
[TestMethod]
public void TestCustomChaosScenario()
{
    RunChaosScenario(context =>
    {
        // Custom chaos logic
        if (context.Random.NextDouble() < 0.1)
        {
            context.FaultInjector.InjectMessageCorruption(message);
        }
    }, TimeSpan.FromSeconds(60));
    
    Assert.IsTrue(metrics.GetSummary().ConsensusSuccessRate > 0.7);
}
```

### Custom Fault Injection

Extend `FaultInjector` class:
```csharp
public class CustomFaultInjector : FaultInjector
{
    public void InjectCustomFault(ConsensusMessage message)
    {
        // Custom fault logic
        metrics.RecordCustomFault();
    }
}
```

## Best Practices

1. **Reproducibility**: Always log the chaos seed for failed tests
2. **Gradual Intensity**: Start with low chaos and gradually increase
3. **Metrics Collection**: Collect comprehensive metrics for analysis
4. **Timeout Management**: Set appropriate timeouts for chaos tests
5. **Resource Cleanup**: Ensure proper cleanup in test teardown

## Troubleshooting

### Test Timeouts
- Increase timeout values for longer chaos scenarios
- Use `[Timeout(milliseconds)]` attribute

### Flaky Tests
- Set `CHAOS_SEED` for reproducible failures
- Analyze metrics to identify failure patterns
- Adjust chaos configuration parameters

### Performance Issues
- Run chaos tests in isolation
- Monitor system resources during tests
- Use quick mode for rapid feedback

## Integration with CI/CD

```yaml
# Example GitHub Actions workflow
- name: Run DBFT Chaos Tests
  run: |
    export CHAOS_SEED=${{ github.run_id }}
    dotnet test --filter TestCategory=ChaosTests --logger "console;verbosity=detailed"
  timeout-minutes: 30
  continue-on-error: true
```

## Future Enhancements

- [ ] Distributed chaos testing across multiple nodes
- [ ] Integration with cloud chaos engineering tools
- [ ] Machine learning for intelligent chaos injection
- [ ] Automated chaos scenario generation
- [ ] Real-time visualization of chaos metrics