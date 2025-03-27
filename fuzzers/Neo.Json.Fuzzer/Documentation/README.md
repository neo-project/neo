# Neo.Json.Fuzzer Documentation

## Overview

Neo.Json.Fuzzer is a specialized fuzzing tool designed to test the Neo.Json library for bugs, vulnerabilities, and performance issues. It generates random or mutated JSON inputs and feeds them to the Neo.Json parser to identify potential issues.

## Documentation Structure

This documentation is organized into the following sections:

### Core Components
- [Mutation Engine](./MutationEngine.md) - Core component that generates and mutates JSON inputs
- [JSON Generation](./JSONGeneration.md) - Explains how JSON is generated for testing
- [JSON Runner](./JsonRunner.md) - Describes how the fuzzer runs JSON tests
- [Utils](./Utils.md) - Documents utility functions used by the fuzzer

### Testing Strategies
- [JPath Testing](./JPathTesting.md) - Strategy for testing JPath query functionality
- [Unicode Testing](./UnicodeTesting.md) - Strategy for testing Unicode handling
- [Numeric Precision Testing](./NumericPrecisionTesting.md) - Strategy for testing numeric precision
- [Streaming Testing](./StreamingTesting.md) - Strategy for testing streaming JSON functionality
- [Concurrent Access Testing](./ConcurrentAccessTesting.md) - Strategy for testing concurrent access

### Analysis and Results
- [DOS Detection](./DOSDetection.md) - Explains the DOS detection capabilities
- [DOS Testing Results](./DOSTestingResults.md) - Contains results of DOS testing
- [Coverage Analysis](./CoverageAnalysis.md) - Explains how code coverage is analyzed
- [Coverage Mapping](./CoverageMapping.md) - Maps coverage points to Neo.Json functionality

### Extension Guide
- [Extending the Fuzzer](./EXTENDING.md) - Guide for extending the fuzzer with new capabilities

## Features

- **Comprehensive Testing**: Tests parsing, serialization, JPath queries, and type conversions
- **Mutation-based Fuzzing**: Intelligently mutates existing JSON to create new test cases
- **Neo.Json-specific Testing**: Targets Neo.Json-specific features and limits
- **DOS Detection**: Identifies potential denial-of-service vectors
- **Coverage Tracking**: Monitors code coverage during fuzzing
- **Memory Usage Tracking**: Monitors memory usage to identify leaks or excessive allocations

## Architecture

The Neo.Json.Fuzzer uses a modular architecture with specialized components:

1. **MutationEngine**: Core facade that coordinates mutation strategies
2. **BaseMutationEngine**: Provides common functionality and utilities
3. **StringMutations**: Handles string-related mutations
4. **NumberMutations**: Manages number-related mutations
5. **BooleanMutations**: Implements boolean-related mutations
6. **StructureMutations**: Handles structural mutations (adding/removing properties, etc.)
7. **NeoJsonSpecificMutations**: Implements Neo.Json-specific mutation strategies

## Usage

```bash
dotnet run -- --runs 100 --detect-dos --verbose
```

For detailed command-line options, run:

```bash
dotnet run -- --help
```

## Getting Started

1. Clone the Neo repository
2. Navigate to the Neo.Json.Fuzzer directory
3. Run the fuzzer with desired options
4. Analyze the results in the output directory

## License

Copyright (C) 2015-2025 The Neo Project.

This project is licensed under the MIT License - see the LICENSE file in the main directory of the repository for details.
