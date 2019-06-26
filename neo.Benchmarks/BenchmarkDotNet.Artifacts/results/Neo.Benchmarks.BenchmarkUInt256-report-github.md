``` ini

BenchmarkDotNet=v0.11.1, OS=ubuntu 18.04
Intel Core i5-4210U CPU 1.70GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=2.2.300
  [Host]     : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT


```
|                     Method |     Mean |    Error |   StdDev |   Median |      Min |      Max | Scaled | ScaledSD | Rank |
|--------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|-------:|---------:|-----:|
| Benchmark_Official_UInt256 | 56.72 us | 1.226 us | 3.537 us | 55.59 us | 52.67 us | 66.44 us |   1.00 |     0.00 |    2 |
|    Benchmark_Code1_UInt256 | 72.52 us | 1.387 us | 1.362 us | 72.15 us | 71.09 us | 75.74 us |   1.28 |     0.08 |    4 |
|    Benchmark_Code2_UInt256 | 57.98 us | 1.210 us | 2.990 us | 57.27 us | 53.39 us | 66.63 us |   1.03 |     0.08 |    3 |
|    Benchmark_Code3_UInt256 | 54.41 us | 1.231 us | 3.551 us | 53.53 us | 48.85 us | 63.87 us |   0.96 |     0.08 |    1 |
