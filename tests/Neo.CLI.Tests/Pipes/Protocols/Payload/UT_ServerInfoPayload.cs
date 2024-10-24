// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ServerInfoPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI.Pipes.Protocols.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.CLI.Tests.Pipes.Protocols.Payload
{
    [TestClass]
    public class UT_ServerInfoPayload
    {
        [TestMethod]
        public void TestFromBytes()
        {
            var expectedPayload = new ServerInfoPayload()
            {
                Nonce = (uint)Random.Shared.Next(),
                Version = 1000,
                Port = 8888,
                BlockHeight = 100,
                HeaderHeight = 1000,
                RemoteNodes = new ServerInfoPayload.RemoteConnectedClient[]
                {
                    new ServerInfoPayload.RemoteConnectedClient()
                    {
                        Port = 9999,
                        LastBlockIndex = 10000,
                    },
                }
            };
            var expectedBytes = expectedPayload.ToByteArray();
            var actualPayload = new ServerInfoPayload();
            actualPayload.FromBytes(expectedBytes);

            Assert.AreEqual(expectedPayload.Size, actualPayload.Size);
            Assert.AreEqual(expectedPayload.Nonce, actualPayload.Nonce);
            Assert.AreEqual(expectedPayload.Version, actualPayload.Version);
            Assert.AreEqual(expectedPayload.Address, actualPayload.Address);
            Assert.AreEqual(expectedPayload.Port, actualPayload.Port);
            Assert.AreEqual(expectedPayload.BlockHeight, actualPayload.BlockHeight);
            Assert.AreEqual(expectedPayload.HeaderHeight, actualPayload.HeaderHeight);
            Assert.AreEqual(expectedPayload.RemoteNodes.Length, actualPayload.RemoteNodes.Length);

            for (int i = 0; i < expectedPayload.RemoteNodes.Length; i++)
            {
                Assert.AreEqual(expectedPayload.RemoteNodes[i].Address, actualPayload.RemoteNodes[i].Address);
                Assert.AreEqual(expectedPayload.RemoteNodes[i].Port, actualPayload.RemoteNodes[i].Port);
                Assert.AreEqual(expectedPayload.RemoteNodes[i].LastBlockIndex, actualPayload.RemoteNodes[i].LastBlockIndex);
            }
        }
    }
}
