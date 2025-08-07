# Core Modifications Refactoring Plan

## Issue Summary
cschuchardt88's review correctly identified that the OpenTelemetry plugin implementation has made direct modifications to core Neo classes, which violates architectural principles and creates maintenance issues.

## Current Problems

### 1. LocalNode.cs Modifications
- Added event delegates and handlers for metrics
- Added metrics timer and tracking fields
- Added byte tracking methods
- Modified internal logic to invoke metrics events

### 2. MemoryPool.cs Modifications  
- Added event delegates and handlers for metrics
- Added metrics timer for periodic stats collection
- Added conflict and batch removal tracking
- Modified core methods to track metrics

### 3. Impact
- **Tight Coupling**: Core classes now depend on metrics concepts
- **Memory Overhead**: All nodes pay the cost even if not using metrics
- **Maintenance Burden**: Core changes needed for metrics updates
- **Testing Complexity**: Core tests must consider metrics side effects

## Recommended Solution

### Approach 1: Use Existing Interfaces Only
The plugin should collect metrics using only:
- Public properties (ConnectedCount, UnconnectedCount, Count, Capacity)
- Existing events (TransactionAdded, TransactionRemoved)
- Plugin interfaces (ICommittingHandler, ICommittedHandler)

### Approach 2: Create Metrics Collection Layer
1. **Polling Strategy**: Use timers in the plugin to periodically poll public properties
2. **Event Observation**: Subscribe to existing public events
3. **Calculated Metrics**: Derive metrics from available data

### Implementation Strategy

```csharp
// In OpenTelemetryPlugin.cs
private void CollectNetworkMetrics()
{
    if (_neoSystem?.LocalNode is LocalNode localNode)
    {
        // Use existing public properties
        var connectedCount = localNode.ConnectedCount;
        var unconnectedCount = localNode.UnconnectedCount;
        
        // Update metrics
        _connectedPeersGauge?.Record(connectedCount);
        _unconnectedPeersGauge?.Record(unconnectedCount);
    }
}

private void CollectMemPoolMetrics()
{
    if (_neoSystem?.MemPool != null)
    {
        var memPool = _neoSystem.MemPool;
        
        // Use existing public properties
        var count = memPool.Count;
        var verifiedCount = memPool.VerifiedCount;
        var unverifiedCount = memPool.UnVerifiedCount;
        var capacity = memPool.Capacity;
        
        // Update metrics
        _mempoolSizeGauge?.Record(count);
        _mempoolVerifiedGauge?.Record(verifiedCount);
        _mempoolUnverifiedGauge?.Record(unverifiedCount);
        
        // Calculate derived metrics
        var capacityRatio = (double)count / capacity;
        _mempoolCapacityRatioGauge?.Record(capacityRatio);
    }
}
```

### Metrics That Cannot Be Collected
Without core modifications, some metrics cannot be collected:
- Exact bytes sent/received (would need network layer hooks)
- Batch removal counts (internal operation)
- Conflict counts (internal operation)
- Message type statistics (would need protocol layer access)

These should be documented as limitations or future enhancement requests for proper plugin interfaces.

## Action Items
1. ✅ Remove all metrics-related code from LocalNode.cs
2. ✅ Remove all metrics-related code from MemoryPool.cs  
3. ✅ Remove INetworkMetricsHandler and IMemPoolMetricsHandler interfaces
4. ✅ Update OpenTelemetryPlugin to use polling and existing events
5. ✅ Document metrics that cannot be collected without core support
6. ✅ Create issue for future enhancement: proper metrics API in core

## Benefits of This Approach
- **Clean Separation**: Core remains focused on blockchain functionality
- **Zero Overhead**: No performance impact when metrics disabled
- **Maintainability**: Plugin changes don't require core modifications
- **Compatibility**: Works with existing Neo versions