// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Transaction.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Extensions;
using Neo.Extensions.Collections;
using Neo.Extensions.IO;
using Neo.Extensions.VM;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System.Numerics;
using System.Runtime.CompilerServices;
using Array = System.Array;

namespace Neo.UnitTests.Network.P2P.Payloads;

[TestClass]
public class UT_Transaction
{
    Transaction _uut = null!;

    [TestInitialize]
    public void TestSetup()
    {
        _uut = (Transaction)RuntimeHelpers.GetUninitializedObject(typeof(Transaction));
    }

    [TestMethod]
    public void Script_Get()
    {
        Assert.IsTrue(_uut.Script.IsEmpty);
    }

    [TestMethod]
    public void FromStackItem()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => ((IInteroperable)_uut).FromStackItem(StackItem.Null));
    }

    [TestMethod]
    public void TestEquals()
    {
        Assert.IsTrue(_uut.Equals(_uut));
        Assert.IsFalse(_uut.Equals(null));
    }

    [TestMethod]
    public void InventoryType_Get()
    {
        Assert.AreEqual(InventoryType.TX, ((IInventory)_uut).InventoryType);
    }

    [TestMethod]
    public void Script_Set()
    {
        byte[] val = TestUtils.GetByteArray(32, 0x42);
        _uut.Script = val;
        var span = _uut.Script.Span;
        Assert.AreEqual(32, span.Length);
        for (int i = 0; i < val.Length; i++)
        {
            Assert.AreEqual(val[i], span[i]);
        }
    }

    [TestMethod]
    public void Gas_Get()
    {
        Assert.AreEqual(0, _uut.SystemFee);
    }

    [TestMethod]
    public void Gas_Set()
    {
        long val = 4200000000;
        _uut.SystemFee = val;
        Assert.AreEqual(val, _uut.SystemFee);
    }

    [TestMethod]
    public void Size_Get()
    {
        _uut.Script = TestUtils.GetByteArray(32, 0x42);
        _uut.Signers = [];
        _uut.Attributes = [];
        _uut.Witnesses = [Witness.Empty];

        Assert.AreEqual(0, _uut.Version);
        Assert.AreEqual(32, _uut.Script.Length);
        Assert.AreEqual(33, _uut.Script.GetVarSize());
        Assert.AreEqual(63, _uut.Size);
    }

    [TestMethod]
    public void CheckNoItems()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        byte[] script = [(byte)OpCode.PUSH0, (byte)OpCode.DROP];
        var tx = new Transaction
        {
            NetworkFee = 1000000,
            SystemFee = 1000000,
            Script = ReadOnlyMemory<byte>.Empty,
            Signers = [new() { Account = script.ToScriptHash() }],
            Attributes = [],
            Witnesses =
            [
                new()
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = script
                }
            ]
        };
        Assert.IsFalse(tx.VerifyWitnesses(TestProtocolSettings.Default, snapshotCache, tx.NetworkFee));
    }

    [TestMethod]
    public void FeeIsMultiSigContract()
    {
        var walletA = TestUtils.GenerateTestWallet("123");
        var walletB = TestUtils.GenerateTestWallet("123");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();

        var a = walletA.CreateAccount();
        var b = walletB.CreateAccount();

        var multiSignContract = Contract.CreateMultiSigContract(2,
        [
            a.GetKey()!.PublicKey,
            b.GetKey()!.PublicKey
        ]);

        walletA.CreateAccount(multiSignContract, a.GetKey());
        var acc = walletB.CreateAccount(multiSignContract, b.GetKey());

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        // Make transaction

        var tx = walletA.MakeTransaction(snapshotCache, [
            new TransferOutput
            {
                AssetId = NativeContract.Governance.GasTokenId,
                ScriptHash = acc.ScriptHash,
                Value = new BigDecimal(BigInteger.One, 8)
            }
        ], acc.ScriptHash);

        Assert.IsNotNull(tx);

        // Sign

        var wrongData = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network + 1);
        Assert.IsFalse(walletA.Sign(wrongData));

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        Assert.IsTrue(walletA.Sign(data));
        Assert.IsTrue(walletB.Sign(data));
        Assert.IsTrue(data.Completed);

        tx.Witnesses = data.GetWitnesses();

        // Fast check

        Assert.IsTrue(tx.VerifyWitnesses(TestProtocolSettings.Default, snapshotCache, tx.NetworkFee));

        // Check

        long verificationGas = 0;
        foreach (var witness in tx.Witnesses)
        {
            using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshotCache,
                settings: TestProtocolSettings.Default, gas: tx.NetworkFee);
            engine.LoadScript(witness.VerificationScript);
            engine.LoadScript(witness.InvocationScript);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
            verificationGas += engine.FeeConsumed;
        }

        var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshotCache);
        Assert.AreEqual(1967100, verificationGas);
        Assert.AreEqual(370000, sizeGas);
        Assert.AreEqual(2337100, tx.NetworkFee);
    }

    [TestMethod]
    public void FeeIsSignatureContractDetailed()
    {
        var wallet = TestUtils.GenerateTestWallet("123");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        // Make transaction

        // self-transfer of 1e-8 GAS
        var tx = wallet.MakeTransaction(snapshotCache, [
            new TransferOutput
            {
                AssetId = NativeContract.Governance.GasTokenId,
                ScriptHash = acc.ScriptHash,
                Value = new BigDecimal(BigInteger.One, 8)
            }
        ], acc.ScriptHash);

        Assert.IsNotNull(tx);

        // check pre-computed network fee (already guessing signature sizes)
        Assert.AreEqual(1250520L, tx.NetworkFee);

        // ----
        // Sign
        // ----

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        // 'from' is always required as witness
        // if not included on cosigner with a scope, its scope should be considered 'CalledByEntry'
        Assert.HasCount(1, data.ScriptHashes);
        Assert.AreEqual(acc.ScriptHash, data.ScriptHashes[0]);
        // will sign tx
        bool signed = wallet.Sign(data);
        Assert.IsTrue(signed);
        // get witnesses from signed 'data'
        tx.Witnesses = data.GetWitnesses();
        Assert.HasCount(1, tx.Witnesses);

        // Fast check

        Assert.IsTrue(tx.VerifyWitnesses(TestProtocolSettings.Default, snapshotCache, tx.NetworkFee));

        // Check

        long verificationGas = 0;
        foreach (var witness in tx.Witnesses)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshotCache,
                settings: TestProtocolSettings.Default, gas: tx.NetworkFee);
            engine.LoadScript(witness.VerificationScript);
            engine.LoadScript(witness.InvocationScript);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
            verificationGas += engine.FeeConsumed;
        }

        // ------------------
        // check tx_size cost
        // ------------------
        Assert.AreEqual(267, tx.Size);

        // will verify tx size, step by step

        // Part I
#pragma warning disable MSTEST0032
        Assert.AreEqual(25, Transaction.HeaderSize);
#pragma warning restore MSTEST0032
        // Part II
        Assert.AreEqual(1, tx.Attributes.GetVarSize());
        Assert.IsEmpty(tx.Attributes);
        Assert.HasCount(1, tx.Signers);
        // Note that Data size and Usage size are different (because of first byte on GetVarSize())
        Assert.AreEqual(22, tx.Signers.GetVarSize());
        // Part III
        Assert.AreEqual(110, tx.Script.GetVarSize());
        // Part IV
        Assert.AreEqual(109, tx.Witnesses.GetVarSize());
        // I + II + III + IV
        Assert.AreEqual(25 + 22 + 1 + 110 + 109, tx.Size);

        Assert.AreEqual(1000, NativeContract.Policy.GetFeePerByte(snapshotCache));
        var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshotCache);

        // final check: verification_cost and tx_size
        Assert.AreEqual(267000, sizeGas);
        Assert.AreEqual(983520, verificationGas);

        // final assert
        Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
    }

    [TestMethod]
    public void FeeIsSignatureContract_TestScope_Global()
    {
        var wallet = TestUtils.GenerateTestWallet("");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        // Make transaction
        // Manually creating script

        byte[] script;
        using (ScriptBuilder sb = new())
        {
            // self-transfer of 1e-8 GAS
            // GAS token uses TokenManagement.Transfer which requires assetId as first parameter
            var value = new BigDecimal(BigInteger.One, 8).Value;
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value, null);
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value, null);
            sb.Emit(OpCode.ASSERT);
            script = sb.ToArray();
        }

        // trying global scope
        var signers = new[]{ new Signer
            {
                Account = acc.ScriptHash,
                Scopes = WitnessScope.Global
            } };

        // using this...

        var tx = wallet.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers);

        Assert.IsNotNull(tx);

        // ----
        // Sign
        // ----

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        bool signed = wallet.Sign(data);
        Assert.IsTrue(signed);

        // get witnesses from signed 'data'
        tx.Witnesses = data.GetWitnesses();
        Assert.HasCount(1, tx.Witnesses);

        // Fast check
        Assert.IsTrue(tx.VerifyWitnesses(TestProtocolSettings.Default, snapshotCache, tx.NetworkFee));

        // Check
        long verificationGas = 0;
        foreach (var witness in tx.Witnesses)
        {
            using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshotCache,
                settings: TestProtocolSettings.Default, gas: tx.NetworkFee);
            engine.LoadScript(witness.VerificationScript);
            engine.LoadScript(witness.InvocationScript);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
            verificationGas += engine.FeeConsumed;
        }
        // get sizeGas
        var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshotCache);
        // final check on sum: verification_cost + tx_size
        Assert.AreEqual(1358520, verificationGas + sizeGas);
        // final assert
        Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
    }

    [TestMethod]
    public void FeeIsSignatureContract_TestScope_CurrentHash_GAS()
    {
        var wallet = TestUtils.GenerateTestWallet("");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));

        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;

        snapshotCache.Commit();

        // Make transaction
        // Manually creating script

        byte[] script;
        using (ScriptBuilder sb = new())
        {
            // self-transfer of 1e-8 GAS
            // GAS token uses TokenManagement.Transfer which requires assetId as first parameter
            BigInteger value = new BigDecimal(BigInteger.One, 8).Value;
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value, null);
            sb.Emit(OpCode.ASSERT);
            script = sb.ToArray();
        }

        // trying CustomContracts scope with TokenManagement.Hash
        // Since GAS token uses TokenManagement contract, we need to allow TokenManagement.Hash
        var signers = new[]{ new Signer
            {
                Account = acc.ScriptHash,
                Scopes = WitnessScope.CustomContracts,
                AllowedContracts = [NativeContract.TokenManagement.Hash]
            } };

        // using this...

        var tx = wallet.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers);

        Assert.IsNotNull(tx);

        // ----
        // Sign
        // ----

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        bool signed = wallet.Sign(data);
        Assert.IsTrue(signed);

        // get witnesses from signed 'data'
        tx.Witnesses = data.GetWitnesses();
        Assert.HasCount(1, tx.Witnesses);

        // Fast check
        Assert.IsTrue(tx.VerifyWitnesses(TestProtocolSettings.Default, snapshotCache, tx.NetworkFee));

        // Check
        long verificationGas = 0;
        foreach (var witness in tx.Witnesses)
        {
            using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshotCache,
                settings: TestProtocolSettings.Default, gas: tx.NetworkFee);
            engine.LoadScript(witness.VerificationScript);
            engine.LoadScript(witness.InvocationScript);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
            verificationGas += engine.FeeConsumed;
        }
        // get sizeGas
        var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshotCache);
        // final check on sum: verification_cost + tx_size
        Assert.AreEqual(1271520, verificationGas + sizeGas);
        // final assert
        Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
    }

    [TestMethod]
    public void FeeIsSignatureContract_TestScope_CalledByEntry_Plus_GAS()
    {
        var wallet = TestUtils.GenerateTestWallet("");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        // Make transaction
        // Manually creating script

        byte[] script;
        using (ScriptBuilder sb = new())
        {
            // self-transfer of 1e-8 GAS
            // GAS token uses TokenManagement.Transfer which requires assetId as first parameter
            var value = new BigDecimal(BigInteger.One, 8).Value;
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value, null);
            sb.Emit(OpCode.ASSERT);
            script = sb.ToArray();
        }

        // trying CalledByEntry together with GAS
        var signers = new[]{ new Signer
            {
                Account = acc.ScriptHash,
                // This combination is supposed to actually be an OR,
                // where it's valid in both Entry and also for Custom hash provided (in any execution level)
                // it would be better to test this in the future including situations
                // where a deeper call level uses this custom witness successfully
                Scopes = WitnessScope.CustomContracts | WitnessScope.CalledByEntry,
                AllowedContracts = [NativeContract.Governance.GasTokenId]
            } };

        // using this...

        var tx = wallet.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers);

        Assert.IsNotNull(tx);

        // ----
        // Sign
        // ----

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        bool signed = wallet.Sign(data);
        Assert.IsTrue(signed);

        // get witnesses from signed 'data'
        tx.Witnesses = data.GetWitnesses();
        Assert.HasCount(1, tx.Witnesses);

        // Fast check
        Assert.IsTrue(tx.VerifyWitnesses(TestProtocolSettings.Default, snapshotCache, tx.NetworkFee));

        // Check
        long verificationGas = 0;
        foreach (var witness in tx.Witnesses)
        {
            using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshotCache,
                settings: TestProtocolSettings.Default, gas: tx.NetworkFee);
            engine.LoadScript(witness.VerificationScript);
            engine.LoadScript(witness.InvocationScript);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
            verificationGas += engine.FeeConsumed;
        }
        // get sizeGas
        var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshotCache);
        // final check on sum: verification_cost + tx_size
        Assert.AreEqual(1271520, verificationGas + sizeGas);
        // final assert
        Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
    }

    [TestMethod]
    public void FeeIsSignatureContract_TestScope_CurrentHash_NEO_FAULT()
    {
        var wallet = TestUtils.GenerateTestWallet("");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));

        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;

        // Make transaction
        // Manually creating script

        byte[] script;
        using (ScriptBuilder sb = new())
        {
            // self-transfer of 1e-8 GAS
            // GAS token uses TokenManagement.Transfer which requires assetId as first parameter
            BigInteger value = new BigDecimal(BigInteger.One, 8).Value;
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value);
            sb.Emit(OpCode.ASSERT);
            script = sb.ToArray();
        }

        // trying global scope
        var signers = new[]{ new Signer
            {
                Account = acc.ScriptHash,
                Scopes = WitnessScope.CustomContracts,
                AllowedContracts = [NativeContract.NEO.Hash]
            } };

        // using this...

        // expects FAULT on execution of 'transfer' Application script
        // due to lack of a valid witness validation
        Assert.ThrowsExactly<InvalidOperationException>(
            () => wallet.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers));
    }

    [TestMethod]
    public void FeeIsSignatureContract_TestScope_CurrentHash_NEO_GAS()
    {
        var wallet = TestUtils.GenerateTestWallet("");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        // Make transaction
        // Manually creating script

        byte[] script;
        using (ScriptBuilder sb = new())
        {
            // self-transfer of 1e-8 GAS
            // GAS token uses TokenManagement.Transfer which requires assetId as first parameter
            BigInteger value = new BigDecimal(BigInteger.One, 8).Value;
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value, null);
            sb.Emit(OpCode.ASSERT);
            script = sb.ToArray();
        }

        // trying two custom hashes, for same target account
        // GAS token uses TokenManagement contract, so use TokenManagement.Hash instead of GasTokenId
        var signers = new[]{ new Signer
            {
                Account = acc.ScriptHash,
                Scopes = WitnessScope.CustomContracts,
                AllowedContracts = [NativeContract.NEO.Hash, NativeContract.TokenManagement.Hash]
            } };

        // using this...

        var tx = wallet.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers);

        Assert.IsNotNull(tx);

        // ----
        // Sign
        // ----

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        bool signed = wallet.Sign(data);
        Assert.IsTrue(signed);

        // get witnesses from signed 'data'
        tx.Witnesses = data.GetWitnesses();
        // only a single witness should exist
        Assert.HasCount(1, tx.Witnesses);
        // no attributes must exist
        Assert.IsEmpty(tx.Attributes);
        // one cosigner must exist
        Assert.HasCount(1, tx.Signers);

        // Fast check
        Assert.IsTrue(tx.VerifyWitnesses(TestProtocolSettings.Default, snapshotCache, tx.NetworkFee));

        // Check
        long verificationGas = 0;
        foreach (var witness in tx.Witnesses)
        {
            using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshotCache,
                settings: TestProtocolSettings.Default, gas: tx.NetworkFee);
            engine.LoadScript(witness.VerificationScript);
            engine.LoadScript(witness.InvocationScript);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
            verificationGas += engine.FeeConsumed;
        }
        // get sizeGas
        var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshotCache);
        // final check on sum: verification_cost + tx_size
        Assert.AreEqual(1291520, verificationGas + sizeGas);
        // final assert
        Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
    }

    [TestMethod]
    public void FeeIsSignatureContract_TestScope_NoScopeFAULT()
    {
        var wallet = TestUtils.GenerateTestWallet("");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));

        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;

        // Make transaction
        // Manually creating script

        byte[] script;
        using (ScriptBuilder sb = new())
        {
            // self-transfer of 1e-8 GAS
            // GAS token uses TokenManagement.Transfer which requires assetId as first parameter
            BigInteger value = new BigDecimal(BigInteger.One, 8).Value;
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value);
            sb.Emit(OpCode.ASSERT);
            script = sb.ToArray();
        }

        // trying with no scope
        var attributes = Array.Empty<TransactionAttribute>();
        var signers = new[]{ new Signer
            {
                Account = acc.ScriptHash,
                Scopes = WitnessScope.CustomContracts,
                AllowedContracts = [NativeContract.NEO.Hash, NativeContract.Governance.GasTokenId]
            } };

        // using this...

        // expects FAULT on execution of 'transfer' Application script
        // due to lack of a valid witness validation
        Assert.ThrowsExactly<InvalidOperationException>(
            () => wallet.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers, attributes));
    }

    [TestMethod]
    public void FeeIsSignatureContract_UnexistingVerificationContractFAULT()
    {
        var wallet = TestUtils.GenerateTestWallet("");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        // Make transaction
        // Manually creating script

        byte[] script;
        using (ScriptBuilder sb = new())
        {
            // self-transfer of 1e-8 GAS
            // GAS token uses TokenManagement.Transfer which requires assetId as first parameter
            BigInteger value = new BigDecimal(BigInteger.One, 8).Value;
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value, null);
            sb.Emit(OpCode.ASSERT);
            script = sb.ToArray();
        }

        // trying global scope
        var signers = new[]{ new Signer
            {
                Account = acc.ScriptHash,
                Scopes = WitnessScope.Global
            } };

        // creating new wallet with missing account for test
        var walletWithoutAcc = TestUtils.GenerateTestWallet("");

        // using this...

        // expects ArgumentException on execution of 'CalculateNetworkFee' due to
        // null witness_script (no account in the wallet, no corresponding witness
        // and no verification contract for the signer)
        Assert.ThrowsExactly<ArgumentException>(
            () => walletWithoutAcc.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers));
    }

    [TestMethod]
    public void Transaction_Reverify_Hashes_Length_Unequal_To_Witnesses_Length()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        Transaction txSimple = new()
        {
            Version = 0x00,
            Nonce = 0x01020304,
            SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS
            NetworkFee = 0x0000000000000001,
            ValidUntilBlock = 0x01020304,
            Attributes = [],
            Signers = [
                new()
                {
                    Account = UInt160.Parse("0x0001020304050607080900010203040506070809"),
                    Scopes = WitnessScope.Global
                }
            ],
            Script = new byte[] { (byte)OpCode.PUSH1 },
            Witnesses = [],
        };
        UInt160[] hashes = txSimple.GetScriptHashesForVerifying(snapshotCache);
        Assert.HasCount(1, hashes);
        Assert.AreNotEqual(VerifyResult.Succeed,
            txSimple.VerifyStateDependent(TestProtocolSettings.Default, snapshotCache, new(), []));
    }

    [TestMethod]
    public void Transaction_Serialize_Deserialize_Simple()
    {
        // good and simple transaction
        Transaction txSimple = new()
        {
            Version = 0x00,
            Nonce = 0x01020304,
            SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS
            NetworkFee = 0x0000000000000001,
            ValidUntilBlock = 0x01020304,
            Signers = [new() { Account = UInt160.Zero }],
            Attributes = [],
            Script = new[] { (byte)OpCode.PUSH1 },
            Witnesses = [Witness.Empty]
        };

        byte[] sTx = txSimple.ToArray();

        // detailed hexstring info (basic checking)
        Assert.AreEqual("00" + // version
            "04030201" + // nonce
            "00e1f50500000000" + // system fee (1 GAS)
            "0100000000000000" + // network fee (1 datoshi)
            "04030201" + // timelimit
            "01000000000000000000000000000000000000000000" + // empty signer
            "00" + // no attributes
            "0111" + // push1 script
            "010000", sTx.ToHexString()); // empty witnesses

        // try to deserialize
        Transaction tx2 = sTx.AsSerializable<Transaction>();

        Assert.AreEqual(0x00, tx2.Version);
        Assert.AreEqual(0x01020304u, tx2.Nonce);
        Assert.AreEqual(UInt160.Zero, tx2.Sender);
        Assert.AreEqual(0x0000000005f5e100, tx2.SystemFee); // 1 GAS (long)BigInteger.Pow(10, 8)
        Assert.AreEqual(0x0000000000000001, tx2.NetworkFee);
        Assert.AreEqual(0x01020304u, tx2.ValidUntilBlock);
        CollectionAssert.AreEqual(Array.Empty<TransactionAttribute>(), tx2.Attributes);
        CollectionAssert.AreEqual(new Signer[]
        {
            new()
            {
                Account = UInt160.Zero,
                AllowedContracts = [],
                AllowedGroups = [],
                Rules = [],
            }
        }, tx2.Signers);
        Assert.IsTrue(tx2.Script.Span.SequenceEqual([(byte)OpCode.PUSH1]));
        Assert.IsTrue(tx2.Witnesses[0].InvocationScript.Span.IsEmpty);
        Assert.IsTrue(tx2.Witnesses[0].VerificationScript.Span.IsEmpty);
    }

    [TestMethod]
    public void Transaction_Serialize_Deserialize_DistinctSigners()
    {
        // the `Signers` must be distinct (regarding account)
        var txDoubleSigners = new Transaction
        {
            Version = 0x00,
            Nonce = 0x01020304,
            SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS
            NetworkFee = 0x0000000000000001,
            ValidUntilBlock = 0x01020304,
            Attributes = [],
            Signers =
            [
                new()
                {
                    Account = UInt160.Parse("0x0001020304050607080900010203040506070809"),
                    Scopes = WitnessScope.Global
                },
                new()
                {
                    Account = UInt160.Parse("0x0001020304050607080900010203040506070809"), // same account as above
                    Scopes = WitnessScope.CalledByEntry // different scope, but still, same account (cannot do that)
                }
            ],
            Script = new[] { (byte)OpCode.PUSH1 },
            Witnesses = [Witness.Empty]
        };

        var sTx = txDoubleSigners.ToArray();

        // no need for detailed hexstring here (see basic tests for it)
        var expected = "000403020100e1f50500000000010000000000000004030201020908070605040302010009080706050403020" +
            "10080090807060504030201000908070605040302010001000111010000";
        Assert.AreEqual(expected, sTx.ToHexString());

        // back to transaction (should fail, due to non-distinct signers)
        Assert.ThrowsExactly<FormatException>(() => sTx.AsSerializable<Transaction>());
    }


    [TestMethod]
    public void Transaction_Serialize_Deserialize_MaxSizeSigners()
    {
        // the `Signers` must respect count
        int maxSigners = 16;

        // --------------------------------------
        // this should pass (respecting max size)
        var signers1 = new Signer[maxSigners];
        for (int i = 0; i < signers1.Length; i++)
        {
            string hex = i.ToString("X4");
            while (hex.Length < 40)
                hex = hex.Insert(0, "0");
            signers1[i] = new Signer
            {
                Account = UInt160.Parse(hex),
                Scopes = WitnessScope.CalledByEntry
            };
        }

        var txSigners1 = new Transaction
        {
            Version = 0x00,
            Nonce = 0x01020304,
            SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS
            NetworkFee = 0x0000000000000001,
            ValidUntilBlock = 0x01020304,
            Attributes = [],
            Signers = signers1, // max + 1 (should fail)
            Script = new[] { (byte)OpCode.PUSH1 },
            Witnesses = [Witness.Empty]
        };

        var sTx1 = txSigners1.ToArray();

        // back to transaction (should fail, due to non-distinct signers)
        Assert.ThrowsExactly<FormatException>(() => _ = sTx1.AsSerializable<Transaction>());

        // ----------------------------
        // this should fail (max + 1)

        var signers = new Signer[maxSigners + 1];
        for (var i = 0; i < maxSigners + 1; i++)
        {
            var hex = i.ToString("X4");
            while (hex.Length < 40)
                hex = hex.Insert(0, "0");
            signers[i] = new Signer
            {
                Account = UInt160.Parse(hex)
            };
        }

        var txSigners = new Transaction
        {
            Version = 0x00,
            Nonce = 0x01020304,
            SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS
            NetworkFee = 0x0000000000000001,
            ValidUntilBlock = 0x01020304,
            Attributes = [],
            Signers = signers, // max + 1 (should fail)
            Script = new[] { (byte)OpCode.PUSH1 },
            Witnesses = [Witness.Empty]
        };

        var sTx2 = txSigners.ToArray();

        // back to transaction (should fail, due to non-distinct signers)
        Assert.ThrowsExactly<FormatException>(() => sTx2.AsSerializable<Transaction>());
    }

    [TestMethod]
    public void FeeIsSignatureContract_TestScope_FeeOnly_Default()
    {
        // None is supposed to be default
        var signer = (Signer)RuntimeHelpers.GetUninitializedObject(typeof(Signer));
        Assert.AreEqual(WitnessScope.None, signer.Scopes);

        var wallet = TestUtils.GenerateTestWallet("");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var acc = wallet.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        // Make transaction
        // Manually creating script

        byte[] script;
        using (var sb = new ScriptBuilder())
        {
            // self-transfer of 1e-8 GAS
            // GAS token uses TokenManagement.Transfer which requires assetId as first parameter
            BigInteger value = new BigDecimal(BigInteger.One, 8).Value;
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", CallFlags.All, NativeContract.Governance.GasTokenId, acc.ScriptHash, acc.ScriptHash, value, null);
            sb.Emit(OpCode.ASSERT);
            script = sb.ToArray();
        }

        // try to use fee only inside the smart contract
        var signers = new[]{
            new Signer()
            {
                Account = acc.ScriptHash,
                Scopes =  WitnessScope.None
            }
        };

        Assert.ThrowsExactly<InvalidOperationException>(
            () => _ = wallet.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers));

        // change to global scope
        signers[0].Scopes = WitnessScope.Global;

        var tx = wallet.MakeTransaction(snapshotCache, script, acc.ScriptHash, signers);

        Assert.IsNotNull(tx);

        // ----
        // Sign
        // ----

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        bool signed = wallet.Sign(data);
        Assert.IsTrue(signed);

        // get witnesses from signed 'data'
        tx.Witnesses = data.GetWitnesses();
        Assert.HasCount(1, tx.Witnesses);

        // Fast check
        Assert.IsTrue(tx.VerifyWitnesses(TestProtocolSettings.Default, snapshotCache, tx.NetworkFee));

        // Check
        long verificationGas = 0;
        foreach (var witness in tx.Witnesses)
        {
            using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshotCache,
                settings: TestProtocolSettings.Default, gas: tx.NetworkFee);
            engine.LoadScript(witness.VerificationScript);
            engine.LoadScript(witness.InvocationScript);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
            verificationGas += engine.FeeConsumed;
        }
        // get sizeGas
        var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshotCache);
        // final check on sum: verification_cost + tx_size
        Assert.AreEqual(1250520, verificationGas + sizeGas);
        // final assert
        Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
    }

    [TestMethod]
    public void ToJson()
    {
        _uut.Script = TestUtils.GetByteArray(32, 0x42);
        _uut.SystemFee = 4200000000;
        _uut.Signers = [new() { Account = UInt160.Zero }];
        _uut.Attributes = [];
        _uut.Witnesses = [Witness.Empty];

        JObject jObj = _uut.ToJson(ProtocolSettings.Default);
        Assert.IsNotNull(jObj);
        Assert.AreEqual("0x0ab073429086d9e48fc87386122917989705d1c81fe4a60bf90e2fc228de3146", jObj["hash"]!.AsString());
        Assert.AreEqual(84, jObj["size"]!.AsNumber());
        Assert.AreEqual(0, jObj["version"]!.AsNumber());
        Assert.IsEmpty((JArray)jObj["attributes"]!);
        Assert.AreEqual("0", jObj["netfee"]!.AsString());
        Assert.AreEqual("QiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA=", jObj["script"]!.AsString());
        Assert.AreEqual("4200000000", jObj["sysfee"]!.AsString());
    }

    [TestMethod]
    public void Test_GetAttribute()
    {
        var tx = new Transaction
        {
            Attributes = [],
            NetworkFee = 0,
            Nonce = (uint)Environment.TickCount,
            Script = new byte[Transaction.MaxTransactionSize],
            Signers = [new Signer { Account = UInt160.Zero }],
            SystemFee = 0,
            ValidUntilBlock = 0,
            Version = 0,
            Witnesses = [],
        };

        Assert.IsNull(tx.GetAttribute<OracleResponse>());
        Assert.IsNull(tx.GetAttribute<HighPriorityAttribute>());

        tx.Attributes = [new HighPriorityAttribute()];

        Assert.IsNull(tx.GetAttribute<OracleResponse>());
        Assert.IsNotNull(tx.GetAttribute<HighPriorityAttribute>());
    }

    [TestMethod]
    public void Test_VerifyStateIndependent()
    {
        var tx = new Transaction
        {
            Attributes = [],
            NetworkFee = 0,
            Nonce = (uint)Environment.TickCount,
            Script = new byte[Transaction.MaxTransactionSize],
            Signers = [new() { Account = UInt160.Zero }],
            SystemFee = 0,
            ValidUntilBlock = 0,
            Version = 0,
            Witnesses = [Witness.Empty],
        };
        Assert.AreEqual(VerifyResult.OverSize, tx.VerifyStateIndependent(TestProtocolSettings.Default));
        tx.Script = Array.Empty<byte>();
        Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(TestProtocolSettings.Default));

        var walletA = TestUtils.GenerateTestWallet("123");
        var walletB = TestUtils.GenerateTestWallet("123");
        var walletC = TestUtils.GenerateTestWallet("123");
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();

        var a = walletA.CreateAccount();
        var b = walletB.CreateAccount();
        var c = walletC.CreateAccount();

        var multiSignContract = Contract.CreateMultiSigContract(2,
        [
            a.GetKey()!.PublicKey,
            b.GetKey()!.PublicKey
        ]);
        var wrongMultisigContract = Contract.CreateMultiSigContract(2,
        [
            a.GetKey()!.PublicKey,
            c.GetKey()!.PublicKey
        ]);

        walletA.CreateAccount(multiSignContract, a.GetKey());
        var acc = walletB.CreateAccount(multiSignContract, b.GetKey());

        walletA.CreateAccount(wrongMultisigContract, a.GetKey());
        var wrongAcc = walletC.CreateAccount(wrongMultisigContract, c.GetKey());

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;
        snapshotCache.Commit();

        // Make transaction

        tx = walletA.MakeTransaction(snapshotCache, [
            new TransferOutput
            {
                AssetId = NativeContract.Governance.GasTokenId,
                ScriptHash = acc.ScriptHash,
                Value = new BigDecimal(BigInteger.One, 8)
            }
        ], acc.ScriptHash);

        // Sign

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        Assert.IsTrue(walletA.Sign(data));
        Assert.IsTrue(walletB.Sign(data));
        Assert.IsTrue(data.Completed);

        tx.Witnesses = data.GetWitnesses();
        Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(TestProtocolSettings.Default));

        // Different invocation script (contains signatures of A&C whereas originally signatures
        // from A&B are required).
        tx.Signers[0].Account = wrongAcc.ScriptHash; // temporary replace Sender's scripthash to be able to construct A&C signature.
        var wrongData = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        Assert.IsTrue(walletA.Sign(wrongData));
        Assert.IsTrue(walletC.Sign(wrongData));
        Assert.IsTrue(wrongData.Completed);

        tx.Signers[0].Account = acc.ScriptHash; // get back the original value of Sender's scripthash.
        tx.Witnesses[0].InvocationScript = wrongData.GetWitnesses()[0].InvocationScript.ToArray();
        Assert.AreEqual(VerifyResult.InvalidSignature, tx.VerifyStateIndependent(TestProtocolSettings.Default));
    }

    [TestMethod]
    public void Test_VerifyStateDependent()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var height = NativeContract.Ledger.CurrentIndex(snapshotCache);
        var tx = new Transaction()
        {
            Attributes = [],
            NetworkFee = 55000,
            Nonce = (uint)Environment.TickCount,
            Script = Array.Empty<byte>(),
            Signers = [new Signer() { Account = UInt160.Zero }],
            SystemFee = 0,
            ValidUntilBlock = height + 1,
            Version = 0,
            Witnesses =
            [
                Witness.Empty,
                new() { InvocationScript = ReadOnlyMemory<byte>.Empty, VerificationScript = new byte[1] }
            ]
        };

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(tx.Sender).Add(NativeContract.Governance.GasTokenId);
        var balance = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        balance.GetInteroperable<AccountState>().Balance = tx.NetworkFee;
        var conflicts = new List<Transaction>();

        Assert.AreEqual(VerifyResult.Invalid, tx.VerifyStateDependent(TestProtocolSettings.Default, snapshotCache, new(), conflicts));
        balance.GetInteroperable<AccountState>().Balance = 0;
        tx.SystemFee = 10;
        Assert.AreEqual(VerifyResult.InsufficientFunds, tx.VerifyStateDependent(TestProtocolSettings.Default, snapshotCache, new(), conflicts));

        var walletA = TestUtils.GenerateTestWallet("123");
        var walletB = TestUtils.GenerateTestWallet("123");

        var a = walletA.CreateAccount();
        var b = walletB.CreateAccount();

        var multiSignContract = Contract.CreateMultiSigContract(2,
        [
            a.GetKey()!.PublicKey,
            b.GetKey()!.PublicKey
        ]);

        walletA.CreateAccount(multiSignContract, a.GetKey());
        var acc = walletB.CreateAccount(multiSignContract, b.GetKey());

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var tokenStateKey2 = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey2))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey2, new StorageItem(tokenState));
        }
        // Then set account balance
        key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        balance = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
        balance.GetInteroperable<AccountState>().Balance = 10000 * Governance.GasTokenFactor;

        // Make transaction

        snapshotCache.Commit();
        tx = walletA.MakeTransaction(snapshotCache, new[]
        {
                new TransferOutput()
                {
                     AssetId = NativeContract.Governance.GasTokenId,
                     ScriptHash = acc.ScriptHash,
                     Value = new BigDecimal(BigInteger.One,8)
                }
        }, acc.ScriptHash);

        // Sign

        var data = new ContractParametersContext(snapshotCache, tx, TestProtocolSettings.Default.Network);
        Assert.IsTrue(walletA.Sign(data));
        Assert.IsTrue(walletB.Sign(data));
        Assert.IsTrue(data.Completed);

        tx.Witnesses = data.GetWitnesses();
        Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateDependent(TestProtocolSettings.Default, snapshotCache, new(), []));
    }

    [TestMethod]
    public void Test_VerifyStateInDependent_Multi()
    {
        var txData = Convert.FromBase64String("AHXd31W0NlsAAAAAAJRGawAAAAAA3g8CAAGSs5x3qmDym1fBc87ZF/F/0yGm6wEAX" +
            "wsDAOQLVAIAAAAMFLqZBJj+L0XZPXNHHM9MBfCza5HnDBSSs5x3qmDym1fBc87ZF/F/0yGm6xTAHwwIdHJhbnNmZXIMFM924ovQ" +
            "BixKR47jVWEBExnzz6TSQWJ9W1I5Af1KAQxAnZvOQOCdkM+j22dS5SdEncZVYVVi1F26MhheNzNImTD4Ekw5kFR6Fojs7gD57Bd" +
            "euo8tLS1UXpzflmKcQ3pniAxAYvGgxtokrk6PVdduxCBwVbdfie+ZxiaDsjK0FYregl24cDr2v5cTLHrURVfJJ1is+4G6Jaer7n" +
            "B1JrDrw+Qt6QxATA5GdR4rKFPPPQQ24+42OP2tz0HylG1LlANiOtIdag3ZPkUfZiBfEGoOteRD1O0UnMdJP4Su7PFhDuCdHu4Ml" +
            "wxAuGFEk2m/rdruleBGYz8DIzExJtwb/TsFxZdHxo4VV8ktv2Nh71Fwhg2bhW2tq8hV6RK2GFXNAU72KAgf/Qv6BQxA0j3srkwY" +
            "333KvGNtw7ZvSG8X36Tqu000CEtDx4SMOt8qhVYGMr9PClsUVcYFHdrJaodilx8ewXDHNIq+OnS7SfwVDCEDAJt1QOEPJWLl/Y+" +
            "snq7CUWaliybkEjSP9ahpJ7+sIqIMIQMCBenO+upaHfxYCvIMjVqiRouwFI8aXkYF/GIsgOYEugwhAhS68M7qOmbxfn4eg56iX9" +
            "i+1s2C5rtuaCUBiQZfRP8BDCECPpsy6om5TQZuZJsST9UOOW7pE2no4qauGxHBcNAiJW0MIQNAjc1BY5b2R4OsWH6h4Vk8V9n+q" +
            "IDIpqGSDpKiWUd4BgwhAqeDS+mzLimB0VfLW706y0LP0R6lw7ECJNekTpjFkQ8bDCECuixw9ZlvNXpDGYcFhZ+uLP6hPhFyligA" +
            "dys9WIqdSr0XQZ7Q3Do=");

        var tx = (Transaction)RuntimeHelpers.GetUninitializedObject(typeof(Transaction));
        MemoryReader reader = new(txData);
        ((ISerializable)tx).Deserialize(ref reader);

        var settings = ProtocolSettings.Default with { Network = 844378958 };
        var result = tx.VerifyStateIndependent(settings);
        Assert.AreEqual(VerifyResult.Succeed, result);
    }
}
