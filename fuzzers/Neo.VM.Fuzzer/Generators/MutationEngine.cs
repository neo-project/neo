// Copyright (C) 2015-2025 The Neo Project.
//
// MutationEngine.cs file belongs to the neo project and is free
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

namespace Neo.VM.Fuzzer.Generators
{
    /// <summary>
    /// Provides various mutation strategies for evolving scripts during fuzzing
    /// </summary>
    public class MutationEngine
    {
        private readonly Random _random;
        private readonly double _mutationRate;

        // Safe opcodes that can be used for mutation
        private readonly OpCode[] _safeOpcodes = new OpCode[]
        {
            OpCode.PUSH0, OpCode.PUSH1, OpCode.PUSH2, OpCode.PUSH3,
            OpCode.PUSH4, OpCode.PUSH5, OpCode.PUSH6, OpCode.PUSH7,
            OpCode.PUSH8, OpCode.PUSH9, OpCode.PUSH10, OpCode.PUSH11,
            OpCode.PUSH12, OpCode.PUSH13, OpCode.PUSH14, OpCode.PUSH15,
            OpCode.PUSH16, OpCode.PUSHM1, OpCode.PUSHNULL, OpCode.NOP,
            OpCode.ADD, OpCode.SUB, OpCode.MUL, OpCode.DIV, OpCode.MOD,
            OpCode.POW, OpCode.AND, OpCode.OR, OpCode.XOR, OpCode.NOT,
            OpCode.INC, OpCode.DEC, OpCode.SIGN, OpCode.ABS, OpCode.BOOLAND,
            OpCode.BOOLOR, OpCode.NUMEQUAL, OpCode.NUMNOTEQUAL, OpCode.LT,
            OpCode.LE, OpCode.GT, OpCode.GE, OpCode.MIN, OpCode.MAX,
            OpCode.WITHIN, OpCode.DUP, OpCode.OVER, OpCode.PICK, OpCode.SWAP,
            OpCode.ROT, OpCode.ROLL, OpCode.DROP, OpCode.NIP, OpCode.TUCK,
            OpCode.DEPTH
        };

        /// <summary>
        /// Initializes a new instance of the MutationEngine class
        /// </summary>
        /// <param name="random">Random number generator</param>
        /// <param name="mutationRate">Rate of mutation (0.0-1.0)</param>
        public MutationEngine(Random random, double mutationRate = 0.1)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _mutationRate = Math.Clamp(mutationRate, 0.0, 1.0);
        }

        /// <summary>
        /// Mutates a script using a randomly selected mutation strategy
        /// </summary>
        /// <param name="script">The original script to mutate</param>
        /// <returns>A mutated version of the script</returns>
        public byte[] MutateScript(byte[] script)
        {
            if (script == null || script.Length == 0)
            {
                // Can't mutate an empty script
                return new byte[] { (byte)OpCode.RET };
            }

            // Make a copy of the original script
            byte[] mutatedScript = script.ToArray();

            // Apply multiple mutations based on mutation rate
            int mutationCount = Math.Max(1, (int)(script.Length * _mutationRate));

            for (int i = 0; i < mutationCount; i++)
            {
                // Choose a mutation strategy
                int strategy = _random.Next(6);

                switch (strategy)
                {
                    case 0:
                        mutatedScript = BitFlipMutation(mutatedScript);
                        break;
                    case 1:
                        mutatedScript = ByteReplaceMutation(mutatedScript);
                        break;
                    case 2:
                        mutatedScript = ByteInsertMutation(mutatedScript);
                        break;
                    case 3:
                        mutatedScript = ByteDeleteMutation(mutatedScript);
                        break;
                    case 4:
                        mutatedScript = ByteSwapMutation(mutatedScript);
                        break;
                    case 5:
                        mutatedScript = OpcodeReplaceMutation(mutatedScript);
                        break;
                }
            }

            // Ensure the script ends with RET
            if (mutatedScript.Length == 0 || mutatedScript[mutatedScript.Length - 1] != (byte)OpCode.RET)
            {
                Array.Resize(ref mutatedScript, mutatedScript.Length + 1);
                mutatedScript[mutatedScript.Length - 1] = (byte)OpCode.RET;
            }

            return mutatedScript;
        }

        /// <summary>
        /// Performs a bit flip mutation on a random bit in the script
        /// </summary>
        private byte[] BitFlipMutation(byte[] script)
        {
            if (script.Length == 0)
                return script;

            byte[] result = script.ToArray();
            int position = _random.Next(result.Length);
            int bit = _random.Next(8);

            // Flip the bit
            result[position] ^= (byte)(1 << bit);

            return result;
        }

        /// <summary>
        /// Replaces a random byte in the script with a new random byte
        /// </summary>
        private byte[] ByteReplaceMutation(byte[] script)
        {
            if (script.Length == 0)
                return script;

            byte[] result = script.ToArray();
            int position = _random.Next(result.Length);

            // Replace with a random byte
            result[position] = (byte)_random.Next(256);

            return result;
        }

        /// <summary>
        /// Inserts a random byte at a random position in the script
        /// </summary>
        private byte[] ByteInsertMutation(byte[] script)
        {
            byte[] result = new byte[script.Length + 1];
            int position = _random.Next(script.Length + 1);

            // Copy bytes before the insertion point
            if (position > 0)
            {
                Buffer.BlockCopy(script, 0, result, 0, position);
            }

            // Insert the new byte
            result[position] = (byte)_random.Next(256);

            // Copy bytes after the insertion point
            if (position < script.Length)
            {
                Buffer.BlockCopy(script, position, result, position + 1, script.Length - position);
            }

            return result;
        }

        /// <summary>
        /// Deletes a random byte from the script
        /// </summary>
        private byte[] ByteDeleteMutation(byte[] script)
        {
            if (script.Length <= 1)
                return script;

            byte[] result = new byte[script.Length - 1];
            int position = _random.Next(script.Length);

            // Copy bytes before the deletion point
            if (position > 0)
            {
                Buffer.BlockCopy(script, 0, result, 0, position);
            }

            // Copy bytes after the deletion point
            if (position < script.Length - 1)
            {
                Buffer.BlockCopy(script, position + 1, result, position, script.Length - position - 1);
            }

            return result;
        }

        /// <summary>
        /// Swaps two random bytes in the script
        /// </summary>
        private byte[] ByteSwapMutation(byte[] script)
        {
            if (script.Length <= 1)
                return script;

            byte[] result = script.ToArray();
            int position1 = _random.Next(result.Length);
            int position2 = _random.Next(result.Length);

            // Ensure the positions are different
            while (position1 == position2 && result.Length > 1)
            {
                position2 = _random.Next(result.Length);
            }

            // Swap the bytes
            byte temp = result[position1];
            result[position1] = result[position2];
            result[position2] = temp;

            return result;
        }

        /// <summary>
        /// Replaces a random opcode in the script with another safe opcode
        /// </summary>
        private byte[] OpcodeReplaceMutation(byte[] script)
        {
            if (script.Length == 0)
                return script;

            byte[] result = script.ToArray();
            int position = _random.Next(result.Length);

            // Replace with a random safe opcode
            result[position] = (byte)_safeOpcodes[_random.Next(_safeOpcodes.Length)];

            return result;
        }

        /// <summary>
        /// Performs crossover between two scripts to create a new script
        /// </summary>
        /// <param name="script1">The first parent script</param>
        /// <param name="script2">The second parent script</param>
        /// <returns>A new script created by combining parts of both parent scripts</returns>
        public byte[] CrossoverScripts(byte[] script1, byte[] script2)
        {
            if (script1 == null || script1.Length == 0)
                return script2?.ToArray() ?? new byte[] { (byte)OpCode.RET };

            if (script2 == null || script2.Length == 0)
                return script1.ToArray();

            // Choose a crossover strategy
            int strategy = _random.Next(3);

            switch (strategy)
            {
                case 0:
                    return SinglePointCrossover(script1, script2);
                case 1:
                    return TwoPointCrossover(script1, script2);
                default:
                    return UniformCrossover(script1, script2);
            }
        }

        /// <summary>
        /// Performs single-point crossover between two scripts
        /// </summary>
        private byte[] SinglePointCrossover(byte[] script1, byte[] script2)
        {
            int point1 = _random.Next(script1.Length);
            int point2 = _random.Next(script2.Length);

            byte[] result = new byte[point1 + (script2.Length - point2)];

            // Copy first part from script1
            Buffer.BlockCopy(script1, 0, result, 0, point1);

            // Copy second part from script2
            Buffer.BlockCopy(script2, point2, result, point1, script2.Length - point2);

            return result;
        }

        /// <summary>
        /// Performs two-point crossover between two scripts
        /// </summary>
        private byte[] TwoPointCrossover(byte[] script1, byte[] script2)
        {
            // Ensure points are in order
            int start1 = _random.Next(script1.Length);
            int end1 = _random.Next(start1, script1.Length);

            int start2 = _random.Next(script2.Length);
            int end2 = _random.Next(start2, script2.Length);

            int middleLength = end2 - start2;
            byte[] result = new byte[start1 + middleLength + (script1.Length - end1)];

            // Copy first part from script1
            Buffer.BlockCopy(script1, 0, result, 0, start1);

            // Copy middle part from script2
            Buffer.BlockCopy(script2, start2, result, start1, middleLength);

            // Copy last part from script1
            Buffer.BlockCopy(script1, end1, result, start1 + middleLength, script1.Length - end1);

            return result;
        }

        /// <summary>
        /// Performs uniform crossover between two scripts
        /// </summary>
        private byte[] UniformCrossover(byte[] script1, byte[] script2)
        {
            int length = Math.Max(script1.Length, script2.Length);
            byte[] result = new byte[length];

            for (int i = 0; i < length; i++)
            {
                if (_random.Next(2) == 0)
                {
                    // Take from script1 if possible
                    result[i] = i < script1.Length ? script1[i] : (byte)OpCode.NOP;
                }
                else
                {
                    // Take from script2 if possible
                    result[i] = i < script2.Length ? script2[i] : (byte)OpCode.NOP;
                }
            }

            return result;
        }
    }
}
