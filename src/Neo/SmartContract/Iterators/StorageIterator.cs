// Copyright (C) 2015-2026 The Neo Project.
//
// StorageIterator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Iterators
{
    internal class StorageIterator : IIterator
    {
        private readonly IEnumerator<(StorageKey Key, StorageItem Value)> enumerator;
        private readonly int prefixLength;
        private readonly FindOptions options;

        /// <summary>
        /// The total length of <see cref="ByteString"/> and <see cref="VM.Types.Buffer"/> items produced by
        /// deserializing the current value, 0 if <see cref="FindOptions.DeserializeValues"/> isn't used.
        /// </summary>
        public int DeserializedLength { get; private set; }

        public StorageIterator(IEnumerator<(StorageKey, StorageItem)> enumerator, int prefixLength, FindOptions options)
        {
            this.enumerator = enumerator;
            this.prefixLength = prefixLength;
            this.options = options;
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
            ReadOnlyMemory<byte> key = enumerator.Current.Key.Key;
            ReadOnlyMemory<byte> value = enumerator.Current.Value.Value;

            if (options.HasFlag(FindOptions.RemovePrefix))
                key = key[prefixLength..];

            DeserializedLength = 0;
            StackItem item = value;
            if (options.HasFlag(FindOptions.DeserializeValues))
            {
                item = BinarySerializer.Deserialize(value, ExecutionEngineLimits.Default, out var deserializedLength);
                DeserializedLength = deserializedLength;
            }

            if (options.HasFlag(FindOptions.PickField0))
                item = ((Array)item)[0];
            else if (options.HasFlag(FindOptions.PickField1))
                item = ((Array)item)[1];

            if (options.HasFlag(FindOptions.KeysOnly))
                return key;
            if (options.HasFlag(FindOptions.ValuesOnly))
                return item;
            return new Struct() { key, item };
        }
    }
}
