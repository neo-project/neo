// Copyright (C) 2015-2025 The Neo Project.
//
// IndividualOpcodeBenchmarks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Neo.VM.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class IndividualOpcodeBenchmarks
    {
        [GlobalSetup]
        public void Setup()
        {
            Console.WriteLine("=== Individual Neo VM Opcode Benchmarks Setup ===");
            Console.WriteLine("Measuring each opcode individually for accurate gas pricing");
            Console.WriteLine(".NET Version: " + Environment.Version);
            Console.WriteLine("Platform: " + Environment.OSVersion);
            Console.WriteLine("Processor Count: " + Environment.ProcessorCount);
            Console.WriteLine();
        }

        #region Constants (0x00-0x20) - Individual Measurements

        [Benchmark]
        [Arguments(OpCode.PUSHINT8)]
        [Arguments(OpCode.PUSHINT16)]
        [Arguments(OpCode.PUSHINT32)]
        [Arguments(OpCode.PUSHINT64)]
        [Arguments(OpCode.PUSHINT128)]
        [Arguments(OpCode.PUSHINT256)]
        [Arguments(OpCode.PUSHT)]
        [Arguments(OpCode.PUSHF)]
        [Arguments(OpCode.PUSHNULL)]
        [Arguments(OpCode.PUSHM1)]
        [Arguments(OpCode.PUSH0)]
        [Arguments(OpCode.PUSH1)]
        [Arguments(OpCode.PUSH2)]
        [Arguments(OpCode.PUSH3)]
        [Arguments(OpCode.PUSH4)]
        [Arguments(OpCode.PUSH5)]
        [Arguments(OpCode.PUSH6)]
        [Arguments(OpCode.PUSH7)]
        [Arguments(OpCode.PUSH8)]
        [Arguments(OpCode.PUSH9)]
        [Arguments(OpCode.PUSH10)]
        [Arguments(OpCode.PUSH11)]
        [Arguments(OpCode.PUSH12)]
        [Arguments(OpCode.PUSH13)]
        [Arguments(OpCode.PUSH14)]
        [Arguments(OpCode.PUSH15)]
        [Arguments(OpCode.PUSH16)]
        public void ConstantOpcodes(OpCode opcode) => ExecuteScript(BuildConstantScript(opcode));

        #endregion

        #region Flow Control (0x21-0x41) - Individual Measurements

        [Benchmark]
        [Arguments(OpCode.NOP)]
        public void FlowControlOpcodes(OpCode opcode) => ExecuteScript(BuildScript(builder => builder.Emit(opcode)));

        [Benchmark]
        [Arguments(OpCode.JMP)]
        [Arguments(OpCode.JMPIF)]
        [Arguments(OpCode.JMPIFNOT)]
        public void JumpOpcodes(OpCode opcode) => ExecuteScript(BuildScript(builder =>
        {
            builder.EmitPush(0);
            builder.Emit(opcode, new[] { (byte)0x02 });
            builder.EmitPush(0);
            builder.Emit(OpCode.DROP);
        }));

        [Benchmark]
        public void CALL() => ExecuteScript(new byte[]
        {
            (byte)OpCode.CALL, 0x01,
            (byte)OpCode.RET,
            (byte)OpCode.PUSH0,
            (byte)OpCode.RET
        });

        [Benchmark]
        public void RET() => ExecuteScript(BuildScript(builder => builder.EmitPush(0)));

        #endregion

        #region Stack Operations (0x43-0x55) - Individual Measurements

        [Benchmark]
        [Arguments(OpCode.DEPTH)]
        [Arguments(OpCode.DROP)]
        public void SimpleStackOpcodes(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                (byte)opcode,
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.NIP)]
        [Arguments(OpCode.OVER)]
        [Arguments(OpCode.TUCK)]
        public void StackOpcodesTwoItems(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                (byte)opcode,
                0x45, // DROP
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.DUP)]
        [Arguments(OpCode.SWAP)]
        public void StackOpcodesTwoItemsFast(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                (byte)opcode,
                0x45, // DROP
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.ROT)]
        public void ROT()
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                0x13, // PUSH3
                (byte)OpCode.ROT,
                0x45, // DROP
                0x45, // DROP
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        public void CLEAR()
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                0x13, // PUSH3
                (byte)OpCode.CLEAR,
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Bitwise Logic (0x90-0x98) - Individual Measurements

        [Benchmark]
        [Arguments(OpCode.INVERT)]
        public void UnaryBitwiseOpcodes(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                (byte)opcode,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.AND)]
        [Arguments(OpCode.OR)]
        [Arguments(OpCode.XOR)]
        [Arguments(OpCode.EQUAL)]
        [Arguments(OpCode.NOTEQUAL)]
        public void BinaryBitwiseOpcodes(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                (byte)opcode,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Arithmetic (0x99-0xBB) - Individual Measurements

        [Benchmark]
        [Arguments(OpCode.SIGN)]
        [Arguments(OpCode.ABS)]
        [Arguments(OpCode.NEGATE)]
        [Arguments(OpCode.INC)]
        [Arguments(OpCode.DEC)]
        [Arguments(OpCode.NOT)]
        [Arguments(OpCode.NZ)]
        public void UnaryArithmeticOpcodes(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                (byte)opcode,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.ADD)]
        [Arguments(OpCode.SUB)]
        [Arguments(OpCode.MUL)]
        [Arguments(OpCode.SHL)]
        [Arguments(OpCode.SHR)]
        public void BinaryArithmeticOpcodesFast(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                (byte)opcode,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.DIV)]
        [Arguments(OpCode.MOD)]
        public void BinaryArithmeticOpcodesSlow(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                (byte)opcode,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.POW)]
        [Arguments(OpCode.SQRT)]
        [Arguments(OpCode.MODPOW)]
        public void ComplexArithmeticOpcodes(OpCode opcode)
        {
            var script = new List<byte>();
            if (opcode == OpCode.POW || opcode == OpCode.MODPOW)
            {
                script.Add(0x02); // PUSHINT32
                script.AddRange(BitConverter.GetBytes(2)); // base = 2
                script.Add(0x02); // PUSHINT32
                script.AddRange(BitConverter.GetBytes(10)); // exponent = 10
            }
            else if (opcode == OpCode.SQRT)
            {
                script.Add(0x04); // PUSHINT32
                script.AddRange(BitConverter.GetBytes(16)); // value = 16
            }
            script.Add((byte)opcode);
            script.Add(0x45); // DROP
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.LT)]
        [Arguments(OpCode.LE)]
        [Arguments(OpCode.GT)]
        [Arguments(OpCode.GE)]
        [Arguments(OpCode.MIN)]
        [Arguments(OpCode.MAX)]
        public void ComparisonOpcodes(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                (byte)opcode,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        public void WITHIN()
        {
            var script = new List<byte>
            {
                0x11, // PUSH1 (value)
                0x10, // PUSH0 (min)
                0x12, // PUSH2 (max)
                (byte)OpCode.WITHIN,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Compound Types (0xBE-0xD4) - Individual Measurements

        [Benchmark]
        [Arguments(OpCode.NEWARRAY0)]
        [Arguments(OpCode.NEWSTRUCT0)]
        [Arguments(OpCode.NEWMAP)]
        public void EmptyCompoundOpcodes(OpCode opcode)
        {
            var script = new List<byte>
            {
                (byte)opcode,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.SIZE)]
        public void SIZE()
        {
            var script = new List<byte>
            {
                (byte)OpCode.NEWARRAY0,
                (byte)OpCode.SIZE,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.PICKITEM)]
        public void PICKITEM()
        {
            var script = new List<byte>
            {
                (byte)OpCode.NEWARRAY0,
                0x11, // PUSH1 (index)
                (byte)OpCode.PICKITEM,
                0x45, // DROP
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(OpCode.APPEND)]
        [Arguments(OpCode.SETITEM)]
        public void ModificationOpcodes(OpCode opcode)
        {
            var script = new List<byte>
            {
                (byte)OpCode.NEWARRAY0,
                0x11, // PUSH1 (value)
                (byte)opcode,
                0x45, // DROP
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Type Operations (0xD8-0xDB) - Individual Measurements

        [Benchmark]
        [Arguments(OpCode.ISNULL)]
        [Arguments(OpCode.ISTYPE)]
        public void TypeCheckOpcodes(OpCode opcode)
        {
            var script = new List<byte>
            {
                0x0B, // PUSHNULL
                (byte)opcode,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Data Operations - Individual Measurements

        [Benchmark]
        public void PUSHDATA1()
        {
            var script = new List<byte>
            {
                0x0C, // PUSHDATA1
                0x05 // Length = 5
            };
            script.AddRange(System.Text.Encoding.UTF8.GetBytes("Hello")); // Data
            script.Add(0x45); // DROP
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        public void NEWARRAY()
        {
            var script = new List<byte>
            {
                0x11, // PUSH1 (size)
                (byte)OpCode.NEWARRAY,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        public void PACK()
        {
            var script = new List<byte>
            {
                0x11, // PUSH1
                0x12, // PUSH2
                0x13, // PUSH3
                0x11, // PUSH3 (count)
                (byte)OpCode.PACK,
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        public void UNPACK()
        {
            var script = new List<byte>
            {
                (byte)OpCode.NEWARRAY0,
                (byte)OpCode.UNPACK,
                0x45, // DROP
                0x45, // DROP
                0x40 // RET
            };

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Helpers

        private static byte[] BuildScript(Action<ScriptBuilder> emitter)
        {
            using var builder = new ScriptBuilder();
            emitter(builder);
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildConstantScript(OpCode opcode) => BuildScript(builder =>
        {
            switch (opcode)
            {
                case OpCode.PUSHINT8:
                    builder.EmitPush((sbyte)42);
                    break;
                case OpCode.PUSHINT16:
                    builder.EmitPush((short)0x1234);
                    break;
                case OpCode.PUSHINT32:
                    builder.EmitPush(0x12345678);
                    break;
                case OpCode.PUSHINT64:
                    builder.EmitPush(0x123456789ABCDEF0L);
                    break;
                case OpCode.PUSHINT128:
                    builder.EmitPush(new BigInteger(new byte[]
                    {
                        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16
                    }));
                    break;
                case OpCode.PUSHINT256:
                    builder.EmitPush(new BigInteger(new byte[]
                    {
                        1, 2, 3, 4, 5, 6, 7, 8,
                        9, 10, 11, 12, 13, 14, 15, 16,
                        17, 18, 19, 20, 21, 22, 23, 24,
                        25, 26, 27, 28, 29, 30, 31, 32
                    }));
                    break;
                case OpCode.PUSHT:
                    builder.EmitPush(true);
                    break;
                case OpCode.PUSHF:
                    builder.EmitPush(false);
                    break;
                case OpCode.PUSHNULL:
                    builder.Emit(OpCode.PUSHNULL);
                    break;
                case OpCode.PUSHM1:
                    builder.EmitPush(-1);
                    break;
                case var op when op >= OpCode.PUSH0 && op <= OpCode.PUSH16:
                    builder.EmitPush((int)(op - OpCode.PUSH0));
                    break;
                default:
                    builder.Emit(opcode);
                    break;
            }
        });

        private static void ExecuteScript(byte[] script)
        {
            using var engine = new ExecutionEngine();
            engine.LoadScript(script);
            var state = engine.Execute();
            if (state != VMState.HALT)
                throw new InvalidOperationException($"Benchmark script exited with state {state}.");
        }

        private static void ExecuteScript(ReadOnlySpan<byte> script) => ExecuteScript(script.ToArray());

        #endregion

        [GlobalCleanup]
        public void Cleanup()
        {
            Console.WriteLine("=== Individual Opcode Benchmarks Complete ===");
        }
    }
}
