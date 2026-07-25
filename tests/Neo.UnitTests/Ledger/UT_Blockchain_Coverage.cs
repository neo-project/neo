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
using Neo.SmartContract.Native;
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
