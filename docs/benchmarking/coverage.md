# Benchmark Coverage Guidance

The benchmark harness can report which syscalls and native contract methods are still missing automated scenarios.

## Generating reports

1. Build the benchmark project and run the suites with coverage reporting enabled:

   ```bash
   export NEO_VM_BENCHMARK=1
   export NEO_BENCHMARK_COVERAGE=1
   dotnet run --project benchmarks/Neo.VM.Benchmarks/Neo.VM.Benchmarks.csproj
   ```

2. After the run completes, CSV files are written to the directory specified by
   `NEO_BENCHMARK_ARTIFACTS` (defaults to `BenchmarkArtifacts/` under the build output):

   - `opcode-missing.csv` – opcodes without registered scenarios. The suite also writes `opcode-coverage.csv` with the full opcode registry.
   - `syscall-missing.csv` – syscalls exercised vs. the runtime registry.
   - `native-missing.csv` – native contract methods touched by the suite.
   - `interop-missing.csv` – combined summary written by the benchmark launcher.

   Each CSV entry contains the category (`syscall` or `native`) and the identifier that still
   requires a scenario. If a file is empty, that category currently has full coverage.

3. The console output also summarizes the missing items whenever `NEO_BENCHMARK_COVERAGE=1` is set. The command exits with a non-zero code when coverage gaps remain for opcodes, syscalls, or native methods, allowing CI pipelines to fail fast if new interops or instructions lack scenarios.

## Keeping suites up to date

- When adding new syscalls or native contract methods upstream, the coverage report will surface them
  as missing. Add scenarios to the appropriate factory (`SyscallScenarioFactory` or
  `NativeScenarioFactory`), then rerun the coverage build to confirm they disappear from the report.
- If a category deliberately omits certain items (e.g., interops that require complex state), document
  the rationale beside those cases in the factories so future contributors understand what remains.
