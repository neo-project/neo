// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Blockchain_UnverifiedBlockCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_Blockchain_UnverifiedBlockCache : TestKit
    {
        private static readonly MethodInfo s_onNewBlock = typeof(Blockchain)
            .GetMethod("OnNewBlock", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static Block MakeBlock(uint index, uint nonce, int transactionCount = 0)
        {
            var transactions = new Transaction[transactionCount];
            for (uint i = 0; i < transactionCount; i++)
            {
                transactions[i] = new Transaction
                {
                    Version = 0,
                    Nonce = nonce + i,
                    SystemFee = 0,
                    NetworkFee = 0,
                    ValidUntilBlock = uint.MaxValue,
                    Signers = [new Signer { Account = UInt160.Zero, Scopes = WitnessScope.Global }],
                    Attributes = [],
                    Script = new byte[65_535],
                    Witnesses = [Witness.Empty]
                };
            }

            return new Block
            {
                Header = new Header
                {
                    Version = 0,
                    Index = index,
                    Timestamp = 1,
                    Nonce = nonce,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = MerkleTree.ComputeRoot([.. transactions.Select(p => p.Hash)]),
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = transactions
            };
        }

        private static VerifyResult OnNewBlock(Blockchain blockchain, Block block)
        {
            var result = (VerifyResult)s_onNewBlock.Invoke(blockchain, [block])!;
            Assert.AreEqual(VerifyResult.UnableToVerify, result);
            return result;
        }

        private static IDictionary GetCache(Blockchain blockchain)
        {
            return (IDictionary)GetField(blockchain, "_blockCacheUnverified")!;
        }

        private static object GetField(Blockchain blockchain, string name)
        {
            return typeof(Blockchain).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(blockchain);
        }

        private static T GetConstant<T>(string name)
        {
            return (T)typeof(Blockchain).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!
                .GetRawConstantValue()!;
        }

        private static IList GetBlocks(object list)
        {
            return (IList)list.GetType().GetProperty("Blocks")!.GetValue(list)!;
        }

        private static void AssertAccounting(Blockchain blockchain)
        {
            var expectedCount = 0;
            long expectedSize = 0;
            foreach (DictionaryEntry entry in GetCache(blockchain))
            {
                var blocks = GetBlocks(entry.Value!);
                expectedCount += blocks.Count;
                expectedSize += blocks.Cast<Block>().Sum(p => (long)p.Size);
            }

            Assert.AreEqual(expectedCount, (int)GetField(blockchain, "_unverifiedBlockCount")!);
            Assert.AreEqual(expectedSize, (long)GetField(blockchain, "_unverifiedBlockCacheSize")!);
            Assert.IsLessThanOrEqualTo(GetConstant<int>("MaxUnverifiedBlockCount"), expectedCount);
            Assert.IsLessThanOrEqualTo(GetConstant<long>("MaxUnverifiedBlockCacheSize"), expectedSize);
        }

        [TestMethod]
        public void CountLimitRejectsFartherBlocksAndEvictsFarthestForCloserBlocks()
        {
            var system = TestBlockchain.GetSystem();
            var blockchain = ActorOfAsTestActorRef(() => new Blockchain(system)).UnderlyingActor;
            var currentHeight = NativeContract.Ledger.CurrentIndex(system.StoreView);
            var firstIndex = currentHeight + 3;
            var maxCount = GetConstant<int>("MaxUnverifiedBlockCount");
            var cache = GetCache(blockchain);

            for (var i = 0; i < maxCount; i++)
                OnNewBlock(blockchain, MakeBlock(firstIndex + (uint)i, (uint)i));

            Assert.AreEqual(maxCount, cache.Count);
            AssertAccounting(blockchain);

            var duplicate = MakeBlock(firstIndex, 0);
            OnNewBlock(blockchain, duplicate);
            Assert.AreEqual(maxCount, cache.Count);
            AssertAccounting(blockchain);

            var farther = MakeBlock(firstIndex + (uint)maxCount, 10_000);
            OnNewBlock(blockchain, farther);
            Assert.IsFalse(cache.Contains(farther.Index));
            Assert.AreEqual(maxCount, cache.Count);
            AssertAccounting(blockchain);

            var farthestIndex = firstIndex + (uint)maxCount - 1;
            var closer = MakeBlock(currentHeight + 2, 20_000);
            OnNewBlock(blockchain, closer);
            Assert.IsTrue(cache.Contains(closer.Index));
            Assert.IsFalse(cache.Contains(farthestIndex));
            Assert.AreEqual(maxCount, cache.Count);
            AssertAccounting(blockchain);
        }

        [TestMethod]
        public void ByteLimitRejectsFartherBlocksAndEvictsFarthestForCloserBlocks()
        {
            var system = TestBlockchain.GetSystem();
            var blockchain = ActorOfAsTestActorRef(() => new Blockchain(system)).UnderlyingActor;
            var currentHeight = NativeContract.Ledger.CurrentIndex(system.StoreView);
            var firstIndex = currentHeight + 3;
            var cache = GetCache(blockchain);
            const int transactionsPerBlock = 64;
            var accepted = 0;

            while (true)
            {
                var candidate = MakeBlock(firstIndex + (uint)accepted,
                    (uint)(accepted * transactionsPerBlock), transactionsPerBlock);
                OnNewBlock(blockchain, candidate);
                AssertAccounting(blockchain);
                if (!cache.Contains(candidate.Index)) break;

                accepted++;
                Assert.IsLessThan(100, accepted, "the byte limit was not reached");
            }

            Assert.IsGreaterThan(0, accepted);
            var farthestIndex = firstIndex + (uint)accepted - 1;
            var closer = MakeBlock(currentHeight + 2, 1_000_000, transactionsPerBlock);
            OnNewBlock(blockchain, closer);
            Assert.IsTrue(cache.Contains(closer.Index));
            Assert.IsFalse(cache.Contains(farthestIndex));
            Assert.AreEqual(accepted, cache.Count);
            AssertAccounting(blockchain);
        }

        [TestMethod]
        public void CapacityChangesNotifyTheOriginalPeerToRetry()
        {
            var system = TestBlockchain.GetSystem();
            var blockchainActor = ActorOfAsTestActorRef(() => new Blockchain(system));
            var blockchain = blockchainActor.UnderlyingActor;
            var currentHeight = NativeContract.Ledger.CurrentIndex(system.StoreView);
            var peer = CreateTestProbe();
            var maxCount = GetConstant<int>("MaxUnverifiedBlockCount");
            var firstIndex = currentHeight + 3;

            peer.Send(system.TaskManager, new TaskManager.Register(new VersionPayload
            {
                UserAgent = "cache-test",
                Capabilities = [new FullNodeCapability(currentHeight)]
            }));
            peer.ExpectMsg<Message>(TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);

            for (var i = 0; i < maxCount; i++)
                peer.Send(blockchainActor, MakeBlock(firstIndex + (uint)i, (uint)i));

            AwaitAssert(
                () => Assert.AreEqual(maxCount, GetCache(blockchain).Count),
                TimeSpan.FromSeconds(5),
                cancellationToken: CancellationToken.None);

            peer.Send(blockchainActor, MakeBlock(firstIndex + (uint)maxCount, 10_000));
            peer.FishForMessage<object>(
                message => message is RemoteNode.RestartInventory,
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);

            peer.Send(blockchainActor, MakeBlock(currentHeight + 2, 20_000));
            peer.FishForMessage<object>(
                message => message is RemoteNode.RestartInventory,
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);

            AwaitAssert(
                () => Assert.AreEqual(maxCount, GetCache(blockchain).Count),
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);
            AssertAccounting(blockchain);
        }
    }
}
