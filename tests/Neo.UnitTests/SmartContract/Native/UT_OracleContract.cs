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
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
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
