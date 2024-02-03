// Copyright (C) 2015-2024 The Neo Project.
//
// NodeService.Import.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.Extensions.Logging;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service
{
    internal partial class NodeService
    {
        internal async Task StopImportBlocksAsync()
        {

            if (_importBlocksTask is not null)
            {
                _importBlocksTokenSource?.Cancel();
                await _importBlocksTask;
                _logger.LogInformation("Stopped importing blocks.");
            }

            _importBlocksTokenSource?.Dispose();
            _importBlocksTokenSource = null;
        }

        private async Task ImportThenStartNeoSystemAsync(bool verify, CancellationToken cancellationToken)
        {
            if (_neoSystem is null) return;
            if (_appSettings.Storage.Engine == nameof(MemoryStore))
            {
                await StartNeoSystemAsync(cancellationToken);
                return;
            }

            _logger.LogInformation("Started importing blocks.");

            using var blocksBeingImported = GetBlocksFromFile(AppContext.BaseDirectory).GetEnumerator();

            while (cancellationToken.IsCancellationRequested == false)
            {
                var blocksToImport = new List<Block>();
                for (var i = 0; i < 10; i++)
                {
                    if (blocksBeingImported.MoveNext() == false) break;
                    blocksToImport.Add(blocksBeingImported.Current);
                }
                if (blocksToImport.Count == 0) break;
                await _neoSystem.Blockchain.Ask<Blockchain.ImportCompleted>(new Blockchain.Import
                {
                    Blocks = blocksToImport,
                    Verify = verify
                }, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                _logger.LogInformation("Import blocks canceled!");
            else
                _logger.LogInformation("Import blocks finished!");

            await StartNeoSystemAsync(cancellationToken);
        }

        private IEnumerable<Block> GetBlocksFromFile(string dir = "")
        {
            if (_neoSystem is null) yield break;
            if (Directory.Exists(dir) == false)
                throw new DirectoryNotFoundException(dir);

            const string PathAcc = "chain.acc";
            const string PathAccZip = PathAcc + ".zip";

            if (File.Exists(PathAcc))
            {
                using FileStream fs = new(Path.Combine(dir, PathAcc), FileMode.Open, FileAccess.Read, FileShare.Read);
                foreach (var block in GetBlocks(fs))
                    yield return block;
            }

            if (File.Exists(PathAccZip))
            {
                using var fs = new FileStream(Path.Combine(dir, PathAccZip), FileMode.Open, FileAccess.Read, FileShare.Read);
                using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
                var entry = zip.GetEntry(PathAcc);
                if (entry is null) yield break;
                using var zs = entry.Open();
                foreach (var block in GetBlocks(zs))
                    yield return block;
            }

            var paths = Directory.EnumerateFiles(dir, "chain.*.acc", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(dir, "chain.*.acc.zip", SearchOption.TopDirectoryOnly))
                .Select(p => new
                {
                    FileName = Path.GetFileName(p),
                    Start = uint.Parse(RegexUtility.SearchNumbersOnly().Match(p).Value),
                    IsCompressed = Path.GetExtension(p).Equals(".zip", StringComparison.InvariantCultureIgnoreCase)
                }).OrderBy(p => p.Start);

            if (paths.Any() == false) yield break;

            var height = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);

            foreach (var path in paths)
            {
                if (path.Start > height + 1u) break;
                if (path.IsCompressed)
                {
                    using var fs = new FileStream(path.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
                    var entry = zip.GetEntry(Path.GetFileNameWithoutExtension(path.FileName));
                    if (entry is null) yield break;
                    using var zs = entry.Open();
                    foreach (var block in GetBlocks(zs, true))
                        yield return block;
                }
                else
                {
                    using var fs = new FileStream(path.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    foreach (var block in GetBlocks(fs, true))
                        yield return block;
                }
            }
        }

        private IEnumerable<Block> GetBlocks(Stream stream, bool read_start = false)
        {
            using var r = new BinaryReader(stream);
            var start = read_start ? r.ReadUInt32() : 0u;
            var count = r.ReadUInt32();
            var end = start + count - 1u;
            var currentHeight = NativeContract.Ledger.CurrentIndex(_neoSystem?.StoreView);
            if (end <= currentHeight) yield break;
            for (var height = start; height <= end; height++)
            {
                var size = r.ReadInt32();
                if (size > Message.PayloadMaxSize)
                {
                    _logger.LogError("Block {Height} exceeds the maximum allowed size.", height);
                    yield break;
                }

                var array = r.ReadBytes(size);
                if (height > currentHeight)
                {
                    var block = array.AsSerializable<Block>();
                    if (block.Index % 10000u == 0u) // every 10,000 blocks; report!
                        _logger.LogInformation("Imported block {Index}.", block.Index);
                    yield return block;
                }
            }
        }
    }
}
