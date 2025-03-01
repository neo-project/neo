// Copyright (C) 2015-2025 The Neo Project.
//
// StorageSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.Core.SmartContract
{
    public class StorageSettings
    {
        public TextFormatterType KeyFormat { get; set; }
        public TextFormatterType ValueFormat { get; set; }
    }
}
