// Copyright (C) 2015-2025 The Neo Project.
//
// UT_GasToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.IO;
using Neo.Extensions.VM;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native;

[TestClass]
public class UT_GasToken
{
    private DataCache _snapshotCache = null!;

    [TestInitialize]
    public void TestSetup()
    {
        _snapshotCache = TestBlockchain.GetTestSnapshotCache();
    }

    [TestMethod]
    public void Check_Name() => Assert.AreEqual("GasToken", Governance.GasTokenName);

    [TestMethod]
    public void Check_Symbol() => Assert.AreEqual("GAS", Governance.GasTokenSymbol);

    [TestMethod]
    public void Check_Decimals() => Assert.AreEqual(8, Governance.GasTokenDecimals);

    [TestMethod]
    public async Task Check_BalanceOfTransferAndBurn()
    {
        var snapshot = _snapshotCache.CloneCache();
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
        byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
        byte[] to = new byte[20];
        var tokenInfo = NativeContract.TokenManagement.GetTokenInfo(snapshot, NativeContract.Governance.GasTokenId);
        var supply = tokenInfo!.TotalSupply;
        Assert.AreEqual(5200000050000000, supply); // 3000000000000000 + 50000000 (neo holder reward)

        var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
        snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
        var keyCount = snapshot.GetChangeSet().Count();
        // Check unclaim

        var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
        Assert.AreEqual(new BigInteger(0.5 * 1000 * 100000000L), unclaim.Value);
        Assert.IsTrue(unclaim.State);

        // Transfer

        Assert.IsTrue(NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.Zero, true, persistingBlock));
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = NativeContract.NEO.Transfer(snapshot, from, null, BigInteger.Zero, true, persistingBlock));
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = NativeContract.NEO.Transfer(snapshot, null, to, BigInteger.Zero, false, persistingBlock));
        Assert.AreEqual(100000000, NativeContract.NEO.BalanceOf(snapshot, from));
        Assert.AreEqual(0, NativeContract.NEO.BalanceOf(snapshot, to));

        Assert.AreEqual(52000500_00000000, NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, new UInt160(from)));
        Assert.AreEqual(0, NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, new UInt160(to)));

        // Check unclaim

        unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
        Assert.AreEqual(new BigInteger(0), unclaim.Value);
        Assert.IsTrue(unclaim.State);

        tokenInfo = NativeContract.TokenManagement.GetTokenInfo(snapshot, NativeContract.Governance.GasTokenId);
        supply = tokenInfo!.TotalSupply;
        Assert.AreEqual(5200050050000000, supply);

        Assert.AreEqual(keyCount + 3, snapshot.GetChangeSet().Count()); // Gas

        // Transfer

        keyCount = snapshot.GetChangeSet().Count();

        using (var engine1 = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(), snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        using (ScriptBuilder sb = new())
        {
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, from, to, 52000500_00000000, null);
            engine1.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, engine1.Execute());
            Assert.IsFalse(engine1.ResultStack.Pop().GetBoolean()); // Not signed
        }

        using (var engine2 = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(new UInt160(from)), snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        using (ScriptBuilder sb = new())
        {
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, from, to, 52000500_00000001, null);
            engine2.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, engine2.Execute());
            Assert.IsFalse(engine2.ResultStack.Pop().GetBoolean()); // More than balance
        }

        using (var engine3 = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(new UInt160(from)), snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        using (ScriptBuilder sb = new())
        {
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, from, to, 52000500_00000000, null);
            engine3.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, engine3.Execute());
            Assert.IsTrue(engine3.ResultStack.Pop().GetBoolean()); // All balance
        }

        // Balance of

        Assert.AreEqual(52000500_00000000, NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, new UInt160(to)));
        Assert.AreEqual(0, NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, new UInt160(from)));

        Assert.AreEqual(keyCount + 1, snapshot.GetChangeSet().Count()); // All

        // Burn

        using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        using (ScriptBuilder sb = new())
        {
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "burn", NativeContract.Governance.GasTokenId, to, BigInteger.MinusOne);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsInstanceOfType<ArgumentOutOfRangeException>(engine.FaultException);
        }

        // Burn more than expected

        using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        using (ScriptBuilder sb = new())
        {
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "burn", NativeContract.Governance.GasTokenId, to, 52000500_00000001);
            var context = engine.LoadScript(sb.ToArray());
            context.GetState<ExecutionContextState>().ScriptHash = NativeContract.Governance.Hash;
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsInstanceOfType<InvalidOperationException>(engine.FaultException);
        }

        // Real burn

        using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        {
            engine.LoadScript(Array.Empty<byte>());
            await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, to, BigInteger.One, assertOwner: false);
            Assert.AreEqual(5200049999999999, NativeContract.TokenManagement.BalanceOf(engine.SnapshotCache, NativeContract.Governance.GasTokenId, new UInt160(to)));
            Assert.AreEqual(2, engine.SnapshotCache.GetChangeSet().Count());
        }

        // Burn all

        using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        {
            engine.LoadScript(Array.Empty<byte>());
            await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, to, 5200049999999999, assertOwner: false);
            Assert.AreEqual(keyCount - 2, engine.SnapshotCache.GetChangeSet().Count());
        }

        // Bad inputs

        using (var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(new UInt160(from)), snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        using (ScriptBuilder sb = new())
        {
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, from, to, BigInteger.MinusOne, null);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsInstanceOfType<ArgumentOutOfRangeException>(engine.FaultException);
        }

        using (var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(), snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        using (ScriptBuilder sb = new())
        {
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, new byte[19], to, BigInteger.One, null);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsInstanceOfType<FormatException>(engine.FaultException);
        }

        using (var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(), snapshot, persistingBlock, settings: TestProtocolSettings.Default))
        using (ScriptBuilder sb = new())
        {
            sb.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transfer", NativeContract.Governance.GasTokenId, from, new byte[19], BigInteger.One, null);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsInstanceOfType<FormatException>(engine.FaultException);
        }
    }

    internal static StorageKey CreateStorageKey(byte prefix, uint key)
    {
        return CreateStorageKey(prefix, BitConverter.GetBytes(key));
    }

    internal static StorageKey CreateStorageKey(byte prefix, byte[]? key = null)
    {
        byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
        buffer[0] = prefix;
        key?.CopyTo(buffer.AsSpan(1));
        return new()
        {
            Id = NativeContract.Governance.Id,
            Key = buffer
        };
    }
}
