// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Blockchain_Coverage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Threading;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_Blockchain_Coverage : TestKit
    {
        private NeoSystem _system;
        private TestProbe _sender;

        [TestInitialize]
        public void Initialize()
        {
            _system = TestBlockchain.GetSystem();
            _sender = CreateTestProbe();
        }

        [TestMethod]
        public void Import_EmptyBlocks_Completes()
        {
            _sender.Send(_system.Blockchain, new Blockchain.Import([], Verify: false));
            _sender.ExpectMsg<Blockchain.ImportCompleted>(TimeSpan.FromSeconds(5), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void FillMemoryPool_Empty_Completes()
        {
            _sender.Send(_system.Blockchain, new Blockchain.FillMemoryPool([]));
            _sender.ExpectMsg<Blockchain.FillCompleted>(TimeSpan.FromSeconds(5), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void Headers_Genesis_IsAccepted()
        {
            var header = NativeContract.Ledger.GetHeader(_system.StoreView, 0);
            Assert.IsNotNull(header);
            _sender.Send(_system.Blockchain, new[] { header });
            // Headers processing may not reply; ensure no unexpected failure reply in short window.
            _sender.ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void Reverify_EmptyList_DoesNotFault()
        {
            _sender.Send(_system.Blockchain, new Blockchain.Reverify([]));
            _sender.ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void Relay_AlreadyKnownBlock_ReturnsAlreadyExists()
        {
            var genesis = NativeContract.Ledger.GetBlock(_system.StoreView, 0);
            Assert.IsNotNull(genesis);

            _sender.Send(_system.Blockchain, genesis);

            var result = _sender.ExpectMsg<Blockchain.RelayResult>(TimeSpan.FromSeconds(5), cancellationToken: CancellationToken.None);

            Assert.AreSame(genesis, result.Inventory);
            Assert.AreEqual(VerifyResult.AlreadyExists, result.Result);
        }

        [TestMethod]
        public void Relay_OutOfOrderBlock_ReplaysAfterPredecessorPersists()
        {
            var keyBytes = new byte[32];
            keyBytes[0] = 0x42;
            var key = new KeyPair(keyBytes);
            var settings = TestProtocolSettings.SoleNode with
            {
                StandbyCommittee = [key.PublicKey],
                SeedList = []
            };
            using var system = new TestBlockchain.TestNeoSystem(settings);
            var sender = CreateTestProbe();
            var consensus = Contract.CreateMultiSigContract(1, [key.PublicKey]);

            Block CreateBlock(uint index, UInt256 prevHash, ulong timestamp)
            {
                var header = new Header
                {
                    Index = index,
                    PrevHash = prevHash,
                    Timestamp = timestamp,
                    Nonce = index,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = consensus.ScriptHash,
                    Witness = Witness.Empty
                };
                using var invocation = new ScriptBuilder();
                invocation.EmitPush(header.Sign(key, settings.Network));
                header.Witness = new Witness
                {
                    InvocationScript = invocation.ToArray(),
                    VerificationScript = consensus.Script
                };
                return new Block { Header = header, Transactions = [] };
            }

            sender.Send(system.Blockchain, new Blockchain.Import([], Verify: false));
            sender.ExpectMsg<Blockchain.ImportCompleted>(TimeSpan.FromSeconds(5), cancellationToken: CancellationToken.None);

            var genesis = system.GenesisBlock;
            var first = CreateBlock(1, genesis.Hash, genesis.Timestamp + 1000);
            var second = CreateBlock(2, first.Hash, first.Timestamp + 1000);

            sender.Send(system.Blockchain, second);
            Assert.AreEqual(VerifyResult.UnableToVerify,
                sender.ExpectMsg<Blockchain.RelayResult>(TimeSpan.FromSeconds(5), cancellationToken: CancellationToken.None).Result);

            sender.Send(system.Blockchain, first);
            Assert.AreEqual(VerifyResult.Succeed,
                sender.ExpectMsg<Blockchain.RelayResult>(TimeSpan.FromSeconds(5), cancellationToken: CancellationToken.None).Result);
            Assert.IsTrue(SpinWait.SpinUntil(() => NativeContract.Ledger.CurrentIndex(system.StoreView) == 2,
                TimeSpan.FromSeconds(5)), "The cached successor block was not persisted.");
        }

        [TestMethod]
        public void Idle_DoesNotFault()
        {
            _sender.Send(_system.Blockchain, Neo.IO.Actors.Idle.Instance);
            _sender.ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void Props_CreatesActor()
        {
            var props = Blockchain.Props(_system);
            Assert.IsNotNull(props);
        }
    }
}
