// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeStringPayload.cs file belongs to the neo project and is free
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
    public class UT_PipeStringPayload
    {
        [TestMethod]
        public void IPipeMessage_ToArray_Null()
        {
            var payload1 = new PipeStringPayload() { Value = string.Empty };
            var expectedBytes = payload1.ToArray();

            var payload2 = new PipeStringPayload() { Value = string.Empty };
            var actualBytes = payload2.ToArray();
            var actualBytesWithoutHeader = actualBytes;

            CollectionAssert.AreEqual(expectedBytes, actualBytesWithoutHeader);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_Null()
        {
            var payload1 = new PipeStringPayload() { Value = string.Empty };
            var expectedBytes = payload1.ToArray();

            var payload2 = new PipeStringPayload();
            payload2.FromArray(expectedBytes);

            var actualBytes = payload2.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(string.Empty, payload2.Value);
        }

        [TestMethod]
        public void IPipeMessage_ToArray_Data()
        {
            var payload1 = new PipeStringPayload() { Value = "漢字文化圈" };
            var expectedBytes = payload1.ToArray();

            var payload2 = new PipeStringPayload() { Value = "漢字文化圈" };
            var actualBytes = payload2.ToArray();
            var actualBytesWithoutHeader = actualBytes;

            CollectionAssert.AreEqual(expectedBytes, actualBytesWithoutHeader);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_Data()
        {
            var payload1 = new PipeStringPayload() { Value = "漢字文化圈" };
            var expectedBytes = payload1.ToArray();

            var payload2 = new PipeStringPayload();
            payload2.FromArray(expectedBytes);

            var actualBytes = payload2.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual("漢字文化圈", payload2.Value);
        }
    }
}
