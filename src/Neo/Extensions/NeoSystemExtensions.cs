// Copyright (C) 2015-2025 The Neo Project.
//
// NeoSystemExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract.Native;
using System.Collections.Generic;

namespace Neo
{
    /// <summary>
    /// Extension methods for <see cref="NeoSystem"/> and related types.
    /// </summary>
    public static class NeoSystemExtensions
    {
        /// <summary>
        /// Gets the upper increment size of blockchain height (in blocks) exceeding
        /// that a transaction should fail validation.
        /// </summary>
        /// <param name="system">The NeoSystem instance.</param>
        /// <returns>MaxValidUntilBlockIncrement value.</returns>
        public static uint GetMaxValidUntilBlockIncrement(this NeoSystem system)
        {
            return system.StoreView.GetMaxValidUntilBlockIncrement(system.Settings);
        }

        /// <summary>
        /// Gets the upper increment size of blockchain height (in blocks) exceeding
        /// that a transaction should fail validation.
        /// </summary>
        /// <param name="snapshot">The snapshot of the store.</param>
        /// <param name="settings">The protocol settings.</param>
        /// <returns>MaxValidUntilBlockIncrement value.</returns>
        public static uint GetMaxValidUntilBlockIncrement(this IReadOnlyStore snapshot, ProtocolSettings settings)
        {
            try
            {
                // Get the current block height from the blockchain.
                var index = NativeContract.Ledger.CurrentIndex(snapshot);

                // Before the Echidna hardfork, use the protocol settings.
                if (!settings.IsHardforkEnabled(Hardfork.HF_Echidna, index))
                    return settings.MaxValidUntilBlockIncrement;

                // After the Echidna hardfork, get the current block time from the Policy contract.
                return NativeContract.Policy.GetMaxValidUntilBlockIncrement(snapshot);
            }
            catch (KeyNotFoundException)
            {
                // At the height of 0, the key not yet exists in the storage.
                return settings.MaxValidUntilBlockIncrement;
            }
        }

        /// <summary>
        /// Gets the length of the chain accessible to smart contracts based
        /// on the current state of the blockchain.
        /// </summary>
        /// <param name="system">The NeoSystem instance.</param>
        /// <returns>MaxTraceableBlocks value (in blocks).</returns>
        public static uint GetMaxTraceableBlocks(this NeoSystem system)
        {
            return system.StoreView.GetMaxTraceableBlocks(system.Settings);
        }

        /// <summary>
        /// Gets the length of the chain accessible to smart contracts based
        /// on the current state of the blockchain.
        /// </summary>
        /// <param name="snapshot">The snapshot of the store.</param>
        /// <param name="settings">The protocol settings.</param>
        /// <returns>MaxTraceableBlocks value (in blocks).</returns>
        public static uint GetMaxTraceableBlocks(this IReadOnlyStore snapshot, ProtocolSettings settings)
        {
            try
            {
                // Get the persisted block height from the blockchain.
                var index = NativeContract.Ledger.CurrentIndex(snapshot);

                // Use protocol settings configuration if HF_Echidna is not yet enabled.
                if (!settings.IsHardforkEnabled(Hardfork.HF_Echidna, index))
                    return settings.MaxTraceableBlocks;

                // Retrieve MillisecondsPerBlock value from native Policy if HF_Echidna is enabled.
                return NativeContract.Policy.GetMaxTraceableBlocks(snapshot);
            }
            catch (KeyNotFoundException)
            {
                // A special case if HF_Echidna is active starting from 0 height and
                // if genesis block is not yet persisted.
                return settings.MaxTraceableBlocks;
            }
        }
    }
}
