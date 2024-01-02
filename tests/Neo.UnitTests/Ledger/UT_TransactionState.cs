// Copyright (C) 2015-2024 The Neo Project.
//
// UT_TransactionState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TransactionState
    {
        TransactionState origin;

        TransactionState originTrimmed;

        [TestInitialize]
        public void Initialize()
        {
            origin = new TransactionState
            {
                BlockIndex = 1,
                Transaction = new Transaction()
                {
                    Attributes = Array.Empty<TransactionAttribute>(),
                    Script = new byte[] { (byte)OpCode.PUSH1 },
                    Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                    Witnesses = new Witness[] { new Witness() {
                        InvocationScript=Array.Empty<byte>(),
                        VerificationScript=Array.Empty<byte>()
                    } }
                }
            };
            originTrimmed = new TransactionState
            {
                BlockIndex = 1,
            };
        }

        [TestMethod]
        public void TestDeserialize()
        {
            var data = BinarySerializer.Serialize(((IInteroperable)origin).ToStackItem(null), ExecutionEngineLimits.Default);
            var reader = new MemoryReader(data);

            TransactionState dest = new();
            ((IInteroperable)dest).FromStackItem(BinarySerializer.Deserialize(ref reader, ExecutionEngineLimits.Default, null));

            dest.BlockIndex.Should().Be(origin.BlockIndex);
            dest.Transaction.Hash.Should().Be(origin.Transaction.Hash);
            dest.Transaction.Should().NotBeNull();
        }

        [TestMethod]
        public void TestDeserializeTrimmed()
        {
            var data = BinarySerializer.Serialize(((IInteroperable)originTrimmed).ToStackItem(null), ExecutionEngineLimits.Default);
            var reader = new MemoryReader(data);

            TransactionState dest = new();
            ((IInteroperable)dest).FromStackItem(BinarySerializer.Deserialize(ref reader, ExecutionEngineLimits.Default, null));

            dest.BlockIndex.Should().Be(originTrimmed.BlockIndex);
            dest.Transaction.Should().Be(null);
            dest.Transaction.Should().BeNull();
        }
    }
}
