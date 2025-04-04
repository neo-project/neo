// Copyright (C) 2015-2025 The Neo Project.
//
// HashIndexState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Extensions;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    class HashIndexState : IInteroperable
    {
        public UInt256 Hash { get; set; } = UInt256.Zero;
        public uint Index { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            var @struct = (Struct)stackItem;
            Hash = new UInt256(@struct[0].GetSpan());
            Index = (uint)@struct[1].GetInteger();
        }

        StackItem IInteroperable.ToStackItem(IReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Hash.ToArray(), Index };
        }
    }
}

#nullable disable
