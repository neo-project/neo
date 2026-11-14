// Copyright (C) 2015-2026 The Neo Project.
//
// DynamicPriceTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Neo.SmartContract
{
    /// <summary>
    /// A table of per-opcode dynamic price calculators, indexed by <see cref="OpCode"/>.
    /// </summary>
    public class DynamicPriceTable
    {
        /// <summary>
        /// Computes the dynamic price of an opcode from the runtime stats collected while executing it.
        /// </summary>
        /// <param name="stats">The opcode parameters for dynamic pricing.</param>
        /// <returns>The price coefficient for the opcode.</returns>
        public delegate long PriceFunc(RunStats stats);

        private PriceFunc?[] _table;
        private readonly object _sync = new();
        private bool _shared;
        /// <summary>
        /// Gets or sets the price calculator for the specified opcode.
        /// </summary>
        /// <param name="opCode">The opcode.</param>
        public PriceFunc? this[OpCode opCode]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _table)[(byte)opCode];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                lock (_sync)
                {
                    if (_shared)
                    {
                        _table = (PriceFunc?[])_table.Clone();
                        _shared = false;
                    }
                    _table[(byte)opCode] = value;
                }
            }
        }

        /// <summary>
        /// Creates a copy of this table, so mutating the copy does not affect the original.
        /// </summary>
        /// <remarks>
        /// The clone shares the backing array until it is modified. A write replaces the
        /// writer's array after copying it, so cloning and reading can safely observe either
        /// the previous or the updated table. Mutations on one table are serialized.
        /// </remarks>
        public DynamicPriceTable Clone()
        {
            lock (_sync)
            {
                _shared = true;
                return new DynamicPriceTable(_table);
            }
        }

        /// <summary>
        /// Creates an empty price table.
        /// </summary>
        public DynamicPriceTable()
        {
            _table = new PriceFunc?[byte.MaxValue + 1];
        }

        private DynamicPriceTable(PriceFunc?[] table)
        {
            _table = table;
            _shared = true;
        }
    }
}
