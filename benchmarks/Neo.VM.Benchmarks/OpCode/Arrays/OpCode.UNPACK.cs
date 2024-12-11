// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.UNPACK.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_UNPACK : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.UNPACK;

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
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
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
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.CLEAR);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

// | Method          | ItemCount | Mean             | Error           | StdDev          | Median           |
// |---------------- |---------- |-----------------:|----------------:|----------------:|-----------------:|
// | Bench_OneOpCode | 2         |         3.479 us |       0.3200 us |       0.9436 us |         2.900 us |
// | Bench_OneGAS    | 2         |   513,467.316 us |  10,244.0303 us |  19,240.7721 us |   512,184.150 us |
// | Bench_OneOpCode | 4         |         3.203 us |       0.1661 us |       0.4574 us |         3.150 us |
// | Bench_OneGAS    | 4         |   129,626.795 us |   2,565.9639 us |   3,054.5975 us |   130,222.000 us |
// | Bench_OneOpCode | 8         |         3.668 us |       0.2038 us |       0.5815 us |         3.500 us |
// | Bench_OneGAS    | 8         |   291,882.041 us |   7,885.5198 us |  23,250.6331 us |   286,752.650 us |
// | Bench_OneOpCode | 16        |         4.416 us |       0.2380 us |       0.6981 us |         4.300 us |
// | Bench_OneGAS    | 16        |   642,757.534 us |  21,564.7563 us |  63,584.1705 us |   657,215.100 us |
// | Bench_OneOpCode | 32        |         5.321 us |       0.2478 us |       0.7151 us |         5.350 us |
// | Bench_OneGAS    | 32        | 1,752,439.210 us |  68,580.6555 us | 202,211.6101 us | 1,821,746.400 us |
// | Bench_OneOpCode | 64        |         9.712 us |       0.5929 us |       1.7202 us |         9.500 us |
// | Bench_OneGAS    | 64        | 2,315,097.352 us | 117,461.1516 us | 346,336.8558 us | 2,314,699.600 us |
// | Bench_OneOpCode | 128       |        13.180 us |       0.6729 us |       1.9308 us |        13.500 us |
// | Bench_OneGAS    | 128       | 2,374,006.539 us | 157,538.3002 us | 464,505.2326 us | 2,354,696.300 us |
// | Bench_OneOpCode | 1024      |        22.739 us |       2.1317 us |       6.2181 us |        23.450 us |
// | Bench_OneGAS    | 1024      | 1,865,747.094 us | 107,959.2625 us | 316,625.8285 us | 1,861,592.300 us |
// | Bench_OneOpCode | 2040      |        32.757 us |       1.9741 us |       5.6957 us |        32.850 us |
// | Bench_OneGAS    | 2040      | 1,540,003.310 us |  88,182.2617 us | 258,623.3086 us | 1,569,928.200 us |


// (int)Math.Log2(_complexFactor)
// | Method          | ItemCount | Mean           | Error          | StdDev         | Median         |
//     |---------------- |---------- |---------------:|---------------:|---------------:|---------------:|
//     | Bench_OneOpCode | 4         |       3.958 us |      0.3158 us |      0.9261 us |       3.500 us |
//     | Bench_OneGAS    | 4         |  81,987.548 us |  1,612.9437 us |  2,153.2335 us |  82,532.200 us |
//     | Bench_OneOpCode | 8         |       3.406 us |      0.0985 us |      0.2645 us |       3.400 us |
//     | Bench_OneGAS    | 8         | 129,254.409 us |  2,560.0838 us |  7,345.3603 us | 128,704.100 us |
//     | Bench_OneOpCode | 16        |       4.455 us |      0.2512 us |      0.7328 us |       4.400 us |
//     | Bench_OneGAS    | 16        | 164,205.854 us |  4,597.2421 us | 13,337.4330 us | 164,306.400 us |
//     | Bench_OneOpCode | 32        |       5.634 us |      0.2780 us |      0.7933 us |       5.650 us |
//     | Bench_OneGAS    | 32        | 352,413.834 us | 11,065.7181 us | 32,453.8357 us | 355,597.400 us |
//     | Bench_OneOpCode | 64        |       8.132 us |      0.4026 us |      1.1679 us |       7.900 us |
//     | Bench_OneGAS    | 64        | 402,783.034 us | 23,184.6934 us | 68,360.5915 us | 386,952.900 us |
//     | Bench_OneOpCode | 128       |      13.280 us |      0.8872 us |      2.5168 us |      13.800 us |
//     | Bench_OneGAS    | 128       | 361,699.914 us | 21,274.4294 us | 62,728.1351 us | 363,205.250 us |
//     | Bench_OneOpCode | 256       |      12.629 us |      2.5149 us |      7.3361 us |       9.000 us |
//     | Bench_OneGAS    | 256       | 258,857.869 us | 19,911.2896 us | 57,766.2617 us | 254,781.800 us |
//     | Bench_OneOpCode | 1024      |      22.502 us |      2.2535 us |      6.6091 us |      23.200 us |
//     | Bench_OneGAS    | 1024      | 180,149.543 us |  6,714.3334 us | 19,797.3636 us | 181,997.800 us |
//     | Bench_OneOpCode | 2040      |      31.508 us |      2.2616 us |      6.5972 us |      32.150 us |
//     | Bench_OneGAS    | 2040      | 152,007.055 us |  3,002.9974 us |  4,401.7546 us | 151,728.000 us |


// | Method       | ItemCount | Mean     | Error    | StdDev   | Median   |
//     |------------- |---------- |---------:|---------:|---------:|---------:|
//     | Bench_OneGAS | 4         | 200.7 ms |  3.78 ms |  4.04 ms | 200.9 ms |
//     | Bench_OneGAS | 8         | 425.9 ms | 10.08 ms | 29.72 ms | 417.7 ms |
//     | Bench_OneGAS | 16        | 221.3 ms |  9.02 ms | 26.47 ms | 225.1 ms |
//     | Bench_OneGAS | 32        | 444.0 ms | 19.90 ms | 58.67 ms | 451.5 ms |
//     | Bench_OneGAS | 64        | 495.8 ms | 29.98 ms | 88.39 ms | 499.7 ms |
//     | Bench_OneGAS | 128       | 464.2 ms | 28.86 ms | 85.09 ms | 457.1 ms |
//     | Bench_OneGAS | 256       | 402.7 ms | 30.06 ms | 88.15 ms | 390.3 ms |
//     | Bench_OneGAS | 512       | 360.1 ms | 26.18 ms | 77.18 ms | 343.9 ms |
//     | Bench_OneGAS | 1024      | 362.8 ms | 21.29 ms | 62.43 ms | 363.5 ms |
//     | Bench_OneGAS | 2040      | 232.9 ms |  3.24 ms |  2.87 ms | 232.4 ms |
