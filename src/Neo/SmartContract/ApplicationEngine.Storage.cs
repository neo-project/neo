// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngine.Storage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Native;

namespace Neo.SmartContract;

partial class ApplicationEngine
{
    /// <summary>
    /// The maximum size of storage keys.
    /// </summary>
    public const int MaxStorageKeySize = 64;

    /// <summary>
    /// The maximum size of storage values.
    /// </summary>
    public const int MaxStorageValueSize = ushort.MaxValue;

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.GetContext.
    /// Gets the storage context for the current contract.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_GetContext = Register("System.Storage.GetContext", nameof(GetStorageContext), 1 << 4, CallFlags.ReadStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.GetReadOnlyContext.
    /// Gets the readonly storage context for the current contract.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_GetReadOnlyContext = Register("System.Storage.GetReadOnlyContext", nameof(GetReadOnlyContext), 1 << 4, CallFlags.ReadStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.AsReadOnly.
    /// Converts the specified storage context to a new readonly storage context.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_AsReadOnly = Register("System.Storage.AsReadOnly", nameof(AsReadOnly), 1 << 4, CallFlags.ReadStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.Get.
    /// Gets the entry with the specified key from the storage.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_Get = Register("System.Storage.Get", nameof(Get), 1 << 15, CallFlags.ReadStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.Find.
    /// Finds the entries from the storage.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_Find = Register("System.Storage.Find", nameof(Find), 1 << 15, CallFlags.ReadStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.Put.
    /// Puts a new entry into the storage.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_Put = Register("System.Storage.Put", nameof(Put), 1 << 15, CallFlags.WriteStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.Delete.
    /// Deletes an entry from the storage.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_Delete = Register("System.Storage.Delete", nameof(Delete), 1 << 15, CallFlags.WriteStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.Local.Get.
    /// Gets the entry with the specified key from the storage.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_Local_Get = Register("System.Storage.Local.Get", nameof(GetLocal), 1 << 15, CallFlags.ReadStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.Local.Find.
    /// Finds the entries from the storage.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_Local_Find = Register("System.Storage.Local.Find", nameof(FindLocal), 1 << 15, CallFlags.ReadStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.Local.Put.
    /// Puts a new entry into the storage.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_Local_Put = Register("System.Storage.Local.Put", nameof(PutLocal), 1 << 15, CallFlags.WriteStates);

    /// <summary>
    /// The <see cref="InteropDescriptor"/> of System.Storage.Local.Delete.
    /// Deletes an entry from the storage.
    /// </summary>
    public static readonly InteropDescriptor System_Storage_Local_Delete = Register("System.Storage.Local.Delete", nameof(DeleteLocal), 1 << 15, CallFlags.WriteStates);

    /// <summary>
    /// The implementation of System.Storage.GetContext.
    /// Gets the storage context for the current contract.
    /// </summary>
    /// <returns>The storage context for the current contract.</returns>
    protected internal StorageContext GetStorageContext()
    {
        ContractState contract = NativeContract.ContractManagement.GetContract(SnapshotCache, CurrentScriptHash!)
            ?? throw new InvalidOperationException("This method can only be called by a deployed contract.");
        return new StorageContext
        {
            Id = contract.Id,
            IsReadOnly = false
        };
    }

    /// <summary>
    /// The implementation of System.Storage.GetReadOnlyContext.
    /// Gets the readonly storage context for the current contract.
    /// </summary>
    /// <returns>The storage context for the current contract.</returns>
    protected internal StorageContext GetReadOnlyContext()
    {
        ContractState contract = NativeContract.ContractManagement.GetContract(SnapshotCache, CurrentScriptHash!)
            ?? throw new InvalidOperationException("This method can only be called by a deployed contract.");
        return new StorageContext
        {
            Id = contract.Id,
            IsReadOnly = true
        };
    }

    /// <summary>
    /// The implementation of System.Storage.AsReadOnly.
    /// Converts the specified storage context to a new readonly storage context.
    /// </summary>
    /// <param name="context">The storage context to convert.</param>
    /// <returns>The readonly storage context.</returns>
    protected internal static StorageContext AsReadOnly(StorageContext context)
    {
        if (!context.IsReadOnly)
            context = new StorageContext
            {
                Id = context.Id,
                IsReadOnly = true
            };
        return context;
    }

    /// <summary>
    /// The implementation of System.Storage.Get.
    /// Gets the entry with the specified key from the storage.
    /// </summary>
    /// <param name="context">The context of the storage.</param>
    /// <param name="key">The key of the entry.</param>
    /// <returns>The value of the entry. Or <see langword="null"/> if the entry doesn't exist.</returns>
    protected internal ReadOnlyMemory<byte>? Get(StorageContext context, byte[] key)
    {
        return SnapshotCache.TryGet(new StorageKey
        {
            Id = context.Id,
            Key = key
        })?.Value;
    }

    /// <summary>
    /// The implementation of System.Storage.Local.Get.
    /// Gets the entry with the specified key from the storage.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    /// <returns>The value of the entry. Or <see langword="null"/> if the entry doesn't exist.</returns>
    protected internal ReadOnlyMemory<byte>? GetLocal(byte[] key)
    {
        return Get(GetReadOnlyContext(), key);
    }

    /// <summary>
    /// The implementation of System.Storage.Find.
    /// Finds the entries from the storage.
    /// </summary>
    /// <param name="context">The context of the storage.</param>
    /// <param name="prefix">The prefix of keys to find.</param>
    /// <param name="options">The options of the search.</param>
    /// <returns>An iterator for the results.</returns>
    protected internal IIterator Find(StorageContext context, byte[] prefix, FindOptions options)
    {
        if ((options & ~FindOptions.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(options), $"Invalid find options: {options}");

        if (options.HasFlag(FindOptions.KeysOnly) &&
            (options.HasFlag(FindOptions.ValuesOnly) ||
             options.HasFlag(FindOptions.DeserializeValues) ||
             options.HasFlag(FindOptions.PickField0) ||
             options.HasFlag(FindOptions.PickField1)))
        {
            throw new ArgumentException("KeysOnly cannot be used with ValuesOnly, DeserializeValues, PickField0, or PickField1", nameof(options));
        }

        if (options.HasFlag(FindOptions.ValuesOnly) && (options.HasFlag(FindOptions.KeysOnly) || options.HasFlag(FindOptions.RemovePrefix)))
            throw new ArgumentException("ValuesOnly cannot be used with KeysOnly or RemovePrefix", nameof(options));

        if (options.HasFlag(FindOptions.PickField0) && options.HasFlag(FindOptions.PickField1))
            throw new ArgumentException("PickField0 and PickField1 cannot be used together", nameof(options));

        if ((options.HasFlag(FindOptions.PickField0) || options.HasFlag(FindOptions.PickField1)) && !options.HasFlag(FindOptions.DeserializeValues))
            throw new ArgumentException("PickField0 or PickField1 requires DeserializeValues", nameof(options));

        var prefixKey = StorageKey.CreateSearchPrefix(context.Id, prefix);
        var direction = options.HasFlag(FindOptions.Backwards) ? SeekDirection.Backward : SeekDirection.Forward;
        return new StorageIterator(SnapshotCache.Find(prefixKey, direction).GetEnumerator(), prefix.Length, options);
    }

    /// <summary>
    /// The implementation of System.Storage.Local.Find.
    /// Finds the entries from the storage.
    /// </summary>
    /// <param name="prefix">The prefix of keys to find.</param>
    /// <param name="options">The options of the search.</param>
    /// <returns>An iterator for the results.</returns>
    protected internal IIterator FindLocal(byte[] prefix, FindOptions options)
    {
        return Find(GetReadOnlyContext(), prefix, options);
    }

    /// <summary>
    /// The implementation of System.Storage.Put.
    /// Puts a new entry into the storage.
    /// </summary>
    /// <param name="context">The context of the storage.</param>
    /// <param name="key">The key of the entry.</param>
    /// <param name="value">The value of the entry.</param>
    protected internal void Put(StorageContext context, byte[] key, byte[] value)
    {
        if (key.Length > MaxStorageKeySize)
            throw new ArgumentException($"Key length {key.Length} exceeds maximum allowed size of {MaxStorageKeySize} bytes.", nameof(key));
        if (value.Length > MaxStorageValueSize)
            throw new ArgumentException($"Value length {value.Length} exceeds maximum allowed size of {MaxStorageValueSize} bytes.", nameof(value));
        if (context.IsReadOnly) throw new ArgumentException("StorageContext is read-only", nameof(context));

        int newDataSize;
        StorageKey skey = new()
        {
            Id = context.Id,
            Key = key
        };
        var item = SnapshotCache.GetAndChange(skey);
        if (item is null)
        {
            newDataSize = key.Length + value.Length;
            SnapshotCache.Add(skey, item = new StorageItem());
        }
        else
        {
            if (value.Length == 0)
                newDataSize = 0;
            else if (value.Length <= item.Value.Length)
                newDataSize = (value.Length - 1) / 4 + 1;
            else if (item.Value.Length == 0)
                newDataSize = value.Length;
            else
                newDataSize = (item.Value.Length - 1) / 4 + 1 + value.Length - item.Value.Length;
        }
        AddFee(newDataSize * StoragePrice);

        item.Value = value;
    }

    /// <summary>
    /// The implementation of System.Storage.Local.Put.
    /// Puts a new entry into the storage.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    /// <param name="value">The value of the entry.</param>
    protected internal void PutLocal(byte[] key, byte[] value)
    {
        Put(GetStorageContext(), key, value);
    }

    /// <summary>
    /// The implementation of System.Storage.Delete.
    /// Deletes an entry from the storage.
    /// </summary>
    /// <param name="context">The context of the storage.</param>
    /// <param name="key">The key of the entry.</param>
    protected internal void Delete(StorageContext context, byte[] key)
    {
        if (context.IsReadOnly) throw new ArgumentException("StorageContext is read-only", nameof(context));
        SnapshotCache.Delete(new StorageKey
        {
            Id = context.Id,
            Key = key
        });
    }

    /// <summary>
    /// The implementation of System.Storage.Local.Delete.
    /// Deletes an entry from the storage.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    protected internal void DeleteLocal(byte[] key)
    {
        Delete(GetStorageContext(), key);
    }
}
