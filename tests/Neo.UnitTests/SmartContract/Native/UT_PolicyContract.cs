// Copyright (C) 2015-2026 The Neo Project.
//
// UT_PolicyContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Extensions.IO;
using Neo.Extensions.VM;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native;

[TestClass]
public class UT_PolicyContract
{
    private DataCache _snapshotCache = null!;

    [TestInitialize]
    public void TestSetup()
    {
        _snapshotCache = TestBlockchain.GetTestSnapshotCache();
    }

    [TestMethod]
    public void Check_Default()
    {
        var snapshot = _snapshotCache.CloneCache();

        var ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(1000, ret.GetInteger());

        ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts });
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(PolicyContract.DefaultAttributeFee, ret.GetInteger());

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = NativeContract.Policy.Call(snapshot, "getAttributeFee", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)byte.MaxValue }));
    }

    [TestMethod]
    public void Check_SetAttributeFee()
    {
        var snapshot = _snapshotCache.CloneCache();

        // Fake blockchain
        Block block = new()
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

        var attr = new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts };

        // Without signature
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
            "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
        });

        var ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(0, ret.GetInteger());

        // With signature, wrong value
        UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 11_0000_0000 });
        });

        ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(0, ret.GetInteger());

        // Proper set
        ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
        Assert.IsInstanceOfType<Null>(ret);

        ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(300300, ret.GetInteger());

        // Set to zero
        ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 0 });
        Assert.IsInstanceOfType<Null>(ret);

        ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(0, ret.GetInteger());
    }

    [TestMethod]
    public void Check_SetFeePerByte()
    {
        var snapshot = _snapshotCache.CloneCache();

        // Fake blockchain

        Block block = new()
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

        // Without signature

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
            "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
        });

        var ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(1000, ret.GetInteger());

        // With signature
        UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
        ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
        Assert.IsInstanceOfType<Null>(ret);

        ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(1, ret.GetInteger());
    }

    [TestMethod]
    public void Check_SetBaseExecFee()
    {
        var snapshot = _snapshotCache.CloneCache();

        // Fake blockchain

        Block block = new()
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

        // Without signature

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
            "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 50 });
        });

        var ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(30, ret.GetInteger());

        // With signature, wrong value
        UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
        });

        ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(30, ret.GetInteger());

        // Proper set
        ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 50 });
        Assert.IsInstanceOfType<Null>(ret);

        ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(50, ret.GetInteger());
    }

    [TestMethod]
    public void Check_SetStoragePrice()
    {
        var snapshot = _snapshotCache.CloneCache();

        // Fake blockchain

        Block block = new()
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

        // Without signature

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
            "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
        });

        var ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(100000, ret.GetInteger());

        // With signature, wrong value
        UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 100000000 });
        });

        ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(100000, ret.GetInteger());

        // Proper set
        ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
        Assert.IsInstanceOfType<Null>(ret);

        ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
        Assert.IsInstanceOfType<Integer>(ret);
        Assert.AreEqual(300300, ret.GetInteger());
    }

    [TestMethod]
    public void Check_BlockAccount()
    {
        var snapshot = _snapshotCache.CloneCache();

        // Fake blockchain

        Block block = new()
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

        // Without signature

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), block,
            "blockAccount",
            new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
        });

        // With signature

        UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
        var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
          "blockAccount",
          new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
        Assert.IsInstanceOfType<Boolean>(ret);
        Assert.IsTrue(ret.GetBoolean());

        // Same account
        ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "blockAccount",
            new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
        Assert.IsInstanceOfType<Boolean>(ret);
        Assert.IsFalse(ret.GetBoolean());

        // Account B

        ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "blockAccount",
            new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
        Assert.IsInstanceOfType<Boolean>(ret);
        Assert.IsTrue(ret.GetBoolean());

        // Check

        Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));
        Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01")));
        Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01")));
    }

    [TestMethod]
    public void Check_Block_UnblockAccount()
    {
        var snapshot = _snapshotCache.CloneCache();

        // Fake blockchain

        Block block = new()
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
        UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

        // Block without signature

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
            "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
        });

        Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

        // Block with signature

        var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
        Assert.IsInstanceOfType<Boolean>(ret);
        Assert.IsTrue(ret.GetBoolean());

        Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

        // Unblock without signature

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
            "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
        });

        Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

        // Unblock with signature

        ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
        Assert.IsInstanceOfType<Boolean>(ret);
        Assert.IsTrue(ret.GetBoolean());

        Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));
    }

    [TestMethod]
    public void TestListBlockedAccounts()
    {
        var snapshot = _snapshotCache.CloneCache();

        // Fake blockchain

        Block block = new()
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
        UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

        var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
            "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
        Assert.IsInstanceOfType<Boolean>(ret);
        Assert.IsTrue(ret.GetBoolean());

        Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

        var sb = new ScriptBuilder()
            .EmitDynamicCall(NativeContract.Policy.Hash, "getBlockedAccounts");

        var engine = ApplicationEngine.Run(sb.ToArray(), snapshot, null, block, TestBlockchain.GetSystem().Settings);

        Assert.IsInstanceOfType<InteropInterface>(engine.ResultStack[0]);

        var iter = engine.ResultStack[0].GetInterface<StorageIterator>()!;
        Assert.IsTrue(iter.Next());
        Assert.AreEqual(new UInt160(iter.Value(new ReferenceCounter()).GetSpan()), UInt160.Zero);
    }

    [TestMethod]
    public void TestWhiteListFee()
    {
        // Create script

        var snapshotCache = _snapshotCache.CloneCache();

        byte[] script;
        using (var sb = new ScriptBuilder())
        {
            sb.EmitDynamicCall(NativeContract.NEO.Hash, "balanceOf", NativeContract.NEO.GetCommitteeAddress(_snapshotCache.CloneCache()));
            script = sb.ToArray();
        }

        var engine = CreateEngineWithCommitteeSigner(snapshotCache, script);

        // Not whitelisted

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(0, engine.ResultStack.Pop().GetInteger());
        Assert.AreEqual(2028330, engine.FeeConsumed);
        Assert.AreEqual(0, NativeContract.Policy.CleanWhitelist(engine, NativeContract.NEO.GetContractState(ProtocolSettings.Default, 0)));
        Assert.IsEmpty(engine.Notifications);

        // Whitelist

        engine = CreateEngineWithCommitteeSigner(snapshotCache, script);

        NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "balanceOf", 1, 0);
        engine.SnapshotCache.Commit();

        // Whitelisted

        Assert.HasCount(1, engine.Notifications); // Whitelist changed
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(0, engine.ResultStack.Pop().GetInteger());
        Assert.AreEqual(1045290, engine.FeeConsumed);

        // Clean white list

        engine.SnapshotCache.Commit();
        engine = CreateEngineWithCommitteeSigner(snapshotCache, script);

        Assert.AreEqual(1, NativeContract.Policy.CleanWhitelist(engine, NativeContract.NEO.GetContractState(ProtocolSettings.Default, 0)));
        Assert.HasCount(1, engine.Notifications); // Whitelist deleted
    }

    [TestMethod]
    public void TestSetWhiteListFeeContractNegativeFixedFee()
    {
        var snapshotCache = _snapshotCache.CloneCache();
        var engine = CreateEngineWithCommitteeSigner(snapshotCache);

        // Register a dummy contract
        UInt160 contractHash;
        using (var sb = new ScriptBuilder())
        {
            sb.Emit(OpCode.RET);
            var script = sb.ToArray();
            contractHash = script.ToScriptHash();
            snapshotCache.DeleteContract(contractHash);
            var manifest = TestUtils.CreateManifest("dummy", ContractParameterType.Any);
            manifest.Abi.Methods = [
                new ContractMethodDescriptor
                    {
                        Name = "foo",
                        Parameters = [],
                        ReturnType = ContractParameterType.Any,
                        Offset = 0,
                        Safe = false
                    }
            ];

            var contract = TestUtils.GetContract(script, manifest);
            snapshotCache.AddContract(contractHash, contract);
        }

        // Invoke SetWhiteListFeeContract with fixedFee negative

        Assert.Throws<ArgumentOutOfRangeException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, contractHash, "foo", 1, -1L));
    }

    [TestMethod]
    public void TestSetWhiteListFeeContractWhenContractNotFound()
    {
        var snapshotCache = _snapshotCache.CloneCache();
        var engine = CreateEngineWithCommitteeSigner(snapshotCache);
        var randomHash = new UInt160(Crypto.Hash160([1, 2, 3]).ToArray());
        Assert.ThrowsExactly<InvalidOperationException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, randomHash, "transfer", 3, 10));
    }

    [TestMethod]
    public void TestSetWhiteListFeeContractWhenContractNotInAbi()
    {
        var snapshotCache = _snapshotCache.CloneCache();
        var engine = CreateEngineWithCommitteeSigner(snapshotCache);
        Assert.ThrowsExactly<InvalidOperationException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "noexists", 0, 10));
    }

    [TestMethod]
    public void TestSetWhiteListFeeContractWhenArgCountMismatch()
    {
        var snapshotCache = _snapshotCache.CloneCache();
        var engine = CreateEngineWithCommitteeSigner(snapshotCache);
        // transfer exists with 4 args
        Assert.ThrowsExactly<InvalidOperationException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "transfer", 0, 10));
    }

    [TestMethod]
    public void TestSetWhiteListFeeContractWhenNotCommittee()
    {
        var snapshotCache = _snapshotCache.CloneCache();
        var tx = new Transaction
        {
            Version = 0,
            Nonce = 1,
            Signers = [new() { Account = UInt160.Zero, Scopes = WitnessScope.Global }],
            Attributes = [],
            Witnesses = [new Witness { }],
            Script = new byte[1],
            NetworkFee = 0,
            SystemFee = 0,
            ValidUntilBlock = 0
        };

        using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshotCache, settings: TestProtocolSettings.Default);
        Assert.ThrowsExactly<InvalidOperationException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "transfer", 4, 10));
    }

    [TestMethod]
    public void TestSetWhiteListFeeContractSetContract()
    {
        var snapshotCache = _snapshotCache.CloneCache();
        var engine = CreateEngineWithCommitteeSigner(snapshotCache);
        NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "transfer", 4, 123_456);

        var method = NativeContract.NEO.GetContractState(ProtocolSettings.Default, 0)
                .Manifest.Abi.Methods.Where(u => u.Name == "balanceOf").Single();

        NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, method.Name, method.Parameters.Length, 123_456);
        Assert.IsTrue(NativeContract.Policy.IsWhitelistFeeContract(engine.SnapshotCache, NativeContract.NEO.Hash, method, out var fixedFee));
        Assert.AreEqual(123_456, fixedFee);
    }

    private static ApplicationEngine CreateEngineWithCommitteeSigner(DataCache snapshotCache, byte[]? script = null)
    {
        // Get committe public keys and calculate m
        var committee = NativeContract.NEO.GetCommittee(snapshotCache);
        var m = (committee.Length / 2) + 1;
        var committeeContract = Contract.CreateMultiSigContract(m, committee);

        // Create Tx needed for CheckWitness / CheckCommittee
        var tx = new Transaction
        {
            Version = 0,
            Nonce = 1,
            Signers = [new() { Account = committeeContract.ScriptHash, Scopes = WitnessScope.Global }],
            Attributes = [],
            Witnesses = [new Witness { InvocationScript = new byte[1], VerificationScript = committeeContract.Script }],
            Script = script ?? [(byte)OpCode.NOP],
            NetworkFee = 0,
            SystemFee = 0,
            ValidUntilBlock = 0
        };

        var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshotCache, settings: TestProtocolSettings.Default);
        engine.LoadScript(tx.Script);

        return engine;
    }
}
