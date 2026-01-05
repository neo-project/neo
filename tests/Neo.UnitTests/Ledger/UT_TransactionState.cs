// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TransactionState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Factories;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Array = System.Array;

namespace Neo.UnitTests.Ledger;

[TestClass]
public class UT_TransactionState
{
    private TransactionState _origin = null!;
    private TransactionState _originTrimmed = null!;

    [TestInitialize]
    public void Initialize()
    {
        _origin = new TransactionState
        {
            BlockIndex = 1,
            Transaction = new Transaction()
            {
                Nonce = RandomNumberFactory.NextUInt32(),
                Attributes = [],
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Signers = [new() { Account = UInt160.Zero }],
                Witnesses = [Witness.Empty]
            }
        };
        _originTrimmed = new TransactionState
        {
            BlockIndex = 1,
        };
    }

    [TestMethod]
    public void TestDeserialize()
    {
        var data = BinarySerializer.Serialize(((IInteroperable)_origin).ToStackItem(null), ExecutionEngineLimits.Default);
        var reader = new MemoryReader(data);

        TransactionState dest = new();
        ((IInteroperable)dest).FromStackItem(BinarySerializer.Deserialize(ref reader, ExecutionEngineLimits.Default, null));

        Assert.AreEqual(_origin.BlockIndex, dest.BlockIndex);
        Assert.AreEqual(_origin.Transaction!.Hash, dest.Transaction!.Hash);
        Assert.IsNotNull(dest.Transaction);
    }

    [TestMethod]
    public void TestClone()
    {
        var clone = (TransactionState)((IInteroperable)_origin).Clone();
        CollectionAssert.AreEqual(
            BinarySerializer.Serialize(((IInteroperable)clone).ToStackItem(null), ExecutionEngineLimits.Default),
            BinarySerializer.Serialize((_origin as IInteroperable).ToStackItem(null), ExecutionEngineLimits.Default)
            );
        clone.Transaction!.Nonce++;
        Assert.AreNotEqual(clone.Transaction.Nonce, _origin.Transaction!.Nonce);
        CollectionAssert.AreNotEqual(
            BinarySerializer.Serialize((clone as IInteroperable).ToStackItem(null), ExecutionEngineLimits.Default),
            BinarySerializer.Serialize((_origin as IInteroperable).ToStackItem(null), ExecutionEngineLimits.Default)
            );
    }

    [TestMethod]
    public void AvoidReplicaBug()
    {
        var replica = new TransactionState();
        (replica as IInteroperable).FromReplica(_origin);
        Assert.AreEqual(replica.Transaction!.Nonce, _origin.Transaction!.Nonce);
        CollectionAssert.AreEqual(
            ((Struct)((IInteroperable)replica).ToStackItem(null))[1].GetSpan().ToArray(),
            ((Struct)((IInteroperable)_origin).ToStackItem(null))[1].GetSpan().ToArray());

        var newOrigin = new TransactionState
        {
            BlockIndex = 2,
            Transaction = new Transaction()
            {
                Nonce = RandomNumberFactory.NextUInt32(),
                NetworkFee = _origin.Transaction.NetworkFee++, // more fee
                Attributes = [],
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Signers = [new() { Account = UInt160.Zero }],
                Witnesses = [ new Witness() {
                    InvocationScript=Array.Empty<byte>(),
                    VerificationScript=Array.Empty<byte>()
                } ]
            }
        };
        (replica as IInteroperable).FromReplica(newOrigin);
        Assert.AreEqual(replica.Transaction.Nonce, newOrigin.Transaction.Nonce);
        Assert.AreEqual(replica.Transaction.NetworkFee, newOrigin.Transaction.NetworkFee);
        CollectionAssert.AreEqual(
            ((Struct)((IInteroperable)replica).ToStackItem(null))[1].GetSpan().ToArray(),
            ((Struct)((IInteroperable)newOrigin).ToStackItem(null))[1].GetSpan().ToArray());
    }

    [TestMethod]
    public void TestDeserializeTrimmed()
    {
        var data = BinarySerializer.Serialize(((IInteroperable)_originTrimmed).ToStackItem(null), ExecutionEngineLimits.Default);
        var reader = new MemoryReader(data);

        TransactionState dest = new();
        ((IInteroperable)dest).FromStackItem(BinarySerializer.Deserialize(ref reader, ExecutionEngineLimits.Default, null));

        Assert.AreEqual(_originTrimmed.BlockIndex, dest.BlockIndex);
        Assert.IsNull(dest.Transaction);
    }
}
