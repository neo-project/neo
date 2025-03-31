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
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                // At the height of 0, the key not yet exists in the storage.
                return settings.MaxValidUntilBlockIncrement;
            }
        }
    }
}
