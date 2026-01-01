// Copyright (C) 2015-2026 The Neo Project.
//
// RangeAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.SmartContract;

class RangeAttribute : ValidatorAttribute
{
    public long MinValue { get; }
    public long MaxValue { get; }

    public RangeAttribute(long min, long max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        MinValue = min;
        MaxValue = max;
    }

    public override void Validate(StackItem item)
    {
        if (item is not Integer)
            throw new InvalidOperationException("The input data is not an integer.");
        var value = item.GetInteger();
        if (value < MinValue || value > MaxValue)
            throw new InvalidOperationException("The value of the input data is out of range.");
    }
}
