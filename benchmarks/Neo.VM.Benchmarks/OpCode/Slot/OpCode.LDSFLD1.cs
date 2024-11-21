// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.LDSFLD1.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_LDSFLD1 : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.PICKITEM;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            throw new NotImplementedException();
        }
    }
}
