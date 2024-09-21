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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.Wallets;
using System;
using System.Text;
using System.Threading;

namespace Neo.Plugins.RpcServer.Tests;

public partial class UT_RpcServer
{
    static readonly string NeoTotalSupplyScript = "wh8MC3RvdGFsU3VwcGx5DBT1Y\u002BpAvCg9TQ4FxI6jBbPyoHNA70FifVtS";
    static readonly string NeoTransferScript = "CxEMFPlu76Cuc\u002BbgteStE4ozsOWTNUdrDBQtYNweHko3YcnMFOes3ceblcI/lRTAHwwIdHJhbnNmZXIMFPVj6kC8KD1NDgXEjqMFs/Kgc0DvQWJ9W1I=";
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
        ["scopes"] = nameof(WitnessScope.CalledByEntry),
        ["allowedcontracts"] = new JArray([NeoToken.NEO.Hash.ToString(), GasToken.GAS.Hash.ToString()]),
        ["allowedgroups"] = new JArray([TestProtocolSettings.SoleNode.StandbyCommittee[0].ToString()]),
        ["rules"] = new JArray([new JObject() { ["action"] = nameof(WitnessRuleAction.Allow), ["condition"] = new JObject { ["type"] = nameof(WitnessConditionType.CalledByEntry) } }]),
    }];
    static readonly JArray multisigSigner = [new JObject()
    {
        ["account"] = MultisigScriptHash.ToString(),
        ["scopes"] = nameof(WitnessScope.CalledByEntry),
    }];

    [TestMethod]
    public void TestInvokeFunction()
    {
        _rpcServer.wallet = _wallet;

        var resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokefunction", new JArray(NeoToken.NEO.Hash.ToString(), "totalSupply", new JArray([]), validatorSigner, true)).GetAwaiter().GetResult();
        Assert.AreEqual(resp.Count, 8);
        Assert.AreEqual(resp["script"], NeoTotalSupplyScript);
        Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
        Assert.IsTrue(resp.ContainsProperty("diagnostics"));
        Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], NeoToken.NEO.Hash.ToString());
        Assert.IsTrue(((JArray)resp["diagnostics"]["storagechanges"]).Count == 0);
        Assert.AreEqual(resp["state"], nameof(VM.VMState.HALT));
        Assert.AreEqual(resp["exception"], null);
        Assert.AreEqual(((JArray)resp["notifications"]).Count, 0);
        Assert.AreEqual(resp["stack"][0]["type"], nameof(Neo.VM.Types.Integer));
        Assert.AreEqual(resp["stack"][0]["value"], "100000000");
        Assert.IsTrue(resp.ContainsProperty("tx"));

        resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokefunction", new JArray(NeoToken.NEO.Hash.ToString(), "symbol")).GetAwaiter().GetResult();
        Assert.AreEqual(resp.Count, 6);
        Assert.IsTrue(resp.ContainsProperty("script"));
        Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
        Assert.AreEqual(resp["state"], nameof(VM.VMState.HALT));
        Assert.AreEqual(resp["exception"], null);
        Assert.AreEqual(((JArray)resp["notifications"]).Count, 0);
        Assert.AreEqual(resp["stack"][0]["type"], nameof(Neo.VM.Types.ByteString));
        Assert.AreEqual(resp["stack"][0]["value"], Convert.ToBase64String(Encoding.UTF8.GetBytes("NEO")));

        // This call triggers not only NEO but also unclaimed GAS
        resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokefunction", new JArray(NeoToken.NEO.Hash.ToString(), "transfer", new JArray([
            new JObject() { ["type"] = nameof(ContractParameterType.Hash160), ["value"] = MultisigScriptHash.ToString() },
            new JObject() { ["type"] = nameof(ContractParameterType.Hash160), ["value"] = ValidatorScriptHash.ToString() },
            new JObject() { ["type"] = nameof(ContractParameterType.Integer), ["value"] = "1" },
            new JObject() { ["type"] = nameof(ContractParameterType.Any) },
        ]), multisigSigner, true)).GetAwaiter().GetResult();
        Assert.AreEqual(resp.Count, 7);
        Assert.AreEqual(resp["script"], NeoTransferScript);
        Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
        Assert.IsTrue(resp.ContainsProperty("diagnostics"));
        Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], NeoToken.NEO.Hash.ToString());
        Assert.IsTrue(((JArray)resp["diagnostics"]["storagechanges"]).Count == 4);
        Assert.AreEqual(resp["state"], nameof(VM.VMState.HALT));
        Assert.AreEqual(resp["exception"], $"The smart contract or address {MultisigScriptHash} ({MultisigAddress}) is not found. " +
                            $"If this is your wallet address and you want to sign a transaction with it, make sure you have opened this wallet.");
        var notifications = (JArray)resp["notifications"];
        Assert.AreEqual(notifications.Count, 2);
        Assert.AreEqual(notifications[0]["eventname"].AsString(), "Transfer");
        Assert.AreEqual(notifications[0]["contract"].AsString(), NeoToken.NEO.Hash.ToString());
        Assert.AreEqual(notifications[0]["state"]["value"][2]["value"], "1");
        Assert.AreEqual(notifications[1]["eventname"].AsString(), "Transfer");
        Assert.AreEqual(notifications[1]["contract"].AsString(), GasToken.GAS.Hash.ToString());
        Assert.AreEqual(notifications[1]["state"]["value"][2]["value"], "50000000");

        _rpcServer.wallet = null;
    }

    [TestMethod]
    public void TestInvokeScript()
    {
        var resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokescript", new JArray(NeoTotalSupplyScript, validatorSigner, true)).GetAwaiter().GetResult();
        Assert.AreEqual(resp.Count, 7);
        Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
        Assert.IsTrue(resp.ContainsProperty("diagnostics"));
        Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], NeoToken.NEO.Hash.ToString());
        Assert.AreEqual(resp["state"], nameof(VM.VMState.HALT));
        Assert.AreEqual(resp["exception"], null);
        Assert.AreEqual(((JArray)resp["notifications"]).Count, 0);
        Assert.AreEqual(resp["stack"][0]["type"], nameof(Neo.VM.Types.Integer));
        Assert.AreEqual(resp["stack"][0]["value"], "100000000");

        resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokescript", new JArray(NeoTransferScript, validatorSigner, true)).GetAwaiter().GetResult();
        Assert.AreEqual(resp.Count, 7);
        Assert.AreEqual(resp["stack"][0]["type"], nameof(Neo.VM.Types.Boolean));
        Assert.AreEqual(resp["stack"][0]["value"], false);
    }

    [TestMethod]
    public async void TestTraverseIterator()
    {
        // GetAllCandidates that should return 0 candidates
        var resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokefunction", new JArray(NeoToken.NEO.Hash.ToString(), "getAllCandidates", new JArray([]), validatorSigner, true)).GetAwaiter().GetResult();
        var sessionId = resp["session"].AsString();
        var iteratorId = resp["stack"][0]["id"].AsString();
        var respArray = (JArray)(JToken)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseiterator", new JArray([sessionId, iteratorId, 100])).GetAwaiter().GetResult();
        Assert.AreEqual(respArray.Count, 0);
        _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "terminatesession", new JArray([sessionId])).GetAwaiter().GetResult();
        Assert.ThrowsException<RpcException>(() => _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseiterator", new JArray([sessionId, iteratorId, 100])).GetAwaiter().GetResult(), "Unknown session");

        // register candidate in snapshot
        resp = (JObject)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokefunction", new JArray(NeoToken.NEO.Hash.ToString(), "registerCandidate",
            new JArray([new JObject()
            {
                ["type"] = nameof(ContractParameterType.PublicKey),
                ["value"] = TestProtocolSettings.SoleNode.StandbyCommittee[0].ToString(),
            }]), validatorSigner, true)).GetAwaiter().GetResult();
        Assert.AreEqual(resp["state"], nameof(VM.VMState.HALT));
        SnapshotCache snapshot = _neoSystem.GetSnapshotCache();
        Transaction? tx = new Transaction
        {
            Nonce = 233,
            ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + _neoSystem.Settings.MaxValidUntilBlockIncrement,
            Signers = [new Signer() { Account = ValidatorScriptHash, Scopes = WitnessScope.CalledByEntry }],
            Attributes = Array.Empty<TransactionAttribute>(),
            Script = Convert.FromBase64String(resp["script"].AsString()),
            Witnesses = null,
        };
        ApplicationEngine engine = ApplicationEngine.Run(tx.Script, snapshot, container: tx, settings: _neoSystem.Settings, gas: 1200_0000_0000);
        engine.SnapshotCache.Commit();

        // GetAllCandidates that should return 1 candidate
        resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokeFunction", new JArray(NeoToken.NEO.Hash.ToString(), "getAllCandidates", new JArray([]), validatorSigner, true)).GetAwaiter().GetResult();
        sessionId = resp["session"].AsString();
        iteratorId = resp["stack"][0]["id"].AsString();
        respArray = (JArray)(JToken)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseIterator", new JArray([sessionId, iteratorId, 100])).GetAwaiter().GetResult();

        Assert.AreEqual(respArray.Count, 1);
        Assert.AreEqual(respArray[0]["type"], nameof(Neo.VM.Types.Struct));
        JArray value = (JArray)respArray[0]["value"];
        Assert.AreEqual(value.Count, 2);
        Assert.AreEqual(value[0]["type"], nameof(Neo.VM.Types.ByteString));
        Assert.AreEqual(value[0]["value"], Convert.ToBase64String(TestProtocolSettings.SoleNode.StandbyCommittee[0].ToArray()));
        Assert.AreEqual(value[1]["type"], nameof(Neo.VM.Types.Integer));
        Assert.AreEqual(value[1]["value"], "0");

        // No result when traversed again
        respArray = (JArray)(JToken)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseIterator", new JArray([sessionId, iteratorId, 100])).GetAwaiter().GetResult();
        Assert.AreEqual(respArray.Count, 0);

        // GetAllCandidates again
        resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokeFunction", new JArray(NeoToken.NEO.Hash.ToString(), "getAllCandidates", new JArray([]), validatorSigner, true)).GetAwaiter().GetResult();
        sessionId = resp["session"].AsString();
        iteratorId = resp["stack"][0]["id"].AsString();

        // Insufficient result count limit
        respArray = (JArray)(JToken)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseIterator", new JArray([sessionId, iteratorId, 0])).GetAwaiter().GetResult();
        Assert.AreEqual(respArray.Count, 0);
        respArray = (JArray)(JToken)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseIterator", new JArray([sessionId, iteratorId, 1])).GetAwaiter().GetResult();
        Assert.AreEqual(respArray.Count, 1);
        respArray = (JArray)(JToken)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseIterator", new JArray([sessionId, iteratorId, 1])).GetAwaiter().GetResult();
        Assert.AreEqual(respArray.Count, 0);

        // Mocking session timeout
        Thread.Sleep((int)_rpcServerSettings.SessionExpirationTime.TotalMilliseconds + 1);
        // build another session that did not expire
        resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "InvokeFunction", new JArray(NeoToken.NEO.Hash.ToString(), "getAllCandidates", new JArray([]), validatorSigner, true)).GetAwaiter().GetResult();
        string notExpiredSessionId = resp["session"].AsString();
        string notExpiredIteratorId = resp["stack"][0]["id"].AsString();
        _rpcServer.OnTimer(new object());
        Assert.ThrowsException<RpcException>(() => _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseIterator", new JArray([sessionId, iteratorId, 100])).GetAwaiter().GetResult(), "Unknown session");
        // If you want to run the following line without exception,
        // DO NOT BREAK IN THE DEBUGGER, because the session expires quickly
        respArray = (JArray)(JToken)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseIterator", new JArray([notExpiredSessionId, notExpiredIteratorId, 1])).GetAwaiter().GetResult();
        Assert.AreEqual(respArray.Count, 1);

        // Mocking disposal
        resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "invokeFunction", new JArray(NeoToken.NEO.Hash.ToString(), "getAllCandidates", new JArray([]), validatorSigner, true)).GetAwaiter().GetResult();
        sessionId = resp["session"].AsString();
        iteratorId = resp["stack"][0]["id"].AsString();
        _rpcServer.Dispose_SmartContract();
        Assert.ThrowsException<RpcException>(() => _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "traverseIterator", new JArray([sessionId, iteratorId, 100])).GetAwaiter().GetResult(), "Unknown session");
    }

    [TestMethod]
    public void TestGetUnclaimedGas()
    {
        var resp = _rpcServer.ProcessRequestMock(_mockHttpContext.Object, "getunclaimedgas", new JArray([MultisigAddress])).GetAwaiter().GetResult();
        Assert.AreEqual(resp["unclaimed"], "50000000");
        Assert.AreEqual(resp["address"], MultisigAddress);
        resp = (JObject)_rpcServer.ProcessRequestMock(_mockHttpContext.Object, "getunclaimedgas", new JArray([ValidatorAddress])).GetAwaiter().GetResult();
        Assert.AreEqual(resp["unclaimed"], "0");
        Assert.AreEqual(resp["address"], ValidatorAddress);
    }
}
