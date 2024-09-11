// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.IO;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Extensions;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
//using Xunit;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public partial class UT_ConsensusService : TestKit
    {
        private NeoSystem _neoSystem;
        private Settings _dbftSettings;
        private TestMemoryStoreProvider _memoryStoreProvider;
        private MemoryStore _memoryStore;
        private readonly NEP6Wallet _wallet = TestUtils.GenerateTestWallet("123");
        private WalletAccount _walletAccount;
        private Mock<NEP6Wallet> _mockWallet;

        const byte NativePrefixAccount = 20;
        const byte NativePrefixTotalSupply = 11;

        [TestInitialize]
        public void TestSetup()
        {
            _memoryStore = new MemoryStore();
            _memoryStoreProvider = new TestMemoryStoreProvider(_memoryStore);
            _neoSystem = new NeoSystem(TestProtocolSettings.SoleNode, _memoryStoreProvider);
            _walletAccount = _wallet.Import("KxuRSsHgJMb3AMSN6B9P3JHNGMFtxmuimqgR9MmXPcv3CLLfusTd");

            _dbftSettings = new Settings();


            _mockWallet = new Mock<NEP6Wallet>(Path.GetRandomFileName(), "12345678", ProtocolSettings.Default, string.Empty);
            _mockWallet.Setup(p => p.GetAccount(It.IsAny<UInt160>())).Returns(_walletAccount);
            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccount.ScriptHash);
            var snapshot = _neoSystem.GetSnapshotCache();
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Please build and test in debug mode
            _neoSystem.MemPool.Clear();
            _memoryStore.Reset();
            var snapshot = _neoSystem.GetSnapshotCache();
            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccount.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();
        }

        [TestMethod]
        public void ConsensusService_SingleNodeActors_OnStart_PrepReq_PrepResponses_Commits()
        {
            // dotnet test /workspaces/neo/tests/Neo.Plugins.DBFTPlugin.Tests/Neo.Plugins.DBFTPlugin.Tests.csproj /property:GenerateFullPaths=true /p:Configuration=Debug /p:Platform="AnyCPU"
            Console.WriteLine($"\n(UT-Consensus) Wallet is: {_mockWallet.Object.GetAccount(UInt160.Zero).GetKey().PublicKey}");

            var mockContext = new Mock<ConsensusContext>(_neoSystem, ProtocolSettings.Default, _mockWallet.Object);

            var timeValues = new[] {
            new DateTime(1980, 06, 01, 0, 0, 1, 001, DateTimeKind.Utc),  // For tests, used below
            new DateTime(1980, 06, 01, 0, 0, 3, 001, DateTimeKind.Utc),  // For receiving block
            new DateTime(1980, 05, 01, 0, 0, 5, 001, DateTimeKind.Utc),  // For Initialize
            new DateTime(1980, 06, 01, 0, 0, 15, 001, DateTimeKind.Utc), // unused
                    };
            for (var i = 0; i < timeValues.Length; i++)
                Console.WriteLine($"time {i}: {timeValues[i]} ");

            int timeIndex = 0;
            var timeMock = new Mock<TestTimeProvider>();
            timeMock.SetupGet(tp => tp.UtcNow).Returns(() => timeValues[0]);
            TestTimeProvider.Current = timeMock.Object;
            ulong defaultTimestamp = 328665601001;
            TestTimeProvider.Current.UtcNow.ToTimestampMS().Should().Be(defaultTimestamp); //1980-06-01 00:00:15:001


            /* ============================ */
            /* From Here we need to fix HEADER AND TEST PROBE AND TESTACTORREF */
            /* ============================ */

            // Creating a test block
            Header myUTHeader = new Header();
            TestUtilsConsensus.SetupHeaderWithValues(
    myUTHeader,
    UInt256.Zero,
    out UInt256 merkRootVal,
    out UInt160 val160,
    out ulong timestampVal,
    out ulong nonceVal,  // Add this line
    out uint indexVal,
    out Witness scriptVal);
            myUTHeader.Size.Should().Be(113);
            Console.WriteLine($"header {myUTHeader} hash {myUTHeader.Hash} {myUTHeader.PrevHash} timestamp {timestampVal}");
            timestampVal.Should().Be(defaultTimestamp);


            TestProbe subscriber = CreateTestProbe();

            TestActorRef<ConsensusService> actorConsensus =
                ActorOfAsTestActorRef<ConsensusService>(
                    Akka.Actor.Props.Create(() =>
                        (ConsensusService)Activator.CreateInstance(
                            typeof(ConsensusService),
                            BindingFlags.Instance | BindingFlags.NonPublic,
                            null,
                            new object[] { _neoSystem, _dbftSettings, mockContext.Object },
                            null
                        )
                    )
                );

            // internal ConsensusService(NeoSystem neoSystem, Settings settings, ConsensusContext context)
            //public ConsensusService(IActorRef localNode, IActorRef taskManager, IActorRef blockchain, IStore store, Wallet wallet)

            var testPersistCompleted = new Blockchain.PersistCompleted
            {
                Block = new Block
                {
                    Header = myUTHeader,
                    Transactions = new Transaction[0]
                }
            };
            Console.WriteLine("\n==========================");
            Console.WriteLine("Telling a new block to actor consensus...");
            Console.WriteLine("will trigger OnPersistCompleted !");
            // OnPersist will not launch timer, we need OnStart
            actorConsensus.Tell(testPersistCompleted);
            Console.WriteLine("\n==========================");

            Console.WriteLine("\n==========================");
            Console.WriteLine("will start consensus!");
            actorConsensus.Tell(new ConsensusService.Start { });

            Console.WriteLine("Waiting for subscriber recovery message...");
            // The next line force a waits, then, subscriber keeps running its thread
            // In the next case it waits for a Msg of type LocalNode.SendDirectly
            // As we may expect, as soon as consensus start it sends a RecoveryRequest of this aforementioned type
            var askingForInitialRecovery = subscriber.ExpectMsg<LocalNode.SendDirectly>();
            Console.WriteLine($"Recovery Message I: {askingForInitialRecovery}");



            Console.WriteLine("Finalizing consensus service actor.");
            Sys.Stop(actorConsensus);
            Console.WriteLine("Actor actorConsensus Stopped.\n");


            // Enable to fail test
            TestTimeProvider.Current.UtcNow.ToTimestampMS().Should().Be(0);
        }
    }


}
