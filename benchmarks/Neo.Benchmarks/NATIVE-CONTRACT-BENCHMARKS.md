Native Contract Benchmarks
==========================

This suite exercises every discoverable native contract method in the Neo
runtime, fabricates the chain state those contracts expect (committee witnesses,
oracle requests, notary deposits, etc.), and reports their execution cost across
the canonical input profiles (`Tiny`, `Small`, `Medium`, `Large`).

Running the suite
-----------------

There are two runners:

### Manual harness (default & CI-friendly)

```
dotnet run -c Release --framework net10.0 -- \
    --native-manual-run \
    --native-iterations 3 \
    --native-warmup 0 \
    --native-output BenchmarkDotNet.Artifacts/manual
```

The manual runner:

- Discovers contracts/methods via reflection and generates benchmark cases for
  every profile.
- Spins up a seeded `NeoSystem` so witness checks, notary validation, and
  candidate registration succeed without special-casing individual contracts.
- Logs progress to stdout (add `--native-verbose` or
  `NEO_NATIVE_BENCH_VERBOSE=1` for per-case telemetry).
- Emits `BenchmarkDotNet.Artifacts/manual/manual-native-contract-summary.txt`
  plus a JSON companion file. Both include coverage counts, per-method stats,
  and a list of any filtered cases.
- Supports inline filter overrides. Pass `--native-contract`, `--native-method`,
  `--native-sizes`, `--native-limit`, or `--native-job` to override the
  corresponding environment variables for a single run (values can be comma or
  space separated, just like the env-based filters).

### Shortcut script

For convenience, the repository ships with
`scripts/run-native-benchmarks.sh`, which wraps the manual runner and exposes
the most common options:

```
scripts/run-native-benchmarks.sh \
    --contract NeoToken \
    --method "get*,onNEP17Payment" \
    --sizes Tiny,Small \
    --limit 50 \
    --iterations 3 \
    --warmup 0 \
    --verbose
```

Any additional arguments placed after `--` are forwarded directly to `dotnet
run`, so you can still customise the build configuration or framework.

By default the manual runner uses the *Balanced* profile (20 measured iterations,
3 warmup passes, 10% trimmed mean). Switch to `--native-job quick` for the old
smoke-test behaviour, or `--native-job thorough` for 40 iterations / 5 warmups
with heavier outlier trimming.

### BenchmarkDotNet (high-fidelity measurements)

```
dotnet run -c Release --framework net10.0 -- -f '*NativeContractMethodBenchmarks*'
```

Use this variant when you can afford a longer run and want BDN's job/diagnostic
output. It relies on the same discovery and argument generation pipeline but
lets you pick the built-in BDN jobs via `NEO_NATIVE_BENCH_JOB=Quick|Short|Default`.

Focusing on a subset
--------------------

Running every contract * every input size takes time. Narrow a run with the
following environment variables (applies to both runners unless noted):

| Variable | Description |
|----------|-------------|
| `NEO_NATIVE_BENCH_CONTRACT` | Comma/space-separated wildcard patterns that match contract names (e.g. `StdLib, NeoToken`). |
| `NEO_NATIVE_BENCH_METHOD` | Wildcard patterns applied to method names (case-insensitive, e.g. `get*`, `*Payment`). |
| `NEO_NATIVE_BENCH_SIZES` | Restrict workload sizes (`Tiny`, `Small`, `Medium`, `Large`). Multiple values allowed. |
| `NEO_NATIVE_BENCH_LIMIT` | Stop after the first _N_ benchmark cases. |
| `NEO_NATIVE_BENCH_JOB` | BenchmarkDotNet only - select `Quick`, `Short`, or `Default`. |
| `NEO_NATIVE_BENCH_ITERATIONS` | Manual runner only - override measured iterations per case (default `5`). |
| `NEO_NATIVE_BENCH_WARMUP` | Manual runner only - warmup passes before measuring (default `1`). |
| `NEO_NATIVE_BENCH_VERBOSE` | Manual runner only - `1/true` streams per-case statistics. |

Example - only benchmark `StdLib` methods with tiny inputs:

```
NEO_NATIVE_BENCH_CONTRACT=StdLib \
NEO_NATIVE_BENCH_SIZES=Tiny \
dotnet run -c Release --framework net10.0 -- --native-manual-run
```
