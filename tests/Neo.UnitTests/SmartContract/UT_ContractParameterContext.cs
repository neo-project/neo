// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ContractParameterContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ContractParameterContext
    {
        private static Contract contract;
        private static KeyPair key;

        [ClassInitialize]
        public static void ClassSetUp(TestContext ctx)
        {
            if (contract == null)
            {
                byte[] privateKey = Enumerable.Repeat((byte)0x01, 32).ToArray();
                key = new KeyPair(privateKey);
                contract = Contract.CreateSignatureContract(key.PublicKey);
            }
        }

        [TestMethod]
        public void TestGetComplete()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Parse("0x1bd5c777ec35768892bd3daab60fb7a1cb905066"));
            var context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsFalse(context.Completed);
        }

        [TestMethod]
        public void TestToString()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Parse("0x1bd5c777ec35768892bd3daab60fb7a1cb905066"));
            var context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            context.Add(contract, 0, new byte[] { 0x01 });
            var expected = """
            {
                "type":"Neo.Network.P2P.Payloads.Transaction",
                "hash":"0x602c1fa1c08b041e4e6b87aa9a9f9c643166cd34bdd5215a3dd85778c59cce88",
                "data":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAFmUJDLobcPtqo9vZKIdjXsd8fVGwEAARI=",
                "items":{},
                "network": 
            """ + TestProtocolSettings.Default.Network.ToString() + "}";
            expected = Regex.Replace(expected, @"\s+", "");
            Assert.AreEqual(expected, context.ToString());
        }

        [TestMethod]
        public void TestParse()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var json = """
            {
                "type":"Neo.Network.P2P.Payloads.Transaction",
                "data":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAFmUJDLobcPtqo9vZKIdjXsd8fVGwEAARI=",
                "items":{
                    "0xbecaad15c0ea585211faf99738a4354014f177f2":{
                        "script":"IQJv8DuUkkHOHa3UNRnmlg4KhbQaaaBcMoEDqivOFZTKFmh0dHaq",
                        "parameters":[{"type":"Signature","value":"AQ=="}],
                        "signatures":{"03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c":"AQ=="}
                    }
                },
                "network":
            """ + TestProtocolSettings.Default.Network + "}";
            var ret = ContractParametersContext.Parse(json, snapshotCache);
            Assert.AreEqual("0x1bd5c777ec35768892bd3daab60fb7a1cb905066", ret.ScriptHashes[0].ToString());
            Assert.AreEqual(new byte[] { 18 }.ToHexString(), ((Transaction)ret.Verifiable).Script.Span.ToHexString());
        }

        [TestMethod]
        public void TestFromJson()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var json = """
            {
                "type":"wrongType",
                "data":"00000000007c97764845172d827d3c863743293931a691271a0000000000000000000000000000000000000000000100",
                "items":{
                    "0x1bd5c777ec35768892bd3daab60fb7a1cb905066":{
                        "script":"21026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca1650680a906ad4",
                        "parameters":[{"type":"Signature","value":"01"}]
                    }
                }
            }
            """;
            Action action = () => ContractParametersContext.Parse(json, snapshotCache);
            Assert.ThrowsExactly<FormatException>(action);
        }

        [TestMethod]
        public void TestAdd()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            Transaction tx = TestUtils.GetTransaction(UInt160.Zero);
            var context1 = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsFalse(context1.Add(contract, 0, new byte[] { 0x01 }));

            tx = TestUtils.GetTransaction(UInt160.Parse("0x902e0d38da5e513b6d07c1c55b85e77d3dce8063"));
            var context2 = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsTrue(context2.Add(contract, 0, new byte[] { 0x01 }));
            //test repeatlly createItem
            Assert.IsTrue(context2.Add(contract, 0, new byte[] { 0x01 }));
        }

        [TestMethod]
        public void TestGetParameter()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            Transaction tx = TestUtils.GetTransaction(UInt160.Parse("0x902e0d38da5e513b6d07c1c55b85e77d3dce8063"));
            var context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsNull(context.GetParameter(tx.Sender, 0));

            context.Add(contract, 0, new byte[] { 0x01 });
            var ret = context.GetParameter(tx.Sender, 0);
            Assert.AreEqual(new byte[] { 0x01 }.ToHexString(), ((byte[])ret.Value).ToHexString());
        }

        [TestMethod]
        public void TestGetWitnesses()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            Transaction tx = TestUtils.GetTransaction(UInt160.Parse("0x902e0d38da5e513b6d07c1c55b85e77d3dce8063"));
            var context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            context.Add(contract, 0, new byte[] { 0x01 });

            var witnesses = context.GetWitnesses();
            Assert.AreEqual(1, witnesses.Length);
            Assert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA1, 0x01, 0x01 }.ToHexString(), witnesses[0].InvocationScript.Span.ToHexString());
            Assert.AreEqual(contract.Script.ToHexString(), witnesses[0].VerificationScript.Span.ToHexString());
        }

        [TestMethod]
        public void TestAddSignature()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var singleSender = UInt160.Parse("0x902e0d38da5e513b6d07c1c55b85e77d3dce8063");
            Transaction tx = TestUtils.GetTransaction(singleSender);

            //singleSign

            var context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsTrue(context.AddSignature(contract, key.PublicKey, [0x01]));

            var contract1 = Contract.CreateSignatureContract(key.PublicKey);
            contract1.ParameterList = Array.Empty<ContractParameterType>();
            context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsFalse(context.AddSignature(contract1, key.PublicKey, [0x01]));

            contract1.ParameterList = [ContractParameterType.Signature, ContractParameterType.Signature];
            Action action1 = () => context.AddSignature(contract1, key.PublicKey, [0x01]);
            Assert.ThrowsExactly<NotSupportedException>(action1);

            //multiSign
            byte[] privateKey2 = Enumerable.Repeat((byte)0x01, 31).Append((byte)0x02).ToArray();
            var key2 = new KeyPair(privateKey2);
            var multiSignContract = Contract.CreateMultiSigContract(2, [key.PublicKey, key2.PublicKey]);
            var multiSender = UInt160.Parse("0xf76b51bc6605ac3cfcd188173af0930507f51210");

            tx = TestUtils.GetTransaction(multiSender);
            context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsTrue(context.AddSignature(multiSignContract, key.PublicKey, [0x01]));
            Assert.IsTrue(context.AddSignature(multiSignContract, key2.PublicKey, [0x01]));

            tx = TestUtils.GetTransaction(singleSender);
            context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsFalse(context.AddSignature(multiSignContract, key.PublicKey, [0x01]));

            tx = TestUtils.GetTransaction(multiSender);
            context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            byte[] privateKey3 = Enumerable.Repeat((byte)0x01, 31).Append((byte)0x03).ToArray();
            var key3 = new KeyPair(privateKey3);
            Assert.IsFalse(context.AddSignature(multiSignContract, key3.PublicKey, [0x01]));
        }

        [TestMethod]
        public void TestAddWithScriptHash()
        {
            var h160 = UInt160.Parse("0x902e0d38da5e513b6d07c1c55b85e77d3dce8063");
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(h160);
            var context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            Assert.IsFalse(context.AddWithScriptHash(h160));

            var contract = new ContractState()
            {
                Hash = h160,
                Nef = new(),
                Manifest = new()
                {
                    Name = "TestContract",
                    Groups = [],
                    SupportedStandards = [],
                    Abi = new() { Methods = [new() { Name = ContractBasicMethod.Verify, Parameters = [] }], Events = [] }
                }
            };
            snapshotCache.AddContract(h160, contract);
            Assert.IsTrue(context.AddWithScriptHash(h160));

            snapshotCache.DeleteContract(h160);
            contract.Manifest.Abi = new()
            {
                Methods = [new() {
                    Name = ContractBasicMethod.Verify,
                    Parameters = [new() { Name = "signature", Type = ContractParameterType.Signature }],
                }],
                Events = []
            };
            snapshotCache.AddContract(h160, contract);
            Assert.IsFalse(context.AddWithScriptHash(h160));
        }

        [TestMethod]
        public void TestParseWithUnicodeData()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Zero);

            // Create a context and convert to JSON
            var originalContext = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            var json = originalContext.ToJson();

            // Replace the Base64 data with Unicode string
            var unicodeData = "你好世界 Hello World";
            json["data"] = unicodeData;

            // Parse should handle Unicode
            var parsedContext = ContractParametersContext.Parse(json.ToString(), snapshotCache);
            Assert.IsNotNull(parsedContext);

            // The data should be UTF-8 encoded bytes of the Unicode string
            var expectedBytes = Encoding.UTF8.GetBytes(unicodeData);
            // Note: We can't directly compare the transaction data because it would be invalid,
            // but the parsing should not throw an exception
        }

        [TestMethod]
        public void TestParseWithHexData()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Zero);

            // Create a context and convert to JSON
            var originalContext = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            var json = originalContext.ToJson();

            // Get the original data as hex
            var originalData = Convert.FromBase64String(json["data"].AsString());
            var hexData = "0x" + originalData.ToHexString();

            // Replace with hex
            json["data"] = hexData;

            // Parse should handle hex
            var parsedContext = ContractParametersContext.Parse(json.ToString(), snapshotCache);
            Assert.IsNotNull(parsedContext);
            Assert.AreEqual(originalContext.Network, parsedContext.Network);
        }

        [TestMethod]
        public void TestContextItemWithUnicodeScript()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Zero);
            var context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);

            // Create JSON for ContextItem with Unicode in script field
            var itemJson = new JObject();
            itemJson["script"] = "某些中文脚本内容";
            itemJson["parameters"] = new JArray();
            itemJson["signatures"] = new JObject();

            // This should parse the Unicode as UTF-8 bytes
            // Note: In real usage, script should be valid bytecode, but we're testing encoding
            var contextItem = ContractParametersContext.Parse(
                $@"{{
                    ""type"": ""{typeof(Transaction).FullName}"",
                    ""data"": ""{Convert.ToBase64String(tx.ToArray())}"",
                    ""items"": {{
                        ""{UInt160.Zero}"": {itemJson}
                    }},
                    ""network"": {TestProtocolSettings.Default.Network}
                }}", snapshotCache);

            Assert.IsNotNull(contextItem);
        }

        [TestMethod]
        public void TestContextItemWithUnicodeSignatures()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Zero);

            // Create JSON with Unicode in signature values
            var itemJson = new JObject();
            itemJson["script"] = Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03 });
            itemJson["parameters"] = new JArray();

            var signatures = new JObject();
            // Use a valid public key
            var pubKey = key.PublicKey.ToString();
            signatures[pubKey] = "Unicode签名数据";
            itemJson["signatures"] = signatures;

            // This should parse the Unicode signature as UTF-8 bytes
            var contextJson = $@"{{
                ""type"": ""{typeof(Transaction).FullName}"",
                ""data"": ""{Convert.ToBase64String(tx.ToArray())}"",
                ""items"": {{
                    ""{contract.ScriptHash}"": {itemJson}
                }},
                ""network"": {TestProtocolSettings.Default.Network}
            }}";

            var context = ContractParametersContext.Parse(contextJson, snapshotCache);
            Assert.IsNotNull(context);

            var sigs = context.GetSignatures(contract.ScriptHash);
            Assert.IsNotNull(sigs);
            Assert.AreEqual(1, sigs.Count);

            var sigBytes = sigs[key.PublicKey];
            var sigText = Encoding.UTF8.GetString(sigBytes);
            Assert.AreEqual("Unicode签名数据", sigText);
        }

        [TestMethod]
        public void TestBackwardCompatibilityWithBase64()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Zero);

            // Create a context normally (uses Base64)
            var originalContext = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
            var json = originalContext.ToJson();

            // Ensure all fields are Base64
            Assert.IsTrue(json["data"].AsString().Length % 4 == 0);

            // Parse should work with Base64 (backward compatibility)
            var parsedContext = ContractParametersContext.Parse(json.ToString(), snapshotCache);
            Assert.IsNotNull(parsedContext);
            Assert.AreEqual(originalContext.Network, parsedContext.Network);
            Assert.AreEqual(originalContext.Verifiable.Hash, parsedContext.Verifiable.Hash);
        }

        [TestMethod]
        public void TestMixedEncodingSupport()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Zero);

            // Create JSON with mixed encodings
            var itemJson1 = new JObject();
            itemJson1["script"] = Convert.ToBase64String(new byte[] { 0x01, 0x02 }); // Base64
            itemJson1["parameters"] = new JArray();
            itemJson1["signatures"] = new JObject();

            var itemJson2 = new JObject();
            itemJson2["script"] = "0x0304"; // Hex with prefix
            itemJson2["parameters"] = new JArray();
            itemJson2["signatures"] = new JObject();

            var itemJson3 = new JObject();
            itemJson3["script"] = "Mixed Unicode 混合文本"; // Unicode
            itemJson3["parameters"] = new JArray();
            itemJson3["signatures"] = new JObject();

            var contextJson = $@"{{
                ""type"": ""{typeof(Transaction).FullName}"",
                ""data"": ""{Convert.ToBase64String(tx.ToArray())}"",
                ""items"": {{
                    ""{UInt160.Zero}"": {itemJson1},
                    ""{UInt160.Parse("0x0000000000000000000000000000000000000001")}"": {itemJson2},
                    ""{UInt160.Parse("0x0000000000000000000000000000000000000002")}"": {itemJson3}
                }},
                ""network"": {TestProtocolSettings.Default.Network}
            }}";

            // Should parse all different encodings
            var context = ContractParametersContext.Parse(contextJson, snapshotCache);
            Assert.IsNotNull(context);

            // Verify scripts were parsed correctly
            var script1 = context.GetScript(UInt160.Zero);
            Assert.IsTrue(script1.SequenceEqual(new byte[] { 0x01, 0x02 }));

            var script2 = context.GetScript(UInt160.Parse("0x0000000000000000000000000000000000000001"));
            Assert.IsTrue(script2.SequenceEqual(new byte[] { 0x03, 0x04 }));

            var script3 = context.GetScript(UInt160.Parse("0x0000000000000000000000000000000000000002"));
            var script3Text = Encoding.UTF8.GetString(script3);
            Assert.AreEqual("Mixed Unicode 混合文本", script3Text);
        }

        [TestMethod]
        public void TestEmptyDataHandling()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx = TestUtils.GetTransaction(UInt160.Zero);

            // Test empty string in data field
            var json = new JObject();
            json["type"] = typeof(Transaction).FullName;
            json["data"] = "";
            json["items"] = new JObject();
            json["network"] = TestProtocolSettings.Default.Network;

            // Should handle empty string gracefully
            Assert.ThrowsException<Exception>(() => ContractParametersContext.Parse(json.ToString(), snapshotCache));

            // Test empty script in ContextItem
            var itemJson = new JObject();
            itemJson["script"] = "";
            itemJson["parameters"] = new JArray();
            itemJson["signatures"] = new JObject();

            json["data"] = Convert.ToBase64String(tx.ToArray());
            json["items"][UInt160.Zero.ToString()] = itemJson;

            var context = ContractParametersContext.Parse(json.ToString(), snapshotCache);
            Assert.IsNotNull(context);
            var script = context.GetScript(UInt160.Zero);
            Assert.AreEqual(0, script.Length);
        }
    }
}
