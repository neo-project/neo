// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineDebugSink.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.Build.Core.SmartContract.Debugger
{
    internal class ApplicationEngineDebugSink
    {
        public ReadOnlyMemory<byte> Script { get; set; } = Memory<byte>.Empty;

        public VMState State { get; set; } = VMState.NONE;

        public long GasFee { get; set; } = 0L;

        public long GasLeft { get; set; } = 0L;

        public IEnumerable<DebugCallSink> CallStack { get; set; } = [];

        public IEnumerable<DebugContractSink> ContractStack { get; set; } = [];

        public IEnumerable<DebugContractSink> PostContractStack { get; set; } = [];

        public IEnumerable<StackItem> Results { get; set; } = [];

        public IVerifiable? ScriptContainer { get; set; }
    }
}
