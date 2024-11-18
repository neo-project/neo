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

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    // | Method          | ItemCount | _type            | Mean      | Error     | StdDev    | Median    |
    // |---------------- |---------- |----------------- |----------:|----------:|----------:|----------:|
    // | Bench_OneOpCode | 2         | Any              |  7.041 us | 0.7113 us | 2.0522 us |  6.400 us |
    // | Bench_OneOpCode | 2         | Pointer          |  5.835 us | 0.2660 us | 0.7632 us |  5.600 us |
    // | Bench_OneOpCode | 2         | Boolean          |  5.671 us | 0.1608 us | 0.4320 us |  5.700 us |
    // | Bench_OneOpCode | 2         | Integer          |  5.461 us | 0.1596 us | 0.4315 us |  5.500 us |
    // | Bench_OneOpCode | 2         | ByteString       |  5.445 us | 0.2349 us | 0.6587 us |  5.100 us |
    // | Bench_OneOpCode | 2         | Buffer           |  7.227 us | 0.7086 us | 2.0669 us |  6.200 us |
    // | Bench_OneOpCode | 2         | Array            |  6.248 us | 0.2577 us | 0.6788 us |  6.000 us |
    // | Bench_OneOpCode | 2         | Struct           |  8.331 us | 0.7329 us | 2.1494 us |  7.500 us |
    // | Bench_OneOpCode | 2         | Map              |  6.459 us | 0.3086 us | 0.8397 us |  6.100 us |
    // | Bench_OneOpCode | 2         | InteropInterface |  5.956 us | 0.2643 us | 0.7324 us |  5.800 us |
    // | Bench_OneOpCode | 32        | Any              |  6.986 us | 0.2601 us | 0.7032 us |  6.900 us |
    // | Bench_OneOpCode | 32        | Pointer          |  8.400 us | 0.6913 us | 1.9945 us |  7.900 us |
    // | Bench_OneOpCode | 32        | Boolean          |  6.341 us | 0.2438 us | 0.6876 us |  6.000 us |
    // | Bench_OneOpCode | 32        | Integer          |  6.332 us | 0.2865 us | 0.8034 us |  6.000 us |
    // | Bench_OneOpCode | 32        | ByteString       |  6.441 us | 0.1341 us | 0.3671 us |  6.300 us |
    // | Bench_OneOpCode | 32        | Buffer           |  6.755 us | 0.2236 us | 0.6196 us |  6.500 us |
    // | Bench_OneOpCode | 32        | Array            |  6.456 us | 0.1323 us | 0.2210 us |  6.400 us |
    // | Bench_OneOpCode | 32        | Struct           |  6.720 us | 0.2356 us | 0.6289 us |  6.600 us |
    // | Bench_OneOpCode | 32        | Map              |  6.109 us | 0.1898 us | 0.5195 us |  5.900 us |
    // | Bench_OneOpCode | 32        | InteropInterface |  6.504 us | 0.1222 us | 0.1753 us |  6.500 us |
    // | Bench_OneOpCode | 128       | Any              |  8.606 us | 0.2357 us | 0.6372 us |  8.500 us |
    // | Bench_OneOpCode | 128       | Pointer          |  8.849 us | 0.2190 us | 0.5809 us |  8.850 us |
    // | Bench_OneOpCode | 128       | Boolean          |  8.640 us | 0.2320 us | 0.6391 us |  8.550 us |
    // | Bench_OneOpCode | 128       | Integer          |  8.355 us | 0.3350 us | 0.9113 us |  8.100 us |
    // | Bench_OneOpCode | 128       | ByteString       |  8.570 us | 0.1537 us | 0.3829 us |  8.600 us |
    // | Bench_OneOpCode | 128       | Buffer           |  8.673 us | 0.2072 us | 0.5637 us |  8.700 us |
    // | Bench_OneOpCode | 128       | Array            |  8.506 us | 0.1682 us | 0.2568 us |  8.500 us |
    // | Bench_OneOpCode | 128       | Struct           |  8.428 us | 0.1696 us | 0.3688 us |  8.400 us |
    // | Bench_OneOpCode | 128       | Map              |  8.638 us | 0.2120 us | 0.5768 us |  8.700 us |
    // | Bench_OneOpCode | 128       | InteropInterface |  8.648 us | 0.1920 us | 0.5224 us |  8.700 us |
    // | Bench_OneOpCode | 1024      | Any              | 28.297 us | 0.5695 us | 1.4700 us | 27.900 us |
    // | Bench_OneOpCode | 1024      | Pointer          | 29.043 us | 0.7969 us | 2.2865 us | 28.050 us |
    // | Bench_OneOpCode | 1024      | Boolean          | 28.297 us | 0.6282 us | 1.8023 us | 27.700 us |
    // | Bench_OneOpCode | 1024      | Integer          | 27.512 us | 0.5487 us | 1.3564 us | 27.050 us |
    // | Bench_OneOpCode | 1024      | ByteString       | 27.248 us | 0.5190 us | 1.0244 us | 27.200 us |
    // | Bench_OneOpCode | 1024      | Buffer           | 28.603 us | 0.5744 us | 1.3763 us | 28.550 us |
    // | Bench_OneOpCode | 1024      | Array            | 28.760 us | 0.5723 us | 1.0465 us | 28.700 us |
    // | Bench_OneOpCode | 1024      | Struct           | 28.471 us | 0.5703 us | 0.7416 us | 28.500 us |
    // | Bench_OneOpCode | 1024      | Map              | 29.179 us | 0.8546 us | 2.4657 us | 28.700 us |
    // | Bench_OneOpCode | 1024      | InteropInterface | 28.358 us | 0.5562 us | 1.4456 us | 28.000 us |
    // | Bench_OneOpCode | 2040      | Any              | 33.814 us | 0.5576 us | 0.6847 us | 33.850 us |
    // | Bench_OneOpCode | 2040      | Pointer          | 33.942 us | 0.6620 us | 0.7358 us | 34.100 us |
    // | Bench_OneOpCode | 2040      | Boolean          | 33.607 us | 0.6755 us | 0.6319 us | 33.300 us |
    // | Bench_OneOpCode | 2040      | Integer          | 33.296 us | 0.6513 us | 0.8915 us | 33.250 us |
    // | Bench_OneOpCode | 2040      | ByteString       | 30.198 us | 0.6018 us | 0.8437 us | 30.450 us |
    // | Bench_OneOpCode | 2040      | Buffer           | 33.836 us | 0.5265 us | 0.4668 us | 33.800 us |
    // | Bench_OneOpCode | 2040      | Array            | 34.392 us | 0.9367 us | 2.6421 us | 33.550 us |
    // | Bench_OneOpCode | 2040      | Struct           | 34.053 us | 0.6844 us | 1.0656 us | 33.900 us |
    // | Bench_OneOpCode | 2040      | Map              | 33.935 us | 0.6740 us | 1.4069 us | 34.350 us |
    // | Bench_OneOpCode | 2040      | InteropInterface | 34.000 us | 0.6825 us | 1.0825 us | 33.800 us |
}
