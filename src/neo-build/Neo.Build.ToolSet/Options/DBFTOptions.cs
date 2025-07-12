// Copyright (C) 2015-2025 The Neo Project.
//
// DBFTOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins;

namespace Neo.Build.ToolSet.Options
{
    internal sealed class DBFTOptions
    {
        public string StoreRoot { get; set; } = string.Empty;
        public bool IgnoreRecoveryLogs { get; set; }
        public uint MaxBlockSize { get; set; }
        public long MaxBlockSystemFee { get; set; }
        public UnhandledExceptionPolicy ExceptionPolicy { get; set; } = UnhandledExceptionPolicy.StopNode;
    }
}
