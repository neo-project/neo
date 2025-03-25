# Event Arguments

This document describes the event argument classes used in the Neo VM Fuzzer for tracking execution events.

## StepEventArgs

The `StepEventArgs` class provides information about each step of execution in the Neo VM. It is used to track instruction execution and gather metrics for fuzzing analysis.

### Properties

- **OpCode**: The operation code being executed
- **InstructionPointer**: The current instruction pointer position
- **StackSize**: The current size of the evaluation stack

### Usage

```csharp
// Example of handling a step event
engine.OnStep += (sender, e) => {
    Console.WriteLine($"Executing opcode: {e.OpCode} at position {e.InstructionPointer}");
    Console.WriteLine($"Current stack size: {e.StackSize}");
};
```

## FaultEventArgs

The `FaultEventArgs` class provides information about faults that occur during script execution. It is used to track and analyze errors that occur during fuzzing.

### Properties

- **ExceptionType**: The type of exception that occurred
- **ExceptionMessage**: The detailed message of the exception
- **InstructionPointer**: The instruction pointer position where the fault occurred

### Usage

```csharp
// Example of handling a fault event
engine.OnFault += (sender, e) => {
    Console.WriteLine($"Fault occurred at position {e.InstructionPointer}");
    Console.WriteLine($"Exception: {e.ExceptionType} - {e.ExceptionMessage}");
};
```

These event argument classes are essential for tracking execution flow and identifying interesting behaviors during fuzzing runs. They enable detailed analysis of script execution and help identify potential vulnerabilities or bugs in the Neo VM.
