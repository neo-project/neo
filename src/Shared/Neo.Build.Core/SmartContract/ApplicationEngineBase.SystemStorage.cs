// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineBase.SystemStorage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using System;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        protected internal StorageContext SystemStorageGetContext()
        {
            return GetStorageContext();
        }

        protected virtual StorageContext SystemStorageGetReadOnlyContext()
        {
            return GetReadOnlyContext();
        }

        protected virtual StorageContext SystemStorageAsReadOnly(StorageContext storageContext)
        {
            return AsReadOnly(storageContext);
        }

        protected virtual ReadOnlyMemory<byte>? SystemStorageGet(StorageContext storageContext, byte[] key)
        {
            return Get(storageContext, key);
        }

        protected virtual IIterator SystemStorageFind(StorageContext storageContext, byte[] prefix, FindOptions options)
        {
            return Find(storageContext, prefix, options);
        }

        protected virtual void SystemStoragePut(StorageContext storageContext, byte[] key, byte[] value)
        {
            Put(storageContext, key, value);
        }

        protected virtual void SystemStorageDelete(StorageContext storageContext, byte[] key)
        {
            Delete(storageContext, key);
        }
    }
}
