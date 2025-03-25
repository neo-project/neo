# DOS Detection in Neo VM Fuzzer

## Overview

This document describes the Denial of Service (DOS) detection capabilities implemented in the Neo VM Fuzzer. The fuzzer is designed to identify potential DOS vectors in Neo VM scripts that could be exploited to cause resource exhaustion or performance degradation in the Neo blockchain.

DOS attacks in a virtual machine context typically exploit inefficient execution paths that consume excessive resources. In the Neo VM, these can manifest as:

1. **Computational DOS**: Scripts that execute an excessive number of operations
2. **Memory DOS**: Scripts that consume excessive memory through stack manipulation
3. **Infinite Loops**: Scripts that create execution paths that never terminate
4. **Exponential Complexity**: Scripts that trigger exponential growth in resource usage

## Detection Mechanisms

The DOS detection system analyzes script execution metrics to identify potential DOS vectors based on the following criteria:

1. **High Instruction Count**: Scripts that execute an excessive number of instructions may indicate a DOS vector. The current threshold is set to 100 instructions.

2. **Excessive Stack Depth**: Scripts that create deep stacks may cause memory exhaustion. The current threshold is set to 5 stack items.

3. **Slow Opcodes**: Scripts that repeatedly execute opcodes with high average execution times may indicate a DOS vector. The current thresholds are:
   - Average execution time > 0.05ms
   - Executed more than 2 times

4. **Long Execution Time**: Scripts that take a long time to execute may indicate a DOS vector. The current threshold is set to 10ms.

5. **Potential Infinite Loops**: Scripts that contain patterns indicative of infinite loops are flagged as potential DOS vectors.

## Detection Strategy

The enhanced fuzzer:

1. Tracks detailed execution metrics:
   - Instruction count per opcode
   - Stack depth throughout execution
   - Memory allocations
   - Execution time per opcode
   - Branch decision patterns

2. Identifies suspicious patterns:
   - Operations with execution time significantly above average
   - Scripts with high instruction-to-progress ratios
   - Repeated state patterns indicating potential infinite loops
   - Exponential growth in resource consumption

3. Scores and ranks scripts by their "DOS potential"

## Implementation

The DOS detection system is implemented in the following components:

1. **DOSDetector**: The main class responsible for analyzing execution metrics and detecting potential DOS vectors.
   - Located in `Utils/DOSDetector.cs`
   - Configurable thresholds for different detection mechanisms
   - Calculates a DOS score based on multiple factors
   - Tracks execution time per opcode
   - Monitors stack depth and memory usage
   - Identifies potential infinite loops by tracking instruction pointer frequencies
   - Provides detailed analysis results with recommendations

   Key methods:
   - `OnStep`: Handles step events from the execution engine to track metrics
   - `AnalyzeExecution`: Analyzes collected metrics to calculate a DOS score
   - `GenerateReport`: Creates a detailed report of the analysis results

2. **VMRunner**: Integrates with the DOSDetector to analyze script execution.
   - Located in `Runners/VMRunner.cs`
   - Performs DOS analysis for both successful and crashed script executions
   - Collects execution metrics such as instruction count, stack depth, and execution time
   - Saves potential DOS vectors to the corpus

   Key methods:
   - `Execute`: Executes a script and performs DOS analysis
   - `SaveDOSVector`: Saves a potential DOS vector to the corpus

3. **InstrumentedExecutionEngine**: Tracks detailed execution metrics used for DOS detection.
   - Located in `Runners/InstrumentedExecutionEngine.cs`
   - Monitors instruction execution, stack operations, and execution time
   - Provides hooks for the DOSDetector to monitor execution
   - Records opcode execution times and frequencies

   Key methods:
   - `Execute`: Executes a script with detailed instrumentation
   - `OnStep`: Fires when an instruction is executed, providing metrics to subscribers

## DOS Score Calculation

The DOS score is calculated based on multiple factors, with each factor contributing a portion to the overall score:

- **Instruction Count**: Up to 0.5 points based on the number of instructions executed
- **Stack Depth**: Up to 0.3 points based on the maximum stack depth
- **Slow Opcodes**: Up to 0.3 points based on the presence of slow opcodes
- **Execution Time**: Up to 0.5 points based on the total execution time
- **Potential Infinite Loops**: Up to 0.5 points based on loop detection

A script is considered a potential DOS vector if its DOS score exceeds the configured threshold (default: 0.1).

## Workflow Integration

The DOS detection is integrated into the fuzzing workflow:

1. The fuzzer executes a script with the instrumented execution engine
2. The DOSDetector analyzes the execution metrics
3. If the DOS score exceeds the threshold, the script is flagged as a potential DOS vector
4. The fuzzer saves the potential DOS vector to the corpus
5. The fuzzer includes the DOS vector in the fuzzing results

### DOS Vector Analysis File Format

Each DOS vector analysis file contains:

```
DOS Vector Analysis: dos-20250326-055929-0_80-High_instruction_count__3411;_Excessive_stack_depth__2048-7b56e5b8
Timestamp: 3/26/2025 5:59:29AM
DOS Score: 0.80
Detection Reason: High instruction count: 3411; Excessive stack depth: 2048

Metrics:
  TotalInstructions: 3411
  MaxStackDepth: 2048
  UniqueOpcodes: 0
  TotalExecutionTimeMs: 3.7773
  InstructionScore: 0.5
  LoopScore: 0
  StackScore: 0.3

Recommendations:
  - Consider adding instruction count limits to prevent excessive execution
  - Consider adding stack depth limits to prevent stack overflow attacks
```

## Test Scripts

The following test scripts are provided to verify the DOS detection functionality:

1. **minimal_dos_vector.neo**: A simple script that triggers DOS detection based on execution time
2. **stack_depth_dos.neo**: A script that focuses on excessive stack depth
3. **comprehensive_dos_vector.neo**: A script that combines multiple DOS vectors
4. **final_dos_test.neo**: A comprehensive test script that triggers multiple DOS detection mechanisms

## Recent Enhancements

1. **DOS Detection for Crashed Scripts**: Modified the VMRunner to perform DOS analysis even when scripts crash with exceptions, ensuring we can detect potential DOS vectors in all scripts.

2. **Adjusted Detection Thresholds**: Lowered the thresholds to make the detection more sensitive:
   - Reduced instruction count threshold from 5000 to 100
   - Reduced stack depth threshold from 50 to 5
   - Reduced slow opcode threshold from 0.2ms to 0.05ms
   - Reduced execution time threshold from 500ms to 10ms

3. **Enhanced Logging**: Added detailed logging about the DOS detection process to help debug and understand why scripts are or aren't being detected as DOS vectors.

## Future Improvements

1. **Improved Loop Detection**: Enhance the detection of potential infinite loops by analyzing execution patterns.
2. **Dynamic Thresholds**: Implement dynamic thresholds based on the average execution metrics of the corpus.
3. **Opcode-Specific Analysis**: Implement more detailed analysis of specific opcodes known to be resource-intensive.
4. **Memory Usage Analysis**: Add detection for scripts that consume excessive memory.
5. **Machine Learning-Based Detection**: Implement anomaly detection for more precise identification of DOS vectors.
6. **Automatic Test Case Generation**: Generate test cases that specifically target DOS vulnerabilities.
7. **Formal Verification Integration**: Integrate with formal verification tools to prove absence of certain DOS vectors.
8. **Automatic Mitigation Recommendations**: Generate mitigation recommendations based on detected patterns.

## Usage

To enable DOS detection when running the fuzzer, use the following command-line options:

```bash
dotnet run -- --detect-dos --dos-threshold 0.1 --track-opcodes --track-memory
```

- `--detect-dos`: Enables DOS detection
- `--dos-threshold`: Sets the threshold for DOS detection (default: 0.1)
- `--track-opcodes`: Enables tracking of opcode execution times (required for DOS detection)
- `--track-memory`: Enables tracking of memory usage (recommended for DOS detection)
