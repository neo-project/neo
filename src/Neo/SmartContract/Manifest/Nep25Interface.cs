// Copyright (C) 2015-2025 The Neo Project.
//
// Nep25Interface.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// interface is only used in conjuction with the InteropInterface type and MUST NOT be used for other types, when used it specifies which interop interface is used.
    /// The only valid defined value for it is "IIterator" which means an iterator object.
    /// When used it MUST be accompanied with the value object that specifies the type of each individual element returned from the iterator.
    /// </summary>
    public enum Nep25Interface
    {
        /// <summary>
        /// Iterator object
        /// </summary>
        IIterator
    }
}
