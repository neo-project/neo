# Neo VM Fuzzer

A fuzzing engine for the Neo Virtual Machine (Neo VM) that generates random but valid scripts, executes them, and tracks any crashes or unexpected behaviors to enhance the robustness of the VM.

## Overview

The Neo VM Fuzzer is designed to:

1. Generate random but valid Neo VM scripts
2. Execute these scripts in the Neo VM
3. Track code coverage and execution metrics
4. Identify crashes, exceptions, and unexpected behaviors
5. Detect potential Denial of Service (DOS) vectors
6. Evolve scripts through mutation to find more issues

This tool helps ensure the Neo VM is robust and secure by systematically testing its behavior with a wide range of inputs.

## Project Location

The Neo VM Fuzzer is located in the `fuzzers` directory of the Neo project, separate from the main solution. This organization reflects its specialized purpose as a fuzzing tool for the Neo VM, which can be run independently from the main Neo project.

## Project Structure

```
fuzzers/Neo.VM.Fuzzer/
├── Program.cs                 # Entry point and main fuzzing loop
├── Generators/                # Script generation components
│   ├── ScriptGenerator.cs     # Generates random but valid Neo VM scripts
│   └── MutationEngine.cs      # Evolves scripts through mutation
├── Runners/                   # VM execution components
│   ├── VMRunner.cs            # Executes scripts in the Neo VM
│   └── InstrumentedExecutionEngine.cs # Custom engine for tracking execution
├── Utils/                     # Utility components
│   ├── CorpusManager.cs       # Manages the corpus of test scripts
│   ├── CoverageTracker.cs     # Tracks code coverage during fuzzing
│   ├── DOSDetector.cs         # Detects potential DOS vectors
│   └── FuzzingResults.cs      # Tracks and analyzes fuzzing results
└── Documentation/             # Documentation files
    ├── FUZZER_ARCHITECTURE.md # Architecture document
    ├── USAGE.md               # Usage instructions
    ├── DOSDetection.md        # DOS detection documentation
    └── EXTENDING.md           # Guide for extending the fuzzer
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Neo VM source code

### Building the Fuzzer

```bash
dotnet build fuzzers/Neo.VM.Fuzzer
```

### Running the Fuzzer

Basic usage:

```bash
dotnet run --project fuzzers/Neo.VM.Fuzzer
```

With options:

```bash
dotnet run --project fuzzers/Neo.VM.Fuzzer -- -i 10000 -s 12345 -o fuzzer-output -t 10000
```

## Command Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `-i, --iterations` | Number of fuzzing iterations to run | 1000 |
| `-s, --seed` | Random seed for reproducible fuzzing | Random |
| `-o, --output` | Output directory for crash reports and interesting scripts | `fuzzer-output` |
| `-t, --timeout` | Timeout in milliseconds for each VM execution | 5000 |
| `-m, --mutation-rate` | Rate of mutation for script evolution (0.0-1.0) | 0.1 |
| `-c, --corpus` | Directory with initial corpus of scripts | None |
| `-v, --verbose` | Enable verbose output | false |
| `-r, --report-interval` | Interval for progress reporting | 100 |
| `--detect-dos` | Enable detection of potential DOS vectors | false |
| `--dos-threshold` | Threshold for flagging potential DOS vectors (0.0-1.0) | 0.8 |
| `--track-memory` | Enable detailed memory tracking for DOS detection | false |
| `--track-opcodes` | Track execution time per opcode for DOS detection | true |

## Documentation

For more detailed information, see the documentation files:

- [Fuzzer Architecture](Documentation/FUZZER_ARCHITECTURE.md) - Overview of the fuzzer's design and components
- [Usage Guide](Documentation/USAGE.md) - Detailed instructions for using the fuzzer
- [DOS Detection](Documentation/DOSDetection.md) - Information on detecting potential DOS vectors
- [Extension Guide](Documentation/EXTENDING.md) - Information on customizing and extending the fuzzer

## Features

- **Random Script Generation**: Creates valid Neo VM scripts with various opcodes and operands
- **Instrumented Execution**: Tracks code coverage and execution details
- **Crash Detection**: Identifies scripts that cause exceptions or unexpected behavior
- **DOS Detection**: Identifies scripts that could lead to denial of service conditions
- **Coverage-Guided Fuzzing**: Focuses on scripts that explore new code paths
- **Mutation Engine**: Evolves scripts to find more issues over time
- **Corpus Management**: Maintains a collection of interesting scripts for further testing
- **Detailed Reporting**: Provides comprehensive analysis of fuzzing results

## Example Usage

### Basic Fuzzing

```bash
dotnet run --project fuzzers/Neo.VM.Fuzzer
```

### Fuzzing with DOS Detection

```bash
dotnet run --project fuzzers/Neo.VM.Fuzzer -- --detect-dos --dos-threshold 0.7 --track-memory
```

### Reproducible Fuzzing with Verbose Output

```bash
dotnet run --project fuzzers/Neo.VM.Fuzzer -- -i 5000 -s 12345 -v --detect-dos
```

## Contributing

Contributions to the Neo VM Fuzzer are welcome. Please follow the standard Neo project contribution guidelines.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
