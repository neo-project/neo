// Copyright (C) 2015-2024 The Neo Project.
//
// ProtocolSettingsModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using System.Collections.Generic;

namespace Neo.Plugins.RestServer.Models.Node
{
    internal class ProtocolSettingsModel
    {
        /// <summary>
        /// Network
        /// </summary>
        /// <example>860833102</example>
        public uint Network { get; set; }

        /// <summary>
        /// AddressVersion
        /// </summary>
        /// <example>53</example>
        public byte AddressVersion { get; set; }
        public int ValidatorsCount { get; set; }
        public uint MillisecondsPerBlock { get; set; }
        public uint MaxValidUntilBlockIncrement { get; set; }
        public uint MaxTransactionsPerBlock { get; set; }
        public int MemoryPoolMaxTransactions { get; set; }
        public uint MaxTraceableBlocks { get; set; }
        public ulong InitialGasDistribution { get; set; }
        public IReadOnlyCollection<string> SeedList { get; set; } = new List<string>().AsReadOnly();
        public IReadOnlyDictionary<string, uint> Hardforks { get; set; } = new Dictionary<string, uint>().AsReadOnly();
        public IReadOnlyList<ECPoint> StandbyValidators { get; set; } = new List<ECPoint>().AsReadOnly();
        public IReadOnlyList<ECPoint> StandbyCommittee { get; set; } = new List<ECPoint>().AsReadOnly();
    }
}
