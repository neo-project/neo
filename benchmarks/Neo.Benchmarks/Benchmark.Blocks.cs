// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark.Block.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using System.Text;

namespace Neo
{
    public class BenchmarkBlock
    {
        private readonly Dictionary<uint, SnapshotCache> _memoryStores = new();
        private readonly Dictionary<uint, Block> _blocks = new();
        private static ProtocolSettings s_protocol;
        private static NeoSystem s_system;

        [GlobalSetup]
        public void Setup()
        {
            s_protocol = ProtocolSettings.Default;
            s_system = new NeoSystem(s_protocol);
            LoadBlocks();
        }

        [Benchmark]
        public void RunBlock1466600()
        {
            RunBench(1466600);
        }

        private (byte[], byte[]) LoadSnapshot(byte[] encodedData)
        {
            using (var memoryStream = new MemoryStream(encodedData))
            using (var reader = new BinaryReader(memoryStream))
            {
                var length1 = reader.ReadInt32();
                var array1 = reader.ReadBytes(length1);
                var length2 = reader.ReadInt32();
                var array2 = reader.ReadBytes(length2);
                return (array1, array2);
            }
        }

        private void LoadBlocks()
        {
            foreach (var file in Directory.GetFiles("./blocks", "*.txt", SearchOption.AllDirectories))
            {
                Console.WriteLine($"Processing file '{file}'");
                var realFile = Path.GetFullPath(file);
                var lines = File.ReadLines(realFile, Encoding.UTF8);
                MemoryStore store = new();
                SnapshotCache snapshot = new(store);
                uint blockId = 0;
                foreach (var (line, index) in lines.Select((line, index) => (line, index)))
                {
                    if (index == 0)
                    {
                        var block = Convert.FromBase64String(line).AsSerializable<Block>();
                        _blocks.Add(block.Index, block);
                        blockId = block.Index;
                    }
                    else
                    {
                        var states = Convert.FromBase64String(line);
                        var (key, value) = LoadSnapshot(states);
                        snapshot.Add(new StorageKey(key), new StorageItem(value));
                    }
                }
                snapshot.Commit();
                _memoryStores.Add(blockId, snapshot);
            }
        }

        private void RunBench(uint blockId)
        {
            var clonedSnapshot = _memoryStores[blockId].CreateSnapshot();
            var block = _blocks[blockId];
            foreach (var transaction in block.Transactions)
            {
                using var engine = ApplicationEngine.Create(TriggerType.Application, transaction, clonedSnapshot, block, s_system.Settings, transaction.SystemFee);
                engine.LoadScript(transaction.Script);
                engine.Execute();
            }
        }
    }
}
