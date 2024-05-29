// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeSerializablePayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Hosting;
using Neo.Hosting.App;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.NamedPipes.Protocol.Payloads;
using Neo.Hosting.App.Tests;
using Neo.Hosting.App.Tests.NamedPipes;
using Neo.Hosting.App.Tests.NamedPipes.Protocol;
using Neo.Hosting.App.Tests.NamedPipes.Protocol.Payloads;
using Neo.Hosting.App.Tests.UTHelpers.Extensions;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes.Protocol.Payloads
{
    public class UT_PipeSerializablePayload
        (ITestOutputHelper testOutputHelper)
    {
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Fact]
        public void IPipeMessage_FromArray_Data()
        {
            var block1 = CreateEmptyPipeBlock();
            var expectedBytes = block1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            var block2 = new PipeSerializablePayload<Block>();
            block2.FromArray(expectedBytes);

            var actualBytes = block2.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = nameof(PipeSerializablePayload<Block>);
            var methodName = nameof(PipeSerializablePayload<Block>.ToArray);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(block1.Size, block2.Size);
            Assert.Equal(block1.Value.Hash, block2.Value.Hash);
        }

        [Fact]
        public void IPipeMessage_ToArray_Data()
        {
            var block1 = CreateEmptyPipeBlock();
            var expectedBytes = block1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            var block2 = CreateEmptyPipeBlock();
            var actualBytes = block2.ToArray();
            var actualBytesWithoutHeader = actualBytes;
            var actualHexString = Convert.ToHexString(actualBytesWithoutHeader);

            var className = nameof(PipeSerializablePayload<Block>);
            var methodName = nameof(PipeSerializablePayload<Block>.ToArray);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytesWithoutHeader);
        }

        private static PipeSerializablePayload<Block> CreateEmptyPipeBlock() =>
            new()
            {
                Value = new Block()
                {
                    Header = new Header()
                    {
                        Version = 0,
                        PrevHash = UInt256.Zero,
                        MerkleRoot = UInt256.Zero,
                        Timestamp = 0,
                        Index = 0,
                        Nonce = 0,
                        PrimaryIndex = 0,
                        NextConsensus = UInt160.Zero,
                        Witness = new Witness()
                        {
                            InvocationScript = Memory<byte>.Empty,
                            VerificationScript = Memory<byte>.Empty,
                        },
                    },
                    Transactions = [],
                }
            };

    }
}
