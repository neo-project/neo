// Copyright (C) 2016-2023 The Neo Project.
//
// The neo-vm is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM
{
    /// <summary>
    /// Represents instructions in the VM script.
    /// </summary>
    [DebuggerDisplay("OpCode={OpCode}")]
    public class Instruction
    {
        /// <summary>
        /// Represents the instruction with <see cref="OpCode.RET"/>.
        /// </summary>
        public static Instruction RET { get; } = new Instruction(OpCode.RET);

        /// <summary>
        /// The <see cref="VM.OpCode"/> of the instruction.
        /// </summary>
        public readonly OpCode OpCode;

        /// <summary>
        /// The operand of the instruction.
        /// </summary>
        public readonly ReadOnlyMemory<byte> Operand;

        private static readonly int[] OperandSizePrefixTable = new int[256];
        private static readonly int[] OperandSizeTable = new int[256];

        /// <summary>
        /// Gets the size of the instruction.
        /// </summary>
        public int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int prefixSize = OperandSizePrefixTable[(int)OpCode];
                return prefixSize > 0
                    ? 1 + prefixSize + Operand.Length
                    : 1 + OperandSizeTable[(int)OpCode];
            }
        }

        /// <summary>
        /// Gets the first operand as <see cref="short"/>.
        /// </summary>
        public short TokenI16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadInt16LittleEndian(Operand.Span);
            }
        }

        /// <summary>
        /// Gets the first operand as <see cref="int"/>.
        /// </summary>
        public int TokenI32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadInt32LittleEndian(Operand.Span);
            }
        }

        /// <summary>
        /// Gets the second operand as <see cref="int"/>.
        /// </summary>
        public int TokenI32_1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadInt32LittleEndian(Operand.Span[4..]);
            }
        }

        /// <summary>
        /// Gets the first operand as <see cref="sbyte"/>.
        /// </summary>
        public sbyte TokenI8
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (sbyte)Operand.Span[0];
            }
        }

        /// <summary>
        /// Gets the second operand as <see cref="sbyte"/>.
        /// </summary>
        public sbyte TokenI8_1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (sbyte)Operand.Span[1];
            }
        }

        /// <summary>
        /// Gets the operand as <see cref="string"/>.
        /// </summary>
        public string TokenString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Encoding.ASCII.GetString(Operand.Span);
            }
        }

        /// <summary>
        /// Gets the first operand as <see cref="ushort"/>.
        /// </summary>
        public ushort TokenU16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(Operand.Span);
            }
        }

        /// <summary>
        /// Gets the first operand as <see cref="uint"/>.
        /// </summary>
        public uint TokenU32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadUInt32LittleEndian(Operand.Span);
            }
        }

        /// <summary>
        /// Gets the first operand as <see cref="byte"/>.
        /// </summary>
        public byte TokenU8
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Operand.Span[0];
            }
        }

        /// <summary>
        /// Gets the second operand as <see cref="byte"/>.
        /// </summary>
        public byte TokenU8_1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Operand.Span[1];
            }
        }

        static Instruction()
        {
            foreach (FieldInfo field in typeof(OpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                OperandSizeAttribute? attribute = field.GetCustomAttribute<OperandSizeAttribute>();
                if (attribute == null) continue;
                int index = (int)(OpCode)field.GetValue(null)!;
                OperandSizePrefixTable[index] = attribute.SizePrefix;
                OperandSizeTable[index] = attribute.Size;
            }
        }

        private Instruction(OpCode opcode)
        {
            this.OpCode = opcode;
            if (!Enum.IsDefined(typeof(OpCode), opcode)) throw new BadScriptException();
        }

        internal Instruction(ReadOnlyMemory<byte> script, int ip) : this((OpCode)script.Span[ip++])
        {
            ReadOnlySpan<byte> span = script.Span;
            int operandSizePrefix = OperandSizePrefixTable[(int)OpCode];
            int operandSize = 0;
            switch (operandSizePrefix)
            {
                case 0:
                    operandSize = OperandSizeTable[(int)OpCode];
                    break;
                case 1:
                    if (ip >= span.Length)
                        throw new BadScriptException($"Instruction out of bounds. InstructionPointer: {ip}");
                    operandSize = span[ip];
                    break;
                case 2:
                    if (ip + 1 >= span.Length)
                        throw new BadScriptException($"Instruction out of bounds. InstructionPointer: {ip}");
                    operandSize = BinaryPrimitives.ReadUInt16LittleEndian(span[ip..]);
                    break;
                case 4:
                    if (ip + 3 >= span.Length)
                        throw new BadScriptException($"Instruction out of bounds. InstructionPointer: {ip}");
                    operandSize = BinaryPrimitives.ReadInt32LittleEndian(span[ip..]);
                    if (operandSize < 0)
                        throw new BadScriptException($"Instruction out of bounds. InstructionPointer: {ip}, operandSize: {operandSize}");
                    break;
            }
            ip += operandSizePrefix;
            if (operandSize > 0)
            {
                if (ip + operandSize > script.Length)
                    throw new BadScriptException($"Instrucion out of bounds. InstructionPointer: {ip}, operandSize: {operandSize}, length: {script.Length}");
                Operand = script.Slice(ip, operandSize);
            }
        }
    }
}
