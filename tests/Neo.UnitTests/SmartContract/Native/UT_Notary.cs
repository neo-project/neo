// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Notary.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions.IO;
using Neo.Extensions.VM;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native;

[TestClass]
public class UT_Notary
{
    private DataCache _snapshot = null!;

    [TestInitialize]
    public void TestSetup()
    {
        _snapshot = TestBlockchain.GetTestSnapshotCache();
    }

    [TestMethod]
    public void Check_Name()
    {
        Assert.AreEqual(nameof(Notary), NativeContract.Notary.Name);
    }

    [TestMethod]
    public void Check_OnNEP17Payment()
    {
        var snapshot = _snapshot.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
        var to = NativeContract.Notary.Hash.ToArray();

        // Set proper current index for deposit's Till parameter check.
        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        snapshot.Add(storageKey, new(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

        // Non-GAS transfer should fail.
        Exception? ex = Assert.Throws<Exception>(
            () => NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.Zero, true, persistingBlock));
        while (ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null)
            ex = tie.InnerException;
        Assert.IsInstanceOfType<InvalidOperationException>(ex);

        // GAS transfer with invalid data format should fail.
        ex = Assert.Throws<Exception>(
            () => TransferGAS(snapshot, from, to, BigInteger.Zero, true, persistingBlock, 5));
        while (ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null)
            ex = tie.InnerException;
        Assert.IsInstanceOfType<FormatException>(ex);

        // GAS transfer with wrong number of data elements should fail.
        var data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() { new() { Type = ContractParameterType.Boolean, Value = true } }
        };
        ex = Assert.Throws<Exception>(
            () => TransferGAS(snapshot, from, to, BigInteger.Zero, true, persistingBlock, data));
        while (ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null)
            ex = tie.InnerException;
        Assert.IsInstanceOfType<FormatException>(ex);

        // Gas transfer with invalid Till parameter should fail.
        data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = persistingBlock.Index } ,
            }
        };
        ex = Assert.Throws<Exception>(
            () => TransferGASWithTransaction(snapshot, from, to, BigInteger.Zero, true, persistingBlock, data));
        while (ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null)
            ex = tie.InnerException;
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(ex);

        // Insufficient first deposit.
        data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = persistingBlock.Index + 100 },
            }
        };
        ex = Assert.Throws<Exception>(
            () => TransferGASWithTransaction(snapshot, from, to, 2 * 1000_0000 - 1, true, persistingBlock, data));
        while (ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null)
            ex = tie.InnerException;
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(ex);

        // Good deposit.
        data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = persistingBlock.Index + 100 },
            }
        };
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, to, 2 * 1000_0000 + 1, true, persistingBlock, data));
    }

    [TestMethod]
    public void Check_ExpirationOf()
    {
        var snapshot = _snapshot.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
        var ntr = NativeContract.Notary.Hash.ToArray();

        // Set proper current index for deposit's Till parameter check.
        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

        // Check that 'till' of an empty deposit is 0 by default.
        Assert.AreEqual(0, Call_ExpirationOf(snapshot, from, persistingBlock));

        // Make initial deposit.
        var till = persistingBlock.Index + 123;
        var data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = till },
            }
        };
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, ntr, 2 * 1000_0000 + 1, true, persistingBlock, data));

        // Ensure deposit's 'till' value is properly set.
        Assert.AreEqual(till, Call_ExpirationOf(snapshot, from, persistingBlock));

        // Make one more deposit with updated 'till' parameter.
        till += 5;
        data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = till },
            }
        };
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, ntr, 5, true, persistingBlock, data));

        // Ensure deposit's 'till' value is properly updated.
        Assert.AreEqual(till, Call_ExpirationOf(snapshot, from, persistingBlock));

        // Make deposit to some side account with custom 'till' value.
        var to = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
        data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Hash160, Value = to },
                new() { Type = ContractParameterType.Integer, Value = till },
            }
        };
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, ntr, 2 * 1000_0000 + 1, true, persistingBlock, data));

        // Default 'till' value should be set for to's deposit.
        var defaultDeltaTill = 5760;
        var expectedTill = persistingBlock.Index - 1 + defaultDeltaTill;
        Assert.AreEqual(expectedTill, Call_ExpirationOf(snapshot, to.ToArray(), persistingBlock));

        // Withdraw own deposit.
        persistingBlock.Header.Index = till + 1;
        var currentBlock = snapshot.GetAndChange(storageKey, () => new StorageItem(new HashIndexState()));
        currentBlock.GetInteroperable<HashIndexState>().Index = till + 1;
        Call_Withdraw(snapshot, from, from, persistingBlock);

        // Check that 'till' value is properly updated.
        Assert.AreEqual(0, Call_ExpirationOf(snapshot, from, persistingBlock));
    }

    [TestMethod]
    public void Check_LockDepositUntil()
    {
        var snapshot = _snapshot.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

        // Set proper current index for deposit's Till parameter check.
        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        snapshot.Add(storageKey, new(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

        // Check that 'till' of an empty deposit is 0 by default.
        Assert.AreEqual(0, Call_ExpirationOf(snapshot, from, persistingBlock));

        // Update `till` value of an empty deposit should fail.
        Assert.IsFalse(Call_LockDepositUntil(snapshot, from, 123, persistingBlock));
        Assert.AreEqual(0, Call_ExpirationOf(snapshot, from, persistingBlock));

        // Make initial deposit.
        var till = persistingBlock.Index + 123;
        var data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = till },
            }
        };

        var hash = NativeContract.Notary.Hash.ToArray();
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, hash, 2 * 1000_0000 + 1, true, persistingBlock, data));

        // Ensure deposit's 'till' value is properly set.
        Assert.AreEqual(till, Call_ExpirationOf(snapshot, from, persistingBlock));

        // Update deposit's `till` value for side account should fail.
        UInt160 other = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
        Assert.IsFalse(Call_LockDepositUntil(snapshot, other.ToArray(), till + 10, persistingBlock));
        Assert.AreEqual(till, Call_ExpirationOf(snapshot, from, persistingBlock));

        // Decrease deposit's `till` value should fail.
        Assert.IsFalse(Call_LockDepositUntil(snapshot, from, till - 1, persistingBlock));
        Assert.AreEqual(till, Call_ExpirationOf(snapshot, from, persistingBlock));

        // Good.
        till += 10;
        Assert.IsTrue(Call_LockDepositUntil(snapshot, from, till, persistingBlock));
        Assert.AreEqual(till, Call_ExpirationOf(snapshot, from, persistingBlock));
    }

    [TestMethod]
    public void Check_BalanceOf()
    {
        var snapshot = _snapshot.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        var fromAddr = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
        var from = fromAddr.ToArray();
        var hash = NativeContract.Notary.Hash.ToArray();

        // Set proper current index for deposit expiration.
        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        snapshot.Add(storageKey, new(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

        // Ensure that default deposit is 0.
        Assert.AreEqual(0, Call_BalanceOf(snapshot, from, persistingBlock));

        // Make initial deposit.
        var till = persistingBlock.Index + 123;
        var deposit1 = 2 * 1_0000_0000;
        var data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = till },
            }
        };
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, hash, deposit1, true, persistingBlock, data));

        // Ensure value is deposited.
        Assert.AreEqual(deposit1, Call_BalanceOf(snapshot, from, persistingBlock));

        // Make one more deposit with updated 'till' parameter.
        var deposit2 = 5;
        data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = till },
            }
        };
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, hash, deposit2, true, persistingBlock, data));

        // Ensure deposit's 'till' value is properly updated.
        Assert.AreEqual(deposit1 + deposit2, Call_BalanceOf(snapshot, from, persistingBlock));

        // Make deposit to some side account.
        UInt160 to = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
        data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Hash160, Value = to },
                new() { Type = ContractParameterType.Integer, Value = till },
            }
        };
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, hash, deposit1, true, persistingBlock, data));

        Assert.AreEqual(deposit1, Call_BalanceOf(snapshot, to.ToArray(), persistingBlock));

        // Process some Notary transaction and check that some deposited funds have been withdrawn.
        var tx1 = TestUtils.GetTransaction(NativeContract.Notary.Hash, fromAddr);
        tx1.Attributes = [new NotaryAssisted() { NKeys = 4 }];
        tx1.NetworkFee = 1_0000_0000;

        // Build block to check transaction fee distribution during Gas OnPersist.
        persistingBlock = new Block
        {
            Header = new Header
            {
                Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero,
                Witness = Witness.Empty,
            },
            Transactions = [tx1]
        };

        // Designate Notary node.
        var privateKey1 = new byte[32];
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(privateKey1);
        var key1 = new KeyPair(privateKey1);
        UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
        var ret = NativeContract.RoleManagement.Call(
            snapshot,
            new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
            new Block
            {
                Header = (Header)RuntimeHelpers.GetUninitializedObject(typeof(Header)),
                Transactions = []
            },
            "designateAsRole",
            new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.P2PNotary) },
            new ContractParameter(ContractParameterType.Array)
            {
                Value = new List<ContractParameter>(){
                    new(ContractParameterType.ByteArray){Value = key1.PublicKey.ToArray()},
                },
            }
        );
        snapshot.Commit();

        // Execute OnPersist script.
        var script = new ScriptBuilder();
        script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
        var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default);
        engine.LoadScript(script.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
        snapshot.Commit();

        // Check that transaction's fees were paid by from's deposit.
        var expectedBalance = deposit1 + deposit2 - tx1.NetworkFee - tx1.SystemFee;
        Assert.AreEqual(expectedBalance, Call_BalanceOf(snapshot, from, persistingBlock));

        // Withdraw own deposit.
        persistingBlock.Header.Index = till + 1;
        var currentBlock = snapshot.GetAndChange(storageKey, () => new StorageItem(new HashIndexState()));
        currentBlock.GetInteroperable<HashIndexState>().Index = till + 1;
        Call_Withdraw(snapshot, from, from, persistingBlock);

        // Check that no deposit is left.
        Assert.AreEqual(0, Call_BalanceOf(snapshot, from, persistingBlock));
    }

    [TestMethod]
    public void Check_Withdraw()
    {
        var snapshot = _snapshot.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        var fromAddr = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
        var from = fromAddr.ToArray();

        // Set proper current index to get proper deposit expiration height.
        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        snapshot.Add(storageKey, new(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

        // Ensure that default deposit is 0.
        Assert.AreEqual(0, Call_BalanceOf(snapshot, from, persistingBlock));

        // Make initial deposit.
        var till = persistingBlock.Index + 123;
        var deposit1 = 2 * 1_0000_0000;
        var data = new ContractParameter
        {
            Type = ContractParameterType.Array,
            Value = new List<ContractParameter>() {
                new() { Type = ContractParameterType.Any },
                new() { Type = ContractParameterType.Integer, Value = till },
            }
        };

        var hash = NativeContract.Notary.Hash.ToArray();
        Assert.IsTrue(TransferGASWithTransaction(snapshot, from, hash, deposit1, true, persistingBlock, data));

        // Ensure value is deposited.
        Assert.AreEqual(deposit1, Call_BalanceOf(snapshot, from, persistingBlock));

        // Unwitnessed withdraw should fail.
        var sideAccount = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
        Assert.IsFalse(Call_Withdraw(snapshot, from, sideAccount.ToArray(), persistingBlock, false));

        // Withdraw missing (zero) deposit should fail.
        Assert.IsFalse(Call_Withdraw(snapshot, sideAccount.ToArray(), sideAccount.ToArray(), persistingBlock));

        // Withdraw before deposit expiration should fail.
        Assert.IsFalse(Call_Withdraw(snapshot, from, from, persistingBlock));

        // Good.
        persistingBlock.Header.Index = till + 1;
        var currentBlock = snapshot.GetAndChange(storageKey, () => new StorageItem(new HashIndexState()));
        currentBlock.GetInteroperable<HashIndexState>().Index = till + 1;
        Assert.IsTrue(Call_Withdraw(snapshot, from, from, persistingBlock));

        // Check that no deposit is left.
        Assert.AreEqual(0, Call_BalanceOf(snapshot, from, persistingBlock));
    }

    internal static BigInteger Call_BalanceOf(DataCache snapshot, byte[] address, Block persistingBlock)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Notary.Hash, "balanceOf", address);
        engine.LoadScript(script.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Integer>(result);

        return result.GetInteger();
    }

    internal static BigInteger Call_ExpirationOf(DataCache snapshot, byte[] address, Block persistingBlock)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Notary.Hash, "expirationOf", address);
        engine.LoadScript(script.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Integer>(result);

        return result.GetInteger();
    }

    internal static bool Call_LockDepositUntil(DataCache snapshot, byte[] address, uint till, Block persistingBlock)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Transaction()
            {
                Signers = [new() { Account = new UInt160(address), Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = null!
            },
            snapshot, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Notary.Hash, "lockDepositUntil", address, till);
        engine.LoadScript(script.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Neo.VM.Types.Boolean>(result);

        return result.GetBoolean();
    }

    internal static bool Call_Withdraw(DataCache snapshot, byte[] from, byte[] to, Block persistingBlock, bool witnessedByFrom = true)
    {
        var accFrom = UInt160.Zero;
        if (witnessedByFrom)
        {
            accFrom = new UInt160(from);
        }
        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Transaction()
            {
                Signers = [new() { Account = accFrom, Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = null!
            },
            snapshot, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Notary.Hash, "withdraw", from, to);
        engine.LoadScript(script.ToArray());

        if (engine.Execute() != VMState.HALT)
        {
            throw engine.FaultException!;
        }

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Neo.VM.Types.Boolean>(result);

        return result.GetBoolean();
    }

    [TestMethod]
    public void Check_GetMaxNotValidBeforeDelta()
    {
        const uint defaultMaxNotValidBeforeDelta = 140;
        Assert.AreEqual(defaultMaxNotValidBeforeDelta, NativeContract.Notary.GetMaxNotValidBeforeDelta(_snapshot));
    }

    [TestMethod]
    public void Check_SetMaxNotValidBeforeDelta()
    {
        var snapshot = _snapshot.CloneCache();
        var persistingBlock = new Block
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = 1000,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
        var committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Nep17NativeContractExtensions.ManualWitness(committeeAddress),
            snapshot, persistingBlock, settings: TestProtocolSettings.Default);
        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.Notary.Hash, "setMaxNotValidBeforeDelta", 100);
        engine.LoadScript(script.ToArray());

        var vMState = engine.Execute();
        Assert.AreEqual(VMState.HALT, vMState);
        Assert.AreEqual(100u, NativeContract.Notary.GetMaxNotValidBeforeDelta(snapshot));
    }

    [TestMethod]
    public void Check_OnPersist_FeePerKeyUpdate()
    {
        // Hardcode test values.
        const uint defaultNotaryAssistedFeePerKey = 1000_0000;
        const uint newNotaryAssistedFeePerKey = 5000_0000;
        const byte NKeys = 4;

        // Generate one transaction with NotaryAssisted attribute with hardcoded NKeys values.
        var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
        var tx2 = TestUtils.GetTransaction(from);
        tx2.Attributes = [new NotaryAssisted() { NKeys = NKeys }];

        var netFee = 1_0000_0000; // enough to cover defaultNotaryAssistedFeePerKey, but not enough to cover newNotaryAssistedFeePerKey.
        tx2.NetworkFee = netFee;
        tx2.SystemFee = 1000_0000;

        // Calculate overall expected Notary nodes reward.
        var expectedNotaryReward = (NKeys + 1) * defaultNotaryAssistedFeePerKey;

        // Build block to check transaction fee distribution during Gas OnPersist.
        var persistingBlock = new Block
        {
            Header = new Header
            {
                Index = 10,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero,
                Witness = Witness.Empty,
            },
            Transactions = [tx2]
        };
        var snapshot = _snapshot.CloneCache();

        // Designate Notary node.
        var privateKey1 = new byte[32];
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(privateKey1);

        var key1 = new KeyPair(privateKey1);
        var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
        var ret = NativeContract.RoleManagement.Call(
            snapshot,
            new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
            new Block
            {
                Header = (Header)RuntimeHelpers.GetUninitializedObject(typeof(Header)),
                Transactions = []
            },
            "designateAsRole",
            new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.P2PNotary) },
            new ContractParameter(ContractParameterType.Array)
            {
                Value = new List<ContractParameter>(){
                    new(ContractParameterType.ByteArray) { Value = key1.PublicKey.ToArray() },
                },
            }
        );
        snapshot.Commit();

        // Create engine with custom settings (HF_Echidna should be enabled to properly interact with NotaryAssisted attribute).
        var settings = ProtocolSettings.Default with
        {
            Network = 0x334F454Eu,
            StandbyCommittee = [
                ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1),
                ECPoint.Parse("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", ECCurve.Secp256r1),
                ECPoint.Parse("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", ECCurve.Secp256r1),
                ECPoint.Parse("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", ECCurve.Secp256r1),
                ECPoint.Parse("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", ECCurve.Secp256r1),
                ECPoint.Parse("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", ECCurve.Secp256r1),
                ECPoint.Parse("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", ECCurve.Secp256r1)
            ],
            ValidatorsCount = 7,
            Hardforks = []
        };

        // Imitate Blockchain's Persist behaviour: OnPersist + transactions processing.
        // Execute OnPersist firstly:
        var script = new ScriptBuilder();
        script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);

        var engine = ApplicationEngine.Create(TriggerType.OnPersist,
            new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
            snapshot, persistingBlock, settings: settings);
        engine.LoadScript(script.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
        snapshot.Commit();

        // Process transaction that changes NotaryServiceFeePerKey after OnPersist.
        ret = NativeContract.Policy.Call(engine, "setAttributeFee",
            new(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.NotaryAssisted },
            new(ContractParameterType.Integer) { Value = newNotaryAssistedFeePerKey });
        Assert.IsNull(ret);
        snapshot.Commit();

        // Process tx2 with NotaryAssisted attribute.
        engine = ApplicationEngine.Create(TriggerType.Application, tx2, snapshot, persistingBlock, settings: TestProtocolSettings.Default, tx2.SystemFee);
        engine.LoadScript(tx2.Script);
        Assert.AreEqual(VMState.HALT, engine.Execute());
        snapshot.Commit();

        // Ensure that Notary reward is distributed based on the old value of NotaryAssisted price
        // and no underflow happens during GAS distribution.
        var validators = NativeContract.NEO.GetNextBlockValidators(engine.SnapshotCache, engine.ProtocolSettings.ValidatorsCount);
        var primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock!.PrimaryIndex]).ToScriptHash();
        Assert.AreEqual(netFee - expectedNotaryReward, NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, primary));

        var scriptHash = Contract.CreateSignatureRedeemScript(key1.PublicKey).ToScriptHash();
        Assert.AreEqual(expectedNotaryReward, NativeContract.TokenManagement.BalanceOf(engine.SnapshotCache, NativeContract.Governance.GasTokenId, scriptHash));
    }

    [TestMethod]
    public void Check_OnPersist_NotaryRewards()
    {
        // Hardcode test values.
        const uint defaultNotaryssestedFeePerKey = 1000_0000;
        const byte NKeys1 = 4;
        const byte NKeys2 = 6;

        // Generate two transactions with NotaryAssisted attributes with hardcoded NKeys values.
        var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
        var tx1 = TestUtils.GetTransaction(from);
        tx1.Attributes = [new NotaryAssisted() { NKeys = NKeys1 }];

        var netFee1 = 1_0000_0000;
        tx1.NetworkFee = netFee1;

        var tx2 = TestUtils.GetTransaction(from);
        tx2.Attributes = [new NotaryAssisted() { NKeys = NKeys2 }];
        var netFee2 = 2_0000_0000;
        tx2.NetworkFee = netFee2;

        // Calculate overall expected Notary nodes reward.
        var expectedNotaryReward = (NKeys1 + 1) * defaultNotaryssestedFeePerKey + (NKeys2 + 1) * defaultNotaryssestedFeePerKey;

        // Build block to check transaction fee distribution during Gas OnPersist.
        var persistingBlock = new Block
        {
            Header = new Header
            {
                Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero,
                Witness = Witness.Empty,
            },
            Transactions = [tx1, tx2]
        };
        var snapshot = _snapshot.CloneCache();

        // Designate several Notary nodes.
        var privateKey1 = new byte[32];
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(privateKey1);

        var key1 = new KeyPair(privateKey1);
        var privateKey2 = new byte[32];
        rng.GetBytes(privateKey2);

        var key2 = new KeyPair(privateKey2);
        var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
        var ret = NativeContract.RoleManagement.Call(
            snapshot,
            new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
            new Block
            {
                Header = (Header)RuntimeHelpers.GetUninitializedObject(typeof(Header)),
                Transactions = []
            },
            "designateAsRole",
            new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.P2PNotary) },
            new ContractParameter(ContractParameterType.Array)
            {
                Value = new List<ContractParameter>(){
                    new(ContractParameterType.ByteArray) { Value = key1.PublicKey.ToArray() },
                    new(ContractParameterType.ByteArray) { Value = key2.PublicKey.ToArray() },
                },
            }
        );
        snapshot.Commit();

        var script = new ScriptBuilder();
        script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
        var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default);

        // Check that block's Primary balance is 0.
        var validators = NativeContract.NEO.GetNextBlockValidators(engine.SnapshotCache, engine.ProtocolSettings.ValidatorsCount);
        var primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock!.PrimaryIndex]).ToScriptHash();
        Assert.AreEqual(0, NativeContract.TokenManagement.BalanceOf(engine.SnapshotCache, NativeContract.Governance.GasTokenId, primary));

        // Execute OnPersist script.
        engine.LoadScript(script.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());

        // Check that proper amount of GAS was minted to block's Primary and the rest
        // is evenly devided between designated Notary nodes as a reward.
        // Notification order: burn tx1, burn tx2, mint primary, mint Notary1, mint Notary2
        Assert.HasCount(2 + 1 + 2, engine.Notifications);

        // Verify primary balance (minted amount = netFee1 + netFee2 - expectedNotaryReward)
        var expectedPrimaryAmount = netFee1 + netFee2 - expectedNotaryReward;
        Assert.AreEqual(expectedPrimaryAmount, NativeContract.TokenManagement.BalanceOf(engine.SnapshotCache, NativeContract.Governance.GasTokenId, primary));

        // Find the mint notification to primary (from=null, to=primary)
        var primaryMintNotification = engine.Notifications.FirstOrDefault(n =>
            n.EventName == "Transfer" &&
            new UInt160(n.State[0].GetSpan()) == NativeContract.Governance.GasTokenId &&
            n.State[1].IsNull &&
            new UInt160(n.State[2].GetSpan()) == primary);
        Assert.IsNotNull(primaryMintNotification, "Primary mint notification not found");
        Assert.AreEqual(expectedPrimaryAmount, primaryMintNotification.State[3].GetInteger());

        var scriptHash1 = Contract.CreateSignatureRedeemScript(key1.PublicKey).ToScriptHash();
        var expectedNotaryRewardPerNode = expectedNotaryReward / 2;
        Assert.AreEqual(expectedNotaryRewardPerNode, NativeContract.TokenManagement.BalanceOf(engine.SnapshotCache, NativeContract.Governance.GasTokenId, scriptHash1));

        // Find the mint notification to Notary1 (from=null, to=scriptHash1)
        var notary1MintNotification = engine.Notifications.FirstOrDefault(n =>
            n.EventName == "Transfer" &&
            new UInt160(n.State[0].GetSpan()) == NativeContract.Governance.GasTokenId &&
            n.State[1].IsNull &&
            new UInt160(n.State[2].GetSpan()) == scriptHash1);
        Assert.IsNotNull(notary1MintNotification, "Notary1 mint notification not found");
        Assert.AreEqual(expectedNotaryRewardPerNode, notary1MintNotification.State[3].GetInteger());

        var scriptHash2 = Contract.CreateSignatureRedeemScript(key2.PublicKey).ToScriptHash();
        Assert.AreEqual(expectedNotaryRewardPerNode, NativeContract.TokenManagement.BalanceOf(engine.SnapshotCache, NativeContract.Governance.GasTokenId, scriptHash2));

        // Find the mint notification to Notary2 (from=null, to=scriptHash2)
        var notary2MintNotification = engine.Notifications.FirstOrDefault(n =>
            n.EventName == "Transfer" &&
            new UInt160(n.State[0].GetSpan()) == NativeContract.Governance.GasTokenId &&
            n.State[1].IsNull &&
            new UInt160(n.State[2].GetSpan()) == scriptHash2);
        Assert.IsNotNull(notary2MintNotification, "Notary2 mint notification not found");
        Assert.AreEqual(expectedNotaryRewardPerNode, notary2MintNotification.State[3].GetInteger());
    }

    internal static StorageKey CreateStorageKey(byte prefix, uint key)
    {
        return CreateStorageKey(prefix, BitConverter.GetBytes(key));
    }

    internal static StorageKey CreateStorageKey(byte prefix, byte[]? key = null)
    {
        var buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
        buffer[0] = prefix;
        key?.CopyTo(buffer.AsSpan(1));
        return new() { Id = NativeContract.Governance.Id, Key = buffer };
    }

    private static bool TransferGAS(DataCache snapshot, byte[]? from, byte[]? to, BigInteger amount, bool signFrom, Block persistingBlock, object? data)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Nep17NativeContractExtensions.ManualWitness(signFrom ? [new UInt160(from)] : []), snapshot, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, from, to, amount, data);
        engine.LoadScript(script.ToArray());

        if (engine.Execute() == VMState.FAULT)
        {
            throw engine.FaultException!;
        }

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Boolean>(result);

        return result.GetBoolean();
    }

    private static bool TransferGASWithTransaction(DataCache snapshot, byte[] from, byte[] to, BigInteger amount, bool signFrom, Block persistingBlock, object data)
    {
        using var engine = ApplicationEngine.Create(TriggerType.Application,
            new Transaction() { Signers = signFrom ? [new() { Account = new(from), Scopes = WitnessScope.Global }] : [], Attributes = [], Witnesses = null! },
            snapshot, persistingBlock, settings: TestProtocolSettings.Default);

        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, from, to, amount, data);
        engine.LoadScript(script.ToArray());

        if (engine.Execute() == VMState.FAULT)
        {
            throw engine.FaultException!;
        }

        var result = engine.ResultStack.Pop();
        Assert.IsInstanceOfType<Boolean>(result);

        return result.GetBoolean();
    }
}
