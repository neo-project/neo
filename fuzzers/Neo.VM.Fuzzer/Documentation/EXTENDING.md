# Extending the Neo VM Fuzzer

This document provides guidance on how to extend and customize the Neo VM Fuzzer for specific testing needs.

## Architecture Overview

Before extending the fuzzer, familiarize yourself with the architecture described in [FUZZER_ARCHITECTURE.md](FUZZER_ARCHITECTURE.md). The fuzzer is designed with modularity in mind, making it straightforward to extend or replace individual components.

## Extension Points

### 1. Script Generation

The `ScriptGenerator` class is responsible for creating random Neo VM scripts. You can extend it to:

- Add new script generation strategies
- Implement domain-specific script patterns
- Create targeted scripts for specific VM features

#### Example: Adding a New Script Generation Strategy

```csharp
public class EnhancedScriptGenerator : ScriptGenerator
{
    public EnhancedScriptGenerator(Random random) : base(random)
    {
    }
    
    // Add a new method for generating scripts with specific patterns
    public byte[] GenerateStackHeavyScript(int maxInstructions = 100)
    {
        var scriptBuilder = new ScriptBuilder();
        
        // Generate a script that heavily exercises stack operations
        for (int i = 0; i < maxInstructions; i++)
        {
            // Add stack-related operations
            if (_random.Next(3) == 0)
            {
                scriptBuilder.Emit(OpCode.PUSH1);
            }
            else if (_random.Next(3) == 1)
            {
                scriptBuilder.Emit(OpCode.DUP);
            }
            else
            {
                scriptBuilder.Emit(OpCode.SWAP);
            }
        }
        
        scriptBuilder.Emit(OpCode.RET);
        return scriptBuilder.ToArray();
    }
}
```

### 2. VM Execution

The `VMRunner` class handles script execution in the Neo VM. You can extend it to:

- Add new instrumentation for specific metrics
- Implement alternative execution strategies
- Add specialized error detection

#### Example: Enhanced Coverage Tracking

```csharp
public class EnhancedVMRunner : VMRunner
{
    public EnhancedVMRunner(int timeoutMs = 5000) : base(timeoutMs)
    {
    }
    
    // Override to add enhanced coverage tracking
    protected override void InstrumentEngine(ExecutionEngine engine, VMExecutionResult result)
    {
        base.InstrumentEngine(engine, result);
        
        // Add additional instrumentation
        engine.OnStep += (sender, e) =>
        {
            // Track additional metrics
            var context = engine.CurrentContext;
            if (context != null)
            {
                // Track branch decisions
                if (e.OpCode == OpCode.JMPIF || e.OpCode == OpCode.JMPIFNOT)
                {
                    bool condition = context.EvaluationStack.Peek().GetBoolean();
                    result.Coverage.Add($"Branch:{e.InstructionPointer}:{condition}");
                }
                
                // Track stack depth patterns
                result.Coverage.Add($"StackPattern:{context.EvaluationStack.Count % 10}");
            }
        };
    }
}
```

### 3. Corpus Management

The `CorpusManager` class manages the collection of test scripts. You can extend it to:

- Implement different corpus selection strategies
- Add script normalization or minimization
- Implement corpus distillation

#### Example: Script Minimization

```csharp
public class EnhancedCorpusManager : CorpusManager
{
    public EnhancedCorpusManager(string outputDir, string? corpusDir = null) 
        : base(outputDir, corpusDir)
    {
    }
    
    // Add script minimization capability
    public byte[] MinimizeScript(byte[] script, Func<byte[], bool> testFunction)
    {
        byte[] minimized = script.ToArray();
        bool changed;
        
        do
        {
            changed = false;
            
            // Try removing chunks of the script
            for (int chunkSize = 16; chunkSize >= 1; chunkSize /= 2)
            {
                for (int i = 0; i < minimized.Length - chunkSize; i++)
                {
                    byte[] candidate = new byte[minimized.Length - chunkSize];
                    Buffer.BlockCopy(minimized, 0, candidate, 0, i);
                    Buffer.BlockCopy(minimized, i + chunkSize, candidate, i, minimized.Length - i - chunkSize);
                    
                    if (testFunction(candidate))
                    {
                        minimized = candidate;
                        changed = true;
                        break;
                    }
                }
                
                if (changed) break;
            }
        } while (changed);
        
        return minimized;
    }
}
```

### 4. Coverage Analysis

The `CoverageTracker` class monitors code coverage. You can extend it to:

- Implement different coverage metrics
- Add visualization capabilities
- Implement differential coverage analysis

#### Example: Branch Coverage

```csharp
public class BranchCoverageTracker : CoverageTracker
{
    private readonly HashSet<string> _branchCoverage = new HashSet<string>();
    
    // Track branch coverage specifically
    public bool HasNewBranchCoverage(HashSet<string> coverage)
    {
        bool hasNewBranches = false;
        
        foreach (var point in coverage)
        {
            if (point.StartsWith("Branch:") && _branchCoverage.Add(point))
            {
                hasNewBranches = true;
            }
        }
        
        return hasNewBranches;
    }
    
    // Generate branch coverage report
    public string GetBranchCoverageReport()
    {
        var report = new System.Text.StringBuilder();
        
        var branches = _branchCoverage
            .Where(c => c.StartsWith("Branch:"))
            .Select(c => c.Substring(7))
            .ToList();
        
        report.AppendLine($"Total Branch Coverage: {branches.Count} branches");
        
        // Group by instruction pointer
        var branchGroups = branches
            .Select(b => b.Split(':'))
            .GroupBy(parts => parts[0])
            .ToList();
        
        foreach (var group in branchGroups)
        {
            var trueCount = group.Count(parts => parts[1] == "True");
            var falseCount = group.Count(parts => parts[1] == "False");
            
            report.AppendLine($"Branch at {group.Key}: True={trueCount}, False={falseCount}");
        }
        
        return report.ToString();
    }
}
```

## Adding New Features

### 1. Custom Mutation Strategies

You can implement custom mutation strategies to target specific aspects of the Neo VM:

```csharp
public static class MutationStrategies
{
    // Mutate jump targets specifically
    public static byte[] MutateJumpTargets(byte[] script, Random random)
    {
        byte[] result = script.ToArray();
        
        // Find jump instructions
        for (int i = 0; i < result.Length - 1; i++)
        {
            OpCode opcode = (OpCode)result[i];
            
            if (opcode == OpCode.JMP || opcode == OpCode.JMPIF || 
                opcode == OpCode.JMPIFNOT || opcode == OpCode.CALL)
            {
                // Mutate the jump offset
                result[i + 1] = (byte)random.Next(256);
            }
        }
        
        return result;
    }
    
    // Focus on arithmetic operations
    public static byte[] MutateArithmetic(byte[] script, Random random)
    {
        byte[] result = script.ToArray();
        
        // Define arithmetic opcodes
        OpCode[] arithmeticOpcodes = new[]
        {
            OpCode.ADD, OpCode.SUB, OpCode.MUL, OpCode.DIV, OpCode.MOD,
            OpCode.SHL, OpCode.SHR, OpCode.BOOLAND, OpCode.BOOLOR,
            OpCode.NUMEQUAL, OpCode.NUMNOTEQUAL, OpCode.LT, OpCode.GT
        };
        
        // Replace some opcodes with arithmetic ones
        for (int i = 0; i < result.Length; i++)
        {
            if (random.Next(10) == 0)
            {
                result[i] = (byte)arithmeticOpcodes[random.Next(arithmeticOpcodes.Length)];
            }
        }
        
        return result;
    }
}
```

### 2. Smart Contract-Specific Fuzzing

For fuzzing Neo smart contracts, you can add contract-specific components:

```csharp
public class SmartContractFuzzer
{
    private readonly ScriptGenerator _generator;
    private readonly VMRunner _runner;
    
    public SmartContractFuzzer(ScriptGenerator generator, VMRunner runner)
    {
        _generator = generator;
        _runner = runner;
    }
    
    // Generate a script that calls a specific smart contract method
    public byte[] GenerateContractMethodCall(byte[] contractScript, string method, int paramCount)
    {
        var scriptBuilder = new ScriptBuilder();
        
        // Generate random parameters
        for (int i = 0; i < paramCount; i++)
        {
            // Add random parameter based on type
            switch (i % 4)
            {
                case 0: // Integer
                    scriptBuilder.Emit(OpCode.PUSHINT32);
                    scriptBuilder.Emit(BitConverter.GetBytes(new Random().Next()));
                    break;
                case 1: // Boolean
                    scriptBuilder.Emit(new Random().Next(2) == 0 ? OpCode.PUSHF : OpCode.PUSHT);
                    break;
                case 2: // String
                    byte[] strBytes = Encoding.UTF8.GetBytes($"param{i}");
                    scriptBuilder.Emit(OpCode.PUSHDATA1);
                    scriptBuilder.Emit((byte)strBytes.Length);
                    foreach (byte b in strBytes)
                        scriptBuilder.Emit(b);
                    break;
                case 3: // Array
                    scriptBuilder.Emit(OpCode.NEWARRAY0);
                    break;
            }
        }
        
        // Add method name
        byte[] methodBytes = Encoding.UTF8.GetBytes(method);
        scriptBuilder.Emit(OpCode.PUSHDATA1);
        scriptBuilder.Emit((byte)methodBytes.Length);
        foreach (byte b in methodBytes)
            scriptBuilder.Emit(b);
        
        // Add parameter count and call
        scriptBuilder.Emit(OpCode.PUSHINT8);
        scriptBuilder.Emit((byte)paramCount);
        
        // Append contract script
        foreach (byte b in contractScript)
            scriptBuilder.Emit(b);
        
        scriptBuilder.Emit(OpCode.RET);
        
        return scriptBuilder.ToArray();
    }
}
```

### 3. Differential Fuzzing

To compare execution between different VM versions:

```csharp
public class DifferentialFuzzer
{
    private readonly ScriptGenerator _generator;
    
    public DifferentialFuzzer(ScriptGenerator generator)
    {
        _generator = generator;
    }
    
    // Compare execution between two VM versions
    public bool CompareExecution(byte[] script, string vmVersion1, string vmVersion2)
    {
        // Load the first VM version
        var engine1 = LoadVMVersion(vmVersion1);
        var result1 = ExecuteScript(engine1, script);
        
        // Load the second VM version
        var engine2 = LoadVMVersion(vmVersion2);
        var result2 = ExecuteScript(engine2, script);
        
        // Compare results
        return CompareResults(result1, result2);
    }
    
    private ExecutionEngine LoadVMVersion(string version)
    {
        // Implementation would load a specific VM version
        // This is a placeholder
        return new ExecutionEngine();
    }
    
    private VMExecutionResult ExecuteScript(ExecutionEngine engine, byte[] script)
    {
        var result = new VMExecutionResult();
        
        try
        {
            engine.LoadScript(script);
            result.State = engine.Execute();
            
            // Capture result stack
            if (engine.ResultStack != null)
            {
                foreach (var item in engine.ResultStack)
                {
                    // Store result for comparison
                }
            }
        }
        catch (Exception ex)
        {
            result.Crashed = true;
            result.ExceptionMessage = ex.Message;
        }
        
        return result;
    }
    
    private bool CompareResults(VMExecutionResult result1, VMExecutionResult result2)
    {
        // Compare VM states
        if (result1.State != result2.State)
            return false;
        
        // Compare crash status
        if (result1.Crashed != result2.Crashed)
            return false;
        
        // Compare exception messages if crashed
        if (result1.Crashed && result1.ExceptionMessage != result2.ExceptionMessage)
            return false;
        
        // Compare result stacks
        // Implementation would compare stack items
        
        return true;
    }
}
```

## Integration with CI/CD

To integrate the fuzzer with continuous integration:

1. Create a dedicated fuzzing project:

```csharp
public class ContinuousFuzzer
{
    private readonly string _outputDir;
    private readonly string _corpusDir;
    
    public ContinuousFuzzer(string outputDir, string corpusDir)
    {
        _outputDir = outputDir;
        _corpusDir = corpusDir;
    }
    
    public int Run(int iterations, int timeoutMinutes)
    {
        // Set up components
        var random = new Random();
        var scriptGenerator = new ScriptGenerator(random);
        var vmRunner = new VMRunner(5000);
        var corpusManager = new CorpusManager(_outputDir, _corpusDir);
        var coverageTracker = new CoverageTracker();
        
        // Load corpus
        corpusManager.LoadCorpus();
        
        // Set timeout
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
        
        int crashCount = 0;
        
        try
        {
            // Main fuzzing loop
            for (int i = 0; i < iterations && !cancellationTokenSource.Token.IsCancellationRequested; i++)
            {
                // Generate script
                byte[] script = random.Next(10) < 3
                    ? corpusManager.GetRandomScript()
                    : scriptGenerator.GenerateRandomScript();
                
                // Execute script
                var result = vmRunner.RunScript(script);
                
                // Handle result
                if (result.Crashed)
                {
                    crashCount++;
                    corpusManager.SaveCrash(script, result.ExceptionMessage);
                }
                else if (coverageTracker.HasNewCoverage(result.Coverage))
                {
                    corpusManager.SaveInteresting(script);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }
        
        // Save coverage report
        coverageTracker.SaveCoverageReport(Path.Combine(_outputDir, "coverage_report.txt"));
        
        return crashCount;
    }
}
```

2. Create a CI script that runs the fuzzer and reports results.

## Performance Optimization

To optimize fuzzer performance:

1. Implement parallel fuzzing:

```csharp
public class ParallelFuzzer
{
    public void RunParallel(int iterations, int threadCount)
    {
        // Shared components
        var coverageTracker = new CoverageTracker();
        var corpusManager = new CorpusManager("output", "corpus");
        
        // Load corpus
        corpusManager.LoadCorpus();
        
        // Create a thread-safe queue for interesting scripts
        var interestingScripts = new ConcurrentQueue<byte[]>();
        
        // Run fuzzing in parallel
        Parallel.For(0, threadCount, threadId =>
        {
            // Thread-local components
            var random = new Random(threadId);
            var scriptGenerator = new ScriptGenerator(random);
            var vmRunner = new VMRunner(5000);
            
            for (int i = 0; i < iterations / threadCount; i++)
            {
                // Generate script
                byte[] script;
                if (random.Next(10) < 3 && corpusManager.CorpusSize > 0)
                {
                    script = corpusManager.GetRandomScript();
                    script = scriptGenerator.MutateScript(script);
                }
                else
                {
                    script = scriptGenerator.GenerateRandomScript();
                }
                
                // Execute script
                var result = vmRunner.RunScript(script);
                
                // Handle result
                if (result.Crashed)
                {
                    lock (corpusManager)
                    {
                        corpusManager.SaveCrash(script, result.ExceptionMessage);
                    }
                }
                else if (coverageTracker.HasNewCoverage(result.Coverage))
                {
                    interestingScripts.Enqueue(script);
                }
            }
        });
        
        // Process interesting scripts
        foreach (var script in interestingScripts)
        {
            corpusManager.SaveInteresting(script);
        }
    }
}
```

## Conclusion

The Neo VM Fuzzer is designed to be extensible and adaptable to various testing needs. By following the patterns and examples in this document, you can customize the fuzzer to focus on specific aspects of the Neo VM or to integrate with your development workflow.

For further assistance or to contribute improvements to the fuzzer, please submit issues or pull requests to the Neo project repository.
