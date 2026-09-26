// Copyright (C) 2015-2026 The Neo Project.
//
// Nep11TokenState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Base token state for native non-divisible NEP-11 tokens.
    /// </summary>
    public class Nep11TokenState : IInteroperable
    {
        /// <summary>
        /// The owner of the token.
        /// </summary>
        public UInt160 Owner = UInt160.Zero;

        /// <summary>
        /// Display name of the asset (NEP-11 metadata <c>name</c>).
        /// </summary>
        public string Name = string.Empty;

        public virtual void FromStackItem(StackItem stackItem)
        {
            var @struct = (Struct)stackItem;
            Owner = new UInt160(@struct[0].GetSpan());
            Name = @struct[1].GetString() ?? string.Empty;
        }

        public virtual StackItem ToStackItem()
        {
            return new Struct()
            {
                Owner.ToArray(),
                Name
            };
        }
    }
}
