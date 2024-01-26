// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark.Blocks.cs file belongs to the neo project and is free
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
        private static ProtocolSettings s_protocol;
        private SnapshotCache _memoryStore;
        private Block _block;

        [Params(1466600, 1691562, 1693182, 1695163, 1941906, 2212440, 1545151, 1691721, 1693194, 1695746, 1952842, 2212544, 1551539, 1691783, 1693241, 1695896, 1955515, 2212575, 1551550, 1691784, 1693269, 1696740, 1976901, 2212681, 1551551, 1691811, 1693274, 1696758, 1991342, 2212706, 1551552, 1691968, 1693286, 1696759, 2027305, 2212723, 1570420, 1692023, 1693294, 1723984, 2037494, 2212800, 1584313, 1692102, 1693308, 1855694, 2080810, 2212807, 1667870, 1692302, 1693343, 1855827, 2116627, 2212833, 1667871, 1692680, 1693373, 1882087, 2137993, 2230072, 1683031, 1692682, 1693398, 1882106, 2141425, 2268049, 1690032, 1692692, 1693399, 1899635, 2141551, 2351614, 1690810, 1692735, 1693428, 1900762, 2158835, 2363914, 1691179, 1692772, 1693650, 1900767, 2171359, 470619, 1691180, 1692921, 1694343, 1900768, 2194622, 715707, 1691181, 1693099, 1694410, 1900845, 2212242, 1691492, 1693100, 1694594, 1907366, 2212278)]
        public uint _blockId = 0;

        [GlobalSetup]
        public void Setup()
        {
            s_protocol = ProtocolSettings.Load("config.json");
            _memoryStore = new SnapshotCache(new MemoryStore());
            LoadBlock(_blockId);
        }

        [Benchmark]
        public void RunBlock()
        {
            RunBench();
        }

        private (int id, byte[], byte[]) LoadSnapshot(byte[] encodedData)
        {
            using (var memoryStream = new MemoryStream(encodedData))
            using (var reader = new BinaryReader(memoryStream))
            {
                var id = reader.ReadInt32();
                var length1 = reader.ReadInt32();
                var array1 = reader.ReadBytes(length1);
                var length2 = reader.ReadInt32();
                var array2 = reader.ReadBytes(length2);
                return (id, array1, array2);
            }
        }

        private void LoadBlock(uint blockId)
        {
            var realFile = Path.GetFullPath($"./blocks/{blockId}.txt");
            var lines = File.ReadLines(realFile, Encoding.UTF8);
            foreach (var (line, index) in lines.Select((line, index) => (line, index)))
            {
                if (index == 0)
                {
                    _block = Convert.FromBase64String(line).AsSerializable<Block>();
                }
                else
                {
                    var states = Convert.FromBase64String(line);
                    var (id, key, value) = LoadSnapshot(states);
                    _memoryStore.Add(new StorageKey
                    {
                        Id = id,
                        Key = key
                    }, new StorageItem(value));
                }
            }
        }

        private void RunBench()
        {
            foreach (var transaction in _block.Transactions)
            {
                using var engine = ApplicationEngine.Create(TriggerType.Application, transaction, _memoryStore, _block, s_protocol, transaction.SystemFee);
                engine.LoadScript(transaction.Script);
                engine.Execute();
                // if (engine.State != VMState.HALT) throw new InvalidOperationException();
            }
        }
    }
}
