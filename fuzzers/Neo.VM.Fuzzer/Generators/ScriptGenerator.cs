// Copyright (C) 2015-2025 The Neo Project.
//
// ScriptGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Neo.VM.Fuzzer.Generators
{
    /// <summary>
    /// Generates random but valid Neo VM scripts for fuzzing
    /// </summary>
    public class ScriptGenerator
    {
        private readonly Random _random;

        // Group opcodes by category for more structured generation
        private static readonly OpCode[] _arithmeticOpcodes = new[]
        {
            OpCode.ADD, OpCode.SUB, OpCode.MUL, OpCode.DIV, OpCode.MOD, OpCode.POW,
            OpCode.SHL, OpCode.SHR, OpCode.NOT, OpCode.BOOLAND, OpCode.BOOLOR,
            OpCode.NUMEQUAL, OpCode.NUMNOTEQUAL, OpCode.LT, OpCode.LE, OpCode.GT, OpCode.GE,
            OpCode.MIN, OpCode.MAX, OpCode.WITHIN
        };

        private static readonly OpCode[] _stackOpcodes = new[]
        {
            OpCode.DUP, OpCode.SWAP, OpCode.TUCK, OpCode.OVER, OpCode.ROT,
            OpCode.DEPTH, OpCode.DROP, OpCode.NIP, OpCode.PICK, OpCode.ROLL
        };

        private static readonly OpCode[] _arrayOpcodes = new[]
        {
            OpCode.NEWARRAY, OpCode.NEWARRAY0, OpCode.NEWSTRUCT, OpCode.NEWSTRUCT0,
            OpCode.APPEND, OpCode.REMOVE, OpCode.HASKEY,
            OpCode.KEYS, OpCode.VALUES, OpCode.PACK, OpCode.UNPACK,
            OpCode.PICKITEM, OpCode.SETITEM, OpCode.SIZE
        };

        private static readonly OpCode[] _controlFlowOpcodes = new[]
        {
            OpCode.JMP, OpCode.JMPIF, OpCode.JMPIFNOT, OpCode.CALL,
            OpCode.RET, OpCode.SYSCALL
        };

        private static readonly OpCode[] _constantOpcodes = new[]
        {
            OpCode.PUSH0, OpCode.PUSHM1, OpCode.PUSH1, OpCode.PUSH2, OpCode.PUSH3,
            OpCode.PUSH4, OpCode.PUSH5, OpCode.PUSH6, OpCode.PUSH7, OpCode.PUSH8,
            OpCode.PUSH9, OpCode.PUSH10, OpCode.PUSH11, OpCode.PUSH12, OpCode.PUSH13,
            OpCode.PUSH14, OpCode.PUSH15, OpCode.PUSH16, OpCode.PUSHDATA1, OpCode.PUSHDATA2,
            OpCode.PUSHDATA4, OpCode.PUSHINT8, OpCode.PUSHINT16, OpCode.PUSHINT32, OpCode.PUSHINT64,
            OpCode.PUSHINT128, OpCode.PUSHINT256, OpCode.PUSHA, OpCode.PUSHNULL,
            OpCode.PUSHF, OpCode.PUSHT
        };

        private static readonly OpCode[] _cryptoOpcodes = new[]
        {
            OpCode.EQUAL
        };

        private static readonly OpCode[] _conversionOpcodes = new[]
        {
            OpCode.CONVERT
        };

        /// <summary>
        /// Initializes a new instance of the ScriptGenerator class
        /// </summary>
        /// <param name="random">Random number generator</param>
        public ScriptGenerator(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Generates a random Neo VM script
        /// </summary>
        /// <param name="maxInstructions">Maximum number of instructions in the script</param>
        /// <returns>A byte array containing the generated script</returns>
        public byte[] GenerateRandomScript(int maxInstructions = 100)
        {
            // Ensure we don't generate too large scripts
            maxInstructions = Math.Min(maxInstructions, 1000);

            var scriptBuilder = new ScriptBuilder();

            // Choose a script generation strategy
            int strategy = _random.Next(4);

            switch (strategy)
            {
                case 0:
                    GenerateArithmeticHeavyScript(scriptBuilder, maxInstructions);
                    break;
                case 1:
                    GenerateStackHeavyScript(scriptBuilder, maxInstructions);
                    break;
                case 2:
                    GenerateArrayHeavyScript(scriptBuilder, maxInstructions);
                    break;
                default:
                    GenerateRandomMixedScript(scriptBuilder, maxInstructions);
                    break;
            }

            // Always end with RET to ensure script terminates
            scriptBuilder.Emit(OpCode.RET);

            return scriptBuilder.ToArray();
        }

        /// <summary>
        /// Generates a script focused on arithmetic operations
        /// </summary>
        private void GenerateArithmeticHeavyScript(ScriptBuilder scriptBuilder, int maxInstructions)
        {
            // First push some values onto the stack
            for (int i = 0; i < _random.Next(5, 10); i++)
            {
                EmitRandomPush(scriptBuilder);
            }

            // Then perform arithmetic operations
            int operations = Math.Min(maxInstructions - 10, _random.Next(10, maxInstructions));

            for (int i = 0; i < operations; i++)
            {
                if (_random.Next(5) == 0)
                {
                    // Occasionally push more values to keep the stack from depleting
                    EmitRandomPush(scriptBuilder);
                }
                else
                {
                    // Emit an arithmetic operation
                    scriptBuilder.Emit(_arithmeticOpcodes[_random.Next(_arithmeticOpcodes.Length)]);
                }
            }
        }

        /// <summary>
        /// Generates a script focused on stack manipulation
        /// </summary>
        private void GenerateStackHeavyScript(ScriptBuilder scriptBuilder, int maxInstructions)
        {
            // First push some values onto the stack
            for (int i = 0; i < _random.Next(5, 15); i++)
            {
                EmitRandomPush(scriptBuilder);
            }

            // Then perform stack operations
            int operations = Math.Min(maxInstructions - 15, _random.Next(10, maxInstructions));

            for (int i = 0; i < operations; i++)
            {
                if (_random.Next(5) == 0)
                {
                    // Occasionally push more values
                    EmitRandomPush(scriptBuilder);
                }
                else
                {
                    // Emit a stack operation
                    OpCode opcode = _stackOpcodes[_random.Next(_stackOpcodes.Length)];

                    // For PICK and ROLL, we need to push an index first
                    if (opcode == OpCode.PICK || opcode == OpCode.ROLL)
                    {
                        // Push a small index to avoid stack underflow
                        EmitSmallIntPush(scriptBuilder, _random.Next(5));
                    }

                    scriptBuilder.Emit(opcode);
                }
            }
        }

        /// <summary>
        /// Generates a script focused on array and struct operations
        /// </summary>
        private void GenerateArrayHeavyScript(ScriptBuilder scriptBuilder, int maxInstructions)
        {
            // First create some arrays
            for (int i = 0; i < _random.Next(1, 3); i++)
            {
                // Push some items for the array
                int arraySize = _random.Next(1, 5);
                for (int j = 0; j < arraySize; j++)
                {
                    EmitRandomPush(scriptBuilder);
                }

                // Create the array
                EmitSmallIntPush(scriptBuilder, arraySize);
                scriptBuilder.Emit(OpCode.PACK);
            }

            // Then perform array operations
            int operations = Math.Min(maxInstructions - 15, _random.Next(10, maxInstructions));

            for (int i = 0; i < operations; i++)
            {
                int op = _random.Next(10);

                if (op < 2)
                {
                    // Create a new array occasionally
                    if (_random.Next(2) == 0)
                    {
                        scriptBuilder.Emit(OpCode.NEWARRAY0);
                    }
                    else
                    {
                        scriptBuilder.Emit(OpCode.NEWSTRUCT0);
                    }
                }
                else if (op < 5)
                {
                    // Push a value and an index for array operations
                    EmitRandomPush(scriptBuilder);
                    EmitSmallIntPush(scriptBuilder, _random.Next(3));

                    // Array operation
                    scriptBuilder.Emit(_arrayOpcodes[_random.Next(_arrayOpcodes.Length)]);
                }
                else
                {
                    // Other array operations
                    scriptBuilder.Emit(_arrayOpcodes[_random.Next(_arrayOpcodes.Length)]);
                }
            }
        }

        /// <summary>
        /// Generates a script with a random mix of operations
        /// </summary>
        private void GenerateRandomMixedScript(ScriptBuilder scriptBuilder, int maxInstructions)
        {
            // Push some initial values
            for (int i = 0; i < _random.Next(3, 8); i++)
            {
                EmitRandomPush(scriptBuilder);
            }

            // Generate random operations
            int operations = Math.Min(maxInstructions - 8, _random.Next(10, maxInstructions));

            for (int i = 0; i < operations; i++)
            {
                int category = _random.Next(7);

                switch (category)
                {
                    case 0:
                        // Arithmetic
                        scriptBuilder.Emit(_arithmeticOpcodes[_random.Next(_arithmeticOpcodes.Length)]);
                        break;
                    case 1:
                        // Stack
                        scriptBuilder.Emit(_stackOpcodes[_random.Next(_stackOpcodes.Length)]);
                        break;
                    case 2:
                        // Array
                        scriptBuilder.Emit(_arrayOpcodes[_random.Next(_arrayOpcodes.Length)]);
                        break;
                    case 3:
                        // Control flow - be careful with these
                        if (_random.Next(5) == 0) // Only 20% chance to avoid too many jumps
                        {
                            EmitSafeControlFlow(scriptBuilder);
                        }
                        else
                        {
                            EmitRandomPush(scriptBuilder);
                        }
                        break;
                    case 4:
                        // Constants
                        EmitRandomPush(scriptBuilder);
                        break;
                    case 5:
                        // Crypto
                        scriptBuilder.Emit(_cryptoOpcodes[_random.Next(_cryptoOpcodes.Length)]);
                        break;
                    case 6:
                        // Conversion
                        scriptBuilder.Emit(_conversionOpcodes[_random.Next(_conversionOpcodes.Length)]);
                        break;
                }

                // Occasionally push more values to keep the stack from depleting
                if (_random.Next(5) == 0)
                {
                    EmitRandomPush(scriptBuilder);
                }
            }
        }

        /// <summary>
        /// Emits a safe control flow instruction (avoiding infinite loops)
        /// </summary>
        private void EmitSafeControlFlow(ScriptBuilder scriptBuilder)
        {
            // For safety, we'll only use forward jumps with small offsets
            OpCode opcode = _controlFlowOpcodes[_random.Next(3)]; // Only JMP, JMPIF, JMPIFNOT

            // Push a condition for conditional jumps
            if (opcode == OpCode.JMPIF || opcode == OpCode.JMPIFNOT)
            {
                EmitRandomPush(scriptBuilder);
            }

            // Emit the control flow instruction with a small forward offset
            scriptBuilder.Emit(opcode);
            scriptBuilder.Emit((byte)_random.Next(1, 10)); // Small forward jump
        }

        /// <summary>
        /// Emits a random push operation
        /// </summary>
        private void EmitRandomPush(ScriptBuilder scriptBuilder)
        {
            int pushType = _random.Next(10);

            switch (pushType)
            {
                case 0:
                    // Push small constant (0-16)
                    scriptBuilder.Emit((OpCode)(_random.Next(17) + (byte)OpCode.PUSH0));
                    break;
                case 1:
                    // Push -1
                    scriptBuilder.Emit(OpCode.PUSHM1);
                    break;
                case 2:
                    // Push true/false
                    scriptBuilder.Emit(_random.Next(2) == 0 ? OpCode.PUSHF : OpCode.PUSHT);
                    break;
                case 3:
                    // Push null
                    scriptBuilder.Emit(OpCode.PUSHNULL);
                    break;
                case 4:
                    // Push small int
                    EmitSmallIntPush(scriptBuilder, _random.Next(100));
                    break;
                case 5:
                    // Push int8
                    scriptBuilder.Emit(OpCode.PUSHINT8);
                    scriptBuilder.Emit((byte)_random.Next(256));
                    break;
                case 6:
                    // Push int16
                    scriptBuilder.Emit(OpCode.PUSHINT16);
                    short int16Value = (short)_random.Next(short.MinValue, short.MaxValue);
                    scriptBuilder.Emit(BitConverter.GetBytes(int16Value));
                    break;
                case 7:
                    // Push int32
                    scriptBuilder.Emit(OpCode.PUSHINT32);
                    int int32Value = _random.Next();
                    scriptBuilder.Emit(BitConverter.GetBytes(int32Value));
                    break;
                case 8:
                    // Push small data
                    EmitSmallData(scriptBuilder);
                    break;
                default:
                    // Push medium data
                    EmitMediumData(scriptBuilder);
                    break;
            }
        }

        /// <summary>
        /// Emits a small integer push
        /// </summary>
        private void EmitSmallIntPush(ScriptBuilder scriptBuilder, int value)
        {
            if (value == -1)
            {
                scriptBuilder.Emit(OpCode.PUSHM1);
            }
            else if (value >= 0 && value <= 16)
            {
                scriptBuilder.Emit((OpCode)((byte)OpCode.PUSH0 + value));
            }
            else
            {
                scriptBuilder.Emit(OpCode.PUSHINT8);
                scriptBuilder.Emit((byte)value);
            }
        }

        /// <summary>
        /// Emits a small data push
        /// </summary>
        private void EmitSmallData(ScriptBuilder scriptBuilder)
        {
            int length = _random.Next(1, 10);
            byte[] data = new byte[length];
            _random.NextBytes(data);

            scriptBuilder.Emit(OpCode.PUSHDATA1);
            scriptBuilder.Emit((byte)length);
            foreach (byte b in data)
            {
                scriptBuilder.Emit(b);
            }
        }

        /// <summary>
        /// Emits a medium-sized data push
        /// </summary>
        private void EmitMediumData(ScriptBuilder scriptBuilder)
        {
            int length = _random.Next(10, 50);
            byte[] data = new byte[length];
            _random.NextBytes(data);

            scriptBuilder.Emit(OpCode.PUSHDATA1);
            scriptBuilder.Emit((byte)length);
            foreach (byte b in data)
            {
                scriptBuilder.Emit(b);
            }
        }
    }

    /// <summary>
    /// Helper class for building Neo VM scripts
    /// </summary>
    public class ScriptBuilder
    {
        private readonly List<byte> _script = new List<byte>();

        /// <summary>
        /// Emits an opcode to the script
        /// </summary>
        /// <param name="opcode">The opcode to emit</param>
        public void Emit(OpCode opcode)
        {
            _script.Add((byte)opcode);
        }

        /// <summary>
        /// Emits a byte to the script
        /// </summary>
        /// <param name="b">The byte to emit</param>
        public void Emit(byte b)
        {
            _script.Add(b);
        }

        /// <summary>
        /// Emits a byte array to the script
        /// </summary>
        /// <param name="bytes">The byte array to emit</param>
        public void Emit(byte[] bytes)
        {
            _script.AddRange(bytes);
        }

        /// <summary>
        /// Converts the script to a byte array
        /// </summary>
        /// <returns>A byte array containing the script</returns>
        public byte[] ToArray()
        {
            return _script.ToArray();
        }
    }
}
