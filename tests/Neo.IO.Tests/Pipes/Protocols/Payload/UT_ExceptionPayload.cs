// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ExceptionPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Pipes.Protocols.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.IO.Tests.Pipes.Protocols.Payload
{
    [TestClass]
    public class UT_ExceptionPayload
    {
        [TestMethod]
        public void TestToByteArrayFromBytes()
        {
            var expected = new ExceptionPayload()
            {
                HResult = 666,
                Message = "Test message",
                StackTrace = "Test stack trace",
                ExceptionName = "ApplicationException"
            };
            var expectedBytes = expected.ToByteArray();
            var actual = new ExceptionPayload();
            actual.FromBytes(expectedBytes);

            Assert.AreEqual(expected.Size, actual.Size);
            Assert.AreEqual(expected.HResult, actual.HResult);
            Assert.AreEqual(expected.Message, actual.Message);
            Assert.AreEqual(expected.StackTrace, actual.StackTrace);
            Assert.AreEqual(expected.ExceptionName, actual.ExceptionName);
        }
    }
}
