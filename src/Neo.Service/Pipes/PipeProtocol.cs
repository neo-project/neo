// Copyright (C) 2015-2024 The Neo Project.
//
// PipeProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Service.Exceptions;
using System.Net;

namespace Neo.Service.Pipes
{
    internal static partial class PipeProtocol
    {
        public static ISerializable? OnExecute(PipeMessageCommand command) =>
            command switch
            {
                PipeMessageCommand.Version => null,
                PipeMessageCommand.Test => null,
                PipeMessageCommand.Start => null,
                PipeMessageCommand.Stop => null,
                _ => GenericException.Create(new ProtocolViolationException())
            };
    }
}
