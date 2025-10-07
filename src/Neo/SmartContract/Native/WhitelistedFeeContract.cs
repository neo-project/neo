// Copyright (C) 2015-2025 The Neo Project.
//
// WhitelistedFeeContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    public class WhitelistedFeeContract : IInteroperable
    {
        /// <summary>
        /// Contract Update Counter
        /// </summary>
        public uint UpdateCounter;

        /// <summary>
        /// The WhiteList (method,pcount/Fee)
        /// </summary>
        public VM.Types.Map WhiteList;

        public virtual void FromStackItem(StackItem stackItem)
        {
            UpdateCounter = (uint)((Array)stackItem)[0].GetInteger();
            WhiteList = ((Array)stackItem)[1] as Map;
        }

        public virtual StackItem ToStackItem(IReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { UpdateCounter, WhiteList.DeepCopy() };
        }
    }
}
