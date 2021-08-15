// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    [AttributeUsage(AttributeTargets.Parameter)]
    abstract class ValidatorAttribute : Attribute
    {
        public abstract void Validate(StackItem item);
    }
}
