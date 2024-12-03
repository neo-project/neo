// Copyright (C) 2015-2024 The Neo Project.
//
// KeyValuePairs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics;
using System.Linq;

namespace Neo.Plugins.Storage
{
    [DebuggerDisplay("Key = {_key}, Value = {_value}")]
    internal class KeyValuePairs(
        byte[] key,
        byte[] value)
    {
        private readonly string _key = key is null ? string.Empty : $"[{string.Join(", ", key.Select(s => "0x" + s.ToString("x02")))}]";
        private readonly string _value = value is null ? string.Empty : $"[{string.Join(", ", value.Select(s => "0x" + s.ToString("x02")))}]";
    }
}
