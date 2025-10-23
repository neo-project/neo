# Analysing Neo VM Opcode Benchmarks

The benchmark suite captures execution statistics for every opcode to support future gas pricing discussions. This note summarises a pragmatic workflow for turning the raw data into actionable insights.

## 1. Inspect the Raw Results

BenchmarkDotNet produces both markdown and CSV summaries inside `benchmarks/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/results/`. The CSV files are designed for tooling; the markdown files provide a quick human-readable snapshot.

Key columns:

- `Benchmark` / `Category` – identifies the opcode and scenario.
- `Mean`, `StdDev`, `Allocated` – per-iteration characteristics.
- `Params` – encoded parameter values (input lengths, stack depth, etc.).

## 2. Aggregate for Comparison

Use `scripts/aggregate_opcode_benchmarks.py` to stitch every CSV together so that you can sort, filter, and chart the entire dataset in one go. Import the generated CSV into your favourite analysis environment (pandas, Excel, R).

Questions worth answering:

- Which opcodes dominate average execution time?
- How wide is the timing range for parameterised opcodes (min vs. max)?
- Are there opcodes with significant memory allocations that may need special treatment?

## 3. Build Category Summaries

Group the data by opcode category (stack, slot, numeric, control flow, exceptions) to understand relative cost within each area. This step is especially helpful for identifying outliers that warrant special review (e.g., `POW`, `MODEXP`, large `CONVERT` calls).

## 4. Map Timings to Gas

Convert the mean duration to datoshi (or any target gas unit) so you can directly compare against the current gas schedule. This is an iterative process:

1. Choose a baseline unit (e.g., map the median `NOP` timing to its current gas price).
2. Compute proposed gas prices for each opcode.
3. Compare with the existing schedule to highlight large deltas.

## 5. Document Findings

Record the adjusted gas recommendations, the rationale (e.g., linear fit, worst-case timing), and any caveats. Keeping the documentation alongside the benchmark output makes it easier to revisit when the VM changes or optimisations land.

> **Note**  
> This branch does not alter runtime gas costs—it supplies the measurement infrastructure. Any pricing changes should be proposed in a separate feature branch once the analysis is complete.
