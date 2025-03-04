# NEP: Reduce Block Time and GAS Generation Rate

This document describes the implementation of the NEP to reduce the block time from 15 seconds to 3 seconds and the GAS generation rate from 5 GAS to 1 GAS per block in the Neo N3 network.

## Overview

This NEP introduces two significant changes to the Neo N3 network parameters:

1. **Block Time Reduction**: Changing the block time from 15 seconds to 3 seconds
2. **GAS Generation Rate Reduction**: Changing the GAS generation rate from 5 GAS to 1 GAS per block

These changes are implemented in the Echidna hardfork.

## Implementation Details

### 1. Block Time Configuration

#### Changes to PolicyContract

The block time configuration has been moved from the dBFT consensus plugin to the PolicyContract native contract. This change allows the block time to be configured through governance rather than being hardcoded.

The following additions have been made to the `PolicyContract` class:

- **Constants**:
  - `DefaultBlockGenTime`: Set to 15000 (15 seconds in milliseconds)
  - `MinBlockGenTime`: Set to 1000 (1 second in milliseconds)
  - `Prefix_BlockGenTime`: Added for storage key (value: 21)

- **New Methods**:
  - `GetBlockGenTime`: Retrieves the current block generation time in milliseconds
  - `SetBlockGenTime`: Allows the Neo Council to set a new block generation time (only callable after the Echidna hardfork)

#### Storage and Initialization

The block generation time is stored in the blockchain state using a storage key with the prefix `Prefix_BlockGenTime`. The initial value of 15000 milliseconds (15 seconds) is set during the initialization of the Echidna hardfork.

```csharp
// Initialize block generation time
engine.SnapshotCache.Add(_blockGenTime, new StorageItem(DefaultBlockGenTime));
```

#### Permission Control

The `SetBlockGenTime` method includes strict permission controls to ensure that only the Neo Council can modify the block generation time:

```csharp
[ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
private void SetBlockGenTime(ApplicationEngine engine, uint milliseconds)
{
    if (milliseconds < MinBlockGenTime) throw new ArgumentOutOfRangeException(nameof(milliseconds));
    if (!CheckCommittee(engine)) throw new InvalidOperationException();
    engine.SnapshotCache.GetAndChange(_blockGenTime).Set(milliseconds);
}
```

### 2. Integration with Consensus Mechanism

The consensus mechanism has been updated to use the block time from the PolicyContract instead of the hardcoded value in the protocol settings.

#### ConsensusService Modifications

- **New Method**:
  - `GetBlockTimeFromPolicyContract`: A helper method that retrieves the block time from the PolicyContract
  
  ```csharp
  private TimeSpan GetBlockTimeFromPolicyContract()
  {
      // Get the current block time from the Policy contract
      uint milliseconds = NativeContract.Policy.GetBlockGenTime(neoSystem.StoreView);
      return TimeSpan.FromMilliseconds(milliseconds);
  }
  ```

- **Timer Updates**:
  - All occurrences of `neoSystem.Settings.MillisecondsPerBlock` and `neoSystem.Settings.TimePerBlock` have been replaced with calls to `GetBlockTimeFromPolicyContract()`

### 3. Changes to GAS Generation Rate

After the hardfork activation, the Neo Council will adjust the GAS generation rate from 5 GAS to 1 GAS per block using the existing governance mechanisms. This change does not require code modifications as the GAS generation rate is already configurable through governance.

## Deployment and Activation

1. **Deployment**: The code changes will be deployed as part of the Echidna hardfork
2. **Activation**: Initially, the block time will remain at 15 seconds
3. **Governance Action**: After the hardfork, the Neo Council will:
   - Call `SetBlockGenTime(3000)` to set the block time to 3 seconds
   - Adjust the GasPerBlock value to 1 GAS

## Affected Parameters

| Parameter                  | Before | After | Notes                                 |
|----------------------------|--------|-------|---------------------------------------|
| Block Time                 | 15s    | 3s    | Configurable via Policy contract      |
| GAS per Block              | 5 GAS  | 1 GAS | Configured by Neo Council             |
| MaxTransactionsPerBlock    | 512    | 512   | Adjusted for shorter block times      |
| MaxValidUntilBlockIncrement| 100    | 100   | Adjusted for shorter block times      |

## Impact and Considerations

### Network Performance

- **Transaction Throughput**: The reduced block time will result in more blocks per time unit, potentially increasing the overall transaction throughput of the network
- **Confirmation Time**: Transactions will be confirmed faster, improving the user experience for dApps and other applications on the Neo network

### Smart Contract Impact

Smart contracts that rely on block time for time-sensitive operations should be reviewed. The reduced block time may affect:

- Time-locked contracts
- Auction mechanisms
- Token distributions or vesting schedules

### Synchronization and Node Performance

Nodes will need to process blocks more frequently. This may require:

- More network bandwidth
- More computational resources
- More storage space over time

## References

- Neo Enhancement Proposal: Reduce Block Time and GAS Generation Rate
- Implementation Pull Request: [#3622](https://github.com/neo-project/neo/pull/3622)
