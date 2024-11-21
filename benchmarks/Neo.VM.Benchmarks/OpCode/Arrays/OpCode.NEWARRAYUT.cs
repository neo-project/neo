// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWARRAYUT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Types;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NEWARRAYUT : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.NEWARRAY_T;

        [ParamsAllValues]
        public StackItemType _type = StackItemType.Any;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.Push(ItemCount);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.NEWARRAY_T,
                _operand = [(byte)_type],
            });
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);


            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            throw new NotImplementedException();
        }
    }
}

// | Method          | ItemCount | _type            | Mean      | Error     | StdDev    | Median    |
// |---------------- |---------- |----------------- |----------:|----------:|----------:|----------:|
// | Bench_OneOpCode | 1         | Any              |  5.758 us | 0.1965 us | 0.5346 us |  5.600 us |
// | Bench_OneOpCode | 1         | Pointer          |  5.558 us | 0.1144 us | 0.2762 us |  5.500 us |
// | Bench_OneOpCode | 1         | Boolean          |  5.388 us | 0.1116 us | 0.1737 us |  5.350 us |
// | Bench_OneOpCode | 1         | Integer          |  5.413 us | 0.1341 us | 0.3603 us |  5.300 us |
// | Bench_OneOpCode | 1         | ByteString       |  5.499 us | 0.1584 us | 0.4145 us |  5.400 us |
// | Bench_OneOpCode | 1         | Buffer           |  5.409 us | 0.1119 us | 0.1774 us |  5.400 us |
// | Bench_OneOpCode | 1         | Array            |  5.740 us | 0.1675 us | 0.4323 us |  5.600 us |
// | Bench_OneOpCode | 1         | Struct           |  5.607 us | 0.1508 us | 0.4050 us |  5.500 us |
// | Bench_OneOpCode | 1         | Map              |  5.875 us | 0.2265 us | 0.6085 us |  5.600 us |
// | Bench_OneOpCode | 1         | InteropInterface |  5.716 us | 0.1254 us | 0.3412 us |  5.600 us |
// | Bench_OneOpCode | 2         | Any              |  6.689 us | 0.5826 us | 1.6809 us |  5.800 us |
// | Bench_OneOpCode | 2         | Pointer          |  5.451 us | 0.1037 us | 0.1704 us |  5.400 us |
// | Bench_OneOpCode | 2         | Boolean          |  5.604 us | 0.1106 us | 0.2183 us |  5.600 us |
// | Bench_OneOpCode | 2         | Integer          |  5.475 us | 0.1130 us | 0.2771 us |  5.400 us |
// | Bench_OneOpCode | 2         | ByteString       |  5.419 us | 0.1113 us | 0.2348 us |  5.400 us |
// | Bench_OneOpCode | 2         | Buffer           |  5.693 us | 0.1383 us | 0.3715 us |  5.600 us |
// | Bench_OneOpCode | 2         | Array            |  5.441 us | 0.1106 us | 0.1878 us |  5.400 us |
// | Bench_OneOpCode | 2         | Struct           |  5.641 us | 0.1528 us | 0.4079 us |  5.500 us |
// | Bench_OneOpCode | 2         | Map              |  5.403 us | 0.1101 us | 0.1614 us |  5.400 us |
// | Bench_OneOpCode | 2         | InteropInterface |  6.483 us | 0.5290 us | 1.4747 us |  5.800 us |
// | Bench_OneOpCode | 32        | Any              |  6.473 us | 0.1891 us | 0.5144 us |  6.300 us |
// | Bench_OneOpCode | 32        | Pointer          |  7.552 us | 0.6316 us | 1.8323 us |  6.500 us |
// | Bench_OneOpCode | 32        | Boolean          |  6.575 us | 0.2137 us | 0.5849 us |  6.300 us |
// | Bench_OneOpCode | 32        | Integer          |  6.412 us | 0.1736 us | 0.4573 us |  6.300 us |
// | Bench_OneOpCode | 32        | ByteString       |  7.166 us | 0.4613 us | 1.2860 us |  6.700 us |
// | Bench_OneOpCode | 32        | Buffer           |  6.857 us | 0.3143 us | 0.8605 us |  6.500 us |
// | Bench_OneOpCode | 32        | Array            |  6.661 us | 0.1811 us | 0.4740 us |  6.500 us |
// | Bench_OneOpCode | 32        | Struct           |  8.023 us | 0.7420 us | 2.1877 us |  6.800 us |
// | Bench_OneOpCode | 32        | Map              |  6.830 us | 0.2524 us | 0.6649 us |  6.600 us |
// | Bench_OneOpCode | 32        | InteropInterface |  6.215 us | 0.1279 us | 0.1068 us |  6.200 us |
// | Bench_OneOpCode | 128       | Any              |  8.964 us | 0.1656 us | 0.3111 us |  9.000 us |
// | Bench_OneOpCode | 128       | Pointer          | 10.573 us | 0.6987 us | 2.0381 us |  9.800 us |
// | Bench_OneOpCode | 128       | Boolean          |  9.712 us | 0.3129 us | 0.8353 us |  9.700 us |
// | Bench_OneOpCode | 128       | Integer          |  8.576 us | 0.1705 us | 0.1751 us |  8.500 us |
// | Bench_OneOpCode | 128       | ByteString       |  8.670 us | 0.1747 us | 0.2615 us |  8.600 us |
// | Bench_OneOpCode | 128       | Buffer           |  8.864 us | 0.1782 us | 0.2378 us |  8.800 us |
// | Bench_OneOpCode | 128       | Array            | 10.038 us | 0.4065 us | 1.1398 us |  9.900 us |
// | Bench_OneOpCode | 128       | Struct           |  8.870 us | 0.1791 us | 0.3320 us |  8.900 us |
// | Bench_OneOpCode | 128       | Map              | 11.195 us | 0.8166 us | 2.4078 us | 10.000 us |
// | Bench_OneOpCode | 128       | InteropInterface |  8.790 us | 0.1795 us | 0.3144 us |  8.700 us |
// | Bench_OneOpCode | 1024      | Any              | 36.712 us | 0.9357 us | 2.7442 us | 35.600 us |
// | Bench_OneOpCode | 1024      | Pointer          | 37.121 us | 0.9627 us | 2.8083 us | 36.150 us |
// | Bench_OneOpCode | 1024      | Boolean          | 35.841 us | 0.8928 us | 2.5760 us | 34.900 us |
// | Bench_OneOpCode | 1024      | Integer          | 35.614 us | 0.6638 us | 1.3257 us | 35.600 us |
// | Bench_OneOpCode | 1024      | ByteString       | 35.746 us | 0.8602 us | 2.5092 us | 34.700 us |
// | Bench_OneOpCode | 1024      | Buffer           | 36.996 us | 1.1436 us | 3.3358 us | 35.900 us |
// | Bench_OneOpCode | 1024      | Array            | 36.142 us | 0.8625 us | 2.5024 us | 35.200 us |
// | Bench_OneOpCode | 1024      | Struct           | 36.966 us | 1.0363 us | 3.0229 us | 36.100 us |
// | Bench_OneOpCode | 1024      | Map              | 36.783 us | 1.0079 us | 2.9081 us | 35.450 us |
// | Bench_OneOpCode | 1024      | InteropInterface | 36.134 us | 0.7789 us | 2.1583 us | 35.500 us |
// | Bench_OneOpCode | 2040      | Any              | 45.053 us | 0.9650 us | 2.7375 us | 44.400 us |
// | Bench_OneOpCode | 2040      | Pointer          | 45.072 us | 0.8928 us | 2.4290 us | 44.600 us |
// | Bench_OneOpCode | 2040      | Boolean          | 44.828 us | 0.8706 us | 1.2761 us | 44.800 us |
// | Bench_OneOpCode | 2040      | Integer          | 44.663 us | 0.9359 us | 2.6550 us | 44.200 us |
// | Bench_OneOpCode | 2040      | ByteString       | 44.647 us | 0.9689 us | 2.7954 us | 43.850 us |
// | Bench_OneOpCode | 2040      | Buffer           | 45.112 us | 0.9324 us | 2.6753 us | 44.400 us |
// | Bench_OneOpCode | 2040      | Array            | 45.529 us | 1.0612 us | 3.0789 us | 44.700 us |
// | Bench_OneOpCode | 2040      | Struct           | 46.471 us | 0.9295 us | 1.8988 us | 46.400 us |
// | Bench_OneOpCode | 2040      | Map              | 45.250 us | 0.8846 us | 1.3772 us | 44.950 us |
// | Bench_OneOpCode | 2040      | InteropInterface | 45.373 us | 0.9096 us | 1.6401 us | 45.000 us |

// | Method          | ItemCount | _type            | Mean       | Error     | StdDev    | Median     |
// |---------------- |---------- |----------------- |-----------:|----------:|----------:|-----------:|
// | Bench_OneOpCode | 1         | Any              |   5.874 ms | 0.1108 ms | 0.1036 ms |   5.859 ms |
// | Bench_OneOpCode | 1         | Pointer          |   5.456 ms | 0.1085 ms | 0.1872 ms |   5.427 ms |
// | Bench_OneOpCode | 1         | Boolean          |   7.026 ms | 0.4813 ms | 1.4192 ms |   7.184 ms |
// | Bench_OneOpCode | 1         | Integer          |   5.305 ms | 0.1054 ms | 0.1641 ms |   5.285 ms |
// | Bench_OneOpCode | 1         | ByteString       |   5.357 ms | 0.1065 ms | 0.1920 ms |   5.304 ms |
// | Bench_OneOpCode | 1         | Buffer           |   6.856 ms | 0.4689 ms | 1.3826 ms |   6.955 ms |
// | Bench_OneOpCode | 1         | Array            |   5.568 ms | 0.1098 ms | 0.1805 ms |   5.531 ms |
// | Bench_OneOpCode | 1         | Struct           |   6.945 ms | 0.5132 ms | 1.5131 ms |   7.077 ms |
// | Bench_OneOpCode | 1         | Map              |   5.571 ms | 0.1105 ms | 0.0923 ms |   5.595 ms |
// | Bench_OneOpCode | 1         | InteropInterface |   5.602 ms | 0.1102 ms | 0.1393 ms |   5.582 ms |
// | Bench_OneOpCode | 2         | Any              |   5.949 ms | 0.1161 ms | 0.1701 ms |   5.906 ms |
// | Bench_OneOpCode | 2         | Pointer          |   5.795 ms | 0.1093 ms | 0.1122 ms |   5.807 ms |
// | Bench_OneOpCode | 2         | Boolean          |   5.914 ms | 0.1170 ms | 0.1602 ms |   5.920 ms |
// | Bench_OneOpCode | 2         | Integer          |   5.663 ms | 0.1127 ms | 0.1721 ms |   5.620 ms |
// | Bench_OneOpCode | 2         | ByteString       |   5.601 ms | 0.1102 ms | 0.1901 ms |   5.592 ms |
// | Bench_OneOpCode | 2         | Buffer           |   7.337 ms | 0.4495 ms | 1.3253 ms |   7.783 ms |
// | Bench_OneOpCode | 2         | Array            |   5.916 ms | 0.1163 ms | 0.1668 ms |   5.904 ms |
// | Bench_OneOpCode | 2         | Struct           |   7.216 ms | 0.4298 ms | 1.2672 ms |   7.379 ms |
// | Bench_OneOpCode | 2         | Map              |   5.912 ms | 0.1142 ms | 0.1315 ms |   5.908 ms |
// | Bench_OneOpCode | 2         | InteropInterface |   5.896 ms | 0.1176 ms | 0.1354 ms |   5.872 ms |
// | Bench_OneOpCode | 32        | Any              |   5.499 ms | 0.9430 ms | 2.4842 ms |   4.099 ms |
// | Bench_OneOpCode | 32        | Pointer          |   9.909 ms | 2.1961 ms | 6.4752 ms |   8.764 ms |
// | Bench_OneOpCode | 32        | Boolean          |   9.620 ms | 2.3282 ms | 6.8646 ms |   7.350 ms |
// | Bench_OneOpCode | 32        | Integer          |   8.917 ms | 2.2685 ms | 6.6888 ms |   5.846 ms |
// | Bench_OneOpCode | 32        | ByteString       |   9.773 ms | 2.1600 ms | 6.3687 ms |   8.541 ms |
// | Bench_OneOpCode | 32        | Buffer           |   9.126 ms | 2.2864 ms | 6.7415 ms |   6.130 ms |
// | Bench_OneOpCode | 32        | Array            |   5.616 ms | 0.9599 ms | 2.5288 ms |   4.130 ms |
// | Bench_OneOpCode | 32        | Struct           |   5.581 ms | 1.1022 ms | 3.0173 ms |   3.967 ms |
// | Bench_OneOpCode | 32        | Map              |   5.646 ms | 0.9574 ms | 2.5220 ms |   4.141 ms |
// | Bench_OneOpCode | 32        | InteropInterface |   9.412 ms | 2.3413 ms | 6.9035 ms |   4.613 ms |
// | Bench_OneOpCode | 128       | Any              |   9.100 ms | 0.1802 ms | 0.4282 ms |   9.060 ms |
// | Bench_OneOpCode | 128       | Pointer          |   9.418 ms | 0.1870 ms | 0.4143 ms |   9.384 ms |
// | Bench_OneOpCode | 128       | Boolean          |   9.522 ms | 0.2457 ms | 0.6725 ms |   9.268 ms |
// | Bench_OneOpCode | 128       | Integer          |   9.441 ms | 0.1686 ms | 0.3700 ms |   9.425 ms |
// | Bench_OneOpCode | 128       | ByteString       |   9.555 ms | 0.1902 ms | 0.5270 ms |   9.477 ms |
// | Bench_OneOpCode | 128       | Buffer           |   8.964 ms | 0.1679 ms | 0.4087 ms |   8.887 ms |
// | Bench_OneOpCode | 128       | Array            |   8.781 ms | 0.1569 ms | 0.3412 ms |   8.755 ms |
// | Bench_OneOpCode | 128       | Struct           |   8.629 ms | 0.1637 ms | 0.3627 ms |   8.570 ms |
// | Bench_OneOpCode | 128       | Map              |   9.047 ms | 0.1801 ms | 0.3838 ms |   9.013 ms |
// | Bench_OneOpCode | 128       | InteropInterface |   9.229 ms | 0.1831 ms | 0.4244 ms |   9.232 ms |
// | Bench_OneOpCode | 1024      | Any              |  53.842 ms | 1.0119 ms | 1.4512 ms |  53.531 ms |
// | Bench_OneOpCode | 1024      | Pointer          |  52.787 ms | 0.7995 ms | 0.6676 ms |  52.853 ms |
// | Bench_OneOpCode | 1024      | Boolean          |  55.477 ms | 1.0705 ms | 1.5353 ms |  55.024 ms |
// | Bench_OneOpCode | 1024      | Integer          |  55.042 ms | 0.9912 ms | 1.1017 ms |  55.274 ms |
// | Bench_OneOpCode | 1024      | ByteString       |  54.556 ms | 0.9682 ms | 0.8085 ms |  54.817 ms |
// | Bench_OneOpCode | 1024      | Buffer           |  52.763 ms | 0.7452 ms | 0.6223 ms |  52.731 ms |
// | Bench_OneOpCode | 1024      | Array            |  52.476 ms | 1.0418 ms | 0.9745 ms |  52.326 ms |
// | Bench_OneOpCode | 1024      | Struct           |  51.683 ms | 0.9807 ms | 0.8693 ms |  51.964 ms |
// | Bench_OneOpCode | 1024      | Map              |  53.182 ms | 0.8752 ms | 0.8596 ms |  53.382 ms |
// | Bench_OneOpCode | 1024      | InteropInterface |  51.976 ms | 0.9355 ms | 1.1136 ms |  51.516 ms |
// | Bench_OneOpCode | 2040      | Any              | 103.675 ms | 1.8846 ms | 1.6707 ms | 103.198 ms |
// | Bench_OneOpCode | 2040      | Pointer          |  99.633 ms | 1.9420 ms | 2.0779 ms |  99.694 ms |
// | Bench_OneOpCode | 2040      | Boolean          | 103.555 ms | 1.9484 ms | 1.8225 ms | 103.217 ms |
// | Bench_OneOpCode | 2040      | Integer          | 101.872 ms | 1.8670 ms | 1.6551 ms | 101.642 ms |
// | Bench_OneOpCode | 2040      | ByteString       | 105.310 ms | 1.7893 ms | 1.4942 ms | 104.845 ms |
// | Bench_OneOpCode | 2040      | Buffer           |  97.849 ms | 1.7743 ms | 1.5729 ms |  97.963 ms |
// | Bench_OneOpCode | 2040      | Array            | 103.512 ms | 1.5280 ms | 1.3546 ms | 103.266 ms |
// | Bench_OneOpCode | 2040      | Struct           | 100.639 ms | 1.9088 ms | 1.5939 ms | 100.650 ms |
// | Bench_OneOpCode | 2040      | Map              | 101.197 ms | 1.9918 ms | 3.3821 ms | 100.562 ms |
// | Bench_OneOpCode | 2040      | InteropInterface |  99.832 ms | 1.7393 ms | 2.1361 ms |  99.760 ms |
