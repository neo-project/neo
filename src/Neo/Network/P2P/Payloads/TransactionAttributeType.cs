// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents the type of a <see cref="TransactionAttribute"/>.
    /// </summary>
    public enum TransactionAttributeType : byte
    {
        /// <summary>
        /// Indicates that the transaction is of high priority.
        /// </summary>
        [ReflectionCache(typeof(HighPriorityAttribute))]
        HighPriority = 0x01,

        /// <summary>
        /// Indicates that the transaction is an oracle response.
        /// </summary>
        [ReflectionCache(typeof(OracleResponse))]
        OracleResponse = 0x11,

        /// <summary>
        /// Indicates that the transaction is not valid before <see cref="NotValidBefore.Height"/>.
        /// </summary>
        [ReflectionCache(typeof(NotValidBefore))]
        NotValidBefore = 0x20,

        /// <summary>
        /// Indicates that the transaction is a reserved attribute.
        /// </summary>
        [ReflectionCache(typeof(ReservedAttribute))]
        ReservedAttribute = 0xFF
    }
}
