# Deriving Dynamic Gas Pricing from Benchmark Data

The benchmark suite in `benchmarks/Neo.VM.Benchmarks` produces per-opcode timing data that can be used to reason about dynamic gas pricing. This document outlines a lightweight workflow for turning the raw measurements into candidate pricing functions.

## 1. Collect Measurements

Run the comprehensive benchmark job and consolidate the resulting CSV files:

```bash
./benchmarks/run_benchmarks.sh
python3 scripts/aggregate_opcode_benchmarks.py \
  --input benchmarks/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/results \
  --csv benchmarks/results/benchmark_summary.csv
```

The aggregated CSV reports, for each opcode variation:

- `mean_ns`, `error_ns`, `stddev_ns`
- Allocation data in bytes
- Benchmark method metadata (`params`, categories, etc.)

## 2. Classify Opcodes

Use the summary to group opcodes into:

- **Constant cost** – variance is minimal across parameter space.
- **Linear / polynomial scaling** – execution time grows predictably with an input parameter (e.g., byte length or collection size).
- **Non-linear / high-variance** – requires guard rails (upper bounds or exponential pricing).

This classification can be automated by calculating e.g. the coefficient of variation or performing a simple regression against the varying parameter recorded in the benchmark name.

## 3. Fit Pricing Functions

For each non-constant opcode:

1. Extract the benchmark rows for that opcode.
2. Parse the parameter payload (e.g., payload length, stack depth).
3. Fit a regression (linear, polynomial, log) that approximates the measured mean.
4. Translate the regression into a gas function:

```
Gas(opcode) = BaseCost(opcode) + Σ parameter_i × Cost(parameter_i)
```

Cap or floor the function using the VM limits (see `Neo.VM.ExecutionEngineLimits`).

## 4. Validate

- Compare the proposed gas function against the observed min/max timings.
- Simulate worst-case inputs within VM limits to confirm the function provides DoS resistance.
- Cross-check with historical gas prices to understand the economic impact.

## 5. Produce a Proposal

Document candidate functions together with:

- The supporting benchmark evidence.
- Any assumptions or extrapolations (e.g., extrapolating from 10 KB data to the limit of 100 KB).
- Migration considerations (network upgrade, compiler updates).

This branch does **not** modify runtime gas prices; it provides the dataset and tooling required to justify future changes.
