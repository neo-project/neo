// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SHR.cs file belongs to the neo project and is free
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
    public class OpCode_SHR
    {
        [ParamsSource(nameof(ShiftParams))]
        public int _shift;

        [ParamsSource(nameof(IntegerParams))]
        public BigInteger _initeger;


        private BenchmarkEngine _engine;

        private const VM.OpCode Opcode = VM.OpCode.SHR;

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
    //
    // | Method | _shift | _initeger            | Mean       | Error     | StdDev    | Median     |
    // |------- |------- |--------------------- |-----------:|----------:|----------:|-----------:|
    // | Bench  | 0      | -5789(...)19968 [78] |   990.8 ns |  30.32 ns |  83.01 ns | 1,000.0 ns |
    // | Bench  | 0      | -9223372036854775808 | 1,016.1 ns |  35.18 ns |  96.30 ns | 1,000.0 ns |
    // | Bench  | 0      | -2147483648          | 1,051.7 ns |  39.34 ns | 107.68 ns | 1,000.0 ns |
    // | Bench  | 0      | 0                    | 1,002.2 ns |  36.03 ns | 101.61 ns | 1,000.0 ns |
    // | Bench  | 0      | 1                    | 1,057.3 ns |  43.49 ns | 120.50 ns | 1,000.0 ns |
    // | Bench  | 0      | 2147483647           | 1,367.7 ns | 135.67 ns | 397.91 ns | 1,200.0 ns |
    // | Bench  | 0      | 9223372036854775807  | 1,025.9 ns |  46.42 ns | 125.50 ns | 1,000.0 ns |
    // | Bench  | 0      | 17014(...)05727 [39] |   993.0 ns |  36.01 ns |  97.97 ns | 1,000.0 ns |
    // | Bench  | 0      | 57896(...)19967 [77] | 1,024.2 ns |  25.13 ns |  70.46 ns | 1,000.0 ns |
    // | Bench  | 2      | -5789(...)19968 [78] | 2,736.8 ns | 112.26 ns | 307.32 ns | 2,600.0 ns |
    // | Bench  | 2      | -9223372036854775808 | 2,629.3 ns |  65.31 ns | 173.20 ns | 2,600.0 ns |
    // | Bench  | 2      | -2147483648          | 2,877.1 ns | 219.52 ns | 633.37 ns | 2,600.0 ns |
    // | Bench  | 2      | 0                    | 2,307.0 ns |  86.39 ns | 235.04 ns | 2,200.0 ns |
    // | Bench  | 2      | 1                    | 2,445.3 ns | 107.67 ns | 292.92 ns | 2,400.0 ns |
    // | Bench  | 2      | 2147483647           | 3,019.2 ns | 262.97 ns | 771.25 ns | 2,800.0 ns |
    // | Bench  | 2      | 9223372036854775807  | 2,661.8 ns | 106.69 ns | 288.43 ns | 2,550.0 ns |
    // | Bench  | 2      | 17014(...)05727 [39] | 2,581.2 ns | 103.76 ns | 280.52 ns | 2,500.0 ns |
    // | Bench  | 2      | 57896(...)19967 [77] | 2,606.7 ns |  78.24 ns | 216.79 ns | 2,500.0 ns |
    // | Bench  | 4      | -5789(...)19968 [78] | 2,598.2 ns |  55.29 ns | 117.84 ns | 2,600.0 ns |
    // | Bench  | 4      | -9223372036854775808 | 3,180.0 ns | 297.27 ns | 876.52 ns | 2,700.0 ns |
    // | Bench  | 4      | -2147483648          | 2,675.0 ns |  86.38 ns | 237.93 ns | 2,600.0 ns |
    // | Bench  | 4      | 0                    | 2,220.0 ns |  71.99 ns | 200.67 ns | 2,200.0 ns |
    // | Bench  | 4      | 1                    | 2,197.7 ns |  66.51 ns | 183.19 ns | 2,100.0 ns |
    // | Bench  | 4      | 2147483647           | 2,241.2 ns |  54.53 ns | 147.43 ns | 2,200.0 ns |
    // | Bench  | 4      | 9223372036854775807  | 2,503.4 ns |  63.83 ns | 175.81 ns | 2,500.0 ns |
    // | Bench  | 4      | 17014(...)05727 [39] | 2,710.8 ns | 124.67 ns | 332.77 ns | 2,600.0 ns |
    // | Bench  | 4      | 57896(...)19967 [77] | 2,631.4 ns |  88.84 ns | 241.69 ns | 2,500.0 ns |
    // | Bench  | 32     | -5789(...)19968 [78] | 2,668.2 ns |  87.35 ns | 236.15 ns | 2,600.0 ns |
    // | Bench  | 32     | -9223372036854775808 | 2,647.6 ns |  85.09 ns | 225.65 ns | 2,600.0 ns |
    // | Bench  | 32     | -2147483648          | 2,350.0 ns |  78.16 ns | 217.88 ns | 2,300.0 ns |
    // | Bench  | 32     | 0                    | 2,219.3 ns |  61.02 ns | 168.06 ns | 2,200.0 ns |
    // | Bench  | 32     | 1                    | 2,303.7 ns |  81.13 ns | 215.13 ns | 2,300.0 ns |
    // | Bench  | 32     | 2147483647           | 2,377.6 ns |  85.30 ns | 230.62 ns | 2,300.0 ns |
    // | Bench  | 32     | 9223372036854775807  | 2,482.0 ns |  84.56 ns | 234.31 ns | 2,400.0 ns |
    // | Bench  | 32     | 17014(...)05727 [39] | 2,654.4 ns | 132.11 ns | 343.36 ns | 2,500.0 ns |
    // | Bench  | 32     | 57896(...)19967 [77] | 3,211.0 ns | 299.76 ns | 883.85 ns | 2,850.0 ns |
    // | Bench  | 64     | -5789(...)19968 [78] | 2,675.3 ns | 114.03 ns | 304.38 ns | 2,550.0 ns |
    // | Bench  | 64     | -9223372036854775808 | 2,354.8 ns |  80.67 ns | 216.71 ns | 2,250.0 ns |
    // | Bench  | 64     | -2147483648          | 2,241.7 ns |  48.25 ns |  80.62 ns | 2,300.0 ns |
    // | Bench  | 64     | 0                    | 2,187.5 ns |  71.52 ns | 197.00 ns | 2,100.0 ns |
    // | Bench  | 64     | 1                    | 2,191.9 ns |  64.09 ns | 174.37 ns | 2,100.0 ns |
    // | Bench  | 64     | 2147483647           | 2,753.6 ns | 244.19 ns | 708.44 ns | 2,400.0 ns |
    // | Bench  | 64     | 9223372036854775807  | 2,827.3 ns | 254.03 ns | 745.03 ns | 2,400.0 ns |
    // | Bench  | 64     | 17014(...)05727 [39] | 3,105.2 ns | 309.24 ns | 892.23 ns | 2,700.0 ns |
    // | Bench  | 64     | 57896(...)19967 [77] | 3,008.2 ns | 246.11 ns | 717.91 ns | 2,600.0 ns |
    // | Bench  | 128    | -5789(...)19968 [78] | 3,236.5 ns | 294.10 ns | 848.54 ns | 2,900.0 ns |
    // | Bench  | 128    | -9223372036854775808 | 2,778.8 ns | 232.34 ns | 681.43 ns | 2,500.0 ns |
    // | Bench  | 128    | -2147483648          | 2,276.2 ns |  75.92 ns | 203.95 ns | 2,200.0 ns |
    // | Bench  | 128    | 0                    | 2,591.1 ns | 180.50 ns | 503.17 ns | 2,400.0 ns |
    // | Bench  | 128    | 1                    | 2,359.6 ns | 111.99 ns | 310.32 ns | 2,200.0 ns |
    // | Bench  | 128    | 2147483647           | 2,829.9 ns | 255.97 ns | 742.63 ns | 2,500.0 ns |
    // | Bench  | 128    | 9223372036854775807  | 2,328.7 ns |  57.06 ns | 156.20 ns | 2,300.0 ns |
    // | Bench  | 128    | 17014(...)05727 [39] | 2,461.6 ns | 108.77 ns | 295.92 ns | 2,400.0 ns |
    // | Bench  | 128    | 57896(...)19967 [77] | 3,096.9 ns | 253.95 ns | 736.75 ns | 2,800.0 ns |
    // | Bench  | 256    | -5789(...)19968 [78] | 2,215.5 ns |  58.50 ns | 157.15 ns | 2,200.0 ns |
    // | Bench  | 256    | -9223372036854775808 | 3,094.0 ns | 289.10 ns | 852.43 ns | 2,750.0 ns |
    // | Bench  | 256    | -2147483648          | 2,357.6 ns | 103.11 ns | 278.76 ns | 2,300.0 ns |
    // | Bench  | 256    | 0                    | 2,446.9 ns |  98.45 ns | 259.37 ns | 2,400.0 ns |
    // | Bench  | 256    | 1                    | 2,829.0 ns | 242.53 ns | 688.03 ns | 2,600.0 ns |
    // | Bench  | 256    | 2147483647           | 2,256.2 ns |  66.40 ns | 183.99 ns | 2,200.0 ns |
    // | Bench  | 256    | 9223372036854775807  | 2,445.3 ns |  92.93 ns | 252.82 ns | 2,350.0 ns |
    // | Bench  | 256    | 17014(...)05727 [39] | 2,570.8 ns | 137.33 ns | 380.55 ns | 2,400.0 ns |
    // | Bench  | 256    | 57896(...)19967 [77] | 2,557.0 ns | 161.71 ns | 439.93 ns | 2,400.0 ns |
}
