// Copyright (C) 2015-2025 The Neo Project.
//
// FasterDbStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Build.Core.Storage
{
    public class FasterDbStore : IStore
    {
        public bool Contains(byte[] key)
        {
            throw new NotImplementedException();
        }

        public void Delete(byte[] key)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IStoreSnapshot GetSnapshot()
        {
            throw new NotImplementedException();
        }

        public void Put(byte[] key, byte[] value)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[]? keyOrPrefix, SeekDirection direction)
        {
            throw new NotImplementedException();
        }

        public byte[]? TryGet(byte[] key)
        {
            throw new NotImplementedException();
        }

        public bool TryGet(byte[] key, [NotNullWhen(true)] out byte[]? value)
        {
            throw new NotImplementedException();
        }
    }
}
