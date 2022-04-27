// Copyright (C) 2015-2021 The Neo Project.
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
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class KeyValueIterator : IIterator
    {
        private readonly IEnumerator<(StackItem Key, StackItem Value)> enumerator;
        private readonly ReferenceCounter referenceCounter;

        public KeyValueIterator(IEnumerator<(StackItem, StackItem)> enumerator, ReferenceCounter referenceCounter)
        {
            this.enumerator = enumerator;
            this.referenceCounter = referenceCounter;
        }

        public void Dispose()
        {
            enumerator.Dispose();
        }

        public bool Next()
        {
            return enumerator.MoveNext();
        }

        public StackItem Value()
        {
            return new Struct(referenceCounter) { enumerator.Current.Key, enumerator.Current.Value };
        }
    }
}
