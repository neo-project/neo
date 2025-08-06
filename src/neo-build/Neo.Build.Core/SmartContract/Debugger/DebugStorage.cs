// Copyright (C) 2015-2025 The Neo Project.
//
// DebugStorage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.SmartContract;
using System.Diagnostics;
using System.Linq;

namespace Neo.Build.Core.SmartContract.Debugger
{
    [DebuggerDisplay("{ToString()}")]
    public class DebugStorage(
        StorageKey key,
        StorageItem value,
        StorageEvent storageEvent)
    {
        public StorageKey Key { get; } = key;

        public StorageItem Value { get; } = value;

        public StorageEvent Event { get; } = storageEvent;

        public override int GetHashCode() =>
            Key.ToArray().XxHash3_32();

        public override string ToString() =>
            string.Format("{{{0} {{Id = {1:d}, Key = [{2}]}}}}",
                nameof(StorageKey),
                Key.Id,
                string.Join(", ", Key.ToArray()[4..].Select(s => $"0x{s:x02}")));
    }

    public enum StorageEvent : byte
    {
        Unknown = 0,
        Read = 1,
        Write = 2,
    }
}
