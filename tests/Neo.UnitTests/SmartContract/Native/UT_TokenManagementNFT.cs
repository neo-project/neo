// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TokenManagementNFT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.IO;
using Neo.Extensions.VM;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native;

[TestClass]
public class UT_TokenManagementNFT
{
    private static readonly UInt160 AssetId = new(Enumerable.Repeat((byte)0x31, UInt160.Length).ToArray());
    private static readonly UInt160 FirstId = new(Enumerable.Repeat((byte)0x41, UInt160.Length).ToArray());
    private static readonly UInt160 SecondId = new(Enumerable.Repeat((byte)0x42, UInt160.Length).ToArray());
    private static readonly UInt160 Recipient = new(Enumerable.Repeat((byte)0x51, UInt160.Length).ToArray());
    private static readonly UInt160 OtherRecipient = new(Enumerable.Repeat((byte)0x52, UInt160.Length).ToArray());

    [TestMethod]
    public void TransferNFT_CallbackSeesUpdatedOwnership()
    {
        var snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        UInt160 owner = AddOwnerContract(snapshot, burn: false);
        AddNFTs(snapshot, owner);

        using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default);
        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.TokenManagement.Hash, "transferNFT", FirstId, owner, Recipient, null);
        engine.LoadScript(script.ToArray()).GetState<ExecutionContextState>().ScriptHash = owner;

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
        Assert.AreEqual(Recipient, NativeContract.TokenManagement.GetNFTInfo(snapshot, FirstId)!.Owner);
        Assert.AreEqual(owner, NativeContract.TokenManagement.GetNFTInfo(snapshot, SecondId)!.Owner);
        Assert.IsTrue(snapshot.Contains(OwnerKey(Recipient, FirstId)));
        Assert.IsFalse(snapshot.Contains(OwnerKey(OtherRecipient, FirstId)));
        Assert.AreEqual(BigInteger.One, NativeContract.TokenManagement.BalanceOf(snapshot, AssetId, owner));
        Assert.AreEqual(BigInteger.One, NativeContract.TokenManagement.BalanceOf(snapshot, AssetId, Recipient));
        Assert.AreEqual(BigInteger.Zero, NativeContract.TokenManagement.BalanceOf(snapshot, AssetId, OtherRecipient));
    }

    [TestMethod]
    public void BurnNFT_CallbackSeesRemovedNFT()
    {
        var snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        UInt160 owner = AddOwnerContract(snapshot, burn: true);
        AddNFTs(snapshot, owner);

        using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default);
        using var script = new ScriptBuilder();
        script.EmitDynamicCall(NativeContract.TokenManagement.Hash, "burnNFT", FirstId);
        engine.LoadScript(script.ToArray()).GetState<ExecutionContextState>().ScriptHash = owner;

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.IsNull(NativeContract.TokenManagement.GetNFTInfo(snapshot, FirstId));
        Assert.AreEqual(owner, NativeContract.TokenManagement.GetNFTInfo(snapshot, SecondId)!.Owner);
        Assert.IsFalse(snapshot.Contains(OwnerKey(owner, FirstId)));
        Assert.AreEqual(BigInteger.One, NativeContract.TokenManagement.GetTokenInfo(snapshot, AssetId)!.TotalSupply);
        Assert.AreEqual(BigInteger.One, NativeContract.TokenManagement.BalanceOf(snapshot, AssetId, owner));
    }

    private static UInt160 AddOwnerContract(DataCache snapshot, bool burn)
    {
        using var prefix = new ScriptBuilder();
        // The fourth callback argument is the balance before the change. Reenter only on the first debit.
        prefix.Emit(OpCode.PUSH3).Emit(OpCode.PICK).Emit(OpCode.PUSH2).Emit(OpCode.NUMEQUAL);

        using var body = new ScriptBuilder();
        for (int i = 0; i < 5; i++) body.Emit(OpCode.DROP);
        if (burn)
        {
            body.EmitDynamicCall(NativeContract.TokenManagement.Hash, "getNFTInfo", FirstId);
            body.Emit(OpCode.ISNULL);
            using var burnCall = new ScriptBuilder();
            burnCall.EmitDynamicCall(NativeContract.TokenManagement.Hash, "burnNFT", FirstId);
            byte[] burnBytes = burnCall.ToArray();
            body.EmitJump(OpCode.JMPIF, 2 + burnBytes.Length);
            body.EmitRaw(burnBytes);
        }
        else
        {
            body.Emit(OpCode.PUSHNULL);
            body.EmitPush(OtherRecipient.ToArray());
            body.EmitSysCall(ApplicationEngine.System_Runtime_GetExecutingScriptHash);
            body.EmitPush(FirstId.ToArray());
            body.Emit(OpCode.PUSH4).Emit(OpCode.PACK);
            body.EmitPush(new BigInteger((byte)CallFlags.All));
            body.EmitPush("transferNFT");
            body.EmitPush(NativeContract.TokenManagement.Hash.ToArray());
            body.EmitSysCall(ApplicationEngine.System_Contract_Call);
            body.Emit(OpCode.DROP);
        }
        body.Emit(OpCode.RET);

        byte[] bodyBytes = body.ToArray();
        using var callback = new ScriptBuilder();
        callback.EmitRaw(prefix.ToArray());
        callback.EmitJump(OpCode.JMPIFNOT, 2 + bodyBytes.Length);
        callback.EmitRaw(bodyBytes);
        for (int i = 0; i < 5; i++) callback.Emit(OpCode.DROP);
        callback.Emit(OpCode.RET);

        var manifest = TestUtils.CreateManifest("_onBalanceChanged", ContractParameterType.Void,
            ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer,
            ContractParameterType.Integer, ContractParameterType.Integer);
        manifest.Abi.Methods[0].Safe = false;
        var contract = TestUtils.GetContract(callback.ToArray(), manifest);
        snapshot.AddContract(contract.Hash, contract);
        return contract.Hash;
    }

    private static void AddNFTs(DataCache snapshot, UInt160 owner)
    {
        snapshot.Add(new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(AssetId), new(new TokenState
        {
            Type = TokenType.NonFungible,
            Owner = owner,
            Name = "Test",
            Symbol = "TST",
            Decimals = 0,
            TotalSupply = 2,
            MaxSupply = -1
        }));
        snapshot.Add(new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(owner).Add(AssetId), new(new AccountState { Balance = 2 }));
        foreach (UInt160 id in new[] { FirstId, SecondId })
        {
            snapshot.Add(new KeyBuilder(NativeContract.TokenManagement.Id, 8).Add(id), new(new NFTState
            {
                AssetId = AssetId,
                Owner = owner,
                Properties = new Map(null)
            }));
            snapshot.Add(OwnerKey(owner, id), new());
            snapshot.Add(new KeyBuilder(NativeContract.TokenManagement.Id, 23).Add(AssetId).Add(id), new());
        }
    }

    private static StorageKey OwnerKey(UInt160 owner, UInt160 id) =>
        new KeyBuilder(NativeContract.TokenManagement.Id, 21).Add(owner).Add(id);
}
