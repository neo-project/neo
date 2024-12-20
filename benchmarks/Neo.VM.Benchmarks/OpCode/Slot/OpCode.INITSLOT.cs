// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.INITSLOT.cs file belongs to the neo project and is free
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
    public class OpCode_INITSLOT
    {
        [Params(1, 4, 16, 64, 128, 255)]
        public byte _arguments = byte.MaxValue;

        [Params(1, 4, 16, 64, 128, 255)]
        public byte _localVariables = byte.MaxValue;

        [Params(0, ushort.MaxValue, ushort.MaxValue * 2)]
        public BigInteger _itemSize = ushort.MaxValue * 2;

        private BenchmarkEngine _engine;

        private const VM.OpCode Opcode = VM.OpCode.INITSLOT;

        [IterationSetup]
        public void Setup()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.Push(_arguments); // a
            initBegin._instruction = builder.Push(_itemSize);
            builder.AddInstruction(VM.OpCode.NEWBUFFER); // a buffer // buffer a-1 buffer
            builder.AddInstruction(VM.OpCode.SWAP); // buffer a // buffer buffer a-1
            builder.AddInstruction(VM.OpCode.DEC); // buffer a-1 // x xx a-2
            builder.AddInstruction(VM.OpCode.DUP);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.AddInstruction(new Instruction
            {
                _opCode = Opcode,
                _operand = [_localVariables, _arguments]
            });

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
}

// | Method | _arguments | _localVariables | _itemSize | Mean      | Error     | StdDev     | Median    |
// |------- |----------- |---------------- |---------- |----------:|----------:|-----------:|----------:|
// | Bench  | 1          | 1               | 0         |  2.848 us | 0.0811 us |  0.2247 us |  2.800 us |
// | Bench  | 1          | 1               | 131070    |  2.955 us | 0.1094 us |  0.3013 us |  2.900 us |
// | Bench  | 1          | 1               | 65535     |  2.882 us | 0.0619 us |  0.1695 us |  2.900 us |
// | Bench  | 1          | 4               | 0         |  2.847 us | 0.0608 us |  0.1359 us |  2.800 us |
// | Bench  | 1          | 4               | 131070    |  2.863 us | 0.0677 us |  0.1887 us |  2.800 us |
// | Bench  | 1          | 4               | 65535     |  2.872 us | 0.0812 us |  0.2251 us |  2.800 us |
// | Bench  | 1          | 16              | 0         |  2.899 us | 0.1107 us |  0.3012 us |  2.800 us |
// | Bench  | 1          | 16              | 131070    |  3.036 us | 0.1535 us |  0.4098 us |  2.900 us |
// | Bench  | 1          | 16              | 65535     |  2.986 us | 0.0631 us |  0.1705 us |  2.900 us |
// | Bench  | 1          | 64              | 0         |  2.960 us | 0.0641 us |  0.1744 us |  2.900 us |
// | Bench  | 1          | 64              | 131070    |  3.115 us | 0.0858 us |  0.2364 us |  3.050 us |
// | Bench  | 1          | 64              | 65535     |  3.029 us | 0.0644 us |  0.1427 us |  3.000 us |
// | Bench  | 1          | 128             | 0         |  3.060 us | 0.0640 us |  0.1294 us |  3.000 us |
// | Bench  | 1          | 128             | 131070    |  3.785 us | 0.3108 us |  0.9163 us |  3.350 us |
// | Bench  | 1          | 128             | 65535     |  3.444 us | 0.2360 us |  0.6617 us |  3.100 us |
// | Bench  | 1          | 255             | 0         |  3.235 us | 0.0684 us |  0.1397 us |  3.200 us |
// | Bench  | 1          | 255             | 131070    |  3.382 us | 0.0716 us |  0.1874 us |  3.300 us |
// | Bench  | 1          | 255             | 65535     |  3.345 us | 0.0701 us |  0.1433 us |  3.300 us |
// | Bench  | 4          | 1               | 0         |  3.552 us | 0.1035 us |  0.2868 us |  3.450 us |
// | Bench  | 4          | 1               | 131070    |  4.695 us | 0.3043 us |  0.8681 us |  4.400 us |
// | Bench  | 4          | 1               | 65535     |  3.742 us | 0.0869 us |  0.2335 us |  3.650 us |
// | Bench  | 4          | 4               | 0         |  3.371 us | 0.0711 us |  0.1576 us |  3.400 us |
// | Bench  | 4          | 4               | 131070    |  3.617 us | 0.0734 us |  0.0786 us |  3.600 us |
// | Bench  | 4          | 4               | 65535     |  3.363 us | 0.0767 us |  0.2101 us |  3.350 us |
// | Bench  | 4          | 16              | 0         |  3.428 us | 0.0718 us |  0.1498 us |  3.400 us |
// | Bench  | 4          | 16              | 131070    |  4.021 us | 0.1663 us |  0.4467 us |  3.900 us |
// | Bench  | 4          | 16              | 65535     |  3.905 us | 0.1392 us |  0.3715 us |  3.900 us |
// | Bench  | 4          | 64              | 0         |  3.485 us | 0.0733 us |  0.1743 us |  3.500 us |
// | Bench  | 4          | 64              | 131070    |  3.963 us | 0.0821 us |  0.2190 us |  3.900 us |
// | Bench  | 4          | 64              | 65535     |  3.692 us | 0.0769 us |  0.1797 us |  3.700 us |
// | Bench  | 4          | 128             | 0         |  3.548 us | 0.1515 us |  0.4148 us |  3.500 us |
// | Bench  | 4          | 128             | 131070    |  3.974 us | 0.1265 us |  0.3420 us |  3.900 us |
// | Bench  | 4          | 128             | 65535     |  3.979 us | 0.0905 us |  0.2463 us |  3.950 us |
// | Bench  | 4          | 255             | 0         |  4.111 us | 0.2230 us |  0.6028 us |  3.850 us |
// | Bench  | 4          | 255             | 131070    |  4.055 us | 0.0850 us |  0.1865 us |  4.000 us |
// | Bench  | 4          | 255             | 65535     |  4.692 us | 0.2785 us |  0.7809 us |  4.400 us |
// | Bench  | 16         | 1               | 0         |  4.819 us | 0.1229 us |  0.3322 us |  4.700 us |
// | Bench  | 16         | 1               | 131070    |  6.138 us | 0.2746 us |  0.7880 us |  6.000 us |
// | Bench  | 16         | 1               | 65535     |  6.784 us | 0.1376 us |  0.3790 us |  6.750 us |
// | Bench  | 16         | 4               | 0         |  5.288 us | 0.2932 us |  0.8220 us |  5.100 us |
// | Bench  | 16         | 4               | 131070    |  5.799 us | 0.2013 us |  0.5577 us |  5.800 us |
// | Bench  | 16         | 4               | 65535     |  6.587 us | 0.1269 us |  0.1187 us |  6.600 us |
// | Bench  | 16         | 16              | 0         |  4.675 us | 0.0969 us |  0.2107 us |  4.600 us |
// | Bench  | 16         | 16              | 131070    |  5.917 us | 0.2050 us |  0.5781 us |  6.000 us |
// | Bench  | 16         | 16              | 65535     |  6.567 us | 0.1317 us |  0.2720 us |  6.500 us |
// | Bench  | 16         | 64              | 0         |  4.840 us | 0.1273 us |  0.3419 us |  4.750 us |
// | Bench  | 16         | 64              | 131070    |  5.969 us | 0.1763 us |  0.4914 us |  6.000 us |
// | Bench  | 16         | 64              | 65535     |  6.974 us | 0.1279 us |  0.2371 us |  6.900 us |
// | Bench  | 16         | 128             | 0         |  6.199 us | 0.4956 us |  1.4220 us |  5.600 us |
// | Bench  | 16         | 128             | 131070    |  5.917 us | 0.2058 us |  0.5806 us |  5.900 us |
// | Bench  | 16         | 128             | 65535     |  7.010 us | 0.1418 us |  0.2520 us |  7.000 us |
// | Bench  | 16         | 255             | 0         |  5.100 us | 0.1104 us |  0.3060 us |  5.000 us |
// | Bench  | 16         | 255             | 131070    |  6.097 us | 0.2228 us |  0.6284 us |  6.050 us |
// | Bench  | 16         | 255             | 65535     |  7.083 us | 0.1441 us |  0.2408 us |  7.050 us |
// | Bench  | 64         | 1               | 0         |  8.236 us | 0.1602 us |  0.2139 us |  8.200 us |
// | Bench  | 64         | 1               | 131070    |  9.817 us | 0.3887 us |  1.1216 us |  9.600 us |
// | Bench  | 64         | 1               | 65535     | 10.331 us | 0.3044 us |  0.8830 us | 10.400 us |
// | Bench  | 64         | 4               | 0         |  8.417 us | 0.1614 us |  0.2099 us |  8.450 us |
// | Bench  | 64         | 4               | 131070    |  9.972 us | 0.3612 us |  1.0537 us |  9.900 us |
// | Bench  | 64         | 4               | 65535     | 10.491 us | 0.2709 us |  0.7946 us | 10.600 us |
// | Bench  | 64         | 16              | 0         |  8.369 us | 0.1686 us |  0.3327 us |  8.300 us |
// | Bench  | 64         | 16              | 131070    | 10.382 us | 0.2885 us |  0.8369 us | 10.400 us |
// | Bench  | 64         | 16              | 65535     | 10.725 us | 0.2591 us |  0.7433 us | 10.700 us |
// | Bench  | 64         | 64              | 0         |  8.608 us | 0.1740 us |  0.4067 us |  8.500 us |
// | Bench  | 64         | 64              | 131070    |  9.991 us | 0.4491 us |  1.3243 us |  9.650 us |
// | Bench  | 64         | 64              | 65535     | 10.618 us | 0.3049 us |  0.8699 us | 10.600 us |
// | Bench  | 64         | 128             | 0         |  8.586 us | 0.1747 us |  0.2505 us |  8.600 us |
// | Bench  | 64         | 128             | 131070    | 10.039 us | 0.3780 us |  1.1027 us | 10.050 us |
// | Bench  | 64         | 128             | 65535     | 10.624 us | 0.3182 us |  0.9182 us | 10.650 us |
// | Bench  | 64         | 255             | 0         |  8.797 us | 0.1768 us |  0.4303 us |  8.650 us |
// | Bench  | 64         | 255             | 131070    | 10.483 us | 0.4323 us |  1.2609 us | 10.250 us |
// | Bench  | 64         | 255             | 65535     | 10.998 us | 0.2694 us |  0.7465 us | 11.100 us |
// | Bench  | 128        | 1               | 0         | 13.481 us | 0.2726 us |  0.5053 us | 13.400 us |
// | Bench  | 128        | 1               | 131070    | 15.292 us | 0.5183 us |  1.4872 us | 15.000 us |
// | Bench  | 128        | 1               | 65535     | 17.707 us | 0.4912 us |  1.4016 us | 17.500 us |
// | Bench  | 128        | 4               | 0         | 13.319 us | 0.2085 us |  0.2923 us | 13.300 us |
// | Bench  | 128        | 4               | 131070    | 19.532 us | 1.7283 us |  5.0960 us | 16.800 us |
// | Bench  | 128        | 4               | 65535     | 17.139 us | 0.5688 us |  1.6503 us | 17.100 us |
// | Bench  | 128        | 16              | 0         | 13.371 us | 0.2451 us |  0.3958 us | 13.400 us |
// | Bench  | 128        | 16              | 131070    | 19.470 us | 1.7407 us |  5.1052 us | 17.100 us |
// | Bench  | 128        | 16              | 65535     | 17.152 us | 0.6059 us |  1.7674 us | 17.050 us |
// | Bench  | 128        | 64              | 0         | 13.580 us | 0.2726 us |  0.4080 us | 13.600 us |
// | Bench  | 128        | 64              | 131070    | 19.615 us | 1.6890 us |  4.9800 us | 17.050 us |
// | Bench  | 128        | 64              | 65535     | 16.937 us | 0.6282 us |  1.8025 us | 16.900 us |
// | Bench  | 128        | 128             | 0         | 13.759 us | 0.2695 us |  0.4196 us | 13.850 us |
// | Bench  | 128        | 128             | 131070    | 19.574 us | 1.7148 us |  5.0294 us | 17.800 us |
// | Bench  | 128        | 128             | 65535     | 17.493 us | 0.4754 us |  1.3717 us | 17.400 us |
// | Bench  | 128        | 255             | 0         | 13.710 us | 0.2723 us |  0.4158 us | 13.600 us |
// | Bench  | 128        | 255             | 131070    | 18.348 us | 1.2139 us |  3.4632 us | 17.150 us |
// | Bench  | 128        | 255             | 65535     | 17.420 us | 0.5164 us |  1.4566 us | 17.500 us |
// | Bench  | 255        | 1               | 0         | 24.027 us | 0.6440 us |  1.8478 us | 24.000 us |
// | Bench  | 255        | 1               | 131070    | 40.778 us | 4.8478 us | 14.2177 us | 38.500 us |
// | Bench  | 255        | 1               | 65535     | 38.738 us | 3.3634 us |  9.9170 us | 35.350 us |
// | Bench  | 255        | 4               | 0         | 24.185 us | 0.4839 us |  1.3327 us | 24.100 us |
// | Bench  | 255        | 4               | 131070    | 41.972 us | 5.1127 us | 15.0750 us | 38.350 us |
// | Bench  | 255        | 4               | 65535     | 32.008 us | 1.0621 us |  2.9955 us | 31.750 us |
// | Bench  | 255        | 16              | 0         | 24.047 us | 0.5855 us |  1.6988 us | 24.100 us |
// | Bench  | 255        | 16              | 131070    | 42.572 us | 5.5025 us | 16.2241 us | 40.400 us |
// | Bench  | 255        | 16              | 65535     | 39.530 us | 3.8094 us | 11.0518 us | 34.200 us |
// | Bench  | 255        | 64              | 0         | 22.869 us | 0.2739 us |  0.2287 us | 22.900 us |
// | Bench  | 255        | 64              | 131070    | 43.300 us | 5.0686 us | 14.9450 us | 44.300 us |
// | Bench  | 255        | 64              | 65535     | 45.807 us | 6.0362 us | 17.7978 us | 34.400 us |
// | Bench  | 255        | 128             | 0         | 24.202 us | 0.5196 us |  1.4992 us | 24.250 us |
// | Bench  | 255        | 128             | 131070    | 42.033 us | 5.1816 us | 15.2779 us | 36.600 us |
// | Bench  | 255        | 128             | 65535     | 44.485 us | 6.1334 us | 18.0845 us | 34.250 us |
// | Bench  | 255        | 255             | 0         | 24.547 us | 0.4942 us |  1.3528 us | 24.700 us |
// | Bench  | 255        | 255             | 131070    | 42.407 us | 5.0024 us | 14.7496 us | 39.300 us |
// | Bench  | 255        | 255             | 65535     | 44.712 us | 5.3574 us | 15.7964 us | 35.300 us |
