// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.Extensions;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;

namespace Neo.Plugins.RpcServer.Tests;

public partial class UT_RpcServer
{
    static readonly string NeoScriptHash = "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5";
    static readonly string GasScriptHash = "0xd2a4cff31913016155e38e474a2c06d08be276cf";
    static readonly string NeoTotalSupplyScript = "wh8MC3RvdGFsU3VwcGx5DBT1Y\u002BpAvCg9TQ4FxI6jBbPyoHNA70FifVtS";
    static readonly UInt160 ValidatorScriptHash = Contract
        .CreateSignatureRedeemScript(TestProtocolSettings.SoleNode.StandbyCommittee[0])
        .ToScriptHash();
    static readonly string ValidatorAddress = ValidatorScriptHash.ToAddress(ProtocolSettings.Default.AddressVersion);
    static readonly UInt160 MultisigScriptHash = Contract
        .CreateMultiSigRedeemScript(1, TestProtocolSettings.SoleNode.StandbyCommittee)
        .ToScriptHash();
    static readonly string MultisigAddress = MultisigScriptHash.ToAddress(ProtocolSettings.Default.AddressVersion);

    static readonly JArray validatorSigner = [new JObject()
    {
        ["account"] = ValidatorScriptHash.ToString(),
        ["scopes"] = "CalledByEntry",
    }];
    static readonly JArray multisigSigner = [new JObject()
    {
        ["account"] = MultisigScriptHash.ToString(),
        ["scopes"] = "CalledByEntry",
    }];

    [TestMethod]
    public void TestInvokeFunction()
    {
        JObject resp = (JObject)_rpcServer.InvokeFunction(new JArray(NeoScriptHash, "totalSupply", new JArray([]), validatorSigner, true));
        Assert.AreEqual(resp.Count, 7);
        Assert.AreEqual(resp["script"], NeoTotalSupplyScript);
        Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
        Assert.IsTrue(resp.ContainsProperty("diagnostics"));
        Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], NeoScriptHash);
        Assert.IsTrue(((JArray)resp["diagnostics"]["storagechanges"]).Count == 0);
        Assert.AreEqual(resp["state"], "HALT");
        Assert.AreEqual(resp["exception"], null);
        Assert.AreEqual(((JArray)resp["notifications"]).Count, 0);
        Assert.AreEqual(resp["stack"][0]["type"], "Integer");
        Assert.AreEqual(resp["stack"][0]["value"], "100000000");

        // This call triggers not only NEO but also unclaimed GAS
        resp = (JObject)_rpcServer.InvokeFunction(new JArray(NeoScriptHash, "transfer", new JArray([
            new JObject() { ["type"] = "Hash160", ["value"] = MultisigScriptHash.ToString() },
            new JObject() { ["type"] = "Hash160", ["value"] = ValidatorScriptHash.ToString() },
            new JObject() { ["type"] = "Integer", ["value"] = "1" },
            new JObject() { ["type"] = "Any" },
        ]), multisigSigner, true));
        Assert.AreEqual(resp.Count, 7);
        Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
        Assert.IsTrue(resp.ContainsProperty("diagnostics"));
        Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], NeoScriptHash);
        Assert.IsTrue(((JArray)resp["diagnostics"]["storagechanges"]).Count == 4);
        Assert.AreEqual(resp["state"], "HALT");
        Assert.AreEqual(resp["exception"], null);
        JArray notifications = (JArray)resp["notifications"];
        Assert.AreEqual(notifications.Count, 2);
        Assert.AreEqual(notifications[0]["eventname"].AsString(), "Transfer");
        Assert.AreEqual(notifications[0]["contract"].AsString(), NeoScriptHash);
        Assert.AreEqual(((JArray)resp["notifications"])[0]["state"]["value"][2]["value"], "1");
        Assert.AreEqual(notifications[1]["eventname"].AsString(), "Transfer");
        Assert.AreEqual(notifications[1]["contract"].AsString(), GasScriptHash);
        Assert.AreEqual(((JArray)resp["notifications"])[1]["state"]["value"][2]["value"], "50000000");
    }

    [TestMethod]
    public void TestInvokeScript()
    {
        JObject resp = (JObject)_rpcServer.InvokeScript(new JArray(NeoTotalSupplyScript, validatorSigner, true));
        Assert.AreEqual(resp.Count, 7);
        Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
        Assert.IsTrue(resp.ContainsProperty("diagnostics"));
        Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], NeoScriptHash);
        Assert.AreEqual(resp["state"], "HALT");
        Assert.AreEqual(resp["exception"], null);
        Assert.AreEqual(((JArray)resp["notifications"]).Count, 0);
        Assert.AreEqual(resp["stack"][0]["type"], "Integer");
        Assert.AreEqual(resp["stack"][0]["value"], "100000000");
    }

    [TestMethod]
    public void TestTraverseIterator()
    {
        JObject resp = (JObject)_rpcServer.InvokeFunction(new JArray(NeoScriptHash, "getAllCandidates", new JArray([]), validatorSigner, true));
        string sessionId = resp["session"].AsString();
        string iteratorId = resp["stack"][0]["id"].AsString();
        JArray respArray = (JArray)_rpcServer.TraverseIterator([sessionId, iteratorId, 100]);
        Assert.AreEqual(respArray.Count, 0);
        _rpcServer.TerminateSession([sessionId]);
        try
        {
            respArray = (JArray)_rpcServer.TraverseIterator([sessionId, iteratorId, 100]);
        }
        catch (RpcException e)
        {
            Assert.AreEqual(e.Message, "Unknown session");
        }
    }

    [TestMethod]
    public void TestGetUnclaimedGas()
    {
        JObject resp = (JObject)_rpcServer.GetUnclaimedGas([MultisigAddress]);
        Assert.AreEqual(resp["unclaimed"], "50000000");
        Assert.AreEqual(resp["address"], MultisigAddress);
        resp = (JObject)_rpcServer.GetUnclaimedGas([ValidatorAddress]);
        Assert.AreEqual(resp["unclaimed"], "0");
        Assert.AreEqual(resp["address"], ValidatorAddress);
    }
}
