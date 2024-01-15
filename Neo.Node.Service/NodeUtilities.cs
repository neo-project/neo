// Copyright (C) 2015-2024 The Neo Project.
//
// NodeUtilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Node.Service
{
    internal static class NodeUtilities
    {
        public static UInt160? TryParseUInt160(string? value)
        {
            if (string.IsNullOrEmpty(value)) return default;
            if (UInt160.TryParse(value, out var result))
                return result;
            return default;
        }
    }
}
