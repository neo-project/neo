#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/benchmarks/Neo.Benchmarks/Neo.Benchmarks.csproj"
CONFIGURATION="${CONFIGURATION:-Release}"
FRAMEWORK="${FRAMEWORK:-net10.0}"
OUTPUT_DIR="${OUTPUT_DIR:-BenchmarkDotNet.Artifacts/manual}"

contract_filter=""
method_filter=""
size_filter=""
limit_filter=""
job_filter=""
iterations=""
warmup=""
verbose=0

print_usage() {
    cat <<'EOF'
Usage: scripts/run-native-benchmarks.sh [options] [-- <extra dotnet args>]

Options:
  -c, --contract   Comma/space separated contract wildcard(s) (NEO_NATIVE_BENCH_CONTRACT)
  -m, --method     Method wildcard(s) (NEO_NATIVE_BENCH_METHOD)
  -s, --sizes      Size list e.g. Tiny,Small (NEO_NATIVE_BENCH_SIZES)
  -l, --limit      Maximum number of cases to execute (NEO_NATIVE_BENCH_LIMIT)
  -j, --job        Benchmark job profile: quick | short | default (NEO_NATIVE_BENCH_JOB)
      --iterations Number of measured iterations (manual runner only)
      --warmup     Number of warmup passes (manual runner only)
      --framework  Target framework (default: net10.0)
      --configuration Build configuration (default: Release)
      --output     Manual summary output directory (default: BenchmarkDotNet.Artifacts/manual)
      --verbose    Enable verbose per-case logging
  -h, --help       Show this message

EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        -c|--contract) contract_filter="$2"; shift 2 ;;
        -m|--method) method_filter="$2"; shift 2 ;;
        -s|--sizes) size_filter="$2"; shift 2 ;;
        -l|--limit) limit_filter="$2"; shift 2 ;;
        -j|--job) job_filter="$2"; shift 2 ;;
        --iterations) iterations="$2"; shift 2 ;;
        --warmup) warmup="$2"; shift 2 ;;
        --framework) FRAMEWORK="$2"; shift 2 ;;
        --configuration) CONFIGURATION="$2"; shift 2 ;;
        --output) OUTPUT_DIR="$2"; shift 2 ;;
        --verbose) verbose=1; shift ;;
        -h|--help)
            print_usage
            exit 0
            ;;
        --)
            shift
            break
            ;;
        *)
            echo "Unknown option: $1" >&2
            print_usage >&2
            exit 1
            ;;
    esac
done

extra_args=("$@")

cmd=(
    dotnet run
    -c "$CONFIGURATION"
    --framework "$FRAMEWORK"
    --project "$PROJECT"
    --
    --native-manual-run
    --native-output "$OUTPUT_DIR"
)

[[ -n "$contract_filter" ]] && cmd+=("--native-contract" "$contract_filter")
[[ -n "$method_filter" ]] && cmd+=("--native-method" "$method_filter")
[[ -n "$size_filter" ]] && cmd+=("--native-sizes" "$size_filter")
[[ -n "$limit_filter" ]] && cmd+=("--native-limit" "$limit_filter")
[[ -n "$job_filter" ]] && cmd+=("--native-job" "$job_filter")
[[ -n "$iterations" ]] && cmd+=("--native-iterations" "$iterations")
[[ -n "$warmup" ]] && cmd+=("--native-warmup" "$warmup")
[[ "$verbose" -eq 1 ]] && cmd+=("--native-verbose")
cmd+=("${extra_args[@]}")

echo "[run-native-benchmarks] ${cmd[*]}"
exec "${cmd[@]}"
