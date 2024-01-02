// Copyright (C) 2015-2024 The Neo Project.
//
// CompoundType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo.VM.Types
{
    /// <summary>
    /// The base class for complex types in the VM.
    /// </summary>
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}, Id={System.Collections.Generic.ReferenceEqualityComparer.Instance.GetHashCode(this)}")]
    public abstract class CompoundType : StackItem
    {
        /// <summary>
        /// The reference counter used to count the items in the VM object.
        /// </summary>
        protected readonly ReferenceCounter? ReferenceCounter;

        /// <summary>
        /// Create a new <see cref="CompoundType"/> with the specified reference counter.
        /// </summary>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        protected CompoundType(ReferenceCounter? referenceCounter)
        {
            this.ReferenceCounter = referenceCounter;
            referenceCounter?.AddZeroReferred(this);
        }

        /// <summary>
        /// The number of items in this VM object.
        /// </summary>
        public abstract int Count { get; }

        public abstract IEnumerable<StackItem> SubItems { get; }

        public abstract int SubItemsCount { get; }

        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Remove all items from the VM object.
        /// </summary>
        public abstract void Clear();

        internal abstract override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable);

        public sealed override bool GetBoolean()
        {
            return true;
        }

        /// <summary>
        /// The operation is not supported. Always throw <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">This method always throws the exception.</exception>
        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }
    }
}
