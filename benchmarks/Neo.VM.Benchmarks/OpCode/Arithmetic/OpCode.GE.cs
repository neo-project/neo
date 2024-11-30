// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.GE.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Extensions;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_GE
    {
        [ParamsSource(nameof(ScriptParams))]
        public Script _script;
        private BenchmarkEngine _engine;

        public static IEnumerable<Script> ScriptParams()
        {
            return
            [
                new Script("0c04ffffff7f0c0100b8".HexToBytes()),
                new Script("0c20ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0c20ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb8".HexToBytes()),
                new Script("0c2001000000000000000000000000000000000000000000000000000000000000800c200000000000000000000000000000000000000000000000000000000000000080b8".HexToBytes()),
                new Script("0c20ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0c20ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb8".HexToBytes()),
                new Script("0c01000c20ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb8".HexToBytes()),
                new Script("0c2000000000000000000000000000000000000000000000000000000000000000800c200000000000000000000000000000000000000000000000000000000000000080b8".HexToBytes()),
                new Script("0c20ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0c20ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb8".HexToBytes()),
                new Script("0c2000000000000000000000000000000000000000000000000000000000000000800c200000000000000000000000000000000000000000000000000000000000000080b8".HexToBytes()),
                new Script("0c04ffffff7f0c04ffffff7fb8".HexToBytes()),
                new Script("0c04000000800c0400000080b8".HexToBytes()),
                new Script("0c08ffffffffffffff7f0c08ffffffffffffff7fb8".HexToBytes()),
                new Script("0c0800000000000000800c080000000000000080b8".HexToBytes())
            ];
        }

        [IterationSetup]
        public void Setup()
        {
            _engine = new BenchmarkEngine();
            _engine.LoadScript(_script);
            _engine.ExecuteUntil(VM.OpCode.GE);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _engine.Dispose();
        }
        [Benchmark]
        public void Bench_GE()
        {
            _engine.ExecuteNext();
        }
    }

    // | Method   | _script       | Mean     | Error     | StdDev    | Median   |
    //     |--------- |-------------- |---------:|----------:|----------:|---------:|
    //     | Bench_GE | Neo.VM.Script | 1.755 us | 0.0622 us | 0.1692 us | 1.700 us |
    //     | Bench_GE | Neo.VM.Script | 2.226 us | 0.1537 us | 0.4335 us | 2.050 us |
    //     | Bench_GE | Neo.VM.Script | 2.319 us | 0.1354 us | 0.3729 us | 2.200 us |
    //     | Bench_GE | Neo.VM.Script | 2.140 us | 0.1140 us | 0.3062 us | 2.050 us |
    //     | Bench_GE | Neo.VM.Script | 1.975 us | 0.0890 us | 0.2451 us | 1.900 us |
    //     | Bench_GE | Neo.VM.Script | 2.150 us | 0.0546 us | 0.1485 us | 2.100 us |
    //     | Bench_GE | Neo.VM.Script | 2.038 us | 0.0769 us | 0.2119 us | 2.000 us |
    //     | Bench_GE | Neo.VM.Script | 2.186 us | 0.0971 us | 0.2624 us | 2.100 us |
    //     | Bench_GE | Neo.VM.Script | 2.124 us | 0.1782 us | 0.5255 us | 1.900 us |
    //     | Bench_GE | Neo.VM.Script | 1.900 us | 0.1570 us | 0.4453 us | 1.700 us |

}
