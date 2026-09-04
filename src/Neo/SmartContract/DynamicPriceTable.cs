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
using System.Runtime.CompilerServices;

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

        private readonly PriceFunc?[] Table = new PriceFunc?[byte.MaxValue + 1];

        /// <summary>
        /// Gets or sets the price calculator for the specified opcode.
        /// </summary>
        /// <param name="opCode">The opcode.</param>
        public PriceFunc? this[OpCode opCode]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Table[(byte)opCode];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Table[(byte)opCode] = value;
        }
    }
}
