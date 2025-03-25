# VMRunner

The `VMRunner` class is responsible for executing Neo VM scripts and tracking execution results during fuzzing. It provides comprehensive functionality for running scripts, monitoring their execution, and collecting metrics that help assess the effectiveness of the fuzzing process.

## Overview

The VMRunner serves as the execution engine for the Neo VM Fuzzer, providing the following capabilities:

1. Executing Neo VM scripts with configurable timeout settings
2. Tracking code coverage during script execution
3. Monitoring execution metrics such as stack size and invocation depth
4. Detecting crashes and timeouts
5. Recording detailed execution results for analysis

## Class Structure

The VMRunner is implemented in the `Neo.VM.Fuzzer.Runners` namespace and provides two primary execution methods:

1. `RunScript(byte[] script)` - Executes a script and returns a `VMExecutionResult`
2. `Execute(Script script, CancellationToken cancellationToken)` - Executes a script and returns an `ExecutionResult`

## Key Features

### Coverage Tracking

The VMRunner tracks which instructions are executed during script execution, allowing the fuzzer to identify scripts that explore new paths through the Neo VM. This is essential for guided fuzzing, where the goal is to maximize code coverage.

```csharp
// Example of checking for new coverage
bool foundNewCoverage = vmRunner.FoundNewCoverage(script, result);
if (foundNewCoverage) {
    // Save this script as interesting
}
```

### Timeout Handling

Scripts that run indefinitely can stall the fuzzing process. The VMRunner implements timeout detection to terminate long-running scripts:

```csharp
// Configure timeout in constructor
var vmRunner = new VMRunner(timeoutMs: 5000);
```

### Detailed Execution Results

The VMRunner provides comprehensive information about each script execution through the `VMExecutionResult` and `ExecutionResult` classes, including:

- Execution state (HALT, FAULT, etc.)
- Execution time
- Stack information
- Exception details (if any)
- Coverage information

## Integration with Other Components

The VMRunner works closely with other components of the Neo VM Fuzzer:

1. **ScriptGenerator**: Provides scripts for the VMRunner to execute
2. **MutationEngine**: Evolves scripts based on execution results
3. **CorpusManager**: Stores interesting scripts identified by the VMRunner
4. **FuzzingResults**: Aggregates execution metrics from multiple runs

## Usage Example

```csharp
// Initialize components
var vmRunner = new VMRunner(timeoutMs: 5000, verbose: true);
var scriptGenerator = new ScriptGenerator(random);

// Generate and execute a script
byte[] script = scriptGenerator.GenerateRandomScript();
var result = vmRunner.RunScript(script);

// Process the result
if (result.Crashed) {
    Console.WriteLine($"Script crashed with exception: {result.ExceptionType}");
} else if (result.TimedOut) {
    Console.WriteLine("Script execution timed out");
} else {
    Console.WriteLine($"Script executed successfully in {result.ExecutionTimeMs}ms");
}
```

## Implementation Details

The VMRunner uses two different approaches for executing scripts:

1. **InstrumentedExecutionEngine**: A custom execution engine that tracks coverage and execution details
2. **Event-based tracking**: Using the `InstructionPointerChanged` event to monitor execution

This dual approach provides flexibility in how scripts are executed and monitored, allowing for different types of analysis.

## Performance Considerations

The VMRunner is designed to be efficient, but tracking coverage and execution metrics does introduce some overhead. In performance-critical scenarios, the verbose mode can be disabled to reduce logging overhead.
