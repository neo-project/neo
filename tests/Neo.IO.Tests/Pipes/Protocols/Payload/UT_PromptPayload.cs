// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PromptPayload.cs file belongs to the neo project and is free
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.IO.Tests.Pipes.Protocols.Payload
{
    [TestClass]
    public class UT_PromptPayload
    {
        [TestMethod]
        public void TestToByteArrayFromStream()
        {
            var expectedPayload = new PromptPayload()
            {
                Answer = 1,
                Message = "Hello World",
            };
            var expectedBytes = expectedPayload.ToByteArray();
            var actualPayload = new PromptPayload();
            using var actualStream = new MemoryStream(expectedBytes);
            actualPayload.FromStream(actualStream);

            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Answer, actualPayload.Answer);
            Assert.AreEqual(expectedPayload.Message, actualPayload.Message);
        }
    }
}
