// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeExceptionPayload.cs file belongs to the neo project and is free
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

namespace Neo.Plugins.NamedPipeService.Tests.Payloads
{
    [TestClass]
    public class UT_PipeExceptionPayload
    {
        private static readonly string s_exceptionMessage = "Hello";
        private static readonly string s_exceptionStackTrace = "World";

        [TestMethod]
        public void IPipeMessage_FromArray_Data()
        {
            var exception1 = new PipeExceptionPayload()
            {
                Message = s_exceptionMessage,
                StackTrace = s_exceptionStackTrace
            };
            var expectedBytes = exception1.ToArray();

            var exception2 = new PipeExceptionPayload();
            exception2.FromArray(expectedBytes);

            var actualBytes = exception2.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(exception1.IsEmpty, exception2.IsEmpty);
            Assert.AreEqual(exception1.Message, exception2.Message);
            Assert.AreEqual(exception1.StackTrace, exception2.StackTrace);
        }

        [TestMethod]
        public void IPipeMessage_ToArray_Data()
        {
            var exception1 = new PipeExceptionPayload()
            {
                Message = s_exceptionMessage,
                StackTrace = s_exceptionStackTrace
            };
            var expectedBytes = exception1.ToArray();

            var exception2 = new PipeExceptionPayload()
            {
                Message = s_exceptionMessage,
                StackTrace = s_exceptionStackTrace
            };
            var actualBytes = exception2.ToArray();
            var actualBytesWithoutHeader = actualBytes;

            CollectionAssert.AreEqual(expectedBytes, actualBytesWithoutHeader);
        }
    }
}
