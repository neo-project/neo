// Copyright (C) 2015-2024 The Neo Project.
//
// MaxLengthAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    class MaxLengthAttribute : ValidatorAttribute
    {
        public readonly int MaxLength;

        public MaxLengthAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }

        public override void Validate(StackItem item)
        {
            if (item.GetSpan().Length > MaxLength)
                throw new InvalidOperationException("The input exceeds the maximum length.");
        }
    }
}
