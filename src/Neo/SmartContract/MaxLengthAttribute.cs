// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using Neo.VM.Types;

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
