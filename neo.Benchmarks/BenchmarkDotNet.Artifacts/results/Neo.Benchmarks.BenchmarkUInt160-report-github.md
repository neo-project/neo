``` ini

BenchmarkDotNet=v0.11.1, OS=ubuntu 18.04
Intel Core i5-4210U CPU 1.70GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=2.2.300
  [Host] : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT


```
|                     Method | Mean | Error | Min | Max | Median | Scaled | ScaledSD | Rank |
|--------------------------- |-----:|------:|----:|----:|-------:|-------:|---------:|-----:|
| Benchmark_Official_UInt160 |   NA |    NA |  NA |  NA |     NA |      ? |        ? |    ? |
|    Benchmark_Code1_UInt160 |   NA |    NA |  NA |  NA |     NA |      ? |        ? |    ? |
|    Benchmark_Code2_UInt160 |   NA |    NA |  NA |  NA |     NA |      ? |        ? |    ? |
|    Benchmark_Code3_UInt160 |   NA |    NA |  NA |  NA |     NA |      ? |        ? |    ? |

Benchmarks with issues:
  BenchmarkUInt160.Benchmark_Official_UInt160: DefaultJob
  BenchmarkUInt160.Benchmark_Code1_UInt160: DefaultJob
  BenchmarkUInt160.Benchmark_Code2_UInt160: DefaultJob
  BenchmarkUInt160.Benchmark_Code3_UInt160: DefaultJob
