// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins
{
    /// <summary>
    /// The reason a transaction was removed.
    /// </summary>
    public enum MemoryPoolTxRemovalReason : byte
    {
        /// <summary>
        /// The transaction was ejected since it was the lowest priority transaction and the memory pool capacity was exceeded.
        /// </summary>
        CapacityExceeded,

        /// <summary>
        /// The transaction was ejected due to failing re-validation after a block was persisted.
        /// </summary>
        NoLongerValid,
    }
}
