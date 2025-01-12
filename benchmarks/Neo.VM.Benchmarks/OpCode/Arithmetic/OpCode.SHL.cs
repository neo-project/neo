// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SHL.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Extensions;
using System.Numerics;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_SHL
    {
        [ParamsSource(nameof(ShiftParams))]
        public int _shift = 256;

        [ParamsSource(nameof(IntegerParams))]
        public BigInteger _initeger = Benchmark_Opcode.MAX_INT;

        private BenchmarkEngine _engine;

        private const VM.OpCode Opcode = VM.OpCode.SHL;

        public static IEnumerable<int> ShiftParams()
        {
            return [
                0,
                2,
                4,
                32,
                64,
                128,
                256
              ];
        }

        public static IEnumerable<BigInteger> IntegerParams()
        {
            return
            [
                Benchmark_Opcode.MAX_INT,
                Benchmark_Opcode.MIN_INT,
                BigInteger.One,
                BigInteger.Zero,
                int.MaxValue,
                int.MinValue,
                long.MaxValue,
                long.MinValue,
                BigInteger.Parse("170141183460469231731687303715884105727") // Mersenne prime 2^127 - 1
            ];
        }

        [IterationSetup]
        public void Setup()
        {
            var builder = new InstructionBuilder();
            builder.Push(_initeger);
            builder.Push(_shift);
            builder.AddInstruction(Opcode);

            _engine = new BenchmarkEngine();
            _engine.LoadScript(builder.ToArray());
            _engine.ExecuteUntil(Opcode);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _engine.Dispose();
        }

        [Benchmark]
        public void Bench()
        {
            _engine.ExecuteNext();
        }
    }

    // NA means there is an uncatchable exception thrown, transaction fault.

    // | Method | _shift | _initeger            | Mean       | Error     | StdDev      | Median     |
    // |------- |------- |--------------------- |-----------:|----------:|------------:|-----------:|
    // | Bench  | 0      | -5789(...)19968 [78] |   973.5 ns |  33.65 ns |    89.83 ns | 1,000.0 ns |
    // | Bench  | 0      | -9223372036854775808 | 1,092.2 ns |  59.42 ns |   158.59 ns | 1,050.0 ns |
    // | Bench  | 0      | -2147483648          | 1,087.2 ns |  32.90 ns |    89.50 ns | 1,050.0 ns |
    // | Bench  | 0      | 0                    |   958.2 ns |  30.57 ns |    85.72 ns |   900.0 ns |
    // | Bench  | 0      | 1                    | 1,098.8 ns |  39.67 ns |   107.92 ns | 1,100.0 ns |
    // | Bench  | 0      | 2147483647           | 1,008.0 ns |  32.20 ns |    88.70 ns | 1,000.0 ns |
    // | Bench  | 0      | 9223372036854775807  |   957.1 ns |  24.71 ns |    66.38 ns |   900.0 ns |
    // | Bench  | 0      | 17014(...)05727 [39] |   985.2 ns |  32.82 ns |    90.39 ns | 1,000.0 ns |
    // | Bench  | 0      | 57896(...)19967 [77] | 1,053.4 ns |  48.13 ns |   132.57 ns | 1,050.0 ns |
    // | Bench  | 2      | -5789(...)19968 [78] |         NA |        NA |          NA |         NA |
    // | Bench  | 2      | -9223372036854775808 | 2,535.1 ns |  71.24 ns |   195.01 ns | 2,450.0 ns |
    // | Bench  | 2      | -2147483648          | 2,602.9 ns |  94.23 ns |   254.76 ns | 2,550.0 ns |
    // | Bench  | 2      | 0                    | 2,855.0 ns | 279.44 ns |   823.93 ns | 2,450.0 ns |
    // | Bench  | 2      | 1                    | 2,507.0 ns | 129.53 ns |   352.40 ns | 2,400.0 ns |
    // | Bench  | 2      | 2147483647           | 2,458.8 ns |  80.37 ns |   217.29 ns | 2,400.0 ns |
    // | Bench  | 2      | 9223372036854775807  | 2,460.9 ns |  69.88 ns |   191.30 ns | 2,400.0 ns |
    // | Bench  | 2      | 17014(...)05727 [39] | 2,683.0 ns |  96.33 ns |   265.32 ns | 2,600.0 ns |
    // | Bench  | 2      | 57896(...)19967 [77] |         NA |        NA |          NA |         NA |
    // | Bench  | 4      | -5789(...)19968 [78] |         NA |        NA |          NA |         NA |
    // | Bench  | 4      | -9223372036854775808 | 2,623.8 ns |  71.38 ns |   191.77 ns | 2,600.0 ns |
    // | Bench  | 4      | -2147483648          | 2,664.0 ns | 111.26 ns |   308.31 ns | 2,500.0 ns |
    // | Bench  | 4      | 0                    | 2,213.9 ns |  48.16 ns |   119.04 ns | 2,200.0 ns |
    // | Bench  | 4      | 1                    | 2,350.0 ns |  79.74 ns |   216.93 ns | 2,250.0 ns |
    // | Bench  | 4      | 2147483647           | 3,684.8 ns | 465.46 ns | 1,365.12 ns | 3,100.0 ns |
    // | Bench  | 4      | 9223372036854775807  | 2,745.3 ns | 133.50 ns |   363.21 ns | 2,600.0 ns |
    // | Bench  | 4      | 17014(...)05727 [39] | 2,495.4 ns |  67.75 ns |   185.46 ns | 2,400.0 ns |
    // | Bench  | 4      | 57896(...)19967 [77] |         NA |        NA |          NA |         NA |
    // | Bench  | 32     | -5789(...)19968 [78] |         NA |        NA |          NA |         NA |
    // | Bench  | 32     | -9223372036854775808 | 2,464.3 ns |  52.73 ns |   113.50 ns | 2,500.0 ns |
    // | Bench  | 32     | -2147483648          | 2,404.4 ns |  51.30 ns |    97.60 ns | 2,400.0 ns |
    // | Bench  | 32     | 0                    | 2,198.9 ns |  66.50 ns |   182.04 ns | 2,200.0 ns |
    // | Bench  | 32     | 1                    | 2,387.1 ns |  87.11 ns |   241.39 ns | 2,450.0 ns |
    // | Bench  | 32     | 2147483647           | 2,829.2 ns | 182.98 ns |   507.04 ns | 2,700.0 ns |
    // | Bench  | 32     | 9223372036854775807  | 2,474.7 ns |  52.32 ns |   143.23 ns | 2,400.0 ns |
    // | Bench  | 32     | 17014(...)05727 [39] | 2,604.1 ns |  56.66 ns |   153.18 ns | 2,550.0 ns |
    // | Bench  | 32     | 57896(...)19967 [77] |         NA |        NA |          NA |         NA |
    // | Bench  | 64     | -5789(...)19968 [78] |         NA |        NA |          NA |         NA |
    // | Bench  | 64     | -9223372036854775808 | 2,392.1 ns |  51.68 ns |   143.20 ns | 2,400.0 ns |
    // | Bench  | 64     | -2147483648          | 2,566.7 ns |  86.38 ns |   240.79 ns | 2,500.0 ns |
    // | Bench  | 64     | 0                    | 2,229.5 ns |  44.20 ns |   114.08 ns | 2,200.0 ns |
    // | Bench  | 64     | 1                    | 2,436.8 ns |  58.46 ns |   160.03 ns | 2,400.0 ns |
    // | Bench  | 64     | 2147483647           | 2,520.0 ns |  69.54 ns |   193.84 ns | 2,500.0 ns |
    // | Bench  | 64     | 9223372036854775807  | 2,405.7 ns |  65.68 ns |   180.90 ns | 2,400.0 ns |
    // | Bench  | 64     | 17014(...)05727 [39] | 2,565.1 ns |  58.80 ns |   159.98 ns | 2,500.0 ns |
    // | Bench  | 64     | 57896(...)19967 [77] |         NA |        NA |          NA |         NA |
    // | Bench  | 128    | -5789(...)19968 [78] |         NA |        NA |          NA |         NA |
    // | Bench  | 128    | -9223372036854775808 | 2,575.0 ns |  53.23 ns |   143.01 ns | 2,500.0 ns |
    // | Bench  | 128    | -2147483648          | 2,431.8 ns |  63.62 ns |   175.21 ns | 2,400.0 ns |
    // | Bench  | 128    | 0                    | 2,240.4 ns |  47.39 ns |   126.51 ns | 2,250.0 ns |
    // | Bench  | 128    | 1                    | 2,568.2 ns | 124.16 ns |   335.66 ns | 2,500.0 ns |
    // | Bench  | 128    | 2147483647           | 2,295.7 ns |  71.58 ns |   204.21 ns | 2,300.0 ns |
    // | Bench  | 128    | 9223372036854775807  | 2,492.7 ns |  57.69 ns |   152.98 ns | 2,500.0 ns |
    // | Bench  | 128    | 17014(...)05727 [39] | 2,536.8 ns |  58.46 ns |   160.03 ns | 2,500.0 ns |
    // | Bench  | 128    | 57896(...)19967 [77] |         NA |        NA |          NA |         NA |
    // | Bench  | 256    | -5789(...)19968 [78] |         NA |        NA |          NA |         NA |
    // | Bench  | 256    | -9223372036854775808 |         NA |        NA |          NA |         NA |
    // | Bench  | 256    | -2147483648          |         NA |        NA |          NA |         NA |
    // | Bench  | 256    | 0                    | 2,306.8 ns |  93.06 ns |   256.32 ns | 2,200.0 ns |
    // | Bench  | 256    | 1                    |         NA |        NA |          NA |         NA |
    // | Bench  | 256    | 2147483647           |         NA |        NA |          NA |         NA |
    // | Bench  | 256    | 9223372036854775807  |         NA |        NA |          NA |         NA |
    // | Bench  | 256    | 17014(...)05727 [39] |         NA |        NA |          NA |         NA |
    // | Bench  | 256    | 57896(...)19967 [77] |         NA |        NA |          NA |         NA |

}
