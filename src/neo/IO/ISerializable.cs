// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.IO;

namespace Neo.IO
{
    /// <summary>
    /// Represents NEO objects that can be serialized.
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// The size of the object in bytes after serialization.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Serializes the object using the specified <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        void Serialize(BinaryWriter writer);

        /// <summary>
        /// Deserializes the object using the specified <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        void Deserialize(BinaryReader reader);
    }
}
