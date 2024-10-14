// Copyright (C) 2015-2024 The Neo Project.
//
// InstructionBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Buffers.Binary;
using System.Numerics;

namespace Neo.VM.Benchmark;

internal class InstructionBuilder
{
    internal readonly List<Instruction> _instructions = new();

    public InstructionBuilder() { }

    internal Instruction AddInstruction(Instruction instruction)
    {
        _instructions.Add(instruction);
        return instruction;
    }

    internal Instruction AddInstruction(VM.OpCode opcode)
    {
        return AddInstruction(new Instruction
        {
            _opCode = opcode
        });
    }

    internal Instruction Jump(VM.OpCode opcode, JumpTarget target)
    {
        return AddInstruction(new Instruction
        {
            _opCode = opcode,
            _target = target
        });
    }

    internal void Push(bool value)
    {
        AddInstruction(value ? VM.OpCode.PUSHT : VM.OpCode.PUSHF);
    }

    internal Instruction Ret() => AddInstruction(VM.OpCode.RET);

    internal Instruction Push(BigInteger number)
    {
        if (number >= -1 && number <= 16) return AddInstruction(number == -1 ? VM.OpCode.PUSHM1 : VM.OpCode.PUSH0 + (byte)(int)number);
        Span<byte> buffer = stackalloc byte[32];
        if (!number.TryWriteBytes(buffer, out var bytesWritten, isUnsigned: false, isBigEndian: false))
            throw new ArgumentOutOfRangeException(nameof(number));
        var instruction = bytesWritten switch
        {
            1 => new Instruction
            {
                _opCode = VM.OpCode.PUSHINT8,
                _operand = PadRight(buffer, bytesWritten, 1, number.Sign < 0).ToArray()
            },
            2 => new Instruction
            {
                _opCode = VM.OpCode.PUSHINT16,
                _operand = PadRight(buffer, bytesWritten, 2, number.Sign < 0).ToArray()
            },
            <= 4 => new Instruction
            {
                _opCode = VM.OpCode.PUSHINT32,
                _operand = PadRight(buffer, bytesWritten, 4, number.Sign < 0).ToArray()
            },
            <= 8 => new Instruction
            {
                _opCode = VM.OpCode.PUSHINT64,
                _operand = PadRight(buffer, bytesWritten, 8, number.Sign < 0).ToArray()
            },
            <= 16 => new Instruction
            {
                _opCode = VM.OpCode.PUSHINT128,
                _operand = PadRight(buffer, bytesWritten, 16, number.Sign < 0).ToArray()
            },
            <= 32 => new Instruction
            {
                _opCode = VM.OpCode.PUSHINT256,
                _operand = PadRight(buffer, bytesWritten, 32, number.Sign < 0).ToArray()
            },
            _ => throw new ArgumentOutOfRangeException($"Number too large: {bytesWritten}")
        };
        AddInstruction(instruction);
        return instruction;
    }

    internal Instruction Push(string s)
    {
        return Push(Utility.StrictUTF8.GetBytes(s));
    }

    internal Instruction Push(byte[] data)
    {
        VM.OpCode opcode;
        byte[] buffer;
        switch (data.Length)
        {
            case <= byte.MaxValue:
                opcode = VM.OpCode.PUSHDATA1;
                buffer = new byte[sizeof(byte) + data.Length];
                buffer[0] = (byte)data.Length;
                Buffer.BlockCopy(data, 0, buffer, sizeof(byte), data.Length);
                break;
            case <= ushort.MaxValue:
                opcode = VM.OpCode.PUSHDATA2;
                buffer = new byte[sizeof(ushort) + data.Length];
                BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)data.Length);
                Buffer.BlockCopy(data, 0, buffer, sizeof(ushort), data.Length);
                break;
            default:
                opcode = VM.OpCode.PUSHDATA4;
                buffer = new byte[sizeof(uint) + data.Length];
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)data.Length);
                Buffer.BlockCopy(data, 0, buffer, sizeof(uint), data.Length);
                break;
        }
        return AddInstruction(new Instruction
        {
            _opCode = opcode,
            _operand = buffer
        });
    }

    internal void Push(object? obj)
    {
        switch (obj)
        {
            case bool data:
                Push(data);
                break;
            case byte[] data:
                Push(data);
                break;
            case string data:
                Push(data);
                break;
            case BigInteger data:
                Push(data);
                break;
            case char data:
                Push((ushort)data);
                break;
            case sbyte data:
                Push(data);
                break;
            case byte data:
                Push(data);
                break;
            case short data:
                Push(data);
                break;
            case ushort data:
                Push(data);
                break;
            case int data:
                Push(data);
                break;
            case uint data:
                Push(data);
                break;
            case long data:
                Push(data);
                break;
            case ulong data:
                Push(data);
                break;
            case Enum data:
                Push(BigInteger.Parse(data.ToString("d")));
                break;
            case null:
                AddInstruction(VM.OpCode.PUSHNULL);
                break;
            default:
                throw new NotSupportedException($"Unsupported constant value: {obj}");
        }
    }

    // Helper method to reverse stack items
    internal void ReverseStackItems(int count)
    {
        switch (count)
        {
            case 2:
                AddInstruction(VM.OpCode.SWAP);
                break;
            case 3:
                AddInstruction(VM.OpCode.REVERSE3);
                break;
            case 4:
                AddInstruction(VM.OpCode.REVERSE4);
                break;
            default:
                Push(count);
                AddInstruction(VM.OpCode.REVERSEN);
                break;
        }
    }

    internal static ReadOnlySpan<byte> PadRight(Span<byte> buffer, int dataLength, int padLength, bool negative)
    {
        byte pad = negative ? (byte)0xff : (byte)0;
        for (int x = dataLength; x < padLength; x++)
            buffer[x] = pad;
        return buffer[..padLength];
    }

    internal Instruction IsType(VM.Types.StackItemType type)
    {
        return AddInstruction(new Instruction
        {
            _opCode = VM.OpCode.ISTYPE,
            _operand = [(byte)type]
        });
    }

    internal Instruction ChangeType(VM.Types.StackItemType type)
    {
        return AddInstruction(new Instruction
        {
            _opCode = VM.OpCode.CONVERT,
            _operand = [(byte)type]
        });
    }

    internal byte[] ToArray()
    {
        var instructions = _instructions.ToArray();
        instructions.RebuildOffsets();
        instructions.RebuildOperands();
        return instructions.Select(p => p.ToArray()).SelectMany(p => p).ToArray();
    }
}
