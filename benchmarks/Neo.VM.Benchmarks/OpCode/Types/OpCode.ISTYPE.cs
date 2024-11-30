// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.ISTYPE.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_ISTYPE : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.ISTYPE;
        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ItemCount);
            builder.Push(0);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            throw new NotImplementedException();
        }
    }
}
