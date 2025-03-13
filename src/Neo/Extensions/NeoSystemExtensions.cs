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

/* Unmerged change from project 'Neo(net9.0)'
Before:
using System;
After:
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

using System;
*/
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

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System;

namespace Neo
{
    /// <summary>
    /// Extension methods for <see cref="NeoSystem"/> and related types.
    /// </summary>
    public static class NeoSystemExtensions
    {
        /// <summary>
        /// Gets the block generation time based on the current state of the blockchain.
        /// </summary>
        /// <param name="system">The NeoSystem instance.</param>
        /// <returns>The block generation time as a TimeSpan.</returns>
        public static TimeSpan GetBlockGenTime(this NeoSystem system)
        {
            return system.StoreView.GetBlockGenTime(system.Settings);
        }

        /// <summary>
        /// Gets the block generation time based on the current state of the blockchain.
        /// </summary>
        /// <param name="snapshot">The snapshot of the store.</param>
        /// <param name="settings">The protocol settings.</param>
        /// <returns>The block generation time as a TimeSpan.</returns>
        public static TimeSpan GetBlockGenTime(this IReadOnlyStore snapshot, ProtocolSettings settings)
        {
            try
            {
                // Get the current block height from the blockchain
                var index = NativeContract.Ledger.CurrentIndex(snapshot);

                // Before the Echidna hardfork, use the protocol settings
                if (!settings.IsHardforkEnabled(Hardfork.HF_Echidna, index))
                    return TimeSpan.FromMilliseconds(settings.MillisecondsPerBlock);

                // After the Echidna hardfork, get the current block time from the Policy contract
                var milliseconds = NativeContract.Policy.GetMSPerBlock(snapshot);
                return TimeSpan.FromMilliseconds(milliseconds);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                // At the height of 0, the key not yet exists in the storage
                return TimeSpan.FromMilliseconds(settings.MillisecondsPerBlock);
            }
        }
    }
}
