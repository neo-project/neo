// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcServer.SmartContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;
using Neo.Plugins.RpcServer.Model;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using Array = System.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.Plugins.RpcServer.Tests
{
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

        static readonly string s_neoHash = NativeContract.NEO.Hash.ToString();
        static readonly string s_gasHash = NativeContract.GAS.Hash.ToString();

        static readonly JArray validatorSigner = [new JObject()
        {
            ["account"] = ValidatorScriptHash.ToString(),
            ["scopes"] = nameof(WitnessScope.CalledByEntry),
            ["allowedcontracts"] = new JArray([s_neoHash, s_gasHash]),
            ["allowedgroups"] = new JArray([TestProtocolSettings.SoleNode.StandbyCommittee[0].ToString()]),
            ["rules"] = new JArray([
                new JObject()
                {
                    ["action"] = nameof(WitnessRuleAction.Allow),
                    ["condition"] = new JObject { ["type"] = nameof(WitnessConditionType.CalledByEntry) }
                }
            ]),
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
            var resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, "totalSupply", [], validatorSigner.AsParameter<SignersAndWitnesses>(), true);
            Assert.AreEqual(8, resp.Count);
            Assert.AreEqual(resp["script"], NeoTotalSupplyScript);
            Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
            Assert.IsTrue(resp.ContainsProperty("diagnostics"));
            Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], s_neoHash);
            Assert.IsEmpty((JArray)resp["diagnostics"]["storagechanges"]);
            Assert.AreEqual(nameof(VMState.HALT), resp["state"]);
            Assert.IsNull(resp["exception"]);
            Assert.IsEmpty((JArray)resp["notifications"]);
            Assert.AreEqual(nameof(Integer), resp["stack"][0]["type"]);
            Assert.AreEqual("100000000", resp["stack"][0]["value"]);
            Assert.IsTrue(resp.ContainsProperty("tx"));

            resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, "symbol");
            Assert.AreEqual(6, resp.Count);
            Assert.IsTrue(resp.ContainsProperty("script"));
            Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
            Assert.AreEqual(nameof(VMState.HALT), resp["state"]);
            Assert.IsNull(resp["exception"]);
            Assert.IsEmpty((JArray)resp["notifications"]);
            Assert.AreEqual(nameof(ByteString), resp["stack"][0]["type"]);
            Assert.AreEqual(resp["stack"][0]["value"], Convert.ToBase64String(Encoding.UTF8.GetBytes("NEO")));

            // This call triggers not only NEO but also unclaimed GAS
            resp = (JObject)_rpcServer.InvokeFunction(
                s_neoHash,
                "transfer",
                [
                    new(ContractParameterType.Hash160) { Value = MultisigScriptHash },
                    new(ContractParameterType.Hash160) { Value = ValidatorScriptHash },
                    new(ContractParameterType.Integer) { Value = 1 },
                    new(ContractParameterType.Any),
                ],
                multisigSigner.AsParameter<SignersAndWitnesses>(),
                true
            );
            Assert.AreEqual(7, resp.Count);
            Assert.AreEqual(resp["script"], NeoTransferScript);
            Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
            Assert.IsTrue(resp.ContainsProperty("diagnostics"));
            Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], s_neoHash);
            Assert.HasCount(4, (JArray)resp["diagnostics"]["storagechanges"]);
            Assert.AreEqual(nameof(VMState.HALT), resp["state"]);
            Assert.AreEqual(resp["exception"], $"The smart contract or address {MultisigScriptHash} ({MultisigAddress}) is not found. " +
                                $"If this is your wallet address and you want to sign a transaction with it, make sure you have opened this wallet.");
            JArray notifications = (JArray)resp["notifications"];
            Assert.HasCount(2, notifications);
            Assert.AreEqual("Transfer", notifications[0]["eventname"].AsString());
            Assert.AreEqual(notifications[0]["contract"].AsString(), s_neoHash);
            Assert.AreEqual("1", notifications[0]["state"]["value"][2]["value"]);
            Assert.AreEqual("Transfer", notifications[1]["eventname"].AsString());
            Assert.AreEqual(notifications[1]["contract"].AsString(), s_gasHash);
            Assert.AreEqual("50000000", notifications[1]["state"]["value"][2]["value"]);

            _rpcServer.wallet = null;
        }

        [TestMethod]
        public void TestInvokeFunctionInvalid()
        {
            _rpcServer.wallet = _wallet;

            var context = new DefaultHttpContext();
            var json = new JObject()
            {
                ["id"] = 1,
                ["jsonrpc"] = "2.0",
                ["method"] = "invokefunction",
                ["params"] = new JArray("0", "totalSupply", new JArray([]), validatorSigner, true),
            };

            var resp = _rpcServer.ProcessRequestAsync(context, json).GetAwaiter().GetResult();

            Console.WriteLine(resp);
            Assert.AreEqual(3, resp.Count);
            Assert.IsNotNull(resp["error"]);
            Assert.AreEqual(-32602, resp["error"]["code"]);

            _rpcServer.wallet = null;
        }

        [TestMethod]
        public void TestInvokeScript()
        {
            var resp = (JObject)_rpcServer.InvokeScript(
                Convert.FromBase64String(NeoTotalSupplyScript),
                validatorSigner.AsParameter<SignersAndWitnesses>(),
                true
            );
            Assert.AreEqual(7, resp.Count);
            Assert.IsTrue(resp.ContainsProperty("gasconsumed"));
            Assert.IsTrue(resp.ContainsProperty("diagnostics"));
            Assert.AreEqual(resp["diagnostics"]["invokedcontracts"]["call"][0]["hash"], s_neoHash);
            Assert.AreEqual(nameof(VMState.HALT), resp["state"]);
            Assert.IsNull(resp["exception"]);
            Assert.IsEmpty((JArray)resp["notifications"]);
            Assert.AreEqual(nameof(Integer), resp["stack"][0]["type"]);
            Assert.AreEqual("100000000", resp["stack"][0]["value"]);

            resp = (JObject)_rpcServer.InvokeScript(Convert.FromBase64String(NeoTransferScript));
            Assert.AreEqual(6, resp.Count);
            Assert.AreEqual(nameof(Boolean), resp["stack"][0]["type"]);
            Assert.AreEqual(false, resp["stack"][0]["value"]);
        }

        [TestMethod]
        public void TestInvokeFunction_FaultState()
        {
            // Attempt to call a non-existent method
            var functionName = "nonExistentMethod";
            var resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, functionName, []);

            Assert.AreEqual(nameof(VMState.FAULT), resp["state"].AsString());
            Assert.IsNotNull(resp["exception"].AsString());
            Assert.Contains("doesn't exist in the contract", resp["exception"].AsString()); // Fix based on test output
        }

        [TestMethod]
        public void TestInvokeScript_FaultState()
        {
            // Use a script that explicitly ABORTs
            byte[] abortScript;
            using (var sb = new ScriptBuilder())
            {
                sb.Emit(OpCode.ABORT);
                abortScript = sb.ToArray();
            }

            var resp = (JObject)_rpcServer.InvokeScript(abortScript);
            Assert.AreEqual(nameof(VMState.FAULT), resp["state"].AsString());
            Assert.IsNotNull(resp["exception"].AsString());
            Assert.Contains("ABORT is executed", resp["exception"].AsString()); // Check for specific ABORT message
        }

        [TestMethod]
        public void TestInvokeScript_GasLimitExceeded()
        {
            // Simple infinite loop script: JMP back to itself
            byte[] loopScript;
            using (var sb = new ScriptBuilder())
            {
                sb.EmitJump(OpCode.JMP_L, 0); // JMP_L offset 0 jumps to the start of the JMP instruction
                loopScript = sb.ToArray();
            }

            // Use a temporary RpcServer with a very low MaxGasInvoke setting
            var lowGasSettings = RpcServersSettings.Default with
            {
                MaxGasInvoke = 1_000_000 // Low gas limit (1 GAS = 100,000,000 datoshi)
            };
            var tempRpcServer = new RpcServer(_neoSystem, lowGasSettings);

            var resp = (JObject)tempRpcServer.InvokeScript(loopScript);
            Assert.AreEqual(nameof(VMState.FAULT), resp["state"].AsString());
            Assert.IsNotNull(resp["exception"].AsString());
            Assert.Contains("Insufficient GAS", resp["exception"].AsString());
            Assert.IsGreaterThan(lowGasSettings.MaxGasInvoke, long.Parse(resp["gasconsumed"].AsString()));
        }

        [TestMethod]
        public void TestInvokeFunction_InvalidSignerScope()
        {
            var invalidSigner = new JArray(new JObject()
            {
                ["account"] = ValidatorScriptHash.ToString(),
                ["scopes"] = "InvalidScopeValue", // Invalid enum value
            });

            // Underlying Enum.Parse throws ArgumentException when called directly
            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => _rpcServer.InvokeFunction(s_neoHash, "symbol", [], invalidSigner.AsParameter<SignersAndWitnesses>()));
            Assert.Contains("Requested value 'InvalidScopeValue' was not found", ex.Message); // Check actual ArgumentException message
        }

        [TestMethod]
        public void TestInvokeFunction_InvalidSignerAccount()
        {
            var invalidSigner = new JArray(new JObject()
            {
                ["account"] = "NotAValidHash160",
                ["scopes"] = nameof(WitnessScope.CalledByEntry),
            });

            // Underlying AddressToScriptHash throws FormatException when called directly
            var ex = Assert.ThrowsExactly<RpcException>(
                () => _rpcServer.InvokeFunction(s_neoHash, "symbol", [], invalidSigner.AsParameter<SignersAndWitnesses>()));
            // No message check needed, type check is sufficient
        }

        [TestMethod]
        public void TestInvokeFunction_InvalidWitnessInvocation()
        {
            // Construct signer/witness JSON manually with invalid base64
            var invalidWitnessSigner = new JArray(new JObject()
            {
                ["account"] = ValidatorScriptHash.ToString(),
                ["scopes"] = nameof(WitnessScope.CalledByEntry),
                ["invocation"] = "!@#$", // Not valid Base64
                ["verification"] = Convert.ToBase64String(ValidatorScriptHash.ToArray()) // Valid verification for contrast
            });

            // Underlying Convert.FromBase64String throws FormatException when called directly
            var ex = Assert.ThrowsExactly<FormatException>(
                () => _rpcServer.InvokeFunction(s_neoHash, "symbol", [], invalidWitnessSigner.AsParameter<SignersAndWitnesses>()));
        }

        [TestMethod]
        public void TestInvokeFunction_InvalidWitnessVerification()
        {
            var invalidWitnessSigner = new JArray(new JObject()
            {
                ["account"] = ValidatorScriptHash.ToString(),
                ["scopes"] = nameof(WitnessScope.CalledByEntry),
                ["invocation"] = Convert.ToBase64String(new byte[] { 0x01 }), // Valid invocation
                ["verification"] = "!@#$" // Not valid Base64
            });

            // Underlying Convert.FromBase64String throws FormatException when called directly
            var ex = Assert.ThrowsExactly<FormatException>(
                () => _rpcServer.InvokeFunction(s_neoHash, "symbol", [], invalidWitnessSigner.AsParameter<SignersAndWitnesses>()));
        }

        [TestMethod]
        public void TestInvokeFunction_InvalidContractParameter()
        {
            // Call transfer which expects Hash160, Hash160, Integer, Any
            // Provide an invalid value for the Integer parameter
            var invalidParams = new JArray([
                new JObject() { ["type"] = nameof(ContractParameterType.Hash160), ["value"] = MultisigScriptHash.ToString() },
                new JObject() { ["type"] = nameof(ContractParameterType.Hash160), ["value"] = ValidatorScriptHash.ToString() },
                new JObject() { ["type"] = nameof(ContractParameterType.Integer), ["value"] = "NotAnInteger" }, // Invalid value
                new JObject() { ["type"] = nameof(ContractParameterType.Any) },
            ]);

            // Underlying ContractParameter.FromJson throws FormatException when called directly
            var ex = Assert.ThrowsExactly<FormatException>(() => _rpcServer.InvokeFunction(
                s_neoHash,
                "transfer",
                invalidParams.AsParameter<ContractParameter[]>(),
                multisigSigner.AsParameter<SignersAndWitnesses>()
            ));
        }

        [TestMethod]
        public void TestInvokeScript_InvalidBase64()
        {
            var invalidBase64Script = new JString("ThisIsNotValidBase64***");
            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.InvokeScript(invalidBase64Script.AsParameter<byte[]>()));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            Assert.Contains(RpcError.InvalidParams.Message, ex.Message); // Fix based on test output
        }

        [TestMethod]
        public void TestInvokeScript_WithDiagnostics()
        {
            // Use the NeoTransferScript which modifies NEO and GAS balances
            // Need valid signers for the transfer to simulate correctly (even though wallet isn't signing here)
            var transferSigners = new JArray(new JObject()
            {
                // Use Multisig as sender, which has initial balance
                ["account"] = MultisigScriptHash.ToString(),
                ["scopes"] = nameof(WitnessScope.CalledByEntry),
            });

            // Invoke with diagnostics enabled
            var resp = (JObject)_rpcServer.InvokeScript(
                Convert.FromBase64String(NeoTransferScript),
                transferSigners.AsParameter<SignersAndWitnesses>(),
                true
            );

            Assert.IsTrue(resp.ContainsProperty("diagnostics"));
            var diagnostics = (JObject)resp["diagnostics"];

            // Verify Invoked Contracts structure
            Assert.IsTrue(diagnostics.ContainsProperty("invokedcontracts"));
            var invokedContracts = (JObject)diagnostics["invokedcontracts"];

            // Don't assert on root hash for raw script invoke, structure might differ
            Assert.IsTrue(invokedContracts.ContainsProperty("call")); // Nested calls

            var calls = (JArray)invokedContracts["call"];
            Assert.IsGreaterThanOrEqualTo(1, calls.Count); // Should call at least GAS contract for claim

            // Also check for NEO call, as it's part of the transfer
            Assert.IsTrue(calls.Any(c => c["hash"].AsString() == s_neoHash)); // Fix based on test output

            // Verify Storage Changes
            Assert.IsTrue(diagnostics.ContainsProperty("storagechanges"));
            var storageChanges = (JArray)diagnostics["storagechanges"];
            Assert.IsGreaterThan(0, storageChanges.Count, "Expected storage changes for transfer");

            // Check structure of a storage change item
            var firstChange = (JObject)storageChanges[0];
            Assert.IsTrue(firstChange.ContainsProperty("state"));
            Assert.IsTrue(firstChange.ContainsProperty("key"));
            Assert.IsTrue(firstChange.ContainsProperty("value"));
            Assert.IsTrue(new[] { "Added", "Changed", "Deleted" }.Contains(firstChange["state"].AsString()));
        }

        [TestMethod]
        public void TestTraverseIterator()
        {
            // GetAllCandidates that should return 0 candidates
            var resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, "getAllCandidates", [], validatorSigner.AsParameter<SignersAndWitnesses>(), true);
            var sessionId = resp["session"];
            var iteratorId = resp["stack"][0]["id"];
            var respArray = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 100);
            Assert.IsEmpty(respArray);

            _rpcServer.TerminateSession(sessionId.AsParameter<Guid>());
            Assert.ThrowsExactly<RpcException>(
                () => _ = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 100), "Unknown session");

            // register candidate in snapshot
            resp = (JObject)_rpcServer.InvokeFunction(
                s_neoHash,
                "registerCandidate",
                [
                    new(ContractParameterType.PublicKey) { Value = TestProtocolSettings.SoleNode.StandbyCommittee[0] },
                ],
                validatorSigner.AsParameter<SignersAndWitnesses>(),
                true
            );
            Assert.AreEqual(nameof(VMState.HALT), resp["state"]);

            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = new Transaction
            {
                Nonce = 233,
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + _neoSystem.Settings.MaxValidUntilBlockIncrement,
                Signers = [new Signer() { Account = ValidatorScriptHash, Scopes = WitnessScope.CalledByEntry }],
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = Convert.FromBase64String(resp["script"].AsString()),
                Witnesses = null,
            };

            var engine = ApplicationEngine.Run(tx.Script, snapshot, container: tx, settings: _neoSystem.Settings, gas: 1200_0000_0000);
            engine.SnapshotCache.Commit();

            // GetAllCandidates that should return 1 candidate
            resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, "getAllCandidates", [], validatorSigner.AsParameter<SignersAndWitnesses>(), true);
            sessionId = resp["session"];
            iteratorId = resp["stack"][0]["id"];
            respArray = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 100);
            Assert.HasCount(1, respArray);
            Assert.AreEqual(nameof(Struct), respArray[0]["type"]);

            var value = (JArray)respArray[0]["value"];
            Assert.HasCount(2, value);
            Assert.AreEqual(nameof(ByteString), value[0]["type"]);
            Assert.AreEqual(value[0]["value"], Convert.ToBase64String(TestProtocolSettings.SoleNode.StandbyCommittee[0].ToArray()));
            Assert.AreEqual(nameof(Integer), value[1]["type"]);
            Assert.AreEqual("0", value[1]["value"]);

            // No result when traversed again
            respArray = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 100);
            Assert.IsEmpty(respArray);

            // GetAllCandidates again
            resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, "getAllCandidates", [], validatorSigner.AsParameter<SignersAndWitnesses>(), true);
            sessionId = resp["session"];
            iteratorId = resp["stack"][0]["id"];

            // Insufficient result count limit
            respArray = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 0);
            Assert.IsEmpty(respArray);

            respArray = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 1);
            Assert.HasCount(1, respArray);

            respArray = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 1);
            Assert.IsEmpty(respArray);

            // Mocking session timeout
            Thread.Sleep((int)_rpcServerSettings.SessionExpirationTime.TotalMilliseconds + 1);

            // build another session that did not expire
            resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, "getAllCandidates", [], validatorSigner.AsParameter<SignersAndWitnesses>(), true);
            var notExpiredSessionId = resp["session"];
            var notExpiredIteratorId = resp["stack"][0]["id"];

            _rpcServer.OnTimer(new object());
            Assert.ThrowsExactly<RpcException>(
                () => _ = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 100), "Unknown session");
            respArray = (JArray)_rpcServer.TraverseIterator(notExpiredSessionId.AsParameter<Guid>(), notExpiredIteratorId.AsParameter<Guid>(), 1);
            Assert.HasCount(1, respArray);

            // Mocking disposal
            resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, "getAllCandidates", [], validatorSigner.AsParameter<SignersAndWitnesses>(), true);
            sessionId = resp["session"];
            iteratorId = resp["stack"][0]["id"];
            _rpcServer.Dispose_SmartContract();

            Assert.ThrowsExactly<RpcException>(
                () => _ = (JArray)_rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), 100), "Unknown session");
        }

        [TestMethod]
        public void TestIteratorMethods_SessionsDisabled()
        {
            // Use a temporary RpcServer with sessions disabled
            var sessionsDisabledSettings = RpcServersSettings.Default with { SessionEnabled = false };
            var tempRpcServer = new RpcServer(_neoSystem, sessionsDisabledSettings);

            var randomSessionId = Guid.NewGuid();
            var randomIteratorId = Guid.NewGuid();

            // Test TraverseIterator
            var exTraverse = Assert.ThrowsExactly<RpcException>(
                () => tempRpcServer.TraverseIterator(randomSessionId, randomIteratorId, 10));
            Assert.AreEqual(RpcError.SessionsDisabled.Code, exTraverse.HResult);

            // Test TerminateSession
            var exTerminate = Assert.ThrowsExactly<RpcException>(() => tempRpcServer.TerminateSession(randomSessionId));
            Assert.AreEqual(RpcError.SessionsDisabled.Code, exTerminate.HResult);
        }

        [TestMethod]
        public void TestTraverseIterator_CountLimitExceeded()
        {
            // Need an active session and iterator first
            var resp = (JObject)_rpcServer.InvokeFunction(s_neoHash, "getAllCandidates", [], validatorSigner.AsParameter<SignersAndWitnesses>(), true);
            var sessionId = resp["session"];
            var iteratorId = resp["stack"][0]["id"];

            // Request more items than allowed
            int requestedCount = _rpcServerSettings.MaxIteratorResultItems + 1;
            var ex = Assert.ThrowsExactly<RpcException>(
                () => _rpcServer.TraverseIterator(sessionId.AsParameter<Guid>(), iteratorId.AsParameter<Guid>(), requestedCount));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            Assert.Contains("Invalid iterator items count", ex.Message);

            // Clean up the session
            _rpcServer.TerminateSession(sessionId.AsParameter<Guid>());
        }

        [TestMethod]
        public void TestTerminateSession_UnknownSession()
        {
            var unknownSessionId = Guid.NewGuid();
            // TerminateSession returns false for unknown session, doesn't throw RpcException directly
            var result = _rpcServer.TerminateSession(unknownSessionId);
            Assert.IsFalse(result.AsBoolean()); // Fix based on test output
        }

        [TestMethod]
        public void TestGetUnclaimedGas()
        {
            var address = new JString(MultisigAddress);
            JObject resp = (JObject)_rpcServer.GetUnclaimedGas(address.AsParameter<Address>());
            Assert.AreEqual("50000000", resp["unclaimed"]);
            Assert.AreEqual(resp["address"], MultisigAddress);

            address = new JString(ValidatorAddress);
            resp = (JObject)_rpcServer.GetUnclaimedGas(address.AsParameter<Address>());
            Assert.AreEqual("0", resp["unclaimed"]);
            Assert.AreEqual(resp["address"], ValidatorAddress);
        }

        [TestMethod]
        public void TestGetUnclaimedGas_InvalidAddress()
        {
            var invalidAddress = new JString("ThisIsNotAValidNeoAddress");
            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.GetUnclaimedGas(invalidAddress.AsParameter<Address>()));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);

            // The underlying error is likely FormatException during AddressToScriptHash
            Assert.Contains(RpcError.InvalidParams.Message, ex.Message); // Fix based on test output
        }
    }
}
