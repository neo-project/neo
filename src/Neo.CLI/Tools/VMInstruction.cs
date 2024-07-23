// Copyright (C) 2015-2024 The Neo Project.
//
// VMInstruction.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Neo.CLI
{
    using Neo.VM;
    using System.Runtime.CompilerServices;

    [DebuggerDisplay("OpCode={OpCode}, OperandSize={OperandSize}")]
    internal sealed class VMInstruction : IEnumerable<VMInstruction>
    {
        private const int OpCodeSize = 1;

        public int Position { get; private init; }
        public OpCode OpCode { get; private init; }
        public ReadOnlyMemory<byte> Operand { get; private init; }
        public int OperandSize { get; private init; }
        public int OperandPrefixSize { get; private init; }

        private static readonly int[] s_operandSizeTable = new int[256];
        private static readonly int[] s_operandSizePrefixTable = new int[256];

        private readonly ReadOnlyMemory<byte> _script;

        public VMInstruction(ReadOnlyMemory<byte> script, int start = 0)
        {
            if (script.IsEmpty)
                throw new Exception("Bad Script.");

            var opcode = (OpCode)script.Span[start];

            if (Enum.IsDefined(opcode) == false)
                throw new InvalidDataException($"Invalid opcode at Position: {start}.");

            OperandPrefixSize = s_operandSizePrefixTable[(int)opcode];
            OperandSize = OperandPrefixSize switch
            {
                0 => s_operandSizeTable[(int)opcode],
                1 => script.Span[start + 1],
                2 => BinaryPrimitives.ReadUInt16LittleEndian(script.Span[(start + 1)..]),
                4 => unchecked((int)BinaryPrimitives.ReadUInt32LittleEndian(script.Span[(start + 1)..])),
                _ => throw new InvalidDataException($"Invalid opcode prefix at Position: {start}."),
            };

            OperandSize += OperandPrefixSize;

            if (start + OperandSize + OpCodeSize > script.Length)
                throw new IndexOutOfRangeException("Operand size exceeds end of script.");

            Operand = script.Slice(start + OpCodeSize, OperandSize);

            _script = script;
            OpCode = opcode;
            Position = start;
        }

        static VMInstruction()
        {
            foreach (var field in typeof(OpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<OperandSizeAttribute>();
                if (attr == null) continue;

                var index = (uint)(OpCode)field.GetValue(null)!;
                s_operandSizeTable[index] = attr.Size;
                s_operandSizePrefixTable[index] = attr.SizePrefix;
            }
        }

        public IEnumerator<VMInstruction> GetEnumerator()
        {
            var nip = Position + OperandSize + OpCodeSize;
            yield return this;

            VMInstruction? instruct;
            for (var ip = nip; ip < _script.Length; ip += instruct.OperandSize + OpCodeSize)
                yield return instruct = new VMInstruction(_script, ip);
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public T AsToken<T>(uint index = 0)
            where T : unmanaged
        {
            var size = Unsafe.SizeOf<T>();

            if (size > OperandSize)
                throw new ArgumentOutOfRangeException(nameof(T), $"SizeOf {typeof(T).FullName} is too big for operand. OpCode: {OpCode}.");
            if (size + index > OperandSize)
                throw new ArgumentOutOfRangeException(nameof(index), $"SizeOf {typeof(T).FullName} + {index} is too big for operand. OpCode: {OpCode}.");

            var bytes = Operand[..OperandSize].ToArray();
            return Unsafe.As<byte, T>(ref bytes[index]);
        }
    }
}
