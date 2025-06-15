// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineBase.Common.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.SmartContract.Debugger;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        private void AddDebugSinkCallStack(DebugCallSink callSink) =>
            _engineDebugSink.CallStack = [.. _engineDebugSink.CallStack, callSink];

        private void AddDebugSinkContractStack(DebugContractSink contractSink) =>
            _engineDebugSink.ContractStack = [.. _engineDebugSink.ContractStack, contractSink];

        private void AddDebugSinkPostContractStack(DebugContractSink contractSink) =>
            _engineDebugSink.PostContractStack = [.. _engineDebugSink.PostContractStack, contractSink];
    }
}
