// Copyright (C) 2015-2025 The Neo Project.
//
// DisableCompressionCapability.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    /// <summary>
    /// This capability disable the compression p2p mechanism.
    /// </summary>
    public class DisableCompressionCapability : NodeCapability
    {
        public override int Size =>
            base.Size +    // Type
            1;  // Zero (empty VarBytes or String)

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableCompressionCapability"/> class.
        /// </summary>
        public DisableCompressionCapability() : base(NodeCapabilityType.DisableCompression) { }

        protected override void DeserializeWithoutType(ref MemoryReader reader)
        {
            var zero = reader.ReadByte(); // Zero-length byte array or string (see UnknownCapability).
            if (zero != 0)
                throw new FormatException("DisableCompression has some data");
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write((byte)0);
        }
    }
}
