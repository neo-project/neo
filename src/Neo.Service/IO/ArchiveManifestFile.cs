// Copyright (C) 2015-2024 The Neo Project.
//
// ArchiveManifestFile.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Neo.Service.IO
{
    internal sealed class ArchiveManifestFile : ISerializable
    {
        public uint MagicCode { get; } = 0x48435241; // ARCH
        public uint Version { get; } = 0x00000001;
        public uint Network { get; private set; }
        public IReadOnlyDictionary<uint, BlockItem> BlockTable => _blockTable;

        private readonly ConcurrentDictionary<uint, BlockItem> _blockTable = new();

        public int Size =>
            sizeof(uint) +                                          // Magic Code
            sizeof(uint) +                                          // Version
            sizeof(uint) +                                          // Neo Network
            sizeof(int) +                                           // Size of Blocks List
            ((sizeof(uint) + BlockItem.Length) * _blockTable.Count);  // Neo Blocks SHA Checksums

        public static ArchiveManifestFile Create(uint network) =>
            new()
            {
                Network = network,
            };

        public void AddOrUpdateBlockEntry(uint blockIndex, ulong checksum, int size) =>
            _blockTable[blockIndex] = BlockItem.Create(checksum, size);

        public void AddOrUpdateBlockEntry(uint blockIndex, BlockItem blockItem) =>
            _blockTable[blockIndex] = blockItem;

        public void RemoveBlockEntry(uint blockIndex) =>
            _blockTable.TryRemove(blockIndex, out var _);

        public void Deserialize(ref MemoryReader reader)
        {
            if (reader.ReadUInt32() != MagicCode) throw new IOException();
            if (reader.ReadUInt32() != Version) throw new IOException();

            Network = reader.ReadUInt32();

            var size = reader.ReadInt32();
            for (var i = 0u; i < size; i++)
            {
                var blockIndex = reader.ReadUInt32();
                var blockItem = new BlockItem();
                blockItem.Deserialize(ref reader);
                AddOrUpdateBlockEntry(blockIndex, blockItem);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(MagicCode);
            writer.Write(Version);
            writer.Write(Network);
            writer.Write(BlockTable.Count);

            foreach (var item in _blockTable)
            {
                writer.Write(item.Key);
                writer.Write(item.Value);
            }
        }
    }

    internal sealed class BlockItem : ISerializable
    {
        public static int Length { get; } = sizeof(ulong) + sizeof(int);

        public int FileSize { get; private set; }
        public ulong Checksum { get; private set; }

        public int Size =>
            sizeof(ulong) + // Checksum
            sizeof(int);    // FileSize

        public static BlockItem Create(ulong checksum, int fileSize) =>
            new()
            {
                Checksum = checksum,
                FileSize = fileSize,
            };

        public void Deserialize(ref MemoryReader reader)
        {
            Checksum = reader.ReadUInt64();
            FileSize = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Checksum);
            writer.Write(FileSize);
        }
    }
}
