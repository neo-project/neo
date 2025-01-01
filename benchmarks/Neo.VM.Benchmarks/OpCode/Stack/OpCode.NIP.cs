// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NIP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NIP : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.NIP;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(int.MaxValue);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(int.MaxValue);
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(Opcode);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean             | Error           | StdDev         | Median           |
/// |---------------- |---------- |-----------------:|----------------:|---------------:|-----------------:|
/// | Bench_OneOpCode | 4         |       1,502.0 ns |       104.65 ns |       305.3 ns |       1,500.0 ns |
/// | Bench_OneGAS    | 4         | 448,710,720.0 ns | 5,071,622.40 ns | 4,743,998.7 ns | 449,708,200.0 ns |
/// | Bench_OneOpCode | 8         |       1,456.0 ns |       109.00 ns |       321.4 ns |       1,300.0 ns |
/// | Bench_OneGAS    | 8         | 450,164,914.3 ns | 2,727,195.50 ns | 2,417,587.8 ns | 450,657,300.0 ns |
/// | Bench_OneOpCode | 16        |         967.1 ns |        48.89 ns |       132.2 ns |         900.0 ns |
/// | Bench_OneGAS    | 16        | 445,292,933.3 ns | 4,320,305.15 ns | 4,041,216.1 ns | 446,585,400.0 ns |
/// | Bench_OneOpCode | 32        |       1,336.4 ns |       124.52 ns |       365.2 ns |       1,300.0 ns |
/// | Bench_OneGAS    | 32        | 439,578,406.7 ns | 3,017,838.29 ns | 2,822,887.8 ns | 439,952,300.0 ns |
/// | Bench_OneOpCode | 64        |       1,600.0 ns |       136.09 ns |       401.3 ns |       1,700.0 ns |
/// | Bench_OneGAS    | 64        | 445,796,385.7 ns | 2,937,140.91 ns | 2,603,698.9 ns | 445,891,500.0 ns |
/// | Bench_OneOpCode | 128       |       1,232.0 ns |       121.62 ns |       352.8 ns |       1,100.0 ns |
/// | Bench_OneGAS    | 128       | 444,975,820.0 ns | 3,783,980.92 ns | 3,539,538.1 ns | 445,837,700.0 ns |
/// | Bench_OneOpCode | 256       |       1,261.6 ns |       127.18 ns |       373.0 ns |       1,300.0 ns |
/// | Bench_OneGAS    | 256       | 454,435,660.0 ns | 3,367,303.97 ns | 3,149,778.2 ns | 454,368,900.0 ns |
/// | Bench_OneOpCode | 512       |       1,279.8 ns |       150.53 ns |       441.5 ns |       1,100.0 ns |
/// | Bench_OneGAS    | 512       | 444,591,613.3 ns | 3,742,388.20 ns | 3,500,632.2 ns | 444,914,100.0 ns |
/// | Bench_OneOpCode | 1024      |       1,287.9 ns |       102.78 ns |       301.4 ns |       1,200.0 ns |
/// | Bench_OneGAS    | 1024      | 440,300,480.0 ns | 5,631,042.61 ns | 5,267,280.8 ns | 438,291,900.0 ns |
/// | Bench_OneOpCode | 2040      |       1,308.4 ns |       110.64 ns |       317.5 ns |       1,300.0 ns |
/// | Bench_OneGAS    | 2040      | 447,774,933.3 ns | 1,783,355.63 ns | 1,392,326.8 ns | 447,820,550.0 ns |
