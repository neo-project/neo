// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NamedPipeClientTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Service.Pipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo.Service.Tests
{
    internal class UT_NamedPipeClientTests
    {
        private readonly NamedPipeClientStream _clientStream;

        public UT_NamedPipeClientTests()
        {
            _clientStream = new(".", NodeCommandPipeServer.PipeName);
        }

        public JsonNode? TestCommandExit()
        {
            _clientStream.Connect();

            Assert.True(_clientStream.IsConnected);
            Assert.True(_clientStream.CanWrite);
            Assert.True(_clientStream.CanRead);

            var pipeCommentObject = new PipeCommand()
            {
                Exec = CommandType.Shutdown,
                Arguments = new Dictionary<string, string>(),
            };

            using var sw = new StreamWriter(_clientStream, encoding: Encoding.UTF8, leaveOpen: true);
            sw.AutoFlush = true;
            sw.WriteLine(JsonSerializer.Serialize(pipeCommentObject, pipeCommentObject.GetType(), NodeCommandPipeServer.JsonOptions));

            if (OperatingSystem.IsWindows())
                _clientStream.WaitForPipeDrain();

            using var sr = new StreamReader(_clientStream, encoding: Encoding.UTF8, leaveOpen: true);
            return JsonSerializer.Deserialize<JsonNode>(sr.ReadLine()!, NodeCommandPipeServer.JsonOptions);
        }
    }
}
