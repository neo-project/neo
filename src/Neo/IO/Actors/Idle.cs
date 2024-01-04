// Copyright (C) 2015-2024 The Neo Project.
//
// Idle.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Actors
{
    internal sealed class Idle
    {
        public static Idle Instance { get; } = new Idle();
    }
}
