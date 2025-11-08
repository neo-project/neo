// Copyright (C) 2015-2025 The Neo Project.
//
// UT_OracleService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;

namespace Neo.Plugins.OracleService.Tests
{
    [TestClass]
    public class UT_OracleService
    {
        [TestMethod]
        public void TestFilter()
        {
            var json = """
            {
                "Stores": ["Lambton Quay",  "Willis Street"],
                "Manufacturers": [{
                    "Name": "Acme Co",
                    "Products": [{ "Name": "Anvil", "Price": 50 }]
                },{
                    "Name": "Contoso",
                    "Products": [
                        { "Name": "Elbow Grease", "Price": 99.95 },
                        { "Name": "Headlight Fluid", "Price": 4 }
                    ]
                }]
            }
            """;
            Assert.AreEqual(@"[""Acme Co""]", OracleService.Filter(json, "$.Manufacturers[0].Name").ToStrictUtf8String());
            Assert.AreEqual("[50]", OracleService.Filter(json, "$.Manufacturers[0].Products[0].Price").ToStrictUtf8String());
            Assert.AreEqual(@"[""Elbow Grease""]",
                OracleService.Filter(json, "$.Manufacturers[1].Products[0].Name").ToStrictUtf8String());
            Assert.AreEqual(@"[{""Name"":""Elbow Grease"",""Price"":99.95}]",
                OracleService.Filter(json, "$.Manufacturers[1].Products[0]").ToStrictUtf8String());
        }

        [TestMethod]
        public void TestCreateOracleResponseTx()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var executionFactor = NativeContract.Policy.GetExecFeeFactor(snapshotCache);
            Assert.AreEqual((uint)30, executionFactor);

            var feePerByte = NativeContract.Policy.GetFeePerByte(snapshotCache);
            Assert.AreEqual(1000, feePerByte);

            OracleRequest request = new OracleRequest
            {
                OriginalTxid = UInt256.Zero,
                GasForResponse = 100000000 * 1,
                Url = "https://127.0.0.1/test",
                Filter = "",
                CallbackContract = UInt160.Zero,
                CallbackMethod = "callback",
                UserData = []
            };

            byte Prefix_Transaction = 11;
            snapshotCache.Add(NativeContract.Ledger.CreateStorageKey(Prefix_Transaction, request.OriginalTxid), new(new TransactionState()
            {
                BlockIndex = 1,
                Transaction = new() { ValidUntilBlock = 1 }
            }));

            OracleResponse response = new() { Id = 1, Code = OracleResponseCode.Success, Result = new byte[] { 0x00 } };
            ECPoint[] oracleNodes = [ECCurve.Secp256r1.G];
            var tx = OracleService.CreateResponseTx(snapshotCache, request, response, oracleNodes, ProtocolSettings.Default);

            Assert.AreEqual(166, tx.Size);
            Assert.AreEqual(2198650, tx.NetworkFee);
            Assert.AreEqual(97801350, tx.SystemFee);

            // case (2) The size of attribute exceed the maximum limit

            request.GasForResponse = 0_10000000;
            response.Result = new byte[10250];
            tx = OracleService.CreateResponseTx(snapshotCache, request, response, oracleNodes, ProtocolSettings.Default);
            Assert.AreEqual(165, tx.Size);
            Assert.AreEqual(OracleResponseCode.InsufficientFunds, response.Code);
            Assert.AreEqual(2197650, tx.NetworkFee);
            Assert.AreEqual(7802350, tx.SystemFee);
        }
    }
}
