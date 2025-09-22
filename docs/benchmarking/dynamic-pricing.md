# Benchmark Scaling Artifacts

The benchmark suite now exports both raw timing metrics and scaling ratios so you can derive dynamic gas prices directly from the generated CSVs. This document summarizes what is produced and how to interpret it.

## Artifact Overview

Running the benchmarks with coverage enabled (`NEO_VM_BENCHMARK=1 NEO_BENCHMARK_COVERAGE=1 dotnet run …`) emits the following files under `BenchmarkArtifacts/`:

- `opcode-metrics-*.csv`, `syscall-metrics-*.csv`, `native-metrics-*.csv`
  - Per-operation measurements for every variant (Baseline, Single, Saturated).
  - Columns now include:
    - `TotalIterations`, `TotalDataBytes`, `TotalElements`
    - `TotalAllocatedBytes`, `AllocatedBytesPerIteration`, `AllocatedBytesPerByte`, `AllocatedBytesPerElement`
    - `AverageStackDepth`, `PeakStackDepth`, `AverageAltStackDepth`, `PeakAltStackDepth`
    - `TotalGasConsumed`, `GasPerIteration`

- `benchmark-metrics-*.csv`
  - The merged view of all individual metric files. Useful for bulk analysis.

- `benchmark-scaling.csv`
  - Derived ratios comparing Baseline, Single, and Saturated variants:
    - Time growth (`BaselineNsPerIteration`, `SingleNsPerIteration`, `SaturatedNsPerIteration`, `IterationScale`).
    - Time-per-byte growth (`...NsPerByte`, `ByteScale`).
    - Allocation growth (`...AllocatedPerIteration`, `AllocatedIterationScale`, `...AllocatedPerByte`, `AllocatedByteScale`).
    - Stack growth (`...AvgStackDepth`, `StackDepthScale`, `...AvgAltStackDepth`, `AltStackDepthScale`).
    - Gas growth (`...GasPerIteration`, `GasIterationScale`).
    - Also includes the total bytes and total gas consumed per variant for reference.

Use these ratios to spot super-linear behaviour quickly:

- **Time scaling** (`IterationScale`, `ByteScale`): values >1 indicate the operation slows down when the workload grows.
- **Allocation scaling**: highlights memory or serialization hotspots that might need higher gas.
- **Stack scaling**: useful for operations that grow recursion or temporary stack usage.
- **Gas scaling**: confirms whether native/syscall implementations are already charging proportionally.

## Workflow Tips

1. Run the suite once to produce artifacts.
2. Load `benchmark-scaling.csv` into your analysis tool (Excel, pandas, etc.).
3. Filter by component (`Opcode`, `Syscall`, `NativeContract`) and operation id to inspect ratios.
4. Fit piecewise-linear gas models using the `...PerIteration` and `...PerByte` numbers.
5. Cross-check with `TotalGasConsumed` to ensure the runtime fee aligns with the measured cost.

## Extensibility

The recorder can be extended further via `BenchmarkResultRecorder` if additional telemetry is required (e.g., VM reference counts, notification counts). All stack/gas metrics are plumbed through the same APIs used for time and allocation.
