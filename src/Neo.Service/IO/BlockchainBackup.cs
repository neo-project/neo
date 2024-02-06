// Copyright (C) 2015-2024 The Neo Project.
//
// BlockchainBackup.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Neo.Service.IO
{
    internal static class BlockchainBackup
    {
        public static IEnumerable<Block> ReadBlocksFromBarFile(uint network, uint currentBlockHeight = 0, string directory = "", IProgress<double>? progress = null)
        {
            var barcFileNames = GetImportFileNames(directory)
                .Where(w => Path.GetExtension(w.FileName).Equals(".barc", StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(o => o.StartIndex);

            foreach (var barcFileItem in barcFileNames)
            {
                var baf = new BlockchainArchiveFile(barcFileItem.FileName, network);
                foreach (var blockIndex in baf.IndexEntries.Where(w => w > currentBlockHeight).Order())
                {
                    var block = baf.Read(blockIndex);
                    if (block is null) yield break;
                    yield return block;
                }
            }
        }

        public static IEnumerable<Block> ReadBlocksFromAccFile(uint currentBlockHeight = 0, string directory = "", IProgress<double>? progress = null)
        {
            var accFileNames = GetImportFileNames(directory)
                .Where(w => Path.GetExtension(w.FileName).Equals(".barc", StringComparison.InvariantCultureIgnoreCase) == false)
                .OrderBy(o => o.StartIndex);

            if (accFileNames.Any() == false) yield break;

            foreach (var accFile in accFileNames)
            {
                using var fs = new FileStream(accFile.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (Path.GetExtension(accFile.FileName).Equals(".zip", StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    foreach (var block in ReadAccBlocksFromStream(fs, currentBlockHeight, true, progress))
                        yield return block;
                }
                else
                {
                    using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
                    var entry = zip.GetEntry(Path.GetFileNameWithoutExtension(accFile.FileName));
                    if (entry is null) yield break;
                    using var zes = entry.Open();
                    foreach (var block in ReadAccBlocksFromStream(zes, currentBlockHeight, true, progress))
                        yield return block;
                }
            }
        }

        private static IEnumerable<Block> ReadAccBlocksFromStream(Stream s, uint currentBlockHeight, bool readFromStart = false, IProgress<double>? progress = null)
        {
            using var r = new BinaryReader(s);
            var start = readFromStart ? r.ReadUInt32() : 0u;
            var count = r.ReadUInt32();
            var end = start + count - 1u;

            if (end <= currentBlockHeight) yield break;

            for (var height = start; height <= end; height++, progress?.Report(100.0 * height / end))
            {
                var size = r.ReadInt32();
                if (size > Message.PayloadMaxSize) yield break;

                var array = r.ReadBytes(size);
                if (height > currentBlockHeight)
                    yield return array.AsSerializable<Block>();
            }
        }

        private static IEnumerable<ImportFileItem> GetImportFileNames(string directory = "")
        {
            if (string.IsNullOrEmpty(directory))
                directory = AppContext.BaseDirectory;

            if (Directory.Exists(directory) == false) throw new DirectoryNotFoundException(directory);

            return Directory.EnumerateFiles(directory, "chain.*.acc", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(directory, "chain.*.acc.zip", SearchOption.TopDirectoryOnly))
                .Concat(Directory.EnumerateFiles(directory, "chain.*.barc", SearchOption.TopDirectoryOnly))
                .Select(s => new ImportFileItem()
                {
                    FileName = Path.GetFileName(s),
                    StartIndex = uint.Parse(RegexUtility.SearchNumbersOnly().Match(s).Value),
                });
        }
    }

    internal sealed class ImportFileItem
    {
        public required string FileName { get; init; }
        public required uint StartIndex { get; init; }
    }
}
