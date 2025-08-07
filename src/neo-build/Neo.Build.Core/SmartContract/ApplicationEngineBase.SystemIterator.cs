// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineBase.SystemIterator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Build.Core.Logging;
using Neo.Extensions;
using Neo.SmartContract.Iterators;
using StackItem = Neo.VM.Types.StackItem;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        protected virtual bool SystemIteratorNext(IIterator iterator)
        {
            var result = IteratorNext(iterator);

            _traceLogger.LogDebug(DebugEventLog.IteratorNext,
                "{SysCall} Iterator=\"{Iterator}\" Result=\"{Result}\"",
                nameof(System_Iterator_Next), iterator.GetType().Name, result);

            return result;
        }

        protected virtual StackItem SystemIteratorValue(IIterator iterator)
        {
            var result = IteratorValue(iterator);

            _traceLogger.LogDebug(DebugEventLog.IteratorGet,
                "{SysCall} Iterator=\"{Iterator}\" Result=\"{Result}\"",
                nameof(System_Iterator_Value), iterator.GetType().Name, result.ToJson());

            return result;
        }
    }
}
