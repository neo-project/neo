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
            builder.Push(ItemCount);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.NEWARRAY_T,
                _operand = [(byte)_type],
            });

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
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
    }
}

// | Method          | ItemCount | _type            | Mean             | Error          | StdDev         | Median           |
// |---------------- |---------- |----------------- |-----------------:|---------------:|---------------:|-----------------:|
// | Bench_OneOpCode | 2         | Any              |         6.205 us |      0.1727 us |      0.4489 us |         6.100 us |
// | Bench_OneGAS    | 2         | Any              |    14,587.874 us |    274.8255 us |    305.4678 us |    14,641.100 us |
// | Bench_OneOpCode | 2         | Pointer          |         5.987 us |      0.1595 us |      0.4256 us |         5.900 us |
// | Bench_OneGAS    | 2         | Pointer          |    14,298.086 us |    204.3664 us |    181.1655 us |    14,253.500 us |
// | Bench_OneOpCode | 2         | Boolean          |         5.966 us |      0.1582 us |      0.4278 us |         5.900 us |
// | Bench_OneGAS    | 2         | Boolean          |    14,386.543 us |    241.5646 us |    214.1408 us |    14,423.600 us |
// | Bench_OneOpCode | 2         | Integer          |         5.837 us |      0.1750 us |      0.4671 us |         5.700 us |
// | Bench_OneGAS    | 2         | Integer          |    15,059.938 us |    300.4857 us |    390.7163 us |    15,082.500 us |
// | Bench_OneOpCode | 2         | ByteString       |         6.018 us |      0.2320 us |      0.6071 us |         5.800 us |
// | Bench_OneGAS    | 2         | ByteString       |    14,494.071 us |    227.1997 us |    391.9084 us |    14,451.450 us |
// | Bench_OneOpCode | 2         | Buffer           |         7.756 us |      0.7569 us |      2.2317 us |         6.700 us |
// | Bench_OneGAS    | 2         | Buffer           |    14,331.889 us |    286.3773 us |    470.5261 us |    14,278.800 us |
// | Bench_OneOpCode | 2         | Array            |         5.965 us |      0.1725 us |      0.4635 us |         5.800 us |
// | Bench_OneGAS    | 2         | Array            |    14,287.355 us |    219.5093 us |    458.1976 us |    14,212.300 us |
// | Bench_OneOpCode | 2         | Struct           |         5.940 us |      0.1228 us |      0.3235 us |         5.900 us |
// | Bench_OneGAS    | 2         | Struct           |    14,490.679 us |    236.9623 us |    263.3830 us |    14,454.300 us |
// | Bench_OneOpCode | 2         | Map              |         6.269 us |      0.1839 us |      0.5033 us |         6.100 us |
// | Bench_OneGAS    | 2         | Map              |    14,647.865 us |    291.5806 us |    335.7846 us |    14,732.000 us |
// | Bench_OneOpCode | 2         | InteropInterface |         6.031 us |      0.1477 us |      0.3994 us |         5.900 us |
// | Bench_OneGAS    | 2         | InteropInterface |    14,747.453 us |    256.5790 us |    488.1682 us |    14,676.700 us |
// | Bench_OneOpCode | 32        | Any              |         7.153 us |      0.2712 us |      0.7332 us |         6.900 us |
// | Bench_OneGAS    | 32        | Any              |    31,750.556 us |    624.3104 us |    613.1563 us |    31,635.750 us |
// | Bench_OneOpCode | 32        | Pointer          |         6.657 us |      0.1371 us |      0.2253 us |         6.600 us |
// | Bench_OneGAS    | 32        | Pointer          |    31,901.329 us |    494.9273 us |    508.2539 us |    31,845.200 us |
// | Bench_OneOpCode | 32        | Boolean          |         6.887 us |      0.1806 us |      0.4851 us |         6.700 us |
// | Bench_OneGAS    | 32        | Boolean          |    32,325.831 us |    509.9386 us |    500.8279 us |    32,365.450 us |
// | Bench_OneOpCode | 32        | Integer          |         6.656 us |      0.1593 us |      0.4280 us |         6.550 us |
// | Bench_OneGAS    | 32        | Integer          |    32,261.106 us |    629.3783 us |    673.4277 us |    32,228.300 us |
// | Bench_OneOpCode | 32        | ByteString       |         6.849 us |      0.2661 us |      0.6963 us |         6.600 us |
// | Bench_OneGAS    | 32        | ByteString       |    32,487.932 us |    626.8610 us |    696.7543 us |    32,277.000 us |
// | Bench_OneOpCode | 32        | Buffer           |         6.662 us |      0.1319 us |      0.1715 us |         6.700 us |
// | Bench_OneGAS    | 32        | Buffer           |    31,807.032 us |    607.8898 us |    675.6679 us |    31,937.200 us |
// | Bench_OneOpCode | 32        | Array            |         6.751 us |      0.1366 us |      0.2911 us |         6.700 us |
// | Bench_OneGAS    | 32        | Array            |    31,973.413 us |    611.7148 us |    915.5859 us |    31,846.650 us |
// | Bench_OneOpCode | 32        | Struct           |         6.947 us |      0.1929 us |      0.5015 us |         6.800 us |
// | Bench_OneGAS    | 32        | Struct           |    31,280.100 us |    618.4921 us |    548.2772 us |    31,346.300 us |
// | Bench_OneOpCode | 32        | Map              |         6.645 us |      0.1201 us |      0.2256 us |         6.600 us |
// | Bench_OneGAS    | 32        | Map              |    31,843.756 us |    612.1831 us |    817.2469 us |    31,653.300 us |
// | Bench_OneOpCode | 32        | InteropInterface |         6.661 us |      0.1365 us |      0.1461 us |         6.600 us |
// | Bench_OneGAS    | 32        | InteropInterface |    32,042.608 us |    627.1112 us |  1,047.7620 us |    31,706.100 us |
// | Bench_OneOpCode | 128       | Any              |         9.743 us |      0.2448 us |      0.6784 us |         9.800 us |
// | Bench_OneGAS    | 128       | Any              |    77,882.967 us |  1,172.3240 us |  1,096.5926 us |    77,868.900 us |
// | Bench_OneOpCode | 128       | Pointer          |        11.073 us |      0.7745 us |      2.2347 us |        10.400 us |
// | Bench_OneGAS    | 128       | Pointer          |    81,307.581 us |  1,523.4889 us |  1,496.2698 us |    81,040.650 us |
// | Bench_OneOpCode | 128       | Boolean          |         8.981 us |      0.1830 us |      0.1797 us |         8.950 us |
// | Bench_OneGAS    | 128       | Boolean          |    83,509.231 us |    915.3937 us |    764.3952 us |    83,445.600 us |
// | Bench_OneOpCode | 128       | Integer          |         9.812 us |      0.2263 us |      0.6308 us |         9.900 us |
// | Bench_OneGAS    | 128       | Integer          |    84,292.989 us |  1,623.1718 us |  1,804.1511 us |    83,819.400 us |
// | Bench_OneOpCode | 128       | ByteString       |         9.037 us |      0.1800 us |      0.3200 us |         9.000 us |
// | Bench_OneGAS    | 128       | ByteString       |    83,594.133 us |  1,185.3032 us |  1,108.7333 us |    83,607.600 us |
// | Bench_OneOpCode | 128       | Buffer           |         9.061 us |      0.1755 us |      0.3027 us |         9.050 us |
// | Bench_OneGAS    | 128       | Buffer           |    80,137.392 us |  1,012.3624 us |    845.3684 us |    80,413.000 us |
// | Bench_OneOpCode | 128       | Array            |         9.298 us |      0.1897 us |      0.3609 us |         9.200 us |
// | Bench_OneGAS    | 128       | Array            |    81,525.729 us |  1,527.6311 us |  1,354.2051 us |    81,239.350 us |
// | Bench_OneOpCode | 128       | Struct           |         9.844 us |      0.2506 us |      0.6603 us |         9.900 us |
// | Bench_OneGAS    | 128       | Struct           |    80,312.947 us |  1,304.1024 us |  1,219.8582 us |    80,373.400 us |
// | Bench_OneOpCode | 128       | Map              |        11.500 us |      0.7052 us |      2.0793 us |        10.550 us |
// | Bench_OneGAS    | 128       | Map              |    80,056.596 us |  1,537.3071 us |  2,052.2608 us |    80,090.200 us |
// | Bench_OneOpCode | 128       | InteropInterface |         9.816 us |      0.2303 us |      0.6304 us |        10.100 us |
// | Bench_OneGAS    | 128       | InteropInterface |    80,188.726 us |  1,371.9332 us |  1,524.9001 us |    79,829.000 us |
// | Bench_OneOpCode | 1024      | Any              |        37.132 us |      0.9131 us |      2.6634 us |        36.450 us |
// | Bench_OneGAS    | 1024      | Any              |   523,237.457 us |  8,131.3631 us |  7,208.2416 us |   523,452.700 us |
// | Bench_OneOpCode | 1024      | Pointer          |        37.274 us |      1.1202 us |      3.2499 us |        36.000 us |
// | Bench_OneGAS    | 1024      | Pointer          |   528,770.367 us |  8,553.9098 us |  8,001.3325 us |   528,965.500 us |
// | Bench_OneOpCode | 1024      | Boolean          |        38.493 us |      1.1688 us |      3.4463 us |        38.350 us |
// | Bench_OneGAS    | 1024      | Boolean          |   544,005.229 us |  9,359.8327 us |  9,611.8591 us |   543,872.100 us |
// | Bench_OneOpCode | 1024      | Integer          |        37.017 us |      1.1516 us |      3.3594 us |        35.650 us |
// | Bench_OneGAS    | 1024      | Integer          |   541,311.193 us |  9,269.2020 us |  8,670.4173 us |   538,974.400 us |
// | Bench_OneOpCode | 1024      | ByteString       |        36.096 us |      0.8061 us |      2.2737 us |        35.350 us |
// | Bench_OneGAS    | 1024      | ByteString       |   575,850.163 us | 10,332.5411 us | 19,907.2989 us |   575,968.500 us |
// | Bench_OneOpCode | 1024      | Buffer           |        37.273 us |      0.8866 us |      2.5580 us |        36.050 us |
// | Bench_OneGAS    | 1024      | Buffer           |   544,162.200 us | 10,334.2559 us | 11,057.5381 us |   540,097.350 us |
// | Bench_OneOpCode | 1024      | Array            |        37.063 us |      0.9591 us |      2.7518 us |        36.100 us |
// | Bench_OneGAS    | 1024      | Array            |   548,226.027 us |  7,227.0404 us |  6,760.1781 us |   549,102.700 us |
// | Bench_OneOpCode | 1024      | Struct           |        36.338 us |      0.7843 us |      2.2502 us |        35.800 us |
// | Bench_OneGAS    | 1024      | Struct           |   539,669.228 us | 10,605.3982 us | 11,347.6572 us |   537,335.050 us |
// | Bench_OneOpCode | 1024      | Map              |        36.601 us |      0.9485 us |      2.7516 us |        35.500 us |
// | Bench_OneGAS    | 1024      | Map              |   546,612.635 us | 10,840.4177 us | 11,132.3109 us |   549,507.200 us |
// | Bench_OneOpCode | 1024      | InteropInterface |        37.904 us |      1.0396 us |      3.0654 us |        37.000 us |
// | Bench_OneGAS    | 1024      | InteropInterface |   547,014.460 us | 10,733.4687 us | 12,360.6785 us |   543,601.950 us |
// | Bench_OneOpCode | 2040      | Any              |        46.718 us |      1.1623 us |      3.4087 us |        45.500 us |
// | Bench_OneGAS    | 2040      | Any              | 1,060,025.413 us | 20,765.9571 us | 31,711.8097 us | 1,045,612.600 us |
// | Bench_OneOpCode | 2040      | Pointer          |        45.791 us |      1.0149 us |      2.9606 us |        44.550 us |
// | Bench_OneGAS    | 2040      | Pointer          | 1,044,475.033 us | 18,935.6257 us | 17,712.3961 us | 1,045,282.600 us |
// | Bench_OneOpCode | 2040      | Boolean          |        44.987 us |      0.9002 us |      2.4644 us |        44.300 us |
// | Bench_OneGAS    | 2040      | Boolean          | 1,047,665.527 us | 14,632.4184 us | 13,687.1733 us | 1,050,196.100 us |
// | Bench_OneOpCode | 2040      | Integer          |        44.977 us |      0.9234 us |      2.6643 us |        44.450 us |
// | Bench_OneGAS    | 2040      | Integer          | 1,056,482.013 us | 13,711.5148 us | 12,825.7594 us | 1,055,033.800 us |
// | Bench_OneOpCode | 2040      | ByteString       |        45.492 us |      0.9090 us |      2.5036 us |        45.000 us |
// | Bench_OneGAS    | 2040      | ByteString       | 1,079,720.913 us | 20,547.6011 us | 19,220.2388 us | 1,081,808.800 us |
// | Bench_OneOpCode | 2040      | Buffer           |        45.040 us |      0.8703 us |      2.3078 us |        44.300 us |
// | Bench_OneGAS    | 2040      | Buffer           | 1,040,920.267 us | 15,436.1759 us | 14,439.0085 us | 1,032,405.700 us |
// | Bench_OneOpCode | 2040      | Array            |        45.669 us |      0.9098 us |      2.6251 us |        45.200 us |
// | Bench_OneGAS    | 2040      | Array            | 1,017,626.980 us | 13,415.3595 us | 12,548.7356 us | 1,014,547.400 us |
// | Bench_OneOpCode | 2040      | Struct           |        45.070 us |      0.8897 us |      1.9342 us |        44.500 us |
// | Bench_OneGAS    | 2040      | Struct           | 1,040,209.573 us | 15,019.3435 us | 14,049.1032 us | 1,039,025.000 us |
// | Bench_OneOpCode | 2040      | Map              |        44.920 us |      0.8448 us |      1.9913 us |        44.500 us |
// | Bench_OneGAS    | 2040      | Map              | 1,024,835.400 us | 16,174.8004 us | 13,506.6909 us | 1,029,174.000 us |
// | Bench_OneOpCode | 2040      | InteropInterface |        44.826 us |      0.8993 us |      2.5513 us |        43.900 us |
// | Bench_OneGAS    | 2040      | InteropInterface | 1,020,677.033 us |  9,695.5166 us |  9,069.1923 us | 1,021,052.300 us |
