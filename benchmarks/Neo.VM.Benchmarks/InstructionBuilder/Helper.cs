// Copyright (C) 2015-2025 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Buffers.Binary;

namespace Neo.VM.Benchmark
{
    public static class Helper
    {
        public static void RebuildOffsets(this IReadOnlyList<Instruction> instructions)
        {
            var offset = 0;
            foreach (var instruction in instructions)
            {
                instruction._offset = offset;
                offset += instruction.Size;
            }
        }

        public static void RebuildOperands(this IReadOnlyList<Instruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction._target is null) continue;
                bool isLong;
                if (instruction._opCode >= VM.OpCode.JMP && instruction._opCode <= VM.OpCode.CALL_L)
                    isLong = (instruction._opCode - VM.OpCode.JMP) % 2 != 0;
                else
                    isLong = instruction._opCode == VM.OpCode.PUSHA || instruction._opCode == VM.OpCode.CALLA || instruction._opCode == VM.OpCode.TRY_L || instruction._opCode == VM.OpCode.ENDTRY_L;
                if (instruction._opCode == VM.OpCode.TRY || instruction._opCode == VM.OpCode.TRY_L)
                {
                    var offset1 = (instruction._target._instruction?._offset - instruction._offset) ?? 0;
                    var offset2 = (instruction._target2!._instruction?._offset - instruction._offset) ?? 0;
                    if (isLong)
                    {
                        instruction._operand = new byte[sizeof(int) + sizeof(int)];
                        BinaryPrimitives.WriteInt32LittleEndian(instruction._operand, offset1);
                        BinaryPrimitives.WriteInt32LittleEndian(instruction._operand.AsSpan(sizeof(int)), offset2);
                    }
                    else
                    {
                        instruction._operand = new byte[sizeof(sbyte) + sizeof(sbyte)];
                        var sbyte1 = checked((sbyte)offset1);
                        var sbyte2 = checked((sbyte)offset2);
                        instruction._operand[0] = unchecked((byte)sbyte1);
                        instruction._operand[1] = unchecked((byte)sbyte2);
                    }
                }
                else
                {
                    int offset = instruction._target._instruction!._offset - instruction._offset;
                    if (isLong)
                    {
                        instruction._operand = BitConverter.GetBytes(offset);
                    }
                    else
                    {
                        var sbyte1 = checked((sbyte)offset);
                        instruction._operand = [unchecked((byte)sbyte1)];
                    }
                }
            }
        }
    }
}
