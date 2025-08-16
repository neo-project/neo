// Copyright (C) 2015-2025 The Neo Project.
//
// JumpTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public partial class JumpTable
    {
        /// <summary>
        /// Default JumpTable
        /// </summary>
        public static readonly JumpTable Default = new();

        public delegate void DelAction(ExecutionEngine engine, Instruction instruction);
        protected readonly DelAction[] Table = new DelAction[byte.MaxValue];

        public DelAction this[OpCode opCode]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Table[(byte)opCode];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Table[(byte)opCode] = value; }
        }
        public static JumpTable CreateFor<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        TTable>() where TTable : JumpTable, new()
        {
            var table = new TTable();
            table.Initialize(typeof(TTable));
            return table;
        }

        /// <summary>
        /// Jump table constructor
        /// </summary>
        /// <exception cref="InvalidOperationException">Throw an exception if the opcode was already set</exception>
        public JumpTable()
        {
            Initialize(typeof(JumpTable));
            //// Fill defined

            //foreach (var mi in GetType().GetMethods())
            //{
            //    if (Enum.TryParse<OpCode>(mi.Name, true, out var opCode))
            //    {
            //        if (Table[(byte)opCode] is not null)
            //        {
            //            throw new InvalidOperationException($"Opcode {opCode} is already defined.");
            //        }

            //        Table[(byte)opCode] = (DelAction)mi.CreateDelegate(typeof(DelAction), this);
            //    }
            //}

            //// Fill with undefined

            //for (var x = 0; x < Table.Length; x++)
            //{
            //    if (Table[x] is not null) continue;

            //    Table[x] = InvalidOpcode;
            //}
        }
        private void Initialize(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            Type targetType)
        {
            // Fill defined
            foreach (var mi in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (Enum.TryParse<OpCode>(mi.Name, true, out var opCode))
                {
                    if (Table[(byte)opCode] is not null)
                        throw new InvalidOperationException($"Opcode {opCode} is already defined.");

                    Table[(byte)opCode] = (DelAction)mi.CreateDelegate(typeof(DelAction), this);
                }
            }

            // Fill with undefined
            for (var x = 0; x < Table.Length; x++)
            {
                if (Table[x] is null)
                    Table[x] = InvalidOpcode;
            }
        }
        public virtual void InvalidOpcode(ExecutionEngine engine, Instruction instruction)
        {
            throw new InvalidOperationException($"Opcode {instruction.OpCode} is undefined.");
        }
    }
}
