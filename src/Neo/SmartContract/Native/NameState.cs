// Copyright (C) 2015-2026 The Neo Project.
//
// NameState.cs file belongs to the neo project and is free
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
using System;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Per-domain state for native NameService (NEP-11 token state + NNS fields).
    /// </summary>
    public class NameState : Nep11TokenState
    {
        /// <summary>
        /// Expiration timestamp in milliseconds (same unit as <see cref="ApplicationEngine.GetTime"/>).
        /// </summary>
        public ulong Expiration;

        /// <summary>
        /// Optional admin that may manage records (same model as non-native NNS).
        /// </summary>
        public UInt160? Admin;

        public override void FromStackItem(StackItem stackItem)
        {
            var @struct = (Struct)stackItem;
            Owner = new UInt160(@struct[0].GetSpan());
            Name = @struct[1].GetString() ?? string.Empty;
            Expiration = (ulong)@struct[2].GetInteger();
            Admin = @struct[3].IsNull ? null : new UInt160(@struct[3].GetSpan());
        }

        public override StackItem ToStackItem()
        {
            return new Struct()
            {
                Owner.ToArray(),
                Name,
                Expiration,
                Admin is null ? StackItem.Null : Admin.ToArray()
            };
        }

        internal void EnsureNotExpired(ulong now)
        {
            if (now >= Expiration)
                throw new InvalidOperationException("The name has expired.");
        }

        internal void CheckAdmin(ApplicationEngine engine)
        {
            if (engine.CheckWitnessInternal(Owner)) return;
            if (Admin is null || !engine.CheckWitnessInternal(Admin))
                throw new InvalidOperationException("No authorization.");
        }
    }
}
