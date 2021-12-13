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
    internal class StorageIterator : IIterator
    {
        private readonly IEnumerator<(StorageKey Key, StorageItem Value)> enumerator;
        private readonly int prefixLength;
        private readonly FindOptions options;
        private readonly ReferenceCounter referenceCounter;

        public StorageIterator(IEnumerator<(StorageKey, StorageItem)> enumerator, int prefixLength, FindOptions options, ReferenceCounter referenceCounter)
        {
            this.enumerator = enumerator;
            this.prefixLength = prefixLength;
            this.options = options;
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
            byte[] key = enumerator.Current.Key.Key;
            byte[] value = enumerator.Current.Value.Value;

            if (options.HasFlag(FindOptions.RemovePrefix))
                key = key[prefixLength..];

            StackItem item = options.HasFlag(FindOptions.DeserializeValues)
                ? BinarySerializer.Deserialize(value, ExecutionEngineLimits.Default, referenceCounter)
                : value;

            if (options.HasFlag(FindOptions.PickField0))
                item = ((Array)item)[0];
            else if (options.HasFlag(FindOptions.PickField1))
                item = ((Array)item)[1];

            if (options.HasFlag(FindOptions.KeysOnly))
                return key;
            if (options.HasFlag(FindOptions.ValuesOnly))
                return item;
            return new Struct(referenceCounter) { key, item };
        }

        public void Reset() => enumerator.Reset();
    }
}
