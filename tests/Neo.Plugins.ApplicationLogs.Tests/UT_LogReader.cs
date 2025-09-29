// Copyright (C) 2015-2025 The Neo Project.
//
// UT_LogReader.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.Plugins.ApplicationLogs;
using Neo.Plugins.ApplicationLogs.Store.Models;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationLogsSettings = Neo.Plugins.ApplicationLogs.ApplicationLogsSettings;

namespace Neo.Plugins.ApplicationsLogs.Tests
{
    [TestClass]
    public class UT_LogReader
    {
        static readonly string NeoTransferScript = "CxEMFPlu76Cuc\u002BbgteStE4ozsOWTNUdrDBQtYNweHko3YcnMFOes3ceblcI/lRTAHwwIdHJhbnNmZXIMFPVj6kC8KD1NDgXEjqMFs/Kgc0DvQWJ9W1I=";
        static readonly byte[] ValidatorScript = Contract.CreateSignatureRedeemScript(TestProtocolSettings.SoleNode.StandbyCommittee[0]);
        static readonly UInt160 ValidatorScriptHash = ValidatorScript.ToScriptHash();

        static readonly byte[] MultisigScript = Contract.CreateMultiSigRedeemScript(1, TestProtocolSettings.SoleNode.StandbyCommittee);
        static readonly UInt160 MultisigScriptHash = MultisigScript.ToScriptHash();

        public class TestMemoryStoreProvider(MemoryStore memoryStore) : IStoreProvider
        {
            public MemoryStore MemoryStore { get; init; } = memoryStore;
            public string Name => nameof(MemoryStore);
            public IStore GetStore(string path) => MemoryStore;
        }

        private class NeoSystemFixture : IDisposable
        {
            public NeoSystem _neoSystem;
            public TestMemoryStoreProvider _memoryStoreProvider;
            public MemoryStore _memoryStore;
            public readonly NEP6Wallet _wallet = TestUtils.GenerateTestWallet("123");
            public WalletAccount _walletAccount;
            public Transaction[] txs;
            public Block block;
            public LogReader logReader;

            public NeoSystemFixture()
            {
                _memoryStore = new MemoryStore();
                _memoryStoreProvider = new TestMemoryStoreProvider(_memoryStore);
                logReader = new LogReader();
                Plugin.Plugins.Add(logReader);  // initialize before NeoSystem to let NeoSystem load the plugin
                _neoSystem = new NeoSystem(TestProtocolSettings.SoleNode with { Network = ApplicationLogsSettings.Default.Network }, _memoryStoreProvider);
                _walletAccount = _wallet.Import("KxuRSsHgJMb3AMSN6B9P3JHNGMFtxmuimqgR9MmXPcv3CLLfusTd");

                NeoSystem system = _neoSystem;
                txs = [
                    new Transaction
                    {
                        Nonce = 233,
                        ValidUntilBlock = NativeContract.Ledger.CurrentIndex(system.GetSnapshotCache()) + system.Settings.MaxValidUntilBlockIncrement,
                        Signers = [new Signer() { Account = MultisigScriptHash, Scopes = WitnessScope.CalledByEntry }],
                        Attributes = Array.Empty<TransactionAttribute>(),
                        Script = Convert.FromBase64String(NeoTransferScript),
                        NetworkFee = 1000_0000,
                        SystemFee = 1000_0000,
                    }
                ];
                byte[] signature = txs[0].Sign(_walletAccount.GetKey(), ApplicationLogsSettings.Default.Network);
                txs[0].Witnesses = [new Witness
                {
                    InvocationScript = new byte[] { (byte)OpCode.PUSHDATA1, (byte)signature.Length }.Concat(signature).ToArray(),
                    VerificationScript = MultisigScript,
                }];
                block = new Block
                {
                    Header = new Header
                    {
                        Version = 0,
                        PrevHash = _neoSystem.GenesisBlock.Hash,
                        MerkleRoot = new UInt256(),
                        Timestamp = _neoSystem.GenesisBlock.Timestamp + 15_000,
                        Index = 1,
                        NextConsensus = _neoSystem.GenesisBlock.NextConsensus,
                    },
                    Transactions = txs,
                };
                block.Header.MerkleRoot ??= MerkleTree.ComputeRoot(block.Transactions.Select(t => t.Hash).ToArray());
                signature = block.Sign(_walletAccount.GetKey(), ApplicationLogsSettings.Default.Network);
                block.Header.Witness = new Witness
                {
                    InvocationScript = new byte[] { (byte)OpCode.PUSHDATA1, (byte)signature.Length }.Concat(signature).ToArray(),
                    VerificationScript = MultisigScript,
                };
            }

            public void Dispose()
            {
                logReader.Dispose();
                _neoSystem.Dispose();
                _memoryStore.Dispose();
            }
        }

        private static NeoSystemFixture s_neoSystemFixture;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            s_neoSystemFixture = new NeoSystemFixture();
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void ClassCleanup()
        {
            s_neoSystemFixture.Dispose();
        }

        [TestMethod]
        public async Task Test_GetApplicationLog()
        {
            NeoSystem system = s_neoSystemFixture._neoSystem;
            Block block = s_neoSystemFixture.block;
            await system.Blockchain.Ask(block, cancellationToken: CancellationToken.None);  // persist the block

            JObject blockJson = (JObject)s_neoSystemFixture.logReader.GetApplicationLog(block.Hash);
            Assert.AreEqual(blockJson["blockhash"], block.Hash.ToString());

            JArray executions = (JArray)blockJson["executions"];
            Assert.HasCount(2, executions);
            Assert.AreEqual("OnPersist", executions[0]["trigger"]);
            Assert.AreEqual("PostPersist", executions[1]["trigger"]);

            JArray notifications = (JArray)executions[1]["notifications"];
            Assert.HasCount(1, notifications);
            Assert.AreEqual(notifications[0]["contract"], GasToken.GAS.Hash.ToString());
            Assert.AreEqual("Transfer", notifications[0]["eventname"]);  // from null to Validator
            Assert.AreEqual(nameof(ContractParameterType.Any), notifications[0]["state"]["value"][0]["type"]);
            CollectionAssert.AreEqual(Convert.FromBase64String(notifications[0]["state"]["value"][1]["value"].AsString()), ValidatorScriptHash.ToArray());
            Assert.AreEqual("50000000", notifications[0]["state"]["value"][2]["value"]);

            blockJson = (JObject)s_neoSystemFixture.logReader.GetApplicationLog(block.Hash, "PostPersist");
            executions = (JArray)blockJson["executions"];
            Assert.HasCount(1, executions);
            Assert.AreEqual("PostPersist", executions[0]["trigger"]);

            // "true" is invalid but still works
            JObject transactionJson = (JObject)s_neoSystemFixture.logReader.GetApplicationLog(s_neoSystemFixture.txs[0].Hash.ToString(), "true");
            executions = (JArray)transactionJson["executions"];
            Assert.HasCount(1, executions);
            Assert.AreEqual(nameof(VMState.HALT), executions[0]["vmstate"]);
            Assert.AreEqual(true, executions[0]["stack"][0]["value"]);
            notifications = (JArray)executions[0]["notifications"];
            Assert.HasCount(2, notifications);
            Assert.AreEqual("Transfer", notifications[0]["eventname"].AsString());
            Assert.AreEqual(notifications[0]["contract"].AsString(), NeoToken.NEO.Hash.ToString());
            Assert.AreEqual("1", notifications[0]["state"]["value"][2]["value"]);
            Assert.AreEqual("Transfer", notifications[1]["eventname"].AsString());
            Assert.AreEqual(notifications[1]["contract"].AsString(), GasToken.GAS.Hash.ToString());
            Assert.AreEqual("50000000", notifications[1]["state"]["value"][2]["value"]);
        }

        [TestMethod]
        public async Task Test_Commands()
        {
            NeoSystem system = s_neoSystemFixture._neoSystem;
            Block block = s_neoSystemFixture.block;
            await system.Blockchain.Ask(block, cancellationToken: CancellationToken.None);  // persist the block

            s_neoSystemFixture.logReader.OnGetBlockCommand("1");
            s_neoSystemFixture.logReader.OnGetBlockCommand(block.Hash.ToString());
            s_neoSystemFixture.logReader.OnGetContractCommand(NativeContract.NEO.Hash);
            s_neoSystemFixture.logReader.OnGetTransactionCommand(s_neoSystemFixture.txs[0].Hash);

            var blockLog = s_neoSystemFixture.logReader._neostore.GetBlockLog(block.Hash, TriggerType.Application);
            var transactionLog = s_neoSystemFixture.logReader._neostore.GetTransactionLog(s_neoSystemFixture.txs[0].Hash);
            foreach (var log in new BlockchainExecutionModel[] { blockLog, transactionLog })
            {
                Assert.AreEqual(VMState.HALT, log.VmState);
                Assert.IsTrue(log.Stack[0].GetBoolean());
                Assert.AreEqual(2, log.Notifications.Length);
                Assert.AreEqual("Transfer", log.Notifications[0].EventName);
                Assert.AreEqual(log.Notifications[0].ScriptHash, NativeContract.NEO.Hash);
                Assert.AreEqual(1, log.Notifications[0].State[2]);
                Assert.AreEqual("Transfer", log.Notifications[1].EventName);
                Assert.AreEqual(log.Notifications[1].ScriptHash, NativeContract.GAS.Hash);
                Assert.AreEqual(50000000, log.Notifications[1].State[2]);
            }

            List<(BlockchainEventModel eventLog, UInt256 txHash)> neoLogs = s_neoSystemFixture
                .logReader._neostore.GetContractLog(NativeContract.NEO.Hash, TriggerType.Application).ToList();
            Assert.ContainsSingle(neoLogs);
            Assert.AreEqual(neoLogs[0].txHash, s_neoSystemFixture.txs[0].Hash);
            Assert.AreEqual("Transfer", neoLogs[0].eventLog.EventName);
            Assert.AreEqual(neoLogs[0].eventLog.ScriptHash, NativeContract.NEO.Hash);
            Assert.AreEqual(1, neoLogs[0].eventLog.State[2]);
        }
    }
}
