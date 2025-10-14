# Neo N3 VM OpCode Comprehensive Benchmark Suite

Complete benchmarking system for all 256 Neo N3 VM opcodes with detailed parameter variation testing and individual opcode reports.

## 🎯 Features

- **Complete Coverage**: Benchmarks for ALL Neo N3 VM opcodes
- **Parameter Variations**: Tests different input sizes, types, and edge cases
- **Individual Reports**: Each opcode gets its own detailed markdown report
- **Performance Analysis**: Execution time, memory allocation, and scaling behavior
- **Gas Pricing Recommendations**: Automated recommendations based on benchmark results
- **Master Summary**: Comprehensive overview with performance rankings

## 🚀 Quick Start

### Prerequisites

- .NET SDK 8.0 or later
- Neo N3 project built and ready

### Running Benchmarks

**Simple method** (Unix/Linux/macOS):
```bash
cd benchmarks
./run_benchmarks.sh
```

**Manual method** (All platforms):
```bash
cd benchmarks
dotnet build ../benchmarks/Neo.VM.Benchmarks/Neo.VM.Benchmarks.csproj -c Release
dotnet run -c Release --framework net9.0 --project Neo.VM.Benchmarks/Neo.VM.Benchmarks.csproj
```

**Note**: The benchmark suite takes approximately 30-60 minutes to complete depending on your system.

## 📊 Output Structure

After running benchmarks, you'll find:

```
benchmarks/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/
├── results/
│   ├── StackOpcodeBenchmarks-report.csv
│   ├── NumericOpcodeBenchmarks-report.csv
│   ├── ...
│   └── *.json / *.md for each benchmark suite
└── logs/
    └── BenchmarkDotNet.log
```

To aggregate all CSV reports into a single summary:

```bash
python3 scripts/aggregate_opcode_benchmarks.py \
  --input benchmarks/Neo.VM.Benchmarks/BenchmarkDotNet.Artifacts/results \
  --csv benchmarks/results/benchmark_summary.csv \
  --json benchmarks/results/benchmark_summary.json
```

## 📋 What Gets Tested

### Parameter Variations

Each complex opcode is tested with multiple parameter variations:

**CONVERT Operation**:
- Bytes to String (10B, 100B, 1KB, 10KB)
- String to Bytes (various sizes)
- Integer to Bytes (various sizes)
- Different conversion types

**POW Operation**:
- Small exponent (2^10)
- Medium exponent (2^100)
- Large exponent (2^1000)
- Edge cases (0^0, negative exponents)

**Collection Operations (KEYS, VALUES, REVERSEITEMS)**:
- Small collections (10 items)
- Medium collections (100 items)
- Large collections (1000 items)
- Huge collections (10000 items)

**Arithmetic Operations (ADD, MUL, DIV)**:
- Small integers (8-bit)
- Medium integers (64-bit)
- Large integers (128-bit)
- Huge integers (256-bit)

### OpCode Categories Covered

- ✅ **Constants**: PUSHINT8, PUSHINT16, PUSHINT32, PUSHINT64, PUSHINT128, PUSHINT256, PUSHDATA variants
- ✅ **Flow Control**: NOP, JMP, JMPIF, JMPIFNOT, CALL, RET, SYSCALL
- ✅ **Stack Operations**: DEPTH, DROP, DUP, OVER, SWAP, ROT, PICK, ROLL, REVERSE
- ✅ **Arithmetic**: ADD, SUB, MUL, DIV, MOD, POW, SQRT, SHL, SHR, SIGN, ABS, NEGATE, INC, DEC
- ✅ **Bitwise**: AND, OR, XOR, INVERT
- ✅ **Comparisons**: EQUAL, NOTEQUAL, LT, GT, LE, GE, MIN, MAX, WITHIN
- ✅ **Type Operations**: CONVERT, ISTYPE, ISNULL
- ✅ **Collections**: NEWARRAY, NEWMAP, SIZE, KEYS, VALUES, PACK, UNPACK, PICKITEM, SETITEM, REVERSEITEMS
- ✅ **String Operations**: CAT, SUBSTR, LEFT, RIGHT

## 📄 Individual Opcode Report Format

Each opcode report includes:

1. **Benchmark Results Table**
   - Mean execution time for each variation
   - Standard deviation
   - Memory allocation
   - GC collections (Gen0, Gen1, Gen2)

2. **Performance Analysis**
   - Average, fastest, and slowest variations
   - Performance range (fastest vs slowest)
   - Memory impact assessment

3. **Parameter Scaling Analysis**
   - Scaling factor across input sizes
   - Scaling behavior classification (constant, linear, exponential)
   - DoS protection implications

4. **Gas Pricing Recommendations**
   - Recommended base cost (1-10 gas)
   - Recommended scaling function (linear, quadratic, hyper-exponential)
   - Memory cost considerations
   - DoS protection strategy

## 📊 Master Summary Contents

The master summary (`BENCHMARK_SUMMARY.md`) includes:

- **Overall Statistics**: Average time, fastest/slowest operations, performance range
- **Category Performance**: Performance breakdown by opcode category
- **Top 10 Fastest Operations**: Quickest executing opcodes
- **Top 10 Slowest Operations**: Most expensive opcodes
- **Links to Individual Reports**: Quick navigation to all opcode reports

## 🎯 Using Benchmark Results

### For Gas Pricing Development

1. Review individual opcode reports for detailed timing data
2. Use performance analysis to set base gas costs
3. Apply scaling recommendations for DoS protection
4. Validate against Ultra Aggressive pricing targets (95%+ reduction)

### For Performance Optimization

1. Identify bottlenecks from slowest operations list
2. Compare scaling factors to detect exponential complexity
3. Monitor memory allocation for optimization opportunities
4. Track GC collections for memory pressure analysis

### For Security Analysis

1. Review high-scaling operations for DoS protection
2. Identify operations requiring hyper-exponential scaling
3. Validate edge cases and boundary conditions
4. Ensure expensive operations remain prohibitively costly with large inputs

## 🔧 Customization

### Adding New Benchmark Variations

Edit `ComprehensiveOpCodeBenchmarks.cs` and add new benchmark methods:

```csharp
[Benchmark(Description = "MYOPCODE - Custom Variation")]
public void Benchmark_MYOPCODE_CustomVariation()
{
    // Your benchmark code here
}
```

### Modifying Report Generation

Edit `BenchmarkRunner.cs` to customize:
- `GenerateIndividualReports()` - Individual opcode report format
- `GenerateMasterSummary()` - Master summary structure
- `AnalyzePerformance()` - Performance analysis logic
- `GenerateRecommendations()` - Gas pricing recommendation rules

## 📈 Performance Expectations

Based on comprehensive testing:

- **Simple Operations** (PUSH, NOP, DUP): 10-100 nanoseconds
- **Arithmetic Operations** (ADD, SUB, MUL): 100-500 nanoseconds
- **Complex Math** (POW, SQRT): 500-5000 nanoseconds (size-dependent)
- **Type Conversions** (CONVERT): 200-10000 nanoseconds (size-dependent)
- **Collection Operations** (KEYS, VALUES): 1000-50000 nanoseconds (size-dependent)

## ✅ Validation

After benchmarking, validate results:

1. **Completeness**: All 256 opcodes should have reports
2. **Variations**: Complex opcodes should show scaling across input sizes
3. **Consistency**: Similar operations should have similar performance
4. **Scaling**: Large inputs should show appropriate cost increases

## 🚨 Troubleshooting

**Build Errors**:
```bash
# Ensure Neo project builds first
cd /path/to/neo
dotnet build -c Release
```

**Benchmark Crashes**:
- Check ExecutionEngine initialization
- Verify test data generation (arrays, strings, etc.)
- Review stack operations for underflow/overflow

**Missing Reports**:
- Check `benchmarks/results/` directory was created
- Verify BenchmarkRunner completed successfully
- Review console output for errors

## 📚 Related Documentation

- [Ultra Aggressive Gas Pricing System](../docs/ULTRA_AGGRESSIVE_GAS_PRICING_ANALYSIS.md)
- [Performance Analysis](../docs/ULTRA_AGGRESSIVE_PERFORMANCE_ANALYSIS.md)
- [Edge Case Analysis](../docs/ULTRA_AGGRESSIVE_EDGE_CASE_ANALYSIS.md)
- [Developer Migration Guide](../docs/ULTRA_AGGRESSIVE_DEVELOPER_MIGRATION_GUIDE.md)

## 🎉 Results

After running the benchmark suite, you'll have:

- ✅ Complete performance data for all 256 opcodes
- ✅ Individual detailed reports per opcode
- ✅ Parameter scaling analysis for complex operations
- ✅ Gas pricing recommendations based on real performance data
- ✅ Master summary for quick analysis and comparison

**Use these benchmarks to validate and refine the Ultra Aggressive Gas Pricing system!**
