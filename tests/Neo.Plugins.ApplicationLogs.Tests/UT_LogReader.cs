// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ApplicationLogs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Util;
using Microsoft.AspNetCore.Authorization;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.ApplicationLogs;
using Neo.Plugins.ApplicationLogs.Store;
using Neo.Plugins.ApplicationLogs.Store.Models;
using Neo.Plugins.ApplicationLogs.Store.States;
using Neo.Plugins.ApplicationsLogs.Tests.Setup;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Neo.Plugins.ApplicationsLogs.Tests.UT_LogReader;

namespace Neo.Plugins.ApplicationsLogs.Tests
{
    public class UT_LogReader : IClassFixture<NeoSystemFixture>
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

        public class NeoSystemFixture : IDisposable
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
                _neoSystem = new NeoSystem(TestProtocolSettings.SoleNode with { Network = ApplicationLogs.Settings.Default.Network }, _memoryStoreProvider);
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
                byte[] signature = txs[0].Sign(_walletAccount.GetKey(), ApplicationLogs.Settings.Default.Network);
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
                signature = block.Sign(_walletAccount.GetKey(), ApplicationLogs.Settings.Default.Network);
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

        private readonly NeoSystemFixture _neoSystemFixture;

        public UT_LogReader(NeoSystemFixture neoSystemFixture)
        {
            _neoSystemFixture = neoSystemFixture;
        }

        [Fact]
        public async Task Test_GetApplicationLog()
        {
            NeoSystem system = _neoSystemFixture._neoSystem;
            Block block = _neoSystemFixture.block;
            await system.Blockchain.Ask(block);  // persist the block

            JObject blockJson = (JObject)_neoSystemFixture.logReader.GetApplicationLog([block.Hash.ToString()]);
            Assert.Equal(blockJson["blockhash"], block.Hash.ToString());
            JArray executions = (JArray)blockJson["executions"];
            Assert.Equal(executions.Count, 2);
            Assert.Equal(executions[0]["trigger"], "OnPersist");
            Assert.Equal(executions[1]["trigger"], "PostPersist");
            JArray notifications = (JArray)executions[1]["notifications"];
            Assert.Equal(notifications.Count, 1);
            Assert.Equal(notifications[0]["contract"], GasToken.GAS.Hash.ToString());
            Assert.Equal(notifications[0]["eventname"], "Transfer");  // from null to Validator
            Assert.Equal(notifications[0]["state"]["value"][0]["type"], nameof(ContractParameterType.Any));
            Assert.Equal(Convert.FromBase64String(notifications[0]["state"]["value"][1]["value"].AsString()), ValidatorScriptHash.ToArray());
            Assert.Equal(notifications[0]["state"]["value"][2]["value"], "50000000");

            blockJson = (JObject)_neoSystemFixture.logReader.GetApplicationLog([block.Hash.ToString(), "PostPersist"]);
            executions = (JArray)blockJson["executions"];
            Assert.Equal(executions.Count, 1);
            Assert.Equal(executions[0]["trigger"], "PostPersist");

            JObject transactionJson = (JObject)_neoSystemFixture.logReader.GetApplicationLog([_neoSystemFixture.txs[0].Hash.ToString(), true]);  // "true" is invalid but still works
            executions = (JArray)transactionJson["executions"];
            Assert.Equal(executions.Count, 1);
            Assert.Equal(executions[0]["vmstate"], nameof(VMState.HALT));
            Assert.Equal(executions[0]["stack"][0]["value"], true);
            notifications = (JArray)executions[0]["notifications"];
            Assert.Equal(notifications.Count, 2);
            Assert.Equal(notifications[0]["eventname"].AsString(), "Transfer");
            Assert.Equal(notifications[0]["contract"].AsString(), NeoToken.NEO.Hash.ToString());
            Assert.Equal(notifications[0]["state"]["value"][2]["value"], "1");
            Assert.Equal(notifications[1]["eventname"].AsString(), "Transfer");
            Assert.Equal(notifications[1]["contract"].AsString(), GasToken.GAS.Hash.ToString());
            Assert.Equal(notifications[1]["state"]["value"][2]["value"], "50000000");
        }

        [Fact]
        public async Task Test_Commands()
        {
            NeoSystem system = _neoSystemFixture._neoSystem;
            Block block = _neoSystemFixture.block;
            await system.Blockchain.Ask(block);  // persist the block

            _neoSystemFixture.logReader.OnGetBlockCommand("1");
            _neoSystemFixture.logReader.OnGetBlockCommand(block.Hash.ToString());
            _neoSystemFixture.logReader.OnGetContractCommand(NeoToken.NEO.Hash);
            _neoSystemFixture.logReader.OnGetTransactionCommand(_neoSystemFixture.txs[0].Hash);

            BlockchainExecutionModel blockLog = _neoSystemFixture.logReader._neostore.GetBlockLog(block.Hash, TriggerType.Application);
            BlockchainExecutionModel transactionLog = _neoSystemFixture.logReader._neostore.GetTransactionLog(_neoSystemFixture.txs[0].Hash);
            foreach (BlockchainExecutionModel log in new BlockchainExecutionModel[] { blockLog, transactionLog })
            {
                Assert.Equal(log.VmState, VMState.HALT);
                Assert.Equal(log.Stack[0].GetBoolean(), true);
                Assert.Equal(log.Notifications.Count(), 2);
                Assert.Equal(log.Notifications[0].EventName, "Transfer");
                Assert.Equal(log.Notifications[0].ScriptHash, NeoToken.NEO.Hash);
                Assert.Equal(log.Notifications[0].State[2], 1);
                Assert.Equal(log.Notifications[1].EventName, "Transfer");
                Assert.Equal(log.Notifications[1].ScriptHash, GasToken.GAS.Hash);
                Assert.Equal(log.Notifications[1].State[2], 50000000);
            }

            List<(BlockchainEventModel eventLog, UInt256 txHash)> neoLogs = _neoSystemFixture.logReader._neostore.GetContractLog(NeoToken.NEO.Hash, TriggerType.Application).ToList();
            Assert.Equal(neoLogs.Count, 1);
            Assert.Equal(neoLogs[0].txHash, _neoSystemFixture.txs[0].Hash);
            Assert.Equal(neoLogs[0].eventLog.EventName, "Transfer");
            Assert.Equal(neoLogs[0].eventLog.ScriptHash, NeoToken.NEO.Hash);
            Assert.Equal(neoLogs[0].eventLog.State[2], 1);
        }
    }
}
