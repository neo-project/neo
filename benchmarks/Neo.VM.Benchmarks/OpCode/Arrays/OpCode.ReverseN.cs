// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.ReverseN.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_ReverseN : OpCodeBase
{
    [GlobalSetup]
    public void Setup()
    {
        var builder = new InstructionBuilder();
        var initBegin = new JumpTarget();
        builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
        builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.STLOC0);
        initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
        builder.AddInstruction(VM.OpCode.NEWBUFFER);

        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.AddInstruction(VM.OpCode.DEC);
        builder.AddInstruction(VM.OpCode.STLOC0);
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.Jump(VM.OpCode.JMPIF, initBegin);

        builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.REVERSEN);

        script = builder.ToArray();

        var multiBuilder = new InstructionBuilder();
        var initBegin2 = new JumpTarget();
        multiBuilder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
        multiBuilder.Push(ItemCount);
        multiBuilder.AddInstruction(VM.OpCode.STLOC0);
        initBegin2._instruction = multiBuilder.AddInstruction(VM.OpCode.NOP);
        multiBuilder.Push("aaabbbbbbbbbcccccccdddddddeeeeeeefffffff");
        multiBuilder.AddInstruction(VM.OpCode.LDLOC0);
        multiBuilder.AddInstruction(VM.OpCode.DEC);
        multiBuilder.AddInstruction(VM.OpCode.STLOC0);
        multiBuilder.AddInstruction(VM.OpCode.LDLOC0);
        multiBuilder.Jump(VM.OpCode.JMPIF, initBegin2);

        // just keep running until one GAS
        var loopStart = new JumpTarget { _instruction = multiBuilder.AddInstruction(VM.OpCode.NOP) };
        multiBuilder.Push(ItemCount);
        multiBuilder.AddInstruction(VM.OpCode.REVERSEN);
        multiBuilder.Jump(VM.OpCode.JMP, loopStart);

        multiScript = multiBuilder.ToArray();
    }

    [Benchmark]
    public void Bench_ReverseN() => Benchmark_Opcode.RunScript(script);

    /// <summary>
    /// Benchmark how long 1 GAS can run OpCode.REVERSEN.
    /// </summary>
    [Benchmark]
    public void Bench_OneGasReverseN() => Benchmark_Opcode.LoadScript(multiScript).ExecuteOneGASBenchmark();
}

// for "aaabbbbbbbbbcccccccdddddddeeeeeeefffffff"

// BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4249/23H2/2023Update/SunValley3)
// Intel Core i9-14900HX, 1 CPU, 32 logical and 24 physical cores
// .NET SDK 8.0.205
//   [Host]     : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
//   DefaultJob : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
//
//
// | Method               | ItemCount | Mean             | Error         | StdDev        |
// |--------------------- |---------- |-----------------:|--------------:|--------------:|
// | Bench_ReverseN       | 1         |         60.55 us |      0.422 us |      0.395 us |
// | Bench_OneGasReverseN | 1         |    386,538.66 us |  5,509.996 us |  4,884.468 us |
// | Bench_ReverseN       | 2         |         59.70 us |      0.185 us |      0.145 us |
// | Bench_OneGasReverseN | 2         |    407,545.61 us |  5,259.947 us |  4,920.158 us |
// | Bench_ReverseN       | 4         |         60.01 us |      0.286 us |      0.268 us |
// | Bench_OneGasReverseN | 4         |    423,795.19 us |  5,361.421 us |  5,015.076 us |
// | Bench_ReverseN       | 8         |         61.43 us |      0.456 us |      0.404 us |
// | Bench_OneGasReverseN | 8         |    433,683.85 us |  6,647.656 us |  6,218.221 us |
// | Bench_ReverseN       | 16        |         62.14 us |      0.646 us |      0.572 us |
// | Bench_OneGasReverseN | 16        |    498,601.53 us |  2,491.481 us |  2,080.500 us |
// | Bench_ReverseN       | 32        |         67.15 us |      0.558 us |      0.522 us |
// | Bench_OneGasReverseN | 32        |    602,409.70 us |  2,859.685 us |  2,535.036 us |
// | Bench_ReverseN       | 64        |         69.36 us |      0.620 us |      0.580 us |
// | Bench_OneGasReverseN | 64        |    778,469.79 us |  2,975.210 us |  2,783.013 us |
// | Bench_ReverseN       | 128       |         78.36 us |      0.633 us |      0.561 us |
// | Bench_OneGasReverseN | 128       |  1,112,229.53 us |  8,896.450 us |  8,321.745 us |
// | Bench_ReverseN       | 256       |         97.13 us |      0.836 us |      0.782 us |
// | Bench_OneGasReverseN | 256       |  1,763,759.99 us |  8,213.085 us |  7,682.525 us |
// | Bench_ReverseN       | 512       |        129.66 us |      0.805 us |      0.753 us |
// | Bench_OneGasReverseN | 512       |  3,065,050.78 us |  9,179.181 us |  8,137.105 us |
// | Bench_ReverseN       | 1024      |        200.42 us |      1.684 us |      1.576 us |
// | Bench_OneGasReverseN | 1024      |  5,696,789.37 us | 25,210.295 us | 22,348.270 us |
// | Bench_ReverseN       | 2040      |        345.68 us |      3.183 us |      2.978 us |
// | Bench_OneGasReverseN | 2040      | 10,933,539.80 us | 46,907.983 us | 43,877.757 us |


// for
