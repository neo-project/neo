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
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to respond to <see cref="MessageCommand.GetAddr"/> messages.
    /// </summary>
    public class AddrPayload : ISerializable
    {
        /// <summary>
        /// Indicates the maximum number of nodes sent each time.
        /// </summary>
        public const int MaxCountToSend = 200;

        /// <summary>
        /// The list of nodes.
        /// </summary>
        public NetworkAddressWithTime[] AddressList;

        public int Size => AddressList.GetVarSize();

        /// <summary>
        /// Creates a new instance of the <see cref="AddrPayload"/> class.
        /// </summary>
        /// <param name="addresses">The list of nodes.</param>
        /// <returns>The created payload.</returns>
        public static AddrPayload Create(params NetworkAddressWithTime[] addresses)
        {
            return new AddrPayload
            {
                AddressList = addresses
            };
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            AddressList = reader.ReadSerializableArray<NetworkAddressWithTime>(MaxCountToSend);
            if (AddressList.Length == 0)
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(AddressList);
        }
    }
}
