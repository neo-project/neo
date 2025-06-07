// Copyright (C) 2015-2025 The Neo Project.
//
// FunctionFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Build.Core.Factories
{
    public static class FunctionFactory
    {
        private static readonly uint s_networkSeed = 810960196u; // DEV0 Magic Code

        public static readonly Func<uint, uint> GetDevNetwork = static index => (uint)(s_networkSeed & ~(0xf << 24) | (index << 24));
    }
}
