// Copyright (C) 2015-2025 The Neo Project.
//
// IInteroperableVerifiable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the object that can be converted to and from <see cref="StackItem"/>
    /// and allows you to specify whether a verification is required.
    /// </summary>
    public interface IInteroperableVerifiable : IInteroperable
    {
        /// <summary>
        /// Convert a <see cref="StackItem"/> to the current object.
        /// </summary>
        /// <param name="stackItem">The <see cref="StackItem"/> to convert.</param>
        /// <param name="verify">Verify the content</param>
        void FromStackItem(StackItem stackItem, bool verify = true);
    }
}
