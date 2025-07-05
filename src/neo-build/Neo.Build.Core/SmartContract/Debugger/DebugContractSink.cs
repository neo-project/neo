// Copyright (C) 2015-2025 The Neo Project.
//
// DebugContractSink.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using System.Collections.Generic;

namespace Neo.Build.Core.SmartContract.Debugger
{
    public class DebugContractSink(
        ContractState contractState,
        IEnumerable<DebugStorage> storageItems)
    {
        public ContractState Contract { get; set; } = contractState;

        public IEnumerable<DebugStorage> Storage { get; set; } = storageItems;
    }
}
