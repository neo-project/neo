``` ini

BenchmarkDotNet=v0.11.1, OS=ubuntu 18.04
Intel Core i5-4210U CPU 1.70GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=2.2.300
  [Host]     : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT


```
|                      Method |     Mean |    Error |    StdDev |      Min |      Max |   Median | Rank |
|---------------------------- |---------:|---------:|----------:|---------:|---------:|---------:|-----:|
| Benchmark_CompareTo_UInt256 | 123.5 us | 1.164 us | 0.9719 us | 122.3 us | 125.4 us | 123.5 us |    2 |
| Benchmark_CompareTo_UInt160 | 107.8 us | 1.996 us | 1.7693 us | 105.2 us | 112.0 us | 107.4 us |    1 |
