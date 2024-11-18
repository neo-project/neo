// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWARRAY.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NEWARRAY : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.NEWARRAY;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ItemCount);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    // | Method          | ItemCount | Mean      | Error     | StdDev    | Median    |
    //     |---------------- |---------- |----------:|----------:|----------:|----------:|
    //     | Bench_OneOpCode | 2         |  3.484 us | 0.0904 us | 0.2444 us |  3.400 us |
    //     | Bench_OneOpCode | 32        |  3.958 us | 0.0829 us | 0.1954 us |  3.900 us |
    //     | Bench_OneOpCode | 128       |  5.721 us | 0.1100 us | 0.0975 us |  5.750 us |
    //     | Bench_OneOpCode | 1024      | 26.086 us | 0.5238 us | 0.8751 us | 26.000 us |
    //     | Bench_OneOpCode | 2040      | 32.465 us | 0.6344 us | 1.2521 us | 32.100 us |
}
