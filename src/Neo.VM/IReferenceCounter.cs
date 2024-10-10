// Copyright (C) 2015-2024 The Neo Project.
//
// IReferenceCounter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.VM
{
    public interface IReferenceCounter
    {
        int Count { get; }

        void AddZeroReferred(StackItem item);
        void AddReference(StackItem item, CompoundType compoundType);
        void RemoveReference(StackItem item, CompoundType compoundType);

        void AddStackReference(StackItem item, int count = 1);
        void RemoveStackReference(StackItem item);
    }
}
