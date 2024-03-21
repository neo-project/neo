// Copyright (C) 2015-2024 The Neo Project.
//
// PipeCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.CommandLine.Services
{
    internal enum PipeCommand : byte
    {
        Response = 0x20,
        Start = 0xa1,
        Stop = 0xa2,
        Backup = 0xb0,
        Version = 0xe0,
        Error = 0xef,
        Test = 0xff,
    }
}
