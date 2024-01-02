// Copyright (C) 2015-2024 The Neo Project.
//
// InteroperableList.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Native
{
    abstract class InteroperableList<T> : IList<T>, IInteroperable
    {
        private List<T> list;
        private List<T> List => list ??= new();

        public T this[int index] { get => List[index]; set => List[index] = value; }
        public int Count => List.Count;
        public bool IsReadOnly => false;

        public void Add(T item) => List.Add(item);
        public void AddRange(IEnumerable<T> collection) => List.AddRange(collection);
        public void Clear() => List.Clear();
        public bool Contains(T item) => List.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);
        IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();
        public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
        public int IndexOf(T item) => List.IndexOf(item);
        public void Insert(int index, T item) => List.Insert(index, item);
        public bool Remove(T item) => List.Remove(item);
        public void RemoveAt(int index) => List.RemoveAt(index);
        public void Sort() => List.Sort();

        protected abstract T ElementFromStackItem(StackItem item);
        protected abstract StackItem ElementToStackItem(T element, ReferenceCounter referenceCounter);

        public void FromStackItem(StackItem stackItem)
        {
            List.Clear();
            foreach (StackItem item in (Array)stackItem)
            {
                Add(ElementFromStackItem(item));
            }
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, this.Select(p => ElementToStackItem(p, referenceCounter)));
        }
    }
}
