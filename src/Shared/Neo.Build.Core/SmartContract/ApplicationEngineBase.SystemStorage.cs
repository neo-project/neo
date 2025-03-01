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
        protected internal StorageContext SystemStorageGetContext()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Storage_GetContext));

            var result = GetStorageContext();

            string[] resultString = [result.Id.ToString(), result.IsReadOnly.ToString()];

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Storage_GetContext), string.Join(';', resultString));

            return result;
        }

        protected virtual StorageContext SystemStorageGetReadOnlyContext()
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall}",
                nameof(System_Storage_GetReadOnlyContext));

            var result = GetReadOnlyContext();

            string[] resultString = [result.Id.ToString(), result.IsReadOnly.ToString()];

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Storage_GetReadOnlyContext), string.Join(';', resultString));

            return result;
        }

        protected virtual StorageContext SystemStorageAsReadOnly(StorageContext storageContext)
        {
            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} id={Id}, readonly={ReadOnly}",
                nameof(System_Storage_AsReadOnly), storageContext.Id, storageContext.IsReadOnly);

            var result = AsReadOnly(storageContext);

            string[] resultStrings = [result.Id.ToString(), result.IsReadOnly.ToString()];

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Storage_AsReadOnly), string.Join(';', resultStrings));

            return result;
        }

        protected virtual ReadOnlyMemory<byte>? SystemStorageGet(StorageContext storageContext, byte[] key)
        {
            var keyString = GetStorageKeyValueString(key, _storageSettings.KeyFormat);

            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} id={Id}, readonly={ReadOnly}, key={Key}",
                nameof(System_Storage_Get), storageContext.Id, storageContext.IsReadOnly, keyString);

            var result = Get(storageContext, key)?.Span.ToArray() ?? [];

            var resultString = GetStorageKeyValueString(result, _storageSettings.ValueFormat);

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Storage_Get), resultString);

            return result;
        }

        protected virtual IIterator SystemStorageFind(StorageContext storageContext, byte[] prefix, FindOptions options)
        {
            var prefixString = GetStorageKeyValueString(prefix, _storageSettings.KeyFormat);

            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} id={Id}, readonly={ReadOnly}, prefix={Prefix}, options={Options}",
                nameof(System_Storage_Find), storageContext.Id, storageContext.IsReadOnly, prefixString, options.ToString());

            var result = Find(storageContext, prefix, options);

            _traceLogger.LogInformation(VMEventLog.Result,
                "{SysCall} result={Result}",
                nameof(System_Storage_Find), result.GetType().Name);

            return result;
        }

        protected virtual void SystemStoragePut(StorageContext storageContext, byte[] key, byte[] value)
        {
            var keyString = GetStorageKeyValueString(key, _storageSettings.KeyFormat);
            var valueString = GetStorageKeyValueString(value, _storageSettings.ValueFormat);

            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} id={Id}, readonly={ReadOnly}, key={Key}, value={Value}",
                nameof(System_Runtime_Platform), storageContext.Id, storageContext.IsReadOnly, keyString, valueString);

            Put(storageContext, key, value);
        }

        protected virtual void SystemStorageDelete(StorageContext storageContext, byte[] key)
        {
            var keyString = GetStorageKeyValueString(key, _storageSettings.KeyFormat);

            _traceLogger.LogInformation(VMEventLog.Call,
                "{SysCall} id={Id}, readonly={ReadOnly}, key={Key}",
                nameof(System_Runtime_Platform), storageContext.Id, storageContext.IsReadOnly, keyString);

            Delete(storageContext, key);
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
