// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.HASKEY.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_HASKEY : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.HASKEY;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);

            builder.Push(ItemCount);// push the number of array items
            builder.AddInstruction(VM.OpCode.PACK); // pack itemcount items as an array

            builder.Push(new BigInteger(0)); // the index
            builder.AddInstruction(Opcode);  // HASKEY

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);

            builder.Push(ItemCount);// push the number of array items
            builder.AddInstruction(VM.OpCode.PACK); // pack itemcount items as an array

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) }; // now deadloop until you reach 1 GAS
            builder.AddInstruction(VM.OpCode.DUP);
            builder.Push(new BigInteger(0)); // the index
            builder.AddInstruction(Opcode);  // HASKEY
            builder.AddInstruction(VM.OpCode.DROP); // Drop the haskey result to clear the stack
            builder.Jump(VM.OpCode.JMP, loopBegin); // loop
            return builder.ToArray();
        }
    }
}

/// | Method | ItemCount | Mean | Error | StdDev | Median |
/// | ---------------- | ---------- | --------------:| --------------:| --------------:| --------------:|
/// | Bench_OneOpCode | 4 | 3.248 us | 0.0962 us | 0.2568 us | 3.200 us |
/// | Bench_OneGAS | 4 | 74,225.206 us | 1,378.1887 us | 1,415.2983 us | 74,352.600 us |
/// | Bench_OneOpCode | 8 | 3.612 us | 0.1824 us | 0.5321 us | 3.500 us |
/// | Bench_OneGAS | 8 | 76,327.457 us | 1,045.4895 us | 926.7992 us | 76,400.250 us |
/// | Bench_OneOpCode | 16 | 4.079 us | 0.2004 us | 0.5846 us | 3.900 us |
/// | Bench_OneGAS | 16 | 75,192.031 us | 1,415.8505 us | 1,390.5545 us | 74,870.650 us |
/// | Bench_OneOpCode | 32 | 5.112 us | 0.2663 us | 0.7725 us | 5.000 us |
/// | Bench_OneGAS | 32 | 75,857.130 us | 1,469.6428 us | 1,858.6276 us | 76,339.200 us |
/// | Bench_OneOpCode | 64 | 6.219 us | 0.3227 us | 0.9463 us | 6.300 us |
/// | Bench_OneGAS | 64 | 75,104.112 us | 1,462.8695 us | 1,502.2592 us | 75,085.000 us |
/// | Bench_OneOpCode | 128 | 7.916 us | 1.2663 us | 3.7337 us | 6.000 us |
/// | Bench_OneGAS | 128 | 74,524.787 us | 1,190.4420 us | 1,113.5401 us | 74,585.500 us |
/// | Bench_OneOpCode | 256 | 11.904 us | 1.5944 us | 4.7012 us | 13.300 us |
/// | Bench_OneGAS | 256 | 74,676.473 us | 1,443.9591 us | 1,350.6803 us | 74,575.900 us |
/// | Bench_OneOpCode | 512 | 6.166 us | 0.5779 us | 1.5426 us | 6.300 us |
/// | Bench_OneGAS | 512 | 74,311.340 us | 1,373.6849 us | 1,284.9457 us | 74,257.900 us |
/// | Bench_OneOpCode | 1024 | 8.280 us | 0.5787 us | 1.6133 us | 8.300 us |
/// | Bench_OneGAS | 1024 | 76,817.562 us | 1,492.8381 us | 1,466.1666 us | 76,597.800 us |
/// | Bench_OneOpCode | 2040 | 13.983 us | 0.6613 us | 1.9079 us | 14.200 us |
/// | Bench_OneGAS | 2040 | 76,128.674 us | 1,412.1090 us | 1,569.5554 us | 75,931.400 us |
