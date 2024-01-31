// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeServer.Protocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Service.Pipes;
using Neo.Service.Pipes.Payloads;
using System.IO.Pipes;

namespace Neo.Service.Tests
{
    public partial class UT_PipeServer
    {
        [Fact]
        public void Get_Protocol_Version_Payload()
        {
            using var clientStream = new NamedPipeClientStream(NamedPipeService.PipeName);
            clientStream.Connect();

            Assert.True(clientStream.IsConnected);
            Assert.True(clientStream.CanWrite);
            Assert.True(clientStream.CanRead);

            var resultMessage = PipeMessage.ReadFromStream(clientStream);

            Assert.NotNull(resultMessage);
            Assert.NotNull(resultMessage.Payload);
            Assert.IsType<PipeVersionPayload>(resultMessage.Payload);

            var resultVersion = (PipeVersionPayload)resultMessage.Payload;

            Assert.Equal(TEST_NETWORK, resultVersion.Network);
            Assert.Equal(NodeUtilities.GetApplicationVersionNumber(), resultVersion.Version);
            Assert.InRange(resultVersion.Version, 0, int.MaxValue);
            Assert.InRange(resultVersion.Nonce, 0u, uint.MaxValue);
        }
    }
}
