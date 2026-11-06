// Copyright (C) 2015-2026 The Neo Project.
//
// UT_OracleContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Linq;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_OracleContract
    {
        private static readonly byte[] Ret = [(byte)OpCode.RET];

        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        private static OracleRequest SampleRequest(string url = "https://example.com/api", string filter = "$.a")
        {
            return new OracleRequest
            {
                OriginalTxid = UInt256.Zero,
                GasForResponse = 10_00000000,
                Url = url,
                Filter = filter,
                CallbackContract = UInt160.Parse("0x0000000000000000000000000000000000000001"),
                CallbackMethod = "callback",
                UserData = [1, 2, 3]
            };
        }

        private void SeedRequest(ulong id, OracleRequest request)
        {
            var key = new KeyBuilder(NativeContract.Oracle.Id, 7).AddBigEndian(id);
            _snapshot.Add(key, StorageItem.CreateSealed(request));
        }

        private void SeedOracleNodes(int count)
        {
            if (count == 0) return;
            var nodes = new Neo.VM.Types.Array(Enumerable.Range(1, count)
                .Select(i => new KeyPair(Enumerable.Repeat((byte)i, 32).ToArray()).PublicKey)
                .OrderBy(p => p).Select(p => (StackItem)(ByteString)p.EncodePoint(true)));
            _snapshot.Add(NativeContract.RoleManagement.CreateStorageKey((byte)Role.Oracle, 1u),
                new StorageItem(BinarySerializer.Serialize(nodes, ExecutionEngineLimits.Default)));
        }

        [TestMethod]
        [DataRow(2u, 22, 10000000L)]
        [DataRow(1u, 0, 10000000L)]
        [DataRow(1u, 1, 10000000L)]
        [DataRow(1u, 4, 13000000L)]
        [DataRow(1u, 8, 22000000L)]
        [DataRow(1u, 9, 24000000L)]
        [DataRow(1u, 22, 51000000L)]
        [DataRow(0u, 4, 13000000L)]
        public void RequestResponseBudgetIsGatedByHuyao(uint activationHeight, int nodeCount, long minimum)
        {
            SeedOracleNodes(nodeCount);
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, activationHeight)
            };
            foreach (long budget in new[] { minimum - 1, minimum })
            {
                var snapshot = _snapshot.CloneCache();
                using var engine = ApplicationEngine.Create(TriggerType.Application, EmptyTx(), snapshot, settings: settings);
                using var script = new ScriptBuilder();
                script.EmitDynamicCall(NativeContract.Oracle.Hash, "request",
                    "https://example.invalid", null, "callback", null, budget);
                engine.LoadScript(script.ToArray());
                engine.CurrentContext.GetState<ExecutionContextState>().ScriptHash = NativeContract.StdLib.Hash;
                Assert.AreEqual(budget < minimum ? VMState.FAULT : VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
                if (budget < minimum)
                {
                    Assert.Contains($"must be at least {minimum}", engine.FaultException.GetBaseException().Message);
                    Assert.IsEmpty(NativeContract.Oracle.GetRequestsByUrl(snapshot, "https://example.invalid"));
                }
                else
                {
                    var requests = NativeContract.Oracle.GetRequestsByUrl(snapshot, "https://example.invalid").ToArray();
                    Assert.HasCount(1, requests);
                    Assert.AreEqual(budget, requests[0].Item2.GasForResponse);
                }
            }
        }

        [TestMethod]
        [DataRow(4, 10000L, 1000L)]
        [DataRow(4, 15000L, 1000L)]
        [DataRow(4, 300000L, 1000L)]
        [DataRow(1, 300000L, 1000L)]
        [DataRow(3, 300000L, 1000L)]
        [DataRow(4, 305000L, 1000L)]
        [DataRow(8, 300000L, 1000L)]
        [DataRow(9, 300000L, 1000L)]
        [DataRow(16, 300000L, 1000L)]
        [DataRow(17, 300000L, 1000L)]
        [DataRow(22, 300000L, 1000L)]
        [DataRow(4, 300000L, 10000L)]
        [DataRow(4, 1000000L, 1000L)]
        public void ResponseBudgetCoversNetworkAndFinish(int nodeCount, long picoFactor, long feePerByte)
        {
            SeedOracleNodes(nodeCount);
            _snapshot.GetAndChange(NativeContract.Policy.CreateStorageKey(18)).Set(picoFactor);
            _snapshot.GetAndChange(NativeContract.Policy.CreateStorageKey(10)).Set(feePerByte);
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, 0u)
            };
            var nodes = Enumerable.Range(1, nodeCount)
                .Select(i => new KeyPair(Enumerable.Repeat((byte)i, 32).ToArray()).PublicKey).ToArray();
            var contract = Contract.CreateMultiSigContract(nodeCount - (nodeCount - 1) / 3, nodes);
            var tx = EmptyTx([new OracleResponse { Id = 1, Code = OracleResponseCode.InsufficientFunds, Result = ReadOnlyMemory<byte>.Empty }]);
            tx.Script = OracleResponse.FixedScript;
            tx.Signers = [new() { Account = NativeContract.Oracle.Hash }, new() { Account = contract.ScriptHash }];
            tx.Witnesses = [Witness.Empty, new Witness { VerificationScript = contract.Script }];
            var networkFee = tx.CalculateNetworkFee(_snapshot, settings,
                hash => hash == contract.ScriptHash ? contract.Script : null);

            var callback = new ContractState
            {
                Id = 123,
                Hash = UInt160.Parse("0x0000000000000000000000000000000000000001"),
                Nef = new NefFile { Compiler = "test", Source = "", Script = new byte[] { (byte)OpCode.INITSLOT, 0, 4, (byte)OpCode.RET }, Tokens = [] },
                Manifest = TestUtils.CreateDefaultManifest()
            };
            callback.Nef.CheckSum = NefFile.ComputeChecksum(callback.Nef);
            callback.Manifest.Abi.Methods = [new Neo.SmartContract.Manifest.ContractMethodDescriptor
            {
                Name = "callback", Offset = 0, ReturnType = ContractParameterType.Void,
                Parameters = [
                    new() { Name = "url", Type = ContractParameterType.String },
                    new() { Name = "userData", Type = ContractParameterType.Any },
                    new() { Name = "code", Type = ContractParameterType.Integer },
                    new() { Name = "result", Type = ContractParameterType.ByteArray }
                ]
            }];
            _snapshot.Add(NativeContract.ContractManagement.CreateStorageKey(8, callback.Hash), new StorageItem(callback));
            var request = SampleRequest();
            request.CallbackContract = callback.Hash;
            request.UserData = BinarySerializer.Serialize(StackItem.Null, ExecutionEngineLimits.Default);
            SeedRequest(1, request);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, _snapshot, settings: settings);
            long minimum = NativeContract.Oracle.GetMinimumResponseBudget(engine);
            Assert.AreEqual(0L, engine.FeeConsumed, "Estimation must not execute scripts or charge fees.");
            engine.LoadScript(tx.Script);
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            long expected = Math.Max(10000000L, (2 * (networkFee + engine.FeeConsumed) + 999999) / 1000000 * 1000000);
            Assert.AreEqual(expected, minimum, $"nodes={nodeCount}, network={networkFee}, finish={engine.FeeConsumed}");
        }

        private static Transaction EmptyTx(TransactionAttribute[] attributes = null)
        {
            return new Transaction
            {
                Version = 0,
                Nonce = 1,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Attributes = attributes ?? [],
                Signers = [new Signer { Account = UInt160.Zero, Scopes = WitnessScope.CalledByEntry }],
                Script = Ret,
                Witnesses = []
            };
        }

        [TestMethod]
        public void GetPrice_Default_IsPositive_AndMatchesCall()
        {
            var price = NativeContract.Oracle.GetPrice(_snapshot);
            Assert.IsTrue(price > 0);

            var viaCall = NativeContract.Oracle.Call(_snapshot, "getPrice");
            Assert.IsInstanceOfType<Integer>(viaCall);
            Assert.AreEqual(price, (long)viaCall.GetInteger());
        }

        [TestMethod]
        public void GetRequest_Missing_ReturnsNull()
        {
            Assert.IsNull(NativeContract.Oracle.GetRequest(_snapshot, 999_999));
        }

        [TestMethod]
        public void GetRequest_Seeded_ReturnsClone()
        {
            var original = SampleRequest();
            SeedRequest(42, original);

            var got = NativeContract.Oracle.GetRequest(_snapshot, 42);
            Assert.IsNotNull(got);
            Assert.AreEqual(original.Url, got.Url);
            Assert.AreEqual(original.Filter, got.Filter);
            Assert.AreEqual(original.CallbackMethod, got.CallbackMethod);
            Assert.AreEqual(original.CallbackContract, got.CallbackContract);
            Assert.AreEqual(original.GasForResponse, got.GasForResponse);
            Assert.AreSequenceEqual(original.UserData, got.UserData);
        }

        [TestMethod]
        public void GetRequests_EnumeratesSeeded()
        {
            SeedRequest(1, SampleRequest("https://a.test"));
            SeedRequest(2, SampleRequest("https://b.test"));

            var all = NativeContract.Oracle.GetRequests(_snapshot).OrderBy(p => p.Item1).ToArray();
            Assert.IsTrue(all.Length >= 2);
            Assert.IsTrue(all.Any(p => p.Item1 == 1 && p.Item2.Url == "https://a.test"));
            Assert.IsTrue(all.Any(p => p.Item1 == 2 && p.Item2.Url == "https://b.test"));
        }

        [TestMethod]
        public void GetRequestsByUrl_Empty_WhenNoList()
        {
            var list = NativeContract.Oracle.GetRequestsByUrl(_snapshot, "https://no-such-url").ToArray();
            Assert.IsEmpty(list);
        }

        [TestMethod]
        public void Verify_WithoutOracleResponseAttribute_ReturnsFalse()
        {
            var result = NativeContract.Oracle.Call(_snapshot, EmptyTx(), null, "verify");
            Assert.IsInstanceOfType<Neo.VM.Types.Boolean>(result);
            Assert.IsFalse(result.GetBoolean());
        }

        [TestMethod]
        public void Verify_WithOracleResponseAttribute_ReturnsTrue()
        {
            var tx = EmptyTx(
            [
                new OracleResponse
                {
                    Id = 1,
                    Code = OracleResponseCode.Success,
                    Result = new byte[] { 0x01 }
                }
            ]);
            tx.Nonce = 2;

            var result = NativeContract.Oracle.Call(_snapshot, tx, null, "verify");
            Assert.IsTrue(result.GetBoolean());
        }

        [TestMethod]
        public void SetPrice_NonCommittee_Throws()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                NativeContract.Oracle.Call(_snapshot, "setPrice",
                    new ContractParameter(ContractParameterType.Integer) { Value = 1_00000000L }));
        }

        [TestMethod]
        public void SetPrice_NonPositive_Throws()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                NativeContract.Oracle.Call(_snapshot, "setPrice",
                    new ContractParameter(ContractParameterType.Integer) { Value = 0L }));
        }

        [TestMethod]
        public void Request_UrlTooLong_Throws()
        {
            var longUrl = new string('x', 300);
            Assert.ThrowsExactly<ArgumentException>(() =>
                NativeContract.Oracle.Call(_snapshot, "request",
                    new ContractParameter(ContractParameterType.String) { Value = longUrl },
                    new ContractParameter(ContractParameterType.String) { Value = null },
                    new ContractParameter(ContractParameterType.String) { Value = "cb" },
                    new ContractParameter(ContractParameterType.Any) { Value = null },
                    new ContractParameter(ContractParameterType.Integer) { Value = 10_00000000L }));
        }

        [TestMethod]
        public void Request_CallbackStartsWithUnderscore_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                NativeContract.Oracle.Call(_snapshot, "request",
                    new ContractParameter(ContractParameterType.String) { Value = "https://ok" },
                    new ContractParameter(ContractParameterType.String) { Value = null },
                    new ContractParameter(ContractParameterType.String) { Value = "_hidden" },
                    new ContractParameter(ContractParameterType.Any) { Value = null },
                    new ContractParameter(ContractParameterType.Integer) { Value = 10_00000000L }));
        }

        [TestMethod]
        public void Request_GasForResponseTooLow_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                NativeContract.Oracle.Call(_snapshot, "request",
                    new ContractParameter(ContractParameterType.String) { Value = "https://ok" },
                    new ContractParameter(ContractParameterType.String) { Value = null },
                    new ContractParameter(ContractParameterType.String) { Value = "cb" },
                    new ContractParameter(ContractParameterType.Any) { Value = null },
                    new ContractParameter(ContractParameterType.Integer) { Value = 1L }));
        }

        [TestMethod]
        public void Finish_WithoutOracleResponse_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                NativeContract.Oracle.Call(_snapshot, EmptyTx(), null, "finish"));
        }
    }
}
