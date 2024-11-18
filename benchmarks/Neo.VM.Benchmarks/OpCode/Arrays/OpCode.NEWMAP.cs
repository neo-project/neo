// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWMAP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NEWMAP : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.NEWMAP;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWMAP);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    //     | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
    //     |---------------- |---------- |---------:|----------:|----------:|---------:|
    //     | Bench_OneOpCode | 1         | 2.436 us | 0.1130 us | 0.3036 us | 2.350 us |
    //     | Bench_OneOpCode | 32        | 2.382 us | 0.1007 us | 0.2756 us | 2.300 us |
    //     | Bench_OneOpCode | 128       | 2.280 us | 0.0493 us | 0.1255 us | 2.300 us |
    //     | Bench_OneOpCode | 1024      | 2.444 us | 0.1008 us | 0.2708 us | 2.400 us |
    //     | Bench_OneOpCode | 2040      | 2.359 us | 0.0643 us | 0.1694 us | 2.300 us |
}
