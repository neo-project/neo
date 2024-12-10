// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PUSHINT8UtoUPUSHINT256.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System.Numerics;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_PUSHINT8UtoUPUSHINT256
    {

        private BenchmarkEngine _engine;

        [ParamsSource(nameof(PushValues))]
        public BigInteger _value = 1;

        public static IEnumerable<BigInteger> PushValues()
        {
            return
            [
                Benchmark_Opcode.MAX_INT,
                Benchmark_Opcode.MIN_INT,
                short.MaxValue,
                short.MinValue,
                ushort.MaxValue,
                ushort.MinValue,
                int.MaxValue,
                int.MinValue,
                uint.MaxValue,
                uint.MaxValue,
                ulong.MaxValue,
                ulong.MinValue,
                long.MaxValue,
                long.MinValue,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16,
                -1
            ];
        }

        [IterationSetup]
        public void Setup()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(VM.OpCode.NOP);

            builder.Push(_value);
            _engine = new BenchmarkEngine();
            _engine.LoadScript(builder.ToArray());
            _engine.ExecuteUntil(VM.OpCode.NOP);
            // _engine.ExecuteNext();
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _engine.Dispose();
        }

        [Benchmark]
        public void Bench() => _engine.ExecuteNext();
    }
}


// | Method | _value               | Mean     | Error    | StdDev   | Median   |
// |------- |--------------------- |---------:|---------:|---------:|---------:|
// | Bench  | -5789(...)19968 [78] | 12.36 us | 0.234 us | 0.473 us | 12.30 us |
// | Bench  | -9223372036854775808 | 11.94 us | 0.240 us | 0.257 us | 11.90 us |
// | Bench  | -2147483648          | 11.84 us | 0.240 us | 0.267 us | 11.80 us |
// | Bench  | -32768               | 11.90 us | 0.241 us | 0.287 us | 11.90 us |
// | Bench  | -1                   | 12.60 us | 0.255 us | 0.662 us | 12.60 us |
// | Bench  | 0                    | 11.90 us | 0.242 us | 0.347 us | 11.85 us |
// | Bench  | 0                    | 11.77 us | 0.234 us | 0.219 us | 11.70 us |
// | Bench  | 1                    | 10.77 us | 0.210 us | 0.175 us | 10.80 us |
// | Bench  | 2                    | 11.77 us | 0.232 us | 0.205 us | 11.80 us |
// | Bench  | 3                    | 12.13 us | 0.218 us | 0.340 us | 12.10 us |
// | Bench  | 4                    | 11.91 us | 0.232 us | 0.228 us | 11.95 us |
// | Bench  | 5                    | 11.98 us | 0.231 us | 0.204 us | 12.00 us |
// | Bench  | 6                    | 11.99 us | 0.236 us | 0.307 us | 11.95 us |
// | Bench  | 7                    | 12.01 us | 0.241 us | 0.225 us | 12.00 us |
// | Bench  | 8                    | 11.84 us | 0.213 us | 0.236 us | 11.90 us |
// | Bench  | 9                    | 12.90 us | 0.485 us | 1.321 us | 12.40 us |
// | Bench  | 10                   | 12.16 us | 0.245 us | 0.410 us | 12.15 us |
// | Bench  | 11                   | 11.77 us | 0.228 us | 0.224 us | 11.70 us |
// | Bench  | 12                   | 12.17 us | 0.245 us | 0.466 us | 12.20 us |
// | Bench  | 13                   | 11.86 us | 0.254 us | 0.701 us | 12.00 us |
// | Bench  | 14                   | 12.22 us | 0.246 us | 0.485 us | 12.10 us |
// | Bench  | 15                   | 12.06 us | 0.245 us | 0.429 us | 12.00 us |
// | Bench  | 16                   | 11.77 us | 0.236 us | 0.209 us | 11.80 us |
// | Bench  | 32767                | 11.68 us | 0.231 us | 0.257 us | 11.60 us |
// | Bench  | 65535                | 11.73 us | 0.238 us | 0.356 us | 11.70 us |
// | Bench  | 2147483647           | 11.67 us | 0.229 us | 0.273 us | 11.70 us |
// | Bench  | 4294967295           | 11.76 us | 0.297 us | 0.838 us | 11.90 us |
// | Bench  | 4294967295           | 12.07 us | 0.240 us | 0.235 us | 12.05 us |
// | Bench  | 9223372036854775807  | 11.73 us | 0.265 us | 0.756 us | 11.90 us |
// | Bench  | 18446744073709551615 | 12.11 us | 0.243 us | 0.357 us | 12.00 us |
// | Bench  | 57896(...)19967 [77] | 12.17 us | 0.246 us | 0.411 us | 12.05 us |

// | Method | _value               | Mean     | Error    | StdDev   |
// |------- |--------------------- |---------:|---------:|---------:|
// | Bench  | -5789(...)19968 [78] | 45.81 ms | 0.552 ms | 0.461 ms |
// | Bench  | -9223372036854775808 | 56.57 ms | 1.085 ms | 1.114 ms |
// | Bench  | -2147483648          | 53.33 ms | 1.039 ms | 1.155 ms |
// | Bench  | -32768               | 51.77 ms | 0.903 ms | 0.845 ms |
// | Bench  | -1                   | 49.45 ms | 0.935 ms | 0.918 ms |
// | Bench  | 0                    | 47.41 ms | 0.921 ms | 1.350 ms |
// | Bench  | 0                    | 46.69 ms | 0.910 ms | 0.760 ms |
// | Bench  | 1                    | 49.14 ms | 0.980 ms | 1.436 ms |
// | Bench  | 2                    | 48.50 ms | 0.908 ms | 0.805 ms |
// | Bench  | 3                    | 48.97 ms | 0.791 ms | 0.777 ms |
// | Bench  | 4                    | 48.89 ms | 0.814 ms | 0.799 ms |
// | Bench  | 5                    | 48.50 ms | 0.928 ms | 0.868 ms |
// | Bench  | 6                    | 49.49 ms | 0.856 ms | 0.800 ms |
// | Bench  | 7                    | 48.07 ms | 0.715 ms | 0.634 ms |
// | Bench  | 8                    | 49.38 ms | 0.980 ms | 0.962 ms |
// | Bench  | 9                    | 48.53 ms | 0.852 ms | 0.712 ms |
// | Bench  | 10                   | 47.84 ms | 0.903 ms | 0.801 ms |
// | Bench  | 11                   | 49.12 ms | 0.976 ms | 1.234 ms |
// | Bench  | 12                   | 48.74 ms | 0.962 ms | 1.181 ms |
// | Bench  | 13                   | 47.40 ms | 0.724 ms | 0.642 ms |
// | Bench  | 14                   | 48.78 ms | 0.935 ms | 0.874 ms |
// | Bench  | 15                   | 47.61 ms | 0.840 ms | 0.967 ms |
// | Bench  | 16                   | 48.60 ms | 0.810 ms | 0.757 ms |
// | Bench  | 32767                | 50.58 ms | 0.742 ms | 0.695 ms |
// | Bench  | 65535                | 51.33 ms | 0.377 ms | 0.294 ms |
// | Bench  | 2147483647           | 52.12 ms | 1.010 ms | 1.080 ms |
// | Bench  | 4294967295           | 53.21 ms | 0.954 ms | 0.893 ms |
// | Bench  | 4294967295           | 54.29 ms | 0.812 ms | 0.678 ms |
// | Bench  | 9223372036854775807  | 54.69 ms | 1.091 ms | 1.380 ms |
// | Bench  | 18446744073709551615 | 38.34 ms | 0.732 ms | 0.977 ms |
// | Bench  | 57896(...)19967 [77] | 42.34 ms | 0.845 ms | 0.904 ms |
