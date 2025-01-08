// Copyright (C) 2015-2024 The Neo Project.
//
// UnknownCapability.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    /// <summary>
    /// This capability disable the compression p2p mechanism.
    /// </summary>
    public class DisableCompressionCapability : NodeCapability
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownCapability"/> class.
        /// </summary>
        public DisableCompressionCapability() : base(NodeCapabilityType.DisableCompression) { }

        protected override void DeserializeWithoutType(ref MemoryReader reader) { }

        protected override void SerializeWithoutType(BinaryWriter writer) { }
    }
}
