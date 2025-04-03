# Neo VM Fuzzer Usage Guide

This document provides instructions for using the Neo VM Fuzzer to test the Neo Virtual Machine.

## Prerequisites

- .NET 7.0 SDK or later
- Neo VM source code

## Building the Fuzzer

1. Clone the Neo repository if you haven't already:
   ```
   git clone https://github.com/neo-project/neo.git
   cd neo
   ```

2. Build the fuzzer:
   ```
   dotnet build src/Neo.VM.Fuzzer
   ```

## Basic Usage

Run the fuzzer with default settings:

```
dotnet run --project src/Neo.VM.Fuzzer
```

This will:
- Run 1000 fuzzing iterations
- Use a random seed
- Save results to the `fuzzer-output` directory
- Use a 5-second timeout for each script execution

## Command Line Options

The fuzzer supports the following command line options:

| Option | Description | Default |
|--------|-------------|---------|
| `-i, --iterations` | Number of fuzzing iterations to run | 1000 |
| `-s, --seed` | Random seed for reproducible fuzzing | Random |
| `-o, --output` | Output directory for crash reports and interesting scripts | `fuzzer-output` |
| `-t, --timeout` | Timeout in milliseconds for each VM execution | 5000 |
| `-m, --mutation-rate` | Rate of mutation for script evolution (0.0-1.0) | 0.1 |
| `-c, --corpus` | Directory with initial corpus of scripts | None |

### Examples

Run 10,000 iterations with a specific seed:
```
dotnet run --project src/Neo.VM.Fuzzer -- -i 10000 -s 12345
```

Use a custom output directory and longer timeout:
```
dotnet run --project src/Neo.VM.Fuzzer -- -o my-fuzzer-results -t 10000
```

Use an existing corpus of scripts:
```
dotnet run --project src/Neo.VM.Fuzzer -- -c path/to/corpus
```

## Output Directory Structure

The fuzzer creates the following structure in the output directory:

```
output-dir/
├── crashes/           # Scripts that caused crashes
│   ├── crash_*.bin    # Binary script files
│   └── crash_*.txt    # Crash information
├── interesting/       # Scripts that found new coverage
│   └── interesting_*.bin
└── coverage_report.txt # Final coverage report
```

## Analyzing Results

### Crash Analysis

When the fuzzer finds a script that crashes the VM, it saves:
1. The script itself as a binary file (`.bin`)
2. A text file with the same name containing the exception details (`.txt`)

To reproduce a crash:
```
dotnet run --project src/Neo.VM.Fuzzer -- -c output-dir/crashes -i 1
```

### Coverage Analysis

The fuzzer generates a coverage report showing:
- Total coverage points reached
- OpCode coverage statistics
- Most and least frequently executed code paths

This information can help identify areas of the VM that need more testing.

## Continuous Fuzzing

For long-running fuzzing sessions, you can:

1. Start with an initial run:
   ```
   dotnet run --project src/Neo.VM.Fuzzer -- -o initial-run
   ```

2. Use the results as a corpus for subsequent runs:
   ```
   dotnet run --project src/Neo.VM.Fuzzer -- -c initial-run/interesting -o next-run
   ```

3. Repeat this process to iteratively improve coverage.

## Debugging Crashes

When investigating a crash:

1. Examine the crash text file to understand the exception
2. Use the binary script file with a debugger:
   ```csharp
   var script = System.IO.File.ReadAllBytes("path/to/crash.bin");
   var engine = new ExecutionEngine();
   engine.LoadScript(script);
   // Set breakpoints as needed
   engine.Execute();
   ```

## Performance Considerations

- The fuzzer can be resource-intensive, especially with large numbers of iterations
- Consider using a higher timeout for complex scripts
- For very large fuzzing campaigns, run on a dedicated machine

## Extending the Fuzzer

See [EXTENDING.md](EXTENDING.md) for information on customizing and extending the fuzzer for specific testing needs.
