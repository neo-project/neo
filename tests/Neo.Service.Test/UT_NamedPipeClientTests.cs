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
        public JsonNode? TestCommandExit()
        {
            var clientStream = new NamedPipeClientStream(".", NodeCommandPipeServer.PipeName);
            clientStream.Connect();

            Assert.True(clientStream.IsConnected);
            Assert.True(clientStream.CanWrite);
            Assert.True(clientStream.CanRead);

            var pipeCommentObject = new PipeCommand()
            {
                Exec = CommandType.Shutdown,
                Arguments = new Dictionary<string, string>()
                {
                    { "--close1", bool.TrueString },
                    { "--close2", bool.TrueString },
                    { "--close3", bool.TrueString },
                    { "--close4", bool.TrueString },
                    { "--close5", bool.TrueString },
                    { "--close6", bool.TrueString },
                    { "--close7", bool.TrueString },
                    { "--close8", bool.TrueString },
                },
            };

            using var sw = new StreamWriter(clientStream, encoding: Encoding.UTF8, leaveOpen: true);
            sw.AutoFlush = true;
            sw.WriteLine(JsonSerializer.Serialize(pipeCommentObject, pipeCommentObject.GetType(), NodeCommandPipeServer.JsonOptions));

            if (OperatingSystem.IsWindows())
                clientStream.WaitForPipeDrain();

            using var sr = new StreamReader(clientStream, encoding: Encoding.UTF8, leaveOpen: true);
            return JsonSerializer.Deserialize<JsonNode>(sr.ReadLine()!, NodeCommandPipeServer.JsonOptions);
        }
    }
}
