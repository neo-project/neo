// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeContractState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.Models.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Plugins.NamedPipeService.Tests.Payloads
{
    [TestClass]
    public class UT_PipeContractState
    {
        [TestMethod]
        public void IPipeMessage_FromArray_Null()
        {
            var expectedPayload = new PipeContractState();
            var expectedBytes = expectedPayload.ToByteArray();

            var actualPayload = new PipeContractState();
            actualPayload.FromByteArray(expectedBytes);

            var actualBytes = actualPayload.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_Data()
        {
            var expectedPayload = new PipeContractState()
            {
                Id = 1,
                UpdateCounter = 99,
                Hash = UInt160.Parse("0x00AA00AA00AA00AA00AA00FF00FF00FF00FF00FF"),
                Nef = null,
                Manifest = null,
            };
            var expectedBytes = expectedPayload.ToByteArray();

            var actualPayload = new PipeContractState();
            actualPayload.FromByteArray(expectedBytes);

            var actualBytes = actualPayload.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
            Assert.AreEqual(1, actualPayload.Id);
            Assert.AreEqual(expectedPayload.Id, actualPayload.Id);
            Assert.AreEqual(99u, actualPayload.UpdateCounter);
            Assert.AreEqual(expectedPayload.UpdateCounter, actualPayload.UpdateCounter);
            Assert.AreEqual(UInt160.Parse("0x00AA00AA00AA00AA00AA00FF00FF00FF00FF00FF"), actualPayload.Hash);
            Assert.AreEqual(expectedPayload.Hash, actualPayload.Hash);
            Assert.IsNull(actualPayload.Nef);
            Assert.IsNull(actualPayload.Manifest);
        }

        [TestMethod]
        public void IPipeMessage_ToArray_Data()
        {
            var expectedPayload = new PipeContractState()
            {
                Id = 1,
                UpdateCounter = 99,
                Hash = UInt160.Parse("0x00AA00AA00AA00AA00AA00FF00FF00FF00FF00FF"),
                Nef = null,
                Manifest = null,
            };
            var expectedBytes = expectedPayload.ToByteArray();

            var actualPayload = new PipeContractState()
            {
                Id = 1,
                UpdateCounter = 99,
                Hash = UInt160.Parse("0x00AA00AA00AA00AA00AA00FF00FF00FF00FF00FF"),
                Nef = null,
                Manifest = null,
            };
            var actualBytes = actualPayload.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
            Assert.AreEqual(1, expectedPayload.Id);
            Assert.AreEqual(99u, expectedPayload.UpdateCounter);
            Assert.AreEqual(UInt160.Parse("0x00AA00AA00AA00AA00AA00FF00FF00FF00FF00FF"), expectedPayload.Hash);
            Assert.AreEqual(1, actualPayload.Id);
            Assert.AreEqual(99u, actualPayload.UpdateCounter);
            Assert.AreEqual(UInt160.Parse("0x00AA00AA00AA00AA00AA00FF00FF00FF00FF00FF"), actualPayload.Hash);
            Assert.IsNull(expectedPayload.Nef);
            Assert.IsNull(expectedPayload.Manifest);
            Assert.IsNull(actualPayload.Nef);
            Assert.IsNull(actualPayload.Manifest);
        }
    }
}
