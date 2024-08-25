// Copyright (C) 2015-2024 The Neo Project.
//
// PipeContractState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Plugins.Buffers;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;

namespace Neo.Plugins.Models.Payloads
{
    internal class PipeContractState : IPipeMessage
    {
        public int Id { get; set; }

        public ushort UpdateCounter { get; set; }

        public UInt160 Hash { get; set; }

        public NefFile? Nef { get; set; }

        public ContractManifest? Manifest { get; set; }

        private byte[]? manifestBytes => Manifest?
            .ToJson()
            .ToByteArray(false);

        public PipeContractState()
        {
            Id = 0;
            UpdateCounter = 0;
            Hash = new();
        }

        public PipeContractState(ContractState state)
        {
            Id = state.Id;
            UpdateCounter = state.UpdateCounter;
            Hash = state.Hash;
            Nef = state.Nef;
            Manifest = state.Manifest;
        }

        public int Size =>
            sizeof(int) +                   // ID
            sizeof(ushort) +                // UpdateCounter
            (sizeof(int) * 3) +             // Array Buffers
            UInt160.Length +                // Hash
            (Nef?.Size ?? 0) +              // Script
            (manifestBytes?.Length ?? 0);   // Manifest

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Stuffer(buffer);

            Id = wrapper.Read<int>();
            UpdateCounter = wrapper.Read<ushort>();

            var hashBytes = wrapper.ReadArray<byte>();
            Hash = hashBytes.TryCatch(t => t.AsSerializable<UInt160>(), new());

            var nefBytes = wrapper.TryCatch(t => t.ReadArray<byte>(), default);
            Nef = nefBytes.TryCatch(t => t.AsSerializable<NefFile>(), default);

            var manifestBytes = wrapper.TryCatch(t => t.ReadArray<byte>(), default);
            Manifest = manifestBytes.TryCatch(t => ContractManifest.Parse(t), default);
        }

        public byte[] ToArray()
        {
            var wrapper = new Stuffer(Size);

            wrapper.Write(Id);
            wrapper.Write(UpdateCounter);
            wrapper.Write(Hash.ToArray());
            _ = wrapper.TryCatch(t => t.Write(Nef.ToArray()), default);
            _ = wrapper.TryCatch(t => t.Write(manifestBytes!), default);

            return [.. wrapper];
        }
    }
}
