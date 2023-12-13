// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using Neo.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to respond to <see cref="MessageCommand.GetHeaders"/> messages.
    /// </summary>
    public class HeadersPayload : ISerializable
    {
        /// <summary>
        /// Indicates the maximum number of headers sent each time.
        /// </summary>
        public const int MaxHeadersCount = 2000;

        /// <summary>
        /// The list of headers.
        /// </summary>
        public Header[] Headers;

        public int Size => Headers.GetVarSize();

        /// <summary>
        /// Creates a new instance of the <see cref="HeadersPayload"/> class.
        /// </summary>
        /// <param name="headers">The list of headers.</param>
        /// <returns>The created payload.</returns>
        public static HeadersPayload Create(params Header[] headers)
        {
            return new HeadersPayload
            {
                Headers = headers
            };
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            Headers = reader.ReadSerializableArray<Header>(MaxHeadersCount);
            if (Headers.Length == 0) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Headers);
        }
    }
}
