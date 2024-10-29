// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Pipes.Protocols
{
    internal enum NamedPipeCommand : byte
    {
        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Write = 0x00,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        WriteLine = 0x01,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Read = 0x02,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        ReadLine = 0x03,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Trace = 0x04,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Debug = 0x05,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Information = 0x06,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Warning = 0x07,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Error = 0x08,

        [NamedPipeCommand(typeof(Payloads.ExceptionPayload))]
        Exception = 0xe0,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Test = 0xe1,

        [NamedPipeCommand(typeof(Payloads.EmptyPayload))]
        Clear = 0xf0,

        [NamedPipeCommand(typeof(Payloads.EmptyPayload))]
        Reset = 0xf1,

        [NamedPipeCommand(typeof(Payloads.CursorPayload))]
        Cursor = 0xf2,

        [NamedPipeCommand(typeof(Payloads.PromptPayload))]
        Prompt = 0xf3,

        [NamedPipeCommand(typeof(Payloads.StringPayload))]
        Invoke = 0xf4,
    }
}
