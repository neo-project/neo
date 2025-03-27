# Neo.Json.Fuzzer

A specialized fuzzing tool for testing the Neo.Json library's robustness, security, and performance.

## Overview

Neo.Json.Fuzzer systematically tests the Neo.Json library by generating diverse JSON inputs, including edge cases and malformed structures, to identify potential vulnerabilities, crashes, and denial-of-service vectors.

This fuzzer focuses specifically on testing the built-in limits and constraints of the Neo.Json library, such as maximum nesting depth, string length handling, numeric precision, and resource utilization during parsing.

## Key Features

- Comprehensive JSON test case generation
- Mutation-based fuzzing with coverage feedback
- Denial-of-Service (DOS) vector detection
- Detailed coverage analysis and reporting
- Specialized testing for integer boundaries and numeric precision
- JPath query testing with complex structures
- Unicode handling verification
- Concurrent access and streaming JSON testing

## Documentation

The project follows a documentation-first approach, with comprehensive documentation for all components:

### Core Components
- [Main Documentation](./Documentation/README.md) - Central entry point to all documentation
- [Mutation Engine](./Documentation/MutationEngine.md) - Core component for generating and mutating JSON
- [JSON Generation](./Documentation/JSONGeneration.md) - Strategies for generating test cases
- [JSON Runner](./Documentation/JsonRunner.md) - Execution engine for running tests
- [Utils](./Documentation/Utils.md) - Utility functions and helpers

### Testing Strategies
- [JPath Testing](./Documentation/JPathTesting.md) - Testing JPath query functionality
- [Unicode Testing](./Documentation/UnicodeTesting.md) - Testing Unicode character handling
- [Numeric Precision Testing](./Documentation/NumericPrecisionTesting.md) - Testing numeric precision and integer boundaries
- [Streaming Testing](./Documentation/StreamingTesting.md) - Testing streaming JSON functionality
- [Concurrent Access Testing](./Documentation/ConcurrentAccessTesting.md) - Testing concurrent access

### Analysis and Results
- [DOS Detection](./Documentation/DOSDetection.md) - DOS detection capabilities
- [DOS Testing Results](./Documentation/DOSTestingResults.md) - Results of DOS testing
- [Coverage Analysis](./Documentation/CoverageAnalysis.md) - Code coverage analysis
- [Coverage Mapping](./Documentation/CoverageMapping.md) - Mapping coverage points to Neo.Json functionality

### Extension Guide
- [Extending the Fuzzer](./Documentation/EXTENDING.md) - Guide for extending the fuzzer

## Getting Started

### Prerequisites

- .NET SDK 6.0 or later
- Neo project source code

### Building

```bash
dotnet build
```

### Running

```bash
dotnet run -- [options]
```

Common options:
- `--runs N`: Run N fuzzing iterations (default: 0 = infinite)
- `--seed N`: Use specific random seed for reproducible results (default: time-based)
- `--output DIR`: Output directory for results (default: ./output)
- `--detect-dos`: Enable DOS vector detection
- `--verbose`: Enable verbose output
- `--max-depth N`: Maximum JSON nesting depth to generate (default: 10)

Specialized testing options:
- `--jpath-tests`: Run specialized JPath query testing
- `--unicode-tests`: Run specialized Unicode handling tests
- `--numeric-precision-tests`: Run specialized numeric precision tests
- `--streaming-tests`: Run specialized streaming JSON tests
- `--concurrent-access-tests`: Run specialized concurrent access tests
- `--specialized-test-type TYPE`: Run a specific type of specialized test (e.g., "integer_boundaries")
- `--specialized-test-count N`: Number of specialized test cases to generate (default: 100)

See `dotnet run -- --help` for a complete list of options.

## Project Structure

- **Generators/**: Components for generating and mutating JSON test cases
  - **BaseMutationEngine.cs**: Core mutation functionality and Neo.Json-specific constants
  - **MutationEngine.cs**: Facade coordinating various mutation strategies
  - **NumericPrecisionMutations.cs**: Specialized testing for numeric precision and integer boundaries
  - **StringMutations.cs**: String generation and mutation
  - **StructureMutations.cs**: Structure manipulation (objects, arrays)

- **Runners/**: Components for executing tests and collecting results
  - **JsonRunner.cs**: Executes JSON parsing operations and analyzes results
  - **DOSDetector.cs**: Identifies potential denial-of-service vectors

- **Utils/**: Utility classes for analysis, corpus management, etc.
  - **CoverageTracker.cs**: Tracks code coverage during fuzzing
  - **CorpusManager.cs**: Manages the corpus of test cases
  - **FuzzingStatistics.cs**: Collects and reports statistics

- **Documentation/**: Comprehensive documentation following a documentation-first approach

## Testing Results

Initial testing has identified several areas for improvement in Neo.Json:

1. **Duplicate Property Handling**: Neo.Json throws an error on duplicate property names, which differs from the JSON specification
2. **Maximum Nesting Depth**: Hard limit of 64 levels with limited configurability
3. **Performance with Large Inputs**: Significant performance degradation with deeply nested structures
4. **Numeric Precision Issues**: Potential issues with very large or precise numeric values
5. **Non-Standard Format Rejection**: Some non-standard formats (like underscores in numbers) are rejected

## Contributing

Contributions are welcome! Please see [EXTENDING.md](./Documentation/EXTENDING.md) for guidelines on extending the fuzzer with new capabilities.
