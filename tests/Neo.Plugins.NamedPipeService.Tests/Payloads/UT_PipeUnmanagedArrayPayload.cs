// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeUnmanagedArrayPayload.cs file belongs to the neo project and is free
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
    public class UT_PipeUnmanagedArrayPayload
    {
        [TestMethod]
        public void IPipeMessage_FromArray_Null()
        {
            var expectedPayload = new PipeUnmanagedArrayPayload<int>()
            {
                Value = [],
            };
            var expectedBytes = expectedPayload.ToArray();

            var actualPayload = new PipeUnmanagedArrayPayload<int>();
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
            var expectedPayload = new PipeUnmanagedArrayPayload<int>()
            {
                Value = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0],
            };
            var expectedBytes = expectedPayload.ToArray();

            var actualPayload = new PipeUnmanagedArrayPayload<int>();
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
            var expectedPayload = new PipeUnmanagedArrayPayload<int>()
            {
                Value = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0],
            };
            var expectedBytes = expectedPayload.ToArray();

            var actualPayload = new PipeUnmanagedArrayPayload<int>()
            {
                Value = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0],
            };
            var actualBytes = actualPayload.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
        }
    }
}
