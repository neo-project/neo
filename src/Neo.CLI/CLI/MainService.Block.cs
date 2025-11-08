// Copyright (C) 2015-2025 The Neo Project.
//
// MainService.Block.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.ConsoleService;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Neo.CLI
{
    /// <summary>
    /// Partial class implementing block import and export functionality for Neo CLI.
    /// This file contains methods for:
    /// - Reading blocks from file streams (.acc and .acc.zip formats)
    /// - Importing blocks from files into the blockchain
    /// - Exporting blocks from the blockchain to files
    /// - Handling the "export blocks" command
    /// 
    /// Offline package is available at: https://sync.ngd.network/
    /// </summary>
    partial class MainService
    {
        /// <summary>
        /// Process "export blocks" command
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="count">Number of blocks</param>
        /// <param name="path">Path</param>
        [ConsoleCommand("export blocks", Category = "Blockchain Commands")]
        private void OnExportBlocksStartCountCommand(uint start, uint count = uint.MaxValue, string? path = null)
        {
            uint height = NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView);
            if (height < start)
            {
                ConsoleHelper.Error("invalid start height.");
                return;
            }

            count = Math.Min(count, height - start + 1);

            if (string.IsNullOrEmpty(path))
            {
                path = $"chain.{start}.acc";
            }

            WriteBlocks(start, count, path, true);
        }

        /// <summary>
        /// Reads blocks from a stream and yields blocks that are not yet in the blockchain.
        /// </summary>
        /// <param name="stream">The stream to read blocks from.</param>
        /// <param name="readStart">If true, reads the start block index from the stream.</param>
        /// <returns>An enumerable of blocks that are not yet in the blockchain.</returns>
        private IEnumerable<Block> GetBlocks(Stream stream, bool readStart = false)
        {
            using BinaryReader r = new BinaryReader(stream);
            uint start = readStart ? r.ReadUInt32() : 0;
            uint count = r.ReadUInt32();
            uint end = start + count - 1;
            uint currentHeight = NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView);
            if (end <= currentHeight) yield break;
            for (uint height = start; height <= end; height++)
            {
                var size = r.ReadInt32();
                if (size > Message.PayloadMaxSize)
                    throw new ArgumentException($"Block at height {height} has a size of {size} bytes, which exceeds the maximum allowed payload size of {Message.PayloadMaxSize} bytes. This block cannot be processed due to size constraints.");

                byte[] array = r.ReadBytes(size);
                if (height > currentHeight)
                {
                    Block block = array.AsSerializable<Block>();
                    yield return block;
                }
            }
        }

        /// <summary>
        /// Gets blocks from chain.acc files in the current directory.
        /// Supports both uncompressed (.acc) and compressed (.acc.zip) formats.
        /// </summary>
        /// <returns>An enumerable of blocks that are not yet in the blockchain.</returns>
        private IEnumerable<Block> GetBlocksFromFile()
        {
            const string pathAcc = "chain.acc";
            if (File.Exists(pathAcc))
                using (FileStream fs = new(pathAcc, FileMode.Open, FileAccess.Read, FileShare.Read))
                    foreach (var block in GetBlocks(fs))
                        yield return block;

            const string pathAccZip = pathAcc + ".zip";
            if (File.Exists(pathAccZip))
                using (FileStream fs = new(pathAccZip, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (ZipArchive zip = new(fs, ZipArchiveMode.Read))
                using (Stream? zs = zip.GetEntry(pathAcc)?.Open())
                {
                    if (zs is not null)
                    {
                        foreach (var block in GetBlocks(zs))
                            yield return block;
                    }
                }

            var paths = Directory.EnumerateFiles(".", "chain.*.acc", SearchOption.TopDirectoryOnly).Concat(Directory.EnumerateFiles(".", "chain.*.acc.zip", SearchOption.TopDirectoryOnly)).Select(p => new
            {
                FileName = Path.GetFileName(p),
                Start = uint.Parse(Regex.Match(p, @"\d+").Value),
                IsCompressed = p.EndsWith(".zip")
            }).OrderBy(p => p.Start);

            var height = NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView);
            foreach (var path in paths)
            {
                if (path.Start > height + 1) break;
                if (path.IsCompressed)
                    using (FileStream fs = new(path.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (ZipArchive zip = new(fs, ZipArchiveMode.Read))
                    using (var zs = zip.GetEntry(Path.GetFileNameWithoutExtension(path.FileName))?.Open())
                    {
                        if (zs is not null)
                        {
                            foreach (var block in GetBlocks(zs, true))
                                yield return block;
                        }
                    }
                else
                    using (FileStream fs = new(path.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        foreach (var block in GetBlocks(fs, true))
                            yield return block;
            }
        }

        /// <summary>
        /// Exports blocks from the blockchain to a file.
        /// </summary>
        /// <param name="start">The index of the first block to export.</param>
        /// <param name="count">The number of blocks to export.</param>
        /// <param name="path">The path of the file to export to.</param>
        /// <param name="writeStart">If true, writes the start block index to the file.</param>
        private void WriteBlocks(uint start, uint count, string path, bool writeStart)
        {
            uint end = start + count - 1;
            using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough);
            if (fs.Length > 0)
            {
                byte[] buffer = new byte[sizeof(uint)];
                if (writeStart)
                {
                    fs.Seek(sizeof(uint), SeekOrigin.Begin);
                    fs.ReadExactly(buffer);
                    start += BitConverter.ToUInt32(buffer, 0);
                    fs.Seek(sizeof(uint), SeekOrigin.Begin);
                }
                else
                {
                    fs.ReadExactly(buffer);
                    start = BitConverter.ToUInt32(buffer, 0);
                    fs.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                if (writeStart)
                {
                    fs.Write(BitConverter.GetBytes(start), 0, sizeof(uint));
                }
            }
            if (start <= end)
                fs.Write(BitConverter.GetBytes(count), 0, sizeof(uint));
            fs.Seek(0, SeekOrigin.End);
            Console.WriteLine("Export block from " + start + " to " + end);

            using (var percent = new ConsolePercent(start, end))
            {
                for (uint i = start; i <= end; i++)
                {
                    Block block = NativeContract.Ledger.GetBlock(NeoSystem.StoreView, i);
                    byte[] array = block.ToArray();
                    fs.Write(BitConverter.GetBytes(array.Length), 0, sizeof(int));
                    fs.Write(array, 0, array.Length);
                    percent.Value = i;
                }
            }
        }

        /// <summary>
        /// Imports blocks from chain.acc files into the blockchain.
        /// </summary>
        /// <param name="verifyImport">If true, verifies the blocks before importing them.</param>
        /// <returns>A task representing the asynchronous import operation.</returns>
        private async Task ImportBlocksFromFile(bool verifyImport)
        {
            using (var blocksBeingImported = GetBlocksFromFile().GetEnumerator())
            {
                while (true)
                {
                    var blocksToImport = new List<Block>();
                    for (var i = 0; i < 10; i++)
                    {
                        if (!blocksBeingImported.MoveNext()) break;
                        blocksToImport.Add(blocksBeingImported.Current);
                    }
                    if (blocksToImport.Count == 0) break;
                    await NeoSystem.Blockchain.Ask<Blockchain.ImportCompleted>(new Blockchain.Import
                    {
                        Blocks = blocksToImport,
                        Verify = verifyImport
                    });
                    if (NeoSystem is null) return;
                }
            }
        }
    }
}
