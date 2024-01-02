// Copyright (C) 2015-2024 The Neo Project.
//
// ContractMethodAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;

namespace Neo.SmartContract.Native
{
    [DebuggerDisplay("{Name}")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    internal class ContractMethodAttribute : Attribute
    {
        public string Name { get; init; }
        public CallFlags RequiredCallFlags { get; init; }
        public long CpuFee { get; init; }
        public long StorageFee { get; init; }
        public Hardfork? ActiveIn { get; init; } = null;

        public ContractMethodAttribute() { }

        public ContractMethodAttribute(Hardfork activeIn)
        {
            ActiveIn = activeIn;
        }
    }
}
