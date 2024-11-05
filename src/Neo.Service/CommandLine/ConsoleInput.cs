// Copyright (C) 2015-2024 The Neo Project.
//
// ConsoleInput.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Service.CommandLine
{
    internal enum ConsoleInput : int
    {
        Empty = 0,
        Yes = 1,
        No = 2,
        Cancel = 3,
    }
}
