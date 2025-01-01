// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.CAT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_CAT : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.CAT;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.CONVERT,
                _operand = [(byte)StackItemType.ByteString]
            });
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.CONVERT,
                _operand = [(byte)StackItemType.ByteString]
            });

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean          | Error         | StdDev        | Median        |
/// |---------------- |---------- |--------------:|--------------:|--------------:|--------------:|
/// | Bench_OneOpCode | 4         |      2.531 us |     0.1734 us |     0.5084 us |      2.300 us |
/// | Bench_OneGAS    | 4         |  7,354.089 us | 2,107.4629 us | 6,213.9024 us |  3,056.950 us |
/// | Bench_OneOpCode | 8         |      2.188 us |     0.0665 us |     0.1786 us |      2.150 us |
/// | Bench_OneGAS    | 8         |  6,770.574 us | 1,862.5357 us | 5,462.4951 us |  4,202.000 us |
/// | Bench_OneOpCode | 16        |      2.196 us |     0.0583 us |     0.1514 us |      2.200 us |
/// | Bench_OneGAS    | 16        |  7,297.803 us | 1,873.4257 us | 5,523.8383 us |  3,237.550 us |
/// | Bench_OneOpCode | 32        |      2.488 us |     0.1525 us |     0.4447 us |      2.300 us |
/// | Bench_OneGAS    | 32        | 10,363.423 us |   171.1694 us |   142.9342 us | 10,370.600 us |
/// | Bench_OneOpCode | 64        |      2.212 us |     0.0559 us |     0.1522 us |      2.200 us |
/// | Bench_OneGAS    | 64        |  7,836.663 us | 1,643.4393 us | 4,819.9234 us |  6,299.400 us |
/// | Bench_OneOpCode | 128       |      2.125 us |     0.0463 us |     0.1179 us |      2.100 us |
/// | Bench_OneGAS    | 128       |  7,557.801 us | 1,827.8226 us | 5,360.6877 us |  4,057.400 us |
/// | Bench_OneOpCode | 256       |      2.600 us |     0.1930 us |     0.5537 us |      2.400 us |
/// | Bench_OneGAS    | 256       |  8,188.538 us | 2,081.1449 us | 6,103.6383 us |  4,268.200 us |
/// | Bench_OneOpCode | 512       |      2.187 us |     0.0420 us |     0.0629 us |      2.200 us |
/// | Bench_OneGAS    | 512       |  7,020.697 us | 1,361.4439 us | 3,749.8122 us |  4,722.650 us |
/// | Bench_OneOpCode | 1024      |      2.325 us |     0.0400 us |     0.0936 us |      2.300 us |
/// | Bench_OneGAS    | 1024      |  6,066.211 us |   312.1295 us |   838.5158 us |  5,755.600 us |
/// | Bench_OneOpCode | 2040      |      2.323 us |     0.0800 us |     0.2122 us |      2.300 us |
/// | Bench_OneGAS    | 2040      |  8,367.546 us | 1,146.3423 us | 3,214.4671 us |  6,659.300 us |
