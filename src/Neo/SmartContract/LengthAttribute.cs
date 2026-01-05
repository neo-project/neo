// Copyright (C) 2015-2026 The Neo Project.
//
// LengthAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.SmartContract;

class LengthAttribute : ValidatorAttribute
{
    public int MinLength { get; }
    public int MaxLength { get; }

    public LengthAttribute(int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
        MaxLength = maxLength;
    }

    public LengthAttribute(int minLength, int maxLength) : this(maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minLength, maxLength);
        MinLength = minLength;
    }

    public override void Validate(StackItem item)
    {
        int length = item.GetSpan().Length;
        if (length < MinLength || length > MaxLength)
            throw new InvalidOperationException("The length of the input data is out of range.");
    }
}
