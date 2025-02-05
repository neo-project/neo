// Copyright (C) 2015-2025 The Neo Project.
//
// Instruction.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;

namespace Neo.VM.Benchmark
{
    [DebuggerDisplay("{_opCode}")]
    public class Instruction
    {
        private static readonly int[] s_operandSizePrefixTable = new int[256];
        private static readonly int[] s_operandSizeTable = new int[256];

        public VM.OpCode _opCode;
        public byte[]? _operand;
        public JumpTarget? _target;
        public JumpTarget? _target2;
        public int _offset;

        public int Size
        {
            get
            {
                int prefixSize = s_operandSizePrefixTable[(int)_opCode];
                return prefixSize > 0
                    ? sizeof(VM.OpCode) + _operand!.Length
                    : sizeof(VM.OpCode) + s_operandSizeTable[(int)_opCode];
            }
        }

        static Instruction()
        {
            foreach (var field in typeof(VM.OpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<OperandSizeAttribute>();
                if (attribute is null) continue;
                var index = (int)(VM.OpCode)field.GetValue(null)!;
                s_operandSizePrefixTable[index] = attribute.SizePrefix;
                s_operandSizeTable[index] = attribute.Size;
            }
        }

        public byte[] ToArray()
        {
            if (_operand is null) return [(byte)_opCode];
            return _operand.Prepend((byte)_opCode).ToArray();
        }
    }
}
