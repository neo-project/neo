// Copyright (C) 2015-2023 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract
{
    /// <summary>
    /// The storage context used to read and write data in smart contracts.
    /// </summary>
    public class StorageContext
    {
        /// <summary>
        /// The id of the contract that owns the context.
        /// </summary>
        public int Id;

        /// <summary>
        /// Indicates whether the context is read-only.
        /// </summary>
        public bool IsReadOnly;
    }
}
