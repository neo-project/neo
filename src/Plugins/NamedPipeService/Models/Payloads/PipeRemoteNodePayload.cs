// Copyright (C) 2015-2024 The Neo Project.
//
// PipeRemoteNodePayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.Buffers;
using System.Net;
using System.Text;

namespace Neo.Plugins.Models.Payloads
{
    internal class PipeRemoteNodePayload : IPipeMessage
    {
        public IPEndPoint? RemoteEndPoint { get; set; }
        public VersionPayload? Version { get; set; }
        public uint LastBlockIndex { get; set; }

        public int Size =>
            sizeof(int) +
            Encoding.UTF8.GetByteCount($"{RemoteEndPoint}") +
            sizeof(int) +
            (Version?.Size ?? 0);

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Stuffer(buffer);

            RemoteEndPoint = wrapper.TryCatch(t => IPEndPoint.Parse(t.ReadString()), default);
            LastBlockIndex = wrapper.TryCatch(t => t.Read<uint>(), default);

            var bytes = wrapper.TryCatch(t => t.ReadArray<byte>(), default);
            Version = bytes.TryCatch(t => t.AsSerializable<VersionPayload>(), default);
        }

        public byte[] ToArray()
        {
            var wrapper = new Stuffer(Size);

            wrapper.Write($"{RemoteEndPoint}");
            wrapper.Write(LastBlockIndex);

            _ = wrapper.TryCatch(t => t.Write(Version.ToArray()), default);

            return [.. wrapper];
        }
    }
}
