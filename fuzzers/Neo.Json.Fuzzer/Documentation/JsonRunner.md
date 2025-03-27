# Neo.Json.Fuzzer - JSON Runner

## Overview

The JsonRunner component is responsible for executing JSON parsing operations and analyzing the results. It serves as the core execution engine for the Neo.Json.Fuzzer, handling the actual interaction with the Neo.Json library.

## Key Features

- **Execution Management**: Runs JSON parsing operations with configurable timeouts
- **Comprehensive Testing**: Tests multiple aspects of the Neo.Json library:
  - Parsing with different max_nest values
  - JPath operations
  - Type conversions
  - Serialization
- **Error Handling**: Captures and categorizes exceptions for analysis
- **Performance Monitoring**: Tracks execution time and memory usage
- **DOS Detection**: Identifies potential Denial of Service vectors
- **Coverage Tracking**: Collects coverage points for analysis

## Implementation Details

### JsonRunner Class

The `JsonRunner` class executes JSON parsing operations and collects detailed information about the execution:

```csharp
public class JsonRunner
{
    public JsonRunner(int timeoutMs = 5000, bool detectDOS = false, 
                     double dosThreshold = 0.8, bool trackMemory = false);
    public JsonExecutionResult Execute(string json);
}
```

### JsonExecutionResult Class

The `JsonExecutionResult` class contains comprehensive information about the execution of a JSON parsing operation:

```csharp
public class JsonExecutionResult
{
    // Basic execution information
    public bool Success { get; set; }
    public bool Crashed { get; set; }
    public bool TimedOut { get; set; }
    public double ExecutionTimeMs { get; set; }
    public long MemoryUsageBytes { get; set; }
    public Exception? Exception { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public JToken? ParsedToken { get; set; }
    public List<string> Coverage { get; set; }
    public DOSAnalysisResult? DOSAnalysis { get; set; }
    
    // Detailed test results for different operations
    // (parsing, JPath, type conversions, serialization)
    // ...
}
```

## Execution Process

1. **Initialization**: Set up the execution environment with configurable parameters
2. **Execution**: Parse the JSON input with a configurable timeout
3. **Testing**: If parsing succeeds, test JPath operations, type conversions, and serialization
4. **Analysis**: Collect performance metrics and analyze for potential DOS vectors
5. **Coverage**: Track which parts of the Neo.Json library were exercised

## Usage Example

```csharp
// Create a JSON runner with DOS detection enabled
var runner = new JsonRunner(timeoutMs: 5000, detectDOS: true);

// Execute a JSON parsing operation
var result = runner.Execute(jsonInput);

// Check the result
if (result.Success)
{
    Console.WriteLine("Parsing succeeded");
}
else if (result.Crashed)
{
    Console.WriteLine($"Parsing crashed: {result.ExceptionType}");
}
else if (result.TimedOut)
{
    Console.WriteLine("Parsing timed out");
}

// Check for DOS vectors
if (result.DOSAnalysis?.IsPotentialDOSVector == true)
{
    Console.WriteLine($"Potential DOS vector detected: {result.DOSAnalysis.DetectionReason}");
}
```

## Integration with Other Components

- **Program**: The main program uses the JsonRunner to execute JSON inputs
- **JsonGenerator**: Generates JSON inputs for the JsonRunner to execute
- **MutationEngine**: Mutates JSON inputs for the JsonRunner to execute
- **CorpusManager**: Manages the collection of JSON inputs
- **CoverageTracker**: Tracks coverage information from JsonRunner executions
- **FuzzingStatistics**: Records statistics from JsonRunner executions
- **DOSDetector**: Analyzes JsonRunner results for potential DOS vectors

## Future Enhancements

- Add support for custom JPath expressions
- Implement more detailed coverage tracking
- Add support for custom type conversions
- Implement parallel execution for faster fuzzing
