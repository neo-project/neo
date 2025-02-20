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
using System.Threading.Tasks;
using static Neo.Plugins.ApplicationsLogs.Tests.UT_LogReader;
using Settings = Neo.Plugins.ApplicationLogs.Settings;

namespace Neo.Plugins.ApplicationsLogs.Tests
{
    [TestClass]
    public class UT_LogReader
    {
        static readonly string NeoTransferScript = "CxEMFPlu76Cuc\u002BbgteStE4ozsOWTNUdrDBQtYNweHko3YcnMFOes3ceblcI/lRTAHwwIdHJhbnNmZXIMFPVj6kC8KD1NDgXEjqMFs/Kgc0DvQWJ9W1I=";
        static readonly byte[] ValidatorScript = Contract.CreateSignatureRedeemScript(TestProtocolSettings.SoleNode.StandbyCommittee[0]);
        static readonly UInt160 ValidatorScriptHash = ValidatorScript.ToScriptHash();
        static readonly string ValidatorAddress = ValidatorScriptHash.ToAddress(ProtocolSettings.Default.AddressVersion);
        static readonly byte[] MultisigScript = Contract.CreateMultiSigRedeemScript(1, TestProtocolSettings.SoleNode.StandbyCommittee);
        static readonly UInt160 MultisigScriptHash = MultisigScript.ToScriptHash();
        static readonly string MultisigAddress = MultisigScriptHash.ToAddress(ProtocolSettings.Default.AddressVersion);

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
                _neoSystem = new NeoSystem(TestProtocolSettings.SoleNode with { Network = Settings.Default.Network }, _memoryStoreProvider);
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
                byte[] signature = txs[0].Sign(_walletAccount.GetKey(), Settings.Default.Network);
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
                signature = block.Sign(_walletAccount.GetKey(), Settings.Default.Network);
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
            await system.Blockchain.Ask(block);  // persist the block

            JObject blockJson = (JObject)s_neoSystemFixture.logReader.GetApplicationLog([block.Hash.ToString()]);
            Assert.AreEqual(blockJson["blockhash"], block.Hash.ToString());
            JArray executions = (JArray)blockJson["executions"];
            Assert.AreEqual(2, executions.Count);
            Assert.AreEqual(executions[0]["trigger"], "OnPersist");
            Assert.AreEqual(executions[1]["trigger"], "PostPersist");
            JArray notifications = (JArray)executions[1]["notifications"];
            Assert.AreEqual(1, notifications.Count);
            Assert.AreEqual(notifications[0]["contract"], GasToken.GAS.Hash.ToString());
            Assert.AreEqual(notifications[0]["eventname"], "Transfer");  // from null to Validator
            Assert.AreEqual(notifications[0]["state"]["value"][0]["type"], nameof(ContractParameterType.Any));
            Assert.AreEqual(Convert.FromBase64String(notifications[0]["state"]["value"][1]["value"].AsString()), ValidatorScriptHash.ToArray());
            Assert.AreEqual(notifications[0]["state"]["value"][2]["value"], "50000000");

            blockJson = (JObject)s_neoSystemFixture.logReader.GetApplicationLog([block.Hash.ToString(), "PostPersist"]);
            executions = (JArray)blockJson["executions"];
            Assert.AreEqual(1, executions.Count);
            Assert.AreEqual(executions[0]["trigger"], "PostPersist");

            JObject transactionJson = (JObject)s_neoSystemFixture.logReader.GetApplicationLog([s_neoSystemFixture.txs[0].Hash.ToString(), true]);  // "true" is invalid but still works
            executions = (JArray)transactionJson["executions"];
            Assert.AreEqual(1, executions.Count);
            Assert.AreEqual(executions[0]["vmstate"], nameof(VMState.HALT));
            Assert.AreEqual(executions[0]["stack"][0]["value"], true);
            notifications = (JArray)executions[0]["notifications"];
            Assert.AreEqual(2, notifications.Count);
            Assert.AreEqual("Transfer", notifications[0]["eventname"].AsString());
            Assert.AreEqual(notifications[0]["contract"].AsString(), NeoToken.NEO.Hash.ToString());
            Assert.AreEqual(notifications[0]["state"]["value"][2]["value"], "1");
            Assert.AreEqual("Transfer", notifications[1]["eventname"].AsString());
            Assert.AreEqual(notifications[1]["contract"].AsString(), GasToken.GAS.Hash.ToString());
            Assert.AreEqual(notifications[1]["state"]["value"][2]["value"], "50000000");
        }

        [TestMethod]
        public async Task Test_Commands()
        {
            NeoSystem system = s_neoSystemFixture._neoSystem;
            Block block = s_neoSystemFixture.block;
            await system.Blockchain.Ask(block);  // persist the block

            s_neoSystemFixture.logReader.OnGetBlockCommand("1");
            s_neoSystemFixture.logReader.OnGetBlockCommand(block.Hash.ToString());
            s_neoSystemFixture.logReader.OnGetContractCommand(NeoToken.NEO.Hash);
            s_neoSystemFixture.logReader.OnGetTransactionCommand(s_neoSystemFixture.txs[0].Hash);

            BlockchainExecutionModel blockLog = s_neoSystemFixture.logReader._neostore.GetBlockLog(block.Hash, TriggerType.Application);
            BlockchainExecutionModel transactionLog = s_neoSystemFixture.logReader._neostore.GetTransactionLog(s_neoSystemFixture.txs[0].Hash);
            foreach (BlockchainExecutionModel log in new BlockchainExecutionModel[] { blockLog, transactionLog })
            {
                Assert.AreEqual(VMState.HALT, log.VmState);
                Assert.IsTrue(log.Stack[0].GetBoolean());
                Assert.AreEqual(2, log.Notifications.Count());
                Assert.AreEqual("Transfer", log.Notifications[0].EventName);
                Assert.AreEqual(log.Notifications[0].ScriptHash, NeoToken.NEO.Hash);
                Assert.AreEqual(log.Notifications[0].State[2], 1);
                Assert.AreEqual("Transfer", log.Notifications[1].EventName);
                Assert.AreEqual(log.Notifications[1].ScriptHash, GasToken.GAS.Hash);
                Assert.AreEqual(log.Notifications[1].State[2], 50000000);
            }

            List<(BlockchainEventModel eventLog, UInt256 txHash)> neoLogs = s_neoSystemFixture.logReader._neostore.GetContractLog(NeoToken.NEO.Hash, TriggerType.Application).ToList();
            Assert.ContainsSingle(neoLogs);
            Assert.AreEqual(neoLogs[0].txHash, s_neoSystemFixture.txs[0].Hash);
            Assert.AreEqual("Transfer", neoLogs[0].eventLog.EventName);
            Assert.AreEqual(neoLogs[0].eventLog.ScriptHash, NeoToken.NEO.Hash);
            Assert.AreEqual(neoLogs[0].eventLog.State[2], 1);
        }
    }
}
