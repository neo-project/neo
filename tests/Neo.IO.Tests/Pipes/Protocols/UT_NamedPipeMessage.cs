// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NamedPipeMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.IO;
using Neo.IO.Pipes.Protocols;
using Neo.IO.Pipes.Protocols.Payloads;
using Neo.IO.Tests;
using Neo.IO.Tests.Pipes;
using Neo.IO.Tests.Pipes.Protocols;
using Neo.IO.Tests.Pipes.Protocols;
using Neo.IO.Tests.Pipes.Protocols.Payload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.IO.Tests.Pipes.Protocols
{
    [TestClass]
    public class UT_NamedPipeMessage
    {
        [TestMethod]
        public void ToByteArray_FromBytes()
        {
            var expectedMessage = new NamedPipeMessage() { RequestId = 666, Command = NamedPipeCommand.Echo, Payload = new EchoPayload() };
            var expectedBytes = expectedMessage.ToByteArray();

            var actualMessage = NamedPipeMessage.Deserialize(expectedBytes);

            Assert.AreEqual(expectedMessage.Command, actualMessage.Command);
            Assert.AreEqual(expectedMessage.RequestId, actualMessage.RequestId);
            Assert.AreEqual(expectedMessage.Size, actualMessage.Size);
            Assert.AreEqual(expectedMessage.PayloadSize, actualMessage.PayloadSize);
            Assert.AreEqual(expectedMessage.Payload?.Size, actualMessage.Payload?.Size);
            Assert.AreEqual(((EchoPayload)expectedMessage.Payload)?.Message, ((EchoPayload)actualMessage.Payload)?.Message);
        }
    }
}
