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
using Neo.Persistence;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents an object that can be verified in the NEO network.
    /// </summary>
    public interface IVerifiable : ISerializable
    {
        /// <summary>
        /// The hash of the <see cref="IVerifiable"/> object.
        /// </summary>
        UInt256 Hash => this.CalculateHash();

        /// <summary>
        /// The witnesses of the <see cref="IVerifiable"/> object.
        /// </summary>
        Witness[] Witnesses { get; set; }

        /// <summary>
        /// Deserializes the part of the <see cref="IVerifiable"/> object other than <see cref="Witnesses"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        void DeserializeUnsigned(ref MemoryReader reader);

        /// <summary>
        /// Gets the script hashes that should be verified for this <see cref="IVerifiable"/> object.
        /// </summary>
        /// <param name="snapshot">The snapshot to be used.</param>
        /// <returns>The script hashes that should be verified.</returns>
        UInt160[] GetScriptHashesForVerifying(DataCache snapshot);

        /// <summary>
        /// Serializes the part of the <see cref="IVerifiable"/> object other than <see cref="Witnesses"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        void SerializeUnsigned(BinaryWriter writer);
    }
}
