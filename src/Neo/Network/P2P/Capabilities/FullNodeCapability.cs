// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    /// <summary>
    /// Indicates that a node has complete block data.
    /// </summary>
    public class FullNodeCapability : NodeCapability
    {
        /// <summary>
        /// Indicates the current block height of the node.
        /// </summary>
        public uint StartHeight;

        public override int Size =>
            base.Size +    // Type
            sizeof(uint);  // Start Height

        /// <summary>
        /// Initializes a new instance of the <see cref="FullNodeCapability"/> class.
        /// </summary>
        /// <param name="startHeight">The current block height of the node.</param>
        public FullNodeCapability(uint startHeight = 0) : base(NodeCapabilityType.FullNode)
        {
            StartHeight = startHeight;
        }

        protected override void DeserializeWithoutType(ref MemoryReader reader)
        {
            StartHeight = reader.ReadUInt32();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(StartHeight);
        }
    }
}
