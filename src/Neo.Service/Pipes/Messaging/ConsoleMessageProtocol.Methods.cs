// Copyright (C) 2015-2024 The Neo Project.
//
// ConsoleMessageProtocol.Methods.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Pipes.Protocols;
using Neo.IO.Pipes.Protocols.Payloads;

namespace Neo.Service.Pipes.Messaging
{
    internal partial class ConsoleMessageProtocol
    {
        public void Write(string? value)
        {
            var response = NamedPipeMessage.Create(NamedPipeCommand.Write, new StringPayload() { Value = value });
            _output.Write(response.ToByteArray());
        }

        public void WriteLine(string? value)
        {
            var response = NamedPipeMessage.Create(NamedPipeCommand.WriteLine, new StringPayload() { Value = value });
            _output.Write(response.ToByteArray());
        }
    }
}
