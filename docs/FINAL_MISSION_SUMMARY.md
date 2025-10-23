# Neo N3 Opcode Benchmark Suite

This branch introduces a repeatable BenchmarkDotNet suite that measures the execution cost of Neo N3 VM opcodes. The goal is to provide a trustworthy dataset that can inform future gas-pricing work without making assumptions about the repository layout or relying on ad-hoc tooling.

## Components

- `benchmarks/Neo.VM.Benchmarks/BenchmarkProgram.cs` – entry point that exercises every `[Benchmark]` class in the assembly.
- `benchmarks/Neo.VM.Benchmarks/*OpcodeBenchmarks.cs` – category-focused benchmarks (stack, slot, arithmetic, type, control flow, exception handling, etc.) that emit valid Neo VM scripts via `ScriptBuilder`.
- `benchmarks/Neo.VM.Benchmarks/IndividualOpcodeBenchmarks.cs` – single-opcode micro benchmarks used to capture baseline costs.
- `scripts/aggregate_opcode_benchmarks.py` – utility for consolidating the generated `*-report.csv` files into a single CSV/JSON summary for downstream analysis.
- `benchmarks/README.md` – step-by-step instructions for building, running, and aggregating benchmark results.

## Running the Suite

```bash
cd benchmarks
./run_benchmarks.sh                 # Linux/macOS
# or
dotnet run -c Release --project Neo.VM.Benchmarks/Neo.VM.Benchmarks.csproj -- --filter <pattern>
```

The BenchmarkDotNet artifacts and CSV reports are written to `benchmarks/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/`. Use the Python aggregation script to collate the results:

```bash
python3 scripts/aggregate_opcode_benchmarks.py \
  --input benchmarks/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/results \
  --csv benchmarks/results/benchmark_summary.csv \
  --json benchmarks/results/benchmark_summary.json
```

## Next Steps

1. Validate the generated summary with domain experts and cross-check against existing gas schedules.
2. Use the aggregated measurements as input for modelling dynamic gas pricing (see `docs/DynamicGasPricingWhitepaper.md` for background).
3. Add automated sanity checks (e.g., verify that every opcode appears in the summary) once the data pipeline stabilises.

Contributions and refinements are welcome; please keep documentation aligned with the actual project structure so the benchmark system remains easy to operate.
