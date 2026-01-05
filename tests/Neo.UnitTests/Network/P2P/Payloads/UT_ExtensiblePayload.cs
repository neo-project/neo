// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExtensiblePayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Extensions.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using System.Runtime.CompilerServices;

namespace Neo.UnitTests.Network.P2P.Payloads;

[TestClass]
public class UT_ExtensiblePayload
{
    [TestMethod]
    public void Size_Get()
    {
        var test = new ExtensiblePayload()
        {
            Sender = Array.Empty<byte>().ToScriptHash(),
            Category = "123",
            Data = new byte[] { 1, 2, 3 },
            Witness = new()
            {
                InvocationScript = new byte[] { 3, 5, 6 },
                VerificationScript = ReadOnlyMemory<byte>.Empty
            }
        };
        Assert.AreEqual(42, test.Size);
    }

    [TestMethod]
    public void DeserializeAndSerialize()
    {
        var test = new ExtensiblePayload()
        {
            Category = "123",
            ValidBlockStart = 456,
            ValidBlockEnd = 789,
            Sender = Array.Empty<byte>().ToScriptHash(),
            Data = new byte[] { 1, 2, 3 },
            Witness = new()
            {
                InvocationScript = new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH2, (byte)OpCode.PUSH3 },
                VerificationScript = ReadOnlyMemory<byte>.Empty
            }
        };
        var clone = test.ToArray().AsSerializable<ExtensiblePayload>();

        Assert.AreEqual(test.Sender, clone.Witness.ScriptHash);
        Assert.AreEqual(test.Hash, clone.Hash);
        Assert.AreEqual(test.ValidBlockStart, clone.ValidBlockStart);
        Assert.AreEqual(test.ValidBlockEnd, clone.ValidBlockEnd);
        Assert.AreEqual(test.Category, clone.Category);
    }

    [TestMethod]
    public void Witness()
    {
        IVerifiable item = (ExtensiblePayload)RuntimeHelpers.GetUninitializedObject(typeof(ExtensiblePayload));
        item.Witnesses = [new()];
        Assert.HasCount(1, item.Witnesses);
    }
}
