// Copyright (C) 2015-2024 The Neo Project.
//
// StackItem.Vertex.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Cryptography;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Types
{
    partial class StackItem
    {
        /// <summary>
        /// Represents an entry for an object reference.
        ///
        /// This class is used to keep track of references from compound types to other <see cref="CompoundType"/> or <see cref="Buffer"/>.
        /// It contains the referenced item and the number of references to it.
        ///
        /// Use this class to manage references from compound types to their child items.
        /// <remarks>
        /// This is used to track references numbers from the same parent to the same child.
        /// This is used for the purpose of determining strongly connect components.
        /// </remarks>
        /// </summary>
        internal class ObjectReferenceEntry
        {
            /// <summary>
            /// The referenced StackItem.
            /// </summary>
            public StackItem Item;

            /// <summary>
            /// The number of references to the StackItem.
            /// </summary>
            public int References;

            /// <summary>
            /// Initializes a new instance of the ObjectReferenceEntry class with the specified StackItem.
            /// </summary>
            /// <param name="item">The referenced StackItem.</param>
            public ObjectReferenceEntry(StackItem item) => Item = item;
        }

        /// <summary>
        /// The number of references to this StackItem from the evaluation stack.
        ///
        /// This field tracks how many times this item is referenced by the evaluation stack.
        /// It is incremented when the item is pushed onto the stack and decremented when it is popped off.
        ///
        /// Use this field to manage stack references and determine when an item is no longer needed.
        /// </summary>
        internal int StackReferences = 0;

        /// <summary>
        /// A dictionary mapping compound types to their object reference entries.
        ///
        /// This dictionary is used to track references from compound types to their child items.
        /// It allows efficient lookup and management of references.
        ///
        /// Use this dictionary to manage references from compound types to their children.
        /// <remarks>Only <see cref="CompoundType"/> and <see cref="Buffer"/> will be assigned an <see cref="ObjectReferences"/>,
        /// other types will be null.</remarks>
        /// </summary>
        internal Dictionary<CompoundType, ObjectReferenceEntry>? ObjectReferences;

        /// <summary>
        /// Depth-First Number for Tarjan's algorithm.
        /// </summary>
        internal int DFN = -1;

        /// <summary>
        /// Low-link value for Tarjan's algorithm.
        /// </summary>
        internal int LowLink = 0;

        /// <summary>
        /// Indicates whether the item is currently on the stack for Tarjan's algorithm.
        ///
        /// <remarks>
        /// This should only be used for Tarjan algorithm, it can not be used to indicate
        /// whether an item is on the stack or not since it can still be false if a value is
        /// on the stack but the algorithm is not yet running.
        /// </remarks>
        /// </summary>
        internal bool OnStack = false;

        /// <summary>
        /// Returns the successors of the current item based on object references.
        ///
        /// This property provides an enumerable of StackItems that are referenced by this item.
        /// It is used by Tarjan's algorithm to find strongly connected components.
        ///
        /// Use this property when you need to iterate over the successors of a StackItem.
        /// </summary>
        internal IEnumerable<StackItem> Successors => ObjectReferences?.Values.Where(p => p.References > 0).Select(p => p.Item) ?? System.Array.Empty<StackItem>();

        /// <summary>
        /// Resets the strongly connected components-related fields.
        ///
        /// This method resets the DFN, LowLink, and OnStack fields to their default values.
        /// It is used before running Tarjan's algorithm to ensure a clean state.
        ///
        /// Use this method to reset the state of a StackItem for strongly connected components analysis.
        /// </summary>
        internal void Reset() => (DFN, LowLink, OnStack) = (-1, 0, false);


        private static readonly uint s_seed = unchecked((uint)new Random().Next(int.MinValue, int.MaxValue));
        private int _hashCode = 0;

        /// <summary>
        /// Generates a hash code based on the item's span.
        ///
        /// This method provides a hash code for the StackItem based on its byte span.
        /// It is used for efficient storage and retrieval in hash-based collections.
        ///
        /// Use this method when you need a hash code for a StackItem.
        /// </summary>
        /// <returns>The hash code for the StackItem.</returns>
        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                using Murmur32 murmur = new(s_seed);
                _hashCode = BinaryPrimitives.ReadInt32LittleEndian(murmur.ComputeHash(GetSpan().ToArray()));
            }
            return _hashCode;
        }
    }
}
