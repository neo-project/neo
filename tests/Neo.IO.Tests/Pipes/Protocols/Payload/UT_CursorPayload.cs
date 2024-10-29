// Copyright (C) 2015-2024 The Neo Project.
//
// UT_CursorPayload.cs file belongs to the neo project and is free
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
    public class UT_CursorPayload
    {
        [TestMethod]
        public void TestToByteArrayFromStream()
        {
            var expectedPayload = new CursorPayload()
            {
                Left = 1,
                Right = 2,
            };
            var expectedBytes = expectedPayload.ToByteArray();
            var actualPayload = new CursorPayload();
            using var actualStream = new MemoryStream(expectedBytes);
            actualPayload.FromStream(actualStream);

            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Left, actualPayload.Left);
            Assert.AreEqual(expectedPayload.Right, actualPayload.Right);
        }
    }
}
