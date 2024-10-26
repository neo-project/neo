// Copyright (C) 2015-2024 The Neo Project.
//
// ServerInfoPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Buffers;
using System.IO;
using System.Linq;
using System.Net;

namespace Neo.IO.Pipes.Protocols.Payloads
{
    internal class ServerInfoPayload : INamedPipeMessage
    {
        internal class RemoteConnectedClient : INamedPipeMessage
        {
            public IPAddress Address { get; set; } = IPAddress.Any;
            public ushort Port { get; set; }
            public uint LastBlockIndex { get; set; }

            public int Size =>
                MemoryBuffer.GetStringSize($"{Address}") + // Address
                sizeof(ushort) +                           // Port
                sizeof(uint);                              // LastBlockIndex

            public void FromStream(Stream stream)
            {
                using var reader = new MemoryBuffer(stream);
                FromMemoryBuffer(reader);
            }

            public void FromMemoryBuffer(MemoryBuffer reader)
            {
                Address = IPAddress.Parse(reader.ReadString());
                Port = reader.Read<ushort>();
                LastBlockIndex = reader.Read<uint>();
            }

            public byte[] ToByteArray()
            {
                using var ms = new MemoryStream();
                using var writer = new MemoryBuffer(ms);
                writer.WriteString($"{Address}");
                writer.Write(Port);
                writer.Write(LastBlockIndex);
                return ms.ToArray();
            }
        }

        public uint Nonce { get; set; }
        public uint Version { get; set; }
        public IPAddress Address { get; set; } = IPAddress.Loopback;
        public ushort Port { get; set; }
        public uint BlockHeight { get; set; }
        public uint HeaderHeight { get; set; }

        public RemoteConnectedClient[] RemoteNodes { get; set; } = [];

        public int Size =>
            sizeof(uint) +                               // Nonce
            sizeof(uint) +                               // Version
            MemoryBuffer.GetStringSize($"{Address}") +   // Address
            sizeof(ushort) +                             // Port
            sizeof(uint) +                               // BlockHeight
            sizeof(uint) +                               // HeaderHeight
            sizeof(uint) +                               // RemoteNodes.Length
            RemoteNodes.Sum(s => sizeof(int) + s.Size);  // RemoteNodes

        public void FromStream(Stream stream)
        {
            using var reader = new MemoryBuffer(stream);
            FromMemoryBuffer(reader);
        }

        public void FromMemoryBuffer(MemoryBuffer reader)
        {
            Nonce = reader.Read<uint>();
            Version = reader.Read<uint>();
            Address = IPAddress.Parse(reader.ReadString());
            Port = reader.Read<ushort>();
            BlockHeight = reader.Read<uint>();
            HeaderHeight = reader.Read<uint>();

            var count = reader.Read<int>();
            RemoteNodes = new RemoteConnectedClient[count];
            for (var i = 0; i < RemoteNodes.Length; i++)
            {
                var bytes = reader.ReadArray<byte>();
                using var ms = new MemoryStream(bytes);
                using var mb = new MemoryBuffer(ms);
                RemoteNodes[i] = new RemoteConnectedClient();
                RemoteNodes[i].FromMemoryBuffer(mb);
            }
        }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var writer = new MemoryBuffer(ms);
            writer.Write(Nonce);
            writer.Write(Version);
            writer.WriteString($"{Address}");
            writer.Write(Port);
            writer.Write(BlockHeight);
            writer.Write(HeaderHeight);
            writer.Write(RemoteNodes.Length);
            foreach (var node in RemoteNodes)
                writer.WriteArray(node.ToByteArray());
            return ms.ToArray();
        }
    }
}
