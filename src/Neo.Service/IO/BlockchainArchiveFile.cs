// Copyright (C) 2015-2024 The Neo Project.
//
// BlockchainArchiveFile.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Service.IO
{
    internal sealed class BlockchainArchiveFile : IDisposable
    {
        public IReadOnlyCollection<uint> IndexEntries => _manifest.BlockTable.Keys.ToArray();

        private static readonly string s_archiveFileExtension = ".barc";
        private static readonly string s_manifestFileName = "MANIFEST";

        private readonly FileStream _fs;
        private readonly ZipArchive _zip;
        private readonly ArchiveManifestFile _manifest;

        public BlockchainArchiveFile(
            string fileName,
            uint network)
        {
            if (Path.GetExtension(fileName).Equals(s_archiveFileExtension, StringComparison.InvariantCultureIgnoreCase) == false)
                fileName += s_archiveFileExtension;

            _fs = new(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough);
            _zip = new(_fs, ZipArchiveMode.Update, false, Encoding.UTF8);
            _manifest = ReadManifestEntry() ?? ArchiveManifestFile.Create(network);
            if (_manifest.Network != network) throw new InvalidDataException(nameof(network));
        }

        public void Dispose()
        {
            WriteManifestEntry();
            _zip.Dispose();
            _fs.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Delete(uint blockIndex)
        {
            if (blockIndex == 0) throw new ArgumentOutOfRangeException(nameof(blockIndex));

            var entry = _zip.GetEntry($"{blockIndex}") ?? throw new KeyNotFoundException(nameof(blockIndex));
            _manifest.RemoveBlockEntry(blockIndex);
            entry.Delete();
        }

        public Block? Read(uint blockIndex)
        {
            if (blockIndex == 0) throw new ArgumentOutOfRangeException(nameof(blockIndex));

            var entry = _zip.GetEntry($"{blockIndex}") ?? throw new KeyNotFoundException(nameof(blockIndex));
            var manifestBlockTableItem = _manifest.BlockTable[blockIndex];
            var blockBuffer = new byte[manifestBlockTableItem.FileSize];
            using var stream = entry.Open();
            stream.Read(blockBuffer, 0, blockBuffer.Length);

            var shaData = SHA256.HashData(blockBuffer);
            var blockChecksum = BitConverter.ToUInt32(shaData);
            if (blockChecksum != manifestBlockTableItem.Checksum) throw new BadImageFormatException(nameof(blockIndex));

            return blockBuffer.AsSerializable<Block>();
        }

        public void Write(Block block)
        {
            if (block.Index == 0) throw new ArgumentOutOfRangeException(nameof(block));

            var entry = _zip.GetEntry($"{block.Index}") ?? _zip.CreateEntry($"{block.Index}");
            var blockBuffer = block.ToArray();
            var shaData = SHA256.HashData(blockBuffer);
            var checksum = BitConverter.ToUInt32(shaData);
            _manifest.AddOrUpdateBlockEntry(block.Index, checksum, blockBuffer.Length);

            using var stream = entry.Open();
            stream.Write(blockBuffer, 0, blockBuffer.Length);
        }

        private void WriteManifestEntry()
        {
            var entry = _zip.GetEntry(s_manifestFileName) ?? _zip.CreateEntry(s_manifestFileName);
            using var stream = entry.Open();
            using var bw = new BinaryWriter(stream);
            _manifest.Serialize(bw);
        }

        private ArchiveManifestFile? ReadManifestEntry()
        {
            var entry = _zip.GetEntry(s_manifestFileName);
            if (entry is null) return default;
            var manifestBuffer = new byte[entry.Length];
            using var stream = entry.Open();
            stream.Read(manifestBuffer, 0, manifestBuffer.Length);
            return manifestBuffer.AsSerializable<ArchiveManifestFile>();
        }
    }
}
