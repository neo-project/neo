// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractParameterContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Neo.UnitTests.SmartContract;

[TestClass]
public class UT_ContractParameterContext
{
    private static Contract contract = null!;
    private static KeyPair key = null!;

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
        Assert.ThrowsExactly<FormatException>(() => ContractParametersContext.Parse(json, snapshotCache));
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
        var ret = context.GetParameter(tx.Sender, 0)!;
        Assert.AreEqual(new byte[] { 0x01 }.ToHexString(), ((byte[])ret.Value!).ToHexString());
    }

    [TestMethod]
    public void TestGetWitnesses()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        Transaction tx = TestUtils.GetTransaction(UInt160.Parse("0x902e0d38da5e513b6d07c1c55b85e77d3dce8063"));
        var context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        context.Add(contract, 0, new byte[] { 0x01 });

        var witnesses = context.GetWitnesses();
        Assert.HasCount(1, witnesses);
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

        var contract1 = new Contract
        {
            Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
            ParameterList = Array.Empty<ContractParameterType>()
        };
        context = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        Assert.IsFalse(context.AddSignature(contract1, key.PublicKey, [0x01]));

        contract1 = new Contract
        {
            Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
            ParameterList = [ContractParameterType.Signature, ContractParameterType.Signature]
        };
        Assert.ThrowsExactly<NotSupportedException>(() => context.AddSignature(contract1, key.PublicKey, [0x01]));

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
            Nef = (NefFile)RuntimeHelpers.GetUninitializedObject(typeof(NefFile)),
            Manifest = new()
            {
                Name = "TestContract",
                Groups = [],
                SupportedStandards = [],
                Abi = new() { Methods = [new() { Name = ContractBasicMethod.Verify, Parameters = [] }], Events = [] },
                Permissions = [],
                Trusts = WildcardContainer<ContractPermissionDescriptor>.CreateWildcard()
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
}
