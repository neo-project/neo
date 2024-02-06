// Copyright (C) 2015-2024 The Neo Project.
//
// JumpTable.Types.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public partial class JumpTable
    {
        public virtual void ISNULL(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            engine.Push(x.IsNull);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ISTYPE(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            var type = (StackItemType)instruction.TokenU8;
            if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidOperationException($"Invalid type: {type}");
            engine.Push(x.Type == type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CONVERT(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            engine.Push(x.ConvertTo((StackItemType)instruction.TokenU8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ABORTMSG(ExecutionEngine engine, Instruction instruction)
        {
            var msg = engine.Pop().GetString();
            throw new Exception($"{OpCode.ABORTMSG} is executed. Reason: {msg}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ASSERTMSG(ExecutionEngine engine, Instruction instruction)
        {
            var msg = engine.Pop().GetString();
            var x = engine.Pop().GetBoolean();
            if (!x)
                throw new Exception($"{OpCode.ASSERTMSG} is executed with false result. Reason: {msg}");
        }
    }
}
