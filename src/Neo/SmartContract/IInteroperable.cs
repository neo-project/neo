// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System.Runtime.Serialization;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the object that can be converted to and from <see cref="StackItem"/>.
    /// </summary>
    public interface IInteroperable
    {
        /// <summary>
        /// Convert a <see cref="StackItem"/> to the current object.
        /// </summary>
        /// <param name="stackItem">The <see cref="StackItem"/> to convert.</param>
        void FromStackItem(StackItem stackItem);

        /// <summary>
        /// Convert the current object to a <see cref="StackItem"/>.
        /// </summary>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> used by the <see cref="StackItem"/>.</param>
        /// <returns>The converted <see cref="StackItem"/>.</returns>
        StackItem ToStackItem(ReferenceCounter referenceCounter);

        public IInteroperable Clone()
        {
            IInteroperable result = (IInteroperable)FormatterServices.GetUninitializedObject(GetType());
            result.FromStackItem(ToStackItem(null));
            return result;
        }

        public void FromReplica(IInteroperable replica)
        {
            FromStackItem(replica.ToStackItem(null));
        }
    }
}
