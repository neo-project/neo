// Copyright (C) 2015-2024 The Neo Project.
//
// PipeMemoryPoolPayload.cs file belongs to the neo project and is free
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
using System.Linq;

namespace Neo.Plugins.Models.Payloads
{
    internal class PipeMemoryPoolPayload : IPipeMessage
    {
        public Transaction[] UnVerifiedTransactions { get; set; } = [];
        public Transaction[] VerifiedTransactions { get; set; } = [];

        public int Size =>
            sizeof(int) +
            sizeof(int) +
            UnVerifiedTransactions.Sum(s => s.Size + sizeof(int)) +
            VerifiedTransactions.Sum(s => s.Size + sizeof(int));

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Stuffer(buffer);

            var unVSize = wrapper.Read<int>();
            var vSize = wrapper.Read<int>();

            UnVerifiedTransactions = new Transaction[unVSize];
            VerifiedTransactions = new Transaction[vSize];

            for (var i = 0; i < unVSize; i++)
            {
                var unVBytes = wrapper.ReadArray<byte>();
                UnVerifiedTransactions[i] = unVBytes.AsSerializable<Transaction>();
            }

            for (var i = 0; i < unVSize; i++)
            {
                var vBytes = wrapper.ReadArray<byte>();
                VerifiedTransactions[i] = vBytes.AsSerializable<Transaction>();
            }

        }

        public byte[] ToArray()
        {
            var wrapper = new Stuffer(Size);

            wrapper.Write(UnVerifiedTransactions.Length);
            wrapper.Write(VerifiedTransactions.Length);

            foreach (var unVTx in UnVerifiedTransactions)
                wrapper.Write(unVTx.ToArray());
            foreach (var vTx in VerifiedTransactions)
                wrapper.Write(vTx.ToArray());

            return [.. wrapper];
        }
    }
}
