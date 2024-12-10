// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SIGN.cs file belongs to the neo project and is free
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
    public class OpCode_SIGN
    {
        private BenchmarkEngine _engine;

        private VM.OpCode Opcode => VM.OpCode.SIGN;

        [ParamsSource(nameof(Values))]
        public BigInteger _value;


        public static IEnumerable<BigInteger> Values =>
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

        [IterationSetup]
        public void Setup()
        {
            var builder = new InstructionBuilder();
            builder.Push(_value);
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
        public void Bench() => _engine.ExecuteNext();
    }

    // | Method | _value               | Mean     | Error     | StdDev    | Median   |
    //     |------- |--------------------- |---------:|----------:|----------:|---------:|
    //     | Bench  | -5789(...)19968 [78] | 1.958 us | 0.0639 us | 0.1728 us | 1.900 us |
    //     | Bench  | -9223372036854775808 | 1.900 us | 0.0418 us | 0.0863 us | 1.900 us |
    //     | Bench  | -2147483648          | 1.952 us | 0.0835 us | 0.2328 us | 1.900 us |
    //     | Bench  | 0                    | 1.831 us | 0.0579 us | 0.1514 us | 1.800 us |
    //     | Bench  | 1                    | 1.837 us | 0.0403 us | 0.0901 us | 1.800 us |
    //     | Bench  | 2147483647           | 1.808 us | 0.0394 us | 0.0786 us | 1.800 us |
    //     | Bench  | 9223372036854775807  | 1.934 us | 0.0628 us | 0.1665 us | 1.900 us |
    //     | Bench  | 17014(...)05727 [39] | 1.933 us | 0.0694 us | 0.1865 us | 1.900 us |
    //     | Bench  | 57896(...)19967 [77] | 1.932 us | 0.0632 us | 0.1677 us | 1.900 us |
}
