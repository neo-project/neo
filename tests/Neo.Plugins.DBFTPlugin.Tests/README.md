# Neo DBFT Consensus Unit Testing Project

This project implements a comprehensive testing suite for the Neo DBFT (Delegated Byzantine Fault Tolerance) consensus mechanism. The tests are organized in a phased approach, ensuring each component and scenario is properly tested.

## Test Structure

The unit tests are implemented in the following phases, with each phase building upon the previous ones:

### Phase 1: Test Environment and Basic Component Testing
- `TestUtils/MockConsensusComponents.cs`: Provides mock objects and utilities for testing
- `TestUtils/ConsensusTestBase.cs`: Base class for test fixtures with timeout management
- `TestUtils/TestUtilities.cs`: Utilities for preventing hanging tests
- `UT_ConsensusContext.cs`: Tests for the ConsensusContext class
- `Messages/UT_ConsensusMessages.cs`: Tests for message serialization/deserialization
- `UT_Settings.cs`: Tests for plugin configuration settings

### Phase 2: State Transitions and ConsensusService Basic Functionality
- `Consensus/UT_ConsensusService.cs`: Tests for ConsensusService initialization and basic operations

### Phase 3: Message Processing and Validation
- `Consensus/UT_ConsensusMessageProcessing.cs`: Tests for message reception and processing

### Phase 4: Consensus Scenarios and Integration
- `Consensus/UT_ConsensusScenarios_Normal.cs`: Tests for normal consensus flow
- `Consensus/UT_ConsensusScenarios_FaultTolerance.cs`: Tests for fault tolerance scenarios

### Phase 5: Edge Cases and Performance Testing
- `Consensus/UT_ConsensusEdgeCases.cs`: Tests for edge cases and boundary conditions

## Running the Tests

To run the unit tests:

1. Build the solution:
   ```
   dotnet build
   ```

2. Run all tests:
   ```
   dotnet test tests/Neo.Plugins.DBFTPlugin.Tests/Neo.Plugins.DBFTPlugin.Tests.csproj
   ```

3. Run specific test class:
   ```
   dotnet test tests/Neo.Plugins.DBFTPlugin.Tests/Neo.Plugins.DBFTPlugin.Tests.csproj --filter "FullyQualifiedName~UT_ConsensusContext"
   ```

4. Run tests with detailed output:
   ```
   dotnet test tests/Neo.Plugins.DBFTPlugin.Tests/Neo.Plugins.DBFTPlugin.Tests.csproj -v n
   ```

## Test Coverage

The test suite covers the following key aspects of the DBFT consensus mechanism:

1. **Basic Component Functionality**
   - ConsensusContext initialization and state management
   - Message serialization, deserialization, and validation
   - Configuration settings and processing

2. **Consensus Service Operations**
   - Service initialization and startup
   - Message handling and processing
   - Timer management and state transitions

3. **Message Flow Testing**
   - PrepareRequest creation and processing
   - PrepareResponse handling
   - ChangeView processing
   - Commit validation and processing
   - Recovery request and message handling

4. **Consensus Scenarios**
   - Normal consensus flow (Primary perspective)
   - Normal consensus flow (Backup perspective)
   - Primary node failure and view changes
   - Transaction not found scenarios
   - Recovery from node failure

5. **Edge Cases and Special Conditions**
   - Out-of-order message processing
   - Delayed messages after view changes
   - Duplicate message handling
   - Maximum transaction limit
   - Timestamp validation

## Testing Framework Improvements

The test framework includes the following improvements to prevent hanging tests:

1. **Time Control**
   - Enhanced `MockTimeProvider` with thread-safety improvements
   - Added millisecond-level precision for time advancement
   - Added condition-based time advancement to avoid polling loops

2. **Timeout Protection**
   - Added `ConsensusTestBase` with proper cleanup and resource disposal
   - Implemented `ExecuteWithTimeout` to prevent tests from hanging
   - Added automatic cancellation of timer tokens

3. **Deadlock Prevention**
   - Robust cleanup in test teardown to ensure system resources are released
   - Forced garbage collection after tests to prevent lingering references
   - Added `WaitForCondition` to handle asynchronous conditions with timeouts

4. **Test Environment Management**
   - Centralized initialization and cleanup for all consensus tests
   - Direct access to timer cancellation tokens to prevent orphaned timers
   - Standardized approach to managing test components

All consensus scenario tests now use the base class and timeout protection, preventing the tests from hanging indefinitely.

## Contributing

When adding new tests or modifying existing ones, please maintain the current phase structure and ensure that all tests can run independently. All consensus tests should derive from `ConsensusTestBase` to ensure proper resource cleanup.

## Dependencies

The test project requires:
- Neo core library
- MSTest framework
- Akka.TestKit for actor-based testing
