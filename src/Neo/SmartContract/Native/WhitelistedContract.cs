// Copyright (C) 2015-2026 The Neo Project.
//
// WhitelistedContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    public class WhitelistedContract : IInteroperable
    {
        public UInt160 ContractHash { get; set; } = UInt160.Zero;
        public string? Method { get; set; }
        public int ArgCount { get; set; }
        public long FixedFee { get; set; }

        public virtual void FromStackItem(StackItem stackItem)
        {
            var data = (Struct)stackItem;

            ContractHash = new UInt160(data[0].GetSpan());
            Method = data[1].GetString();
            ArgCount = (int)data[2].GetInteger();
            FixedFee = (long)data[3].GetInteger();
        }

        public virtual StackItem ToStackItem(IReferenceCounter? referenceCounter)
        {
            return new Struct(referenceCounter) { ContractHash.ToArray(), Method ?? StackItem.Null, ArgCount, FixedFee };
        }
    }
}
