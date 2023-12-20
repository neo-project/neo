// Copyright (C) 2015-2024 The Neo Project.
//
// Struct.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents a structure in the VM.
    /// </summary>
    public class Struct : Array
    {
        public override StackItemType Type => StackItemType.Struct;

        /// <summary>
        /// Create a structure with the specified fields.
        /// </summary>
        /// <param name="fields">The fields to be included in the structure.</param>
        public Struct(IEnumerable<StackItem>? fields = null)
            : this(null, fields)
        {
        }

        /// <summary>
        /// Create a structure with the specified fields. And make the structure use the specified <see cref="ReferenceCounter"/>.
        /// </summary>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> to be used by this structure.</param>
        /// <param name="fields">The fields to be included in the structure.</param>
        public Struct(ReferenceCounter? referenceCounter, IEnumerable<StackItem>? fields = null)
            : base(referenceCounter, fields)
        {
        }

        /// <summary>
        /// Create a new structure with the same content as this structure. All nested structures will be copied by value.
        /// </summary>
        /// <param name="limits">Execution engine limits</param>
        /// <returns>The copied structure.</returns>
        public Struct Clone(ExecutionEngineLimits limits)
        {
            int count = (int)(limits.MaxStackSize - 1);
            Struct result = new(ReferenceCounter);
            Queue<Struct> queue = new();
            queue.Enqueue(result);
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                Struct a = queue.Dequeue();
                Struct b = queue.Dequeue();
                foreach (StackItem item in b)
                {
                    count--;
                    if (count < 0) throw new InvalidOperationException("Beyond clone limits!");
                    if (item is Struct sb)
                    {
                        Struct sa = new(ReferenceCounter);
                        a.Add(sa);
                        queue.Enqueue(sa);
                        queue.Enqueue(sb);
                    }
                    else
                    {
                        a.Add(item);
                    }
                }
            }
            return result;
        }

        public override StackItem ConvertTo(StackItemType type)
        {
            if (type == StackItemType.Array)
                return new Array(ReferenceCounter, new List<StackItem>(_array));
            return base.ConvertTo(type);
        }

        public override bool Equals(StackItem? other)
        {
            throw new NotSupportedException();
        }

        internal override bool Equals(StackItem? other, ExecutionEngineLimits limits)
        {
            if (other is not Struct s) return false;
            Stack<StackItem> stack1 = new();
            Stack<StackItem> stack2 = new();
            stack1.Push(this);
            stack2.Push(s);
            uint count = limits.MaxStackSize;
            uint maxComparableSize = limits.MaxComparableSize;
            while (stack1.Count > 0)
            {
                if (count-- == 0)
                    throw new InvalidOperationException("Too many struct items to compare.");
                StackItem a = stack1.Pop();
                StackItem b = stack2.Pop();
                if (a is ByteString byteString)
                {
                    if (!byteString.Equals(b, ref maxComparableSize)) return false;
                }
                else
                {
                    if (maxComparableSize == 0)
                        throw new InvalidOperationException("The operand exceeds the maximum comparable size.");
                    maxComparableSize -= 1;
                    if (a is Struct sa)
                    {
                        if (ReferenceEquals(a, b)) continue;
                        if (b is not Struct sb) return false;
                        if (sa.Count != sb.Count) return false;
                        foreach (StackItem item in sa)
                            stack1.Push(item);
                        foreach (StackItem item in sb)
                            stack2.Push(item);
                    }
                    else
                    {
                        if (!a.Equals(b)) return false;
                    }
                }
            }
            return true;
        }
    }
}
