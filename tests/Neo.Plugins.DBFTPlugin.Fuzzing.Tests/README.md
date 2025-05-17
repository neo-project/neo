# DBFT Plugin Fuzzing Tests

This project contains production-ready fuzzing tests for the Neo DBFT consensus plugin. The tests use [SharpFuzz](https://github.com/Metalnem/sharpfuzz) to perform comprehensive fuzzing on the DBFT consensus message handling, capable of running for extended periods (days, weeks, or months).

## Overview

The fuzzing tests focus on testing the robustness of the DBFT consensus mechanism by generating and mutating various consensus messages:

- `ChangeView` messages
- `PrepareRequest` messages
- `PrepareResponse` messages
- `Commit` messages
- `RecoveryRequest` messages
- `RecoveryMessage` messages

The tests simulate the processing of these messages and check for invariant violations that could indicate bugs or vulnerabilities in the consensus mechanism.

## Project Structure

- `Core/` - Core fuzzing implementation
  - `FuzzConsensus.cs` - Main fuzzing entry point
  - `FuzzConsensusInitialization.cs` - Setup and initialization code
  - `FuzzConsensusMessageProcessing.cs` - Message processing logic
  - `FuzzConsensusInvariants.cs` - Invariant checking code
  - `FuzzConsensusUtilities.cs` - Logging and utility methods
  - `FuzzConsensusSimulations.cs` - Consensus simulation scenarios
  - `FuzzConsensusByzantine.cs` - Byzantine behavior simulations
  - `FuzzConsensusRecovery.cs` - Recovery process simulations

- `Generators/` - Corpus generators for different message types
  - `CorpusGenerator.cs` - Main corpus generator
  - `CommitGenerator.cs` - Commit message generator
  - `PrepareRequestGenerator.cs` - PrepareRequest message generator
  - `PrepareResponseGenerator.cs` - PrepareResponse message generator
  - `ChangeViewGenerator.cs` - ChangeView message generator
  - `RecoveryRequestGenerator.cs` - RecoveryRequest message generator
  - `RecoveryMessageGenerator.cs` - RecoveryMessage generator

- `Utils/` - Utility classes
  - `FuzzingHelpers.cs` - Common helper methods
  - `MessageSerializer.cs` - Message serialization utilities

## Running the Fuzzing Tests

### Prerequisites

- .NET 9.0 SDK
- SharpFuzz.CommandLine tool (automatically installed by the script)
- Bash shell environment

### Quick Start

1. Make the script executable:
   ```bash
   chmod +x run_fuzzer.sh
   ```

2. Run the fuzzing script with default settings:
   ```bash
   ./run_fuzzer.sh
   ```

The script will:
1. Generate an initial corpus if one doesn't exist
2. Build the project in release mode
3. Install SharpFuzz if needed
4. Run the fuzzer with appropriate settings
5. Create checkpoints at regular intervals
6. Generate reports of findings

### Command-Line Interface

The `run_fuzzer.sh` script provides a comprehensive command-line interface:

```bash
./run_fuzzer.sh [command] [options]
```

#### Commands

- `fuzz` - Run continuous fuzzing (default)
- `generate-corpus` - Generate corpus files
- `run-corpus` - Run fuzzer on existing corpus
- `analyze <file>` - Analyze a specific test case
- `report` - Generate a report of findings
- `clean` - Clean up temporary files
- `help` - Show help message

#### Options

- `--corpus-dir=DIR` - Set corpus directory (default: "corpus")
- `--findings-dir=DIR` - Set findings directory (default: "findings")
- `--crashes-dir=DIR` - Set crashes directory (default: "crashes")
- `--dict=FILE` - Set dictionary file (default: "fuzzing.dict")
- `--max-len=N` - Set maximum input length (default: 4096)
- `--timeout=N` - Set timeout in seconds (default: 5)
- `--workers=N` - Set number of worker threads (default: 4)
- `--jobs=N` - Set number of jobs (default: 4)
- `--runs=N` - Set number of runs (default: 100000)
- `--debug` - Build in Debug mode instead of Release
- `--checkpoint=N` - Set checkpoint interval in seconds (default: 3600)
- `--runtime=N` - Set total runtime in seconds, 0 for indefinite (default: 0)
- `--corpus-count=N` - Set number of corpus files to generate per type (default: 50)

### Examples

#### Long-Term Fuzzing

Run fuzzing indefinitely with checkpoints every hour:
```bash
./run_fuzzer.sh fuzz
```

Run for 24 hours with checkpoints every 30 minutes:
```bash
./run_fuzzer.sh fuzz --runtime=86400 --checkpoint=1800
```

Run with 16 worker threads for maximum performance:
```bash
./run_fuzzer.sh fuzz --workers=16
```

#### Corpus Generation

Generate a large corpus with 100 files per message type:
```bash
./run_fuzzer.sh generate-corpus --corpus-count=100
```

Generate corpus in a custom directory:
```bash
./run_fuzzer.sh generate-corpus --corpus-dir=my_corpus
```

#### Analysis

Analyze a specific crash file:
```bash
./run_fuzzer.sh analyze crashes/crash-123abc
```

Generate a report of findings:
```bash
./run_fuzzer.sh report
```

### Manual Operation

If you prefer to run commands directly without the script:

#### Generate Corpus

```bash
dotnet run -- generate-corpus [output_dir] [count]
```

Where:
- `output_dir` is the directory to write corpus files (default: "corpus")
- `count` is the number of files to generate per message type (default: 50)

#### Run Corpus

```bash
dotnet run -- run-corpus [input_dir]
```

Where:
- `input_dir` is the directory containing corpus files (default: "corpus")

#### Analyze Test Case

```bash
dotnet run -- analyze path/to/testcase
```

## Interpreting Results

### Fuzzing Output

The fuzzer organizes its output into several directories:

- `findings/` - Interesting test cases that exercise new code paths
- `crashes/` - Inputs that caused invariant violations or crashes
- `reports/` - Session reports and checkpoints
- `corpus/` - The current corpus of test cases
- `corpus_initial_backup/` - Backup of the initial corpus for comparison

### Session Reports

Each fuzzing session creates a detailed report in the `reports/session_TIMESTAMP/` directory:

- `config.txt` - Configuration used for the session
- `report.txt` - Summary of findings and statistics
- `fuzzer_log_N.txt` - Logs for each fuzzing iteration
- `checkpoint_N/` - Checkpoints created during the session

The report includes:
- Runtime statistics
- Corpus growth metrics
- Findings and crashes summary
- Coverage information

### Analyzing Crashes

When the fuzzer finds an issue, it saves the input that triggered it to the `crashes/` directory. To analyze a crash:

1. Use the built-in analysis tool:
   ```bash
   ./run_fuzzer.sh analyze crashes/crash-123abc
   ```

2. Or manually debug the issue:
   ```csharp
   var testData = File.ReadAllBytes("path/to/crash/file");
   using var ms = new MemoryStream(testData);
   FuzzConsensus.Fuzz(ms);
   ```

### Verifying Fuzzing Effectiveness

Even when no issues are found, you can verify the fuzzer is working effectively by:

1. **Checking Corpus Growth**: Compare the initial corpus size with the current size
   ```bash
   find corpus_initial_backup -type f | wc -l
   find corpus -type f | wc -l
   ```

2. **Reviewing Session Reports**: Check the reports for coverage information
   ```bash
   cat reports/session_*/report.txt
   ```

3. **Examining Checkpoints**: Look at how the corpus evolved over time
   ```bash
   ls -la reports/session_*/checkpoint_*/corpus
   ```

4. **Introducing a Deliberate Bug**: Temporarily modify the code to introduce a bug and verify the fuzzer catches it

## Adding New Test Cases

The fuzzing system is designed to be easily extensible. To add new test cases:

### Adding New Corpus Generators

1. Create a new generator method in the appropriate generator class:
   ```csharp
   public static void GenerateWithCustomProperty(string outputDirectory, CustomValue value, string filename = null)
   {
       // Create a message with the custom property
       var message = new SomeMessage { Property = value };

       // Serialize and save the message
       MessageSerializer.WriteMessageToFile(outputDirectory, filename ?? $"custom_{value}.bin", message);
   }
   ```

2. Add the new generator to the `CorpusGenerator.Generate` or `GenerateEdgeCases` method:
   ```csharp
   // In CorpusGenerator.cs
   SomeMessageGenerator.GenerateWithCustomProperty(outputDirectory, customValue);
   ```

3. Run the corpus generation to create the new test cases:
   ```bash
   ./run_fuzzer.sh generate-corpus
   ```

### Adding New Simulation Scenarios

1. Create a new simulation method in the appropriate simulation class:
   ```csharp
   public static void SimulateNewScenario(ConsensusMessage initialMessage)
   {
       // Initialize context
       InitializeContext();

       // Set up validators and other state
       // ...

       // Simulate the scenario
       // ...

       // Check invariants
       CheckInvariants();
   }
   ```

2. Add the new simulation to the `FuzzConsensus.Fuzz` method:
   ```csharp
   // In FuzzConsensus.cs
   if (simulationType == SimulationType.NewScenario)
   {
       SimulateNewScenario(message);
   }
   ```

### Adding New Invariant Checks

1. Add a new invariant check method in `FuzzConsensusInvariants.cs`:
   ```csharp
   protected static void CheckNewInvariant()
   {
       // Check the invariant
       if (invariantViolated)
       {
           throw new InvalidOperationException("New invariant violated");
       }
   }
   ```

2. Call the new invariant check from the `CheckInvariants` method:
   ```csharp
   // In FuzzConsensusInvariants.cs
   protected static void CheckInvariants()
   {
       // Existing checks
       // ...

       // New invariant check
       CheckNewInvariant();
   }
   ```

## Invariants and Properties Checked

The fuzzing tests check a comprehensive set of invariants and properties to ensure the consensus mechanism behaves correctly:

### Safety Invariants

1. **Block Finalization Conditions** - A block should only be finalized when sufficient commits are received
2. **View Number Validity** - View numbers must be non-negative and within valid range
3. **Primary Index Validity** - Primary index must be within the range of validators
4. **Preparation Consistency** - All preparation messages must be consistent
5. **Commit Consistency** - All commit messages must be for the same view
6. **Transaction Consistency** - No duplicate transaction hashes and count within limits
7. **ChangeView Consistency** - New view numbers must be greater than current view
8. **Validator Index Validity** - All validator indices must be valid
9. **Block Index Consistency** - All messages must be for the same block index
10. **Signature Validity** - Commit signatures must be valid
11. **Message Sequence Validity** - Messages must arrive in a valid sequence
12. **State Transition Validity** - State transitions must follow the protocol rules

### Liveness Properties

1. **Normal Consensus** - The system should be able to reach consensus in the normal case
2. **Consensus with View Changes** - The system should be able to reach consensus even after view changes
3. **Bounded View Changes** - The system should not experience an excessive number of view changes
4. **Recovery After Failures** - The system should recover after node failures
5. **Byzantine Fault Tolerance** - The system should tolerate up to f Byzantine nodes (where n=3f+1)
6. **Network Partition Resilience** - The system should maintain safety during network partitions

### Byzantine Scenarios Tested

The fuzzer specifically tests Byzantine behavior through:

1. **Conflicting Messages** - Primary sending conflicting PrepareRequest messages
2. **Mixed View Commits** - Validators committing to different views
3. **Invalid Signatures** - Validators sending messages with invalid signatures
4. **Malformed Messages** - Validators sending malformed or invalid messages
5. **Network Partitions** - Simulating network partitions where validators are split into groups

### Recovery Scenarios Tested

The fuzzer tests recovery mechanisms through:

1. **Node Recovery** - Simulating a node coming back online after being offline
2. **View Changes** - Testing the view change mechanism when the primary fails
3. **Multiple View Changes** - Testing multiple consecutive view changes
4. **Concurrent Consensus Rounds** - Testing concurrent consensus for different block indices

### Verification Methodology

The fuzzer verifies these properties through:

1. **Invariant Checking** - Continuously checking invariants during message processing
2. **Simulation Scenarios** - Running specific scenarios that test edge cases
3. **Long-Term Fuzzing** - Running for extended periods to find rare issues
4. **Corpus Evolution** - Evolving the corpus to explore new code paths
5. **Checkpointing** - Creating checkpoints to track progress and detect regressions

When an invariant is violated or a property is not satisfied, the fuzzer records the input that caused the issue, along with detailed logs and state information for analysis.

## Long-Term Fuzzing Strategies

The DBFT fuzzing system is designed for long-term operation. Here are strategies for effective long-term fuzzing:

### Resource Management

1. **Memory Management**: The fuzzer uses resource limits to prevent excessive memory consumption
2. **CPU Utilization**: Configure worker count based on available CPU cores
3. **Disk Space**: Regularly archive or clean up old findings and reports
4. **Checkpointing**: Use checkpoints to resume fuzzing after interruptions

### Continuous Operation

1. **Run as a Service**: Use systemd or similar to run the fuzzer as a service
   ```bash
   # Example systemd service file
   [Unit]
   Description=Neo DBFT Fuzzer
   After=network.target

   [Service]
   User=neo
   WorkingDirectory=/path/to/neo/tests/Neo.Plugins.DBFTPlugin.Fuzzing.Tests
   ExecStart=/path/to/neo/tests/Neo.Plugins.DBFTPlugin.Fuzzing.Tests/run_fuzzer.sh fuzz --runtime=0
   Restart=on-failure

   [Install]
   WantedBy=multi-user.target
   ```

2. **Monitoring**: Set up monitoring to track fuzzer progress
   ```bash
   # Example monitoring script
   watch -n 60 "cat /path/to/neo/tests/Neo.Plugins.DBFTPlugin.Fuzzing.Tests/reports/session_*/report.txt | tail -n 20"
   ```

3. **Notification**: Set up notifications for crashes
   ```bash
   # Example notification script
   inotifywait -m -e create /path/to/neo/tests/Neo.Plugins.DBFTPlugin.Fuzzing.Tests/crashes/ | \
     while read path action file; do \
       echo "New crash detected: $file" | mail -s "DBFT Fuzzer Crash" admin@example.com; \
     done
   ```

### Corpus Management

1. **Periodic Regeneration**: Periodically regenerate the corpus with new test cases
   ```bash
   # Example cron job (weekly)
   0 0 * * 0 cd /path/to/neo/tests/Neo.Plugins.DBFTPlugin.Fuzzing.Tests && ./run_fuzzer.sh generate-corpus --corpus-count=200
   ```

2. **Corpus Minimization**: Periodically minimize the corpus to remove redundant test cases
   ```bash
   # Example minimization script
   ./run_fuzzer.sh fuzz --minimize-corpus=1 --runs=1000
   ```

3. **Corpus Exchange**: Exchange corpus files between different fuzzing instances

### Effective Mutation Strategies

1. **Dictionary-Based Fuzzing**: Use the provided dictionary file for more effective mutations
2. **Structure-Aware Fuzzing**: The fuzzer understands the structure of consensus messages
3. **Coverage-Guided Fuzzing**: The fuzzer uses coverage information to guide mutations
4. **Value Profile**: The fuzzer uses value profiles to find interesting code paths

By following these strategies, the DBFT fuzzing system can run effectively for extended periods, continuously testing the consensus mechanism for bugs and vulnerabilities.
