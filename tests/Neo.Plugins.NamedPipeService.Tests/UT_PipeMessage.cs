// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.Models;
using Neo.Plugins.Models.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Plugins.NamedPipeService.Tests
{
    [TestClass]
    public class UT_PipeMessage
    {
        [TestMethod]
        public void PipeMessage_Message_Sizes()
        {
            var message1 = PipeMessage.Create(1, PipeCommand.NAck, PipeMessage.Null);
            var message2 = PipeMessage.Create(1, PipeCommand.Exception, PipeMessage.Null);
            var message3 = PipeMessage.Create(1, PipeCommand.Exception, new PipeExceptionPayload()
            {
                Message = "Hello World",
                StackTrace = "Program.cs<Main>() line 99"
            });

            var msg1Bytes = message1.ToArray();
            var msg2Bytes = message2.ToArray();
            var msg3Bytes = message3.ToArray();

            Assert.AreEqual(msg1Bytes.Length, message1.Size);
            Assert.AreEqual(msg2Bytes.Length, message2.Size);
            Assert.AreEqual(msg3Bytes.Length, message3.Size);

            Assert.AreEqual(PipeMessage.HeaderSize, msg1Bytes.Length - message1.Payload.Size);
            Assert.AreEqual(PipeMessage.HeaderSize, msg2Bytes.Length - message2.Payload.Size);
            Assert.AreEqual(PipeMessage.HeaderSize, msg3Bytes.Length - message3.Payload.Size);
        }

        [TestMethod]
        public void PipeMessage_FromArray_ToArray()
        {
            var expected = PipeMessage.Create(0, PipeCommand.NAck, PipeMessage.Null);
            var expectedBytes = expected.ToArray();

            var actual = PipeMessage.Create(expectedBytes);
            var actualBytes = actual.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expected.RequestId, actual.RequestId);
            Assert.AreEqual(expected.Command, actual.Command);
            Assert.IsInstanceOfType<PipeNullPayload>(expected.Payload);
        }

        [TestMethod]
        public void PipeMessage_FromArray_InvalidCommand()
        {
            var expected = PipeMessage.Create(0, (PipeCommand)0xff, PipeMessage.Null);
            var expectedBytes = expected.ToArray();

            Assert.ThrowsException<FormatException>(() => PipeMessage.Create(expectedBytes));
        }
    }
}
