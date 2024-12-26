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

using Neo.Extensions;
using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    /// <summary>
    /// This capability implements a generic extensible format for new
    /// capabilities. This capability has no real type and can not be
    /// serialized. But it allows to ignore any new/unknown types for old nodes
    /// in a safe way.
    /// </summary>
    public class UnknownCapability : NodeCapability
    {
        /// <summary>
        /// Indicates the maximum size of the <see cref="Data"/> field.
        /// </summary>
        public const int MaxDataSize = 1024;

        public ReadOnlyMemory<byte> Data;

        public override int Size =>
            base.Size +    // Type
            Data.GetVarSize();  // Any kind of data enclosed in a single string.

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownCapability"/> class.
        /// </summary>
        /// <param name="type">The type of the <see cref="NodeCapability"/>.</param>
        public UnknownCapability(NodeCapabilityType type) : base(type) { }

        protected override void DeserializeWithoutType(ref MemoryReader reader)
        {
            Data = reader.ReadVarMemory(MaxDataSize);
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            throw new InvalidOperationException("Unknown capability can't be serialized");
        }
    }
}
