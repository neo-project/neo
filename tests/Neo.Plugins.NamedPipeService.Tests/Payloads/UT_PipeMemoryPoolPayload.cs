// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeMemoryPool.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.Models.Payloads;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Plugins.NamedPipeService.Tests.Payloads
{
    [TestClass]
    public class UT_PipeMemoryPoolPayload
    {
        [TestMethod]
        public void IPipeMessage_FromArray_Null()
        {
            var expectedPayload = new PipeMemoryPoolPayload()
            {
                UnVerifiedTransactions = [],
                VerifiedTransactions = [],
            };
            var expectedBytes = expectedPayload.ToArray();

            var actualPayload = new PipeMemoryPoolPayload();
            actualPayload.FromArray(expectedBytes);

            var actualBytes = actualPayload.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_Data()
        {
            var expectedPayload = new PipeMemoryPoolPayload()
            {
                UnVerifiedTransactions = [EmptyTx()],
                VerifiedTransactions = [EmptyTx()],
            };
            var expectedBytes = expectedPayload.ToArray();

            var actualPayload = new PipeMemoryPoolPayload();
            actualPayload.FromArray(expectedBytes);

            var actualBytes = actualPayload.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
        }

        [TestMethod]
        public void IPipeMessage_ToArray_Data()
        {
            var expectedPayload = new PipeMemoryPoolPayload()
            {
                UnVerifiedTransactions = [EmptyTx()],
                VerifiedTransactions = [EmptyTx()],
            };
            var expectedBytes = expectedPayload.ToArray();

            var actualPayload = new PipeMemoryPoolPayload()
            {
                UnVerifiedTransactions = [EmptyTx()],
                VerifiedTransactions = [EmptyTx()],
            };
            var actualBytes = actualPayload.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
        }

        private static Transaction EmptyTx() =>
            new()
            {
                Signers = [
                    new()
                    {
                        Account = new(),
                        Rules = [],
                        AllowedContracts = [],
                        AllowedGroups = [],
                        Scopes = WitnessScope.Global,
                    }
                ],
                Witnesses = [
                    new()
                    {
                        InvocationScript = Memory<byte>.Empty,
                        VerificationScript = Memory<byte>.Empty,
                    }
                ],
                Attributes = [],
                Script = new byte[(byte)OpCode.RET],
            };
    }
}
