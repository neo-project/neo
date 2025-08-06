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

using Microsoft.Extensions.Logging;
using Neo.Build.Core.Logging;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using System;
using System.Linq;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        protected virtual StorageContext SystemStorageGetContext()
        {
            var result = GetStorageContext();

            string[] resultString = [result.Id.ToString(), result.IsReadOnly.ToString()];

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Storage_GetContext), string.Join(';', resultString));

            return result;
        }

        protected virtual StorageContext SystemStorageGetReadOnlyContext()
        {
            var result = GetReadOnlyContext();

            string[] resultString = [result.Id.ToString(), result.IsReadOnly.ToString()];

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} result={Result}",
                nameof(System_Storage_GetReadOnlyContext), string.Join(';', resultString));

            return result;
        }

        protected virtual StorageContext SystemStorageAsReadOnly(StorageContext storageContext)
        {
            var result = AsReadOnly(storageContext);

            string[] resultStrings = [result.Id.ToString(), result.IsReadOnly.ToString()];

            _traceLogger.LogInformation(DebugEventLog.Call,
                "{SysCall} id={Id}, readonly={ReadOnly}, result={Result}",
                nameof(System_Storage_AsReadOnly), storageContext.Id, storageContext.IsReadOnly, string.Join(';', resultStrings));

            return result;
        }

        protected virtual ReadOnlyMemory<byte>? SystemStorageGet(StorageContext storageContext, byte[] key)
        {
            var keyString = GetStorageKeyValueString(key, _storageSettings.KeyFormat);
            var result = Get(storageContext, key)?.Span.ToArray() ?? [];
            var resultString = GetStorageKeyValueString(result, _storageSettings.ValueFormat);

            _traceLogger.LogInformation(DebugEventLog.StorageGet,
                "{SysCall} id={Id}, readonly={ReadOnly}, key={Key}, result={Result}",
                nameof(System_Storage_Get), storageContext.Id, storageContext.IsReadOnly, keyString, resultString);

            return result;
        }

        protected virtual IIterator SystemStorageFind(StorageContext storageContext, byte[] prefix, FindOptions options)
        {
            var prefixString = GetStorageKeyValueString(prefix, _storageSettings.KeyFormat);
            var result = Find(storageContext, prefix, options);

            _traceLogger.LogInformation(DebugEventLog.StorageFind,
                "{SysCall} id={Id}, readonly={ReadOnly}, prefix={Prefix}, options={Options}, result={Result}",
                nameof(System_Storage_Find), storageContext.Id, storageContext.IsReadOnly, prefixString, options.ToString(), result.GetType().Name);

            return result;
        }

        protected virtual void SystemStoragePut(StorageContext storageContext, byte[] key, byte[] value)
        {
            var keyString = GetStorageKeyValueString(key, _storageSettings.KeyFormat);
            var valueString = GetStorageKeyValueString(value, _storageSettings.ValueFormat);

            Put(storageContext, key, value);

            _traceLogger.LogInformation(DebugEventLog.StoragePut,
                "{SysCall} id={Id}, readonly={ReadOnly}, key={Key}, value={Value}",
                nameof(System_Runtime_Platform), storageContext.Id, storageContext.IsReadOnly, keyString, valueString);
        }

        protected virtual void SystemStorageDelete(StorageContext storageContext, byte[] key)
        {
            var keyString = GetStorageKeyValueString(key, _storageSettings.KeyFormat);

            Delete(storageContext, key);

            _traceLogger.LogInformation(DebugEventLog.StorageDelete,
                "{SysCall} id={Id}, readonly={ReadOnly}, key={Key}",
                nameof(System_Runtime_Platform), storageContext.Id, storageContext.IsReadOnly, keyString);
        }

        private string GetStorageKeyValueString(byte[] data, TextFormatterType formatter) =>
            formatter switch
            {
                TextFormatterType.HexString => data.ToHexString(),
                TextFormatterType.String => _encoding.GetString(data),
                TextFormatterType.ArrayString => $"[{string.Join(',', data.Select(static s => s.ToString("x02")))}]",
                TextFormatterType.Default or
                TextFormatterType.Base64String or
                _ => System.Convert.ToBase64String(data),
            };
    }
}
