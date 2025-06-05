# DBFT Consensus Unit Tests

Comprehensive unit tests for the Neo DBFT (Delegated Byzantine Fault Tolerance) consensus plugin, ensuring robustness, security, and reliability of the consensus mechanism.

## 🎯 Overview

This test suite provides complete coverage of the DBFT consensus protocol, including normal operations, failure scenarios, recovery mechanisms, and stress testing. The tests validate that the consensus system can handle Byzantine failures, network partitions, and various edge cases while maintaining blockchain integrity.

## 📊 Test Coverage

### Test Files & Organization

| Test File | Tests | Description |
|-----------|-------|-------------|
| `UT_ConsensusService.cs` | 6 | Service lifecycle and message handling |
| `UT_DBFT_Core.cs` | 3 | Core consensus mechanics |
| `UT_DBFT_Integration.cs` | 4 | Integration scenarios |
| `UT_DBFT_NormalFlow.cs` | 3 | Complete normal consensus flows |
| `UT_DBFT_Failures.cs` | 4 | Failure and attack scenarios |
| `UT_DBFT_Recovery.cs` | 5 | Recovery mechanisms |
| `UT_DBFT_Performance.cs` | 5 | Stress and edge case testing |
| `UT_DBFT_MessageFlow.cs` | 4 | Message passing and validation |

**Total: 34 Tests** - All passing ✅

### Supporting Infrastructure

- **`MockWallet.cs`** - Custom wallet implementation with unique validator keys
- **`MockProtocolSettings.cs`** - Test configuration using Neo's protocol settings
- **`MockBlockchain.cs`** - Test blockchain setup and configuration
- **`MockMemoryStoreProvider.cs`** - In-memory storage provider for testing
- **`MockAutoPilot.cs`** - Test autopilot for actor message handling
- **`ConsensusTestUtilities.cs`** - Advanced testing utilities and message verification

## 🔍 Test Scenarios

### ✅ Normal Consensus Flows
- **Complete Consensus Round**: Full PrepareRequest → PrepareResponse → Commit flow
- **Primary Rotation**: Testing primary validator rotation between rounds
- **Transaction Inclusion**: Consensus with actual transaction sets
- **Multi-Round Consensus**: Sequential block creation scenarios

### ⚠️ Abnormal Scenarios & Fault Tolerance
- **Primary Failure**: Primary node fails during consensus, triggering view changes
- **Byzantine Validators**: Malicious validators sending conflicting messages
- **Invalid Message Handling**: Malformed payloads and wrong parameters
- **Network Partitions**: Simulated network splits and communication failures

### 🔄 Recovery Mechanisms
- **Recovery Request/Response**: Complete recovery message flow
- **State Recovery**: Validators catching up after failures
- **View Change Recovery**: Recovery during view change scenarios
- **Partial Consensus Recovery**: Recovery with partial consensus state
- **Multiple Recovery Requests**: Handling simultaneous recovery requests

### 💪 Robustness & Stress Testing
- **Minimum Validators**: Consensus with minimum validator count (4 validators, f=1)
- **Maximum Byzantine Failures**: Testing f=2 failures in 7-validator setup
- **Stress Testing**: Multiple rapid consensus rounds
- **Large Transaction Sets**: Consensus with 100+ transactions
- **Concurrent View Changes**: Multiple simultaneous view change scenarios

## 🚀 Running the Tests

### Prerequisites
- .NET 9.0 or later
- Neo project dependencies
- Akka.NET TestKit

### Execute Tests
```bash
# Run all DBFT tests
dotnet test tests/Neo.Plugins.DBFTPlugin.Tests

# Run with verbose output
dotnet test tests/Neo.Plugins.DBFTPlugin.Tests --verbosity normal

# Run specific test file
dotnet test tests/Neo.Plugins.DBFTPlugin.Tests --filter "ClassName~UT_DBFT_NormalFlow"

# Run specific test method
dotnet test tests/Neo.Plugins.DBFTPlugin.Tests --filter "TestCompleteConsensusRound"
```

### Expected Results
```
Test summary: total: 34, failed: 0, succeeded: 34, skipped: 0
Build succeeded
```

## 🏗️ Test Architecture

### Actor System Testing
Tests use Akka.NET TestKit for proper actor system testing:
- **TestProbe**: Mock actor dependencies (blockchain, localNode, etc.)
- **Actor Lifecycle**: Verification that actors don't crash under stress
- **Message Flow**: Tracking and validation of consensus messages

### Consensus Message Flow
Tests validate the complete DBFT protocol:
1. **PrepareRequest** from primary validator
2. **PrepareResponse** from backup validators
3. **Commit** messages from all validators
4. **ChangeView** for view changes
5. **RecoveryRequest/RecoveryMessage** for recovery

### Byzantine Fault Tolerance
Comprehensive testing of Byzantine fault tolerance:
- **f=1**: 4 validators can tolerate 1 Byzantine failure
- **f=2**: 7 validators can tolerate 2 Byzantine failures
- **Conflicting Messages**: Validators sending different messages to different nodes
- **Invalid Behavior**: Malformed messages and protocol violations

## 🔧 Key Features

### Realistic Testing
- **Unique Validator Keys**: Each validator has unique private keys
- **Proper Message Creation**: Realistic consensus message generation
- **Network Simulation**: Partition and message loss simulation
- **Time-based Testing**: Timeout and recovery scenarios

### Professional Quality
- **Comprehensive Coverage**: All major DBFT functionality tested
- **Clean Code**: Well-organized, documented, and maintainable
- **No Flaky Tests**: Reliable and deterministic test execution
- **Performance**: Tests complete efficiently (~28 seconds)

### Security Validation
- **Byzantine Resistance**: Malicious validator behavior testing
- **Message Validation**: Invalid and malformed message handling
- **State Consistency**: Consensus state integrity verification
- **Recovery Security**: Safe recovery from failures

The tests provide confidence that the DBFT consensus will maintain blockchain integrity and continue operating correctly under all conditions, including network partitions, validator failures, and malicious attacks.

---

*For more information about Neo's DBFT consensus, see the [Neo Documentation](https://docs.neo.org/).*
