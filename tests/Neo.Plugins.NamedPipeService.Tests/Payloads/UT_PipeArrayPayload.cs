// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeArrayPayload.cs file belongs to the neo project and is free
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
    public class UT_PipeArrayPayload
    {
        [TestMethod]
        public void IPipeMessage_FromArray_Null()
        {
            var expectedPayload = new PipeArrayPayload<PipeUnmanagedPayload<int>>()
            {
                Value = [],
            };
            var expectedBytes = expectedPayload.ToByteArray();

            var actualPayload = new PipeArrayPayload<PipeUnmanagedPayload<int>>();
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
            var expectedPayload = new PipeArrayPayload<PipeUnmanagedPayload<int>>()
            {
                Value = [new() { Value = 1, }, new() { Value = 2, }],
            };
            var expectedBytes = expectedPayload.ToByteArray();

            var actualPayload = new PipeArrayPayload<PipeUnmanagedPayload<int>>();
            actualPayload.FromByteArray(expectedBytes);

            var actualBytes = actualPayload.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
        }

        [TestMethod]
        public void IPipeMessage_ToArray_Data()
        {
            var expectedPayload = new PipeArrayPayload<PipeUnmanagedPayload<int>>()
            {
                Value = [new() { Value = 1, }, new() { Value = 2, }],
            };
            var expectedBytes = expectedPayload.ToByteArray();

            var actualPayload = new PipeArrayPayload<PipeUnmanagedPayload<int>>()
            {
                Value = [new() { Value = 1, }, new() { Value = 2, }],
            };
            var actualBytes = actualPayload.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_AsExceptionPayload()
        {
            var expectedPayload = new PipeArrayPayload<PipeExceptionPayload>()
            {
                Value = [new(new Exception("Hello")), new(new Exception("World"))],
            };
            var expectedBytes = expectedPayload.ToByteArray();

            var actualPayload = new PipeArrayPayload<PipeExceptionPayload>();
            actualPayload.FromByteArray(expectedBytes);

            var actualBytes = actualPayload.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
            Assert.AreEqual("Hello", expectedPayload.Value[0].Message);
            Assert.AreEqual("World", expectedPayload.Value[1].Message);
        }

        [TestMethod]
        public void IPipeMessage_ToArray_AsExceptionPayload()
        {
            var expectedPayload = new PipeArrayPayload<PipeExceptionPayload>()
            {
                Value = [new(new Exception("Hello")), new(new Exception("World"))],
            };
            var expectedBytes = expectedPayload.ToByteArray();

            var actualPayload = new PipeArrayPayload<PipeExceptionPayload>()
            {
                Value = [new(new Exception("Hello")), new(new Exception("World"))],
            };
            var actualBytes = actualPayload.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Size, actualBytes.Length);
            Assert.AreEqual(expectedPayload.Size, expectedBytes.Length);
            Assert.AreEqual(expectedPayload.Value[0].Message, actualPayload.Value[0].Message);
            Assert.AreEqual(expectedPayload.Value[1].Message, actualPayload.Value[1].Message);
        }
    }
}
