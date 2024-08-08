// Copyright (C) 2015-2024 The Neo Project.
//
// MemoryPoolCountModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.RestServer.Models.Ledger
{
    internal class MemoryPoolCountModel
    {
        /// <summary>
        /// Total count all transactions.
        /// </summary>
        /// <example>110</example>
        public int Count { get; set; }
        /// <summary>
        /// Count of unverified transactions
        /// </summary>
        /// <example>10</example>
        public int UnVerifiedCount { get; set; }
        /// <summary>
        /// Count of verified transactions.
        /// </summary>
        /// <example>100</example>
        public int VerifiedCount { get; set; }
    }
}
