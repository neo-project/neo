// Copyright (C) 2015-2024 The Neo Project.
//
// IInteroperable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the object that can be converted to and from <see cref="StackItem"/>.
    /// </summary>
    public interface IInteroperableValidation : IInteroperable
    {
        /// <summary>
        /// Convert a <see cref="StackItem"/> to the current object.
        /// </summary>
        /// <param name="stackItem">The <see cref="StackItem"/> to convert.</param>
        /// <param name="validate">Validate</param>
        void FromStackItem(StackItem stackItem, bool validate);
    }
}
