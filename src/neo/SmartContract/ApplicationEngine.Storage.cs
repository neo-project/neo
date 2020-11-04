using Neo.Ledger;
using Neo.SmartContract.Iterators;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const long StoragePrice = 100000;
        public const int MaxStorageKeySize = 64;
        public const int MaxStorageValueSize = ushort.MaxValue;

        public static readonly InteropDescriptor System_Storage_GetContext = Register("System.Storage.GetContext", nameof(GetStorageContext), 0_00000013, CallFlags.AllowStates, false);
        public static readonly InteropDescriptor System_Storage_GetReadOnlyContext = Register("System.Storage.GetReadOnlyContext", nameof(GetReadOnlyContext), 0_00000013, CallFlags.AllowStates, false);
        public static readonly InteropDescriptor System_Storage_AsReadOnly = Register("System.Storage.AsReadOnly", nameof(AsReadOnly), 0_00000013, CallFlags.AllowStates, false);
        public static readonly InteropDescriptor System_Storage_Get = Register("System.Storage.Get", nameof(Get), 0_00033333, CallFlags.AllowStates, false);
        public static readonly InteropDescriptor System_Storage_Find = Register("System.Storage.Find", nameof(Find), 0_00033333, CallFlags.AllowStates, false);
        public static readonly InteropDescriptor System_Storage_Put = Register("System.Storage.Put", nameof(Put), 0, CallFlags.AllowModifyStates, false);
        public static readonly InteropDescriptor System_Storage_PutEx = Register("System.Storage.PutEx", nameof(PutEx), 0, CallFlags.AllowModifyStates, false);
        public static readonly InteropDescriptor System_Storage_Delete = Register("System.Storage.Delete", nameof(Delete), 1 * StoragePrice, CallFlags.AllowModifyStates, false, false);

        protected internal StorageContext GetStorageContext()
        {
            ContractState contract = Snapshot.Contracts.TryGet(CurrentScriptHash);
            if (!contract.HasStorage) throw new InvalidOperationException();
            return new StorageContext
            {
                Id = contract.Id,
                IsReadOnly = false
            };
        }

        protected internal StorageContext GetReadOnlyContext()
        {
            ContractState contract = Snapshot.Contracts.TryGet(CurrentScriptHash);
            if (!contract.HasStorage) throw new InvalidOperationException();
            return new StorageContext
            {
                Id = contract.Id,
                IsReadOnly = true
            };
        }

        protected internal StorageContext AsReadOnly(StorageContext context)
        {
            if (!context.IsReadOnly)
                context = new StorageContext
                {
                    Id = context.Id,
                    IsReadOnly = true
                };
            return context;
        }

        protected internal byte[] Get(StorageContext context, byte[] key)
        {
            return Snapshot.Storages.TryGet(new StorageKey
            {
                Id = context.Id,
                Key = key.ToArray()
            })?.Value;
        }

        protected internal IIterator Find(StorageContext context, byte[] prefix)
        {
            byte[] prefix_key = StorageKey.CreateSearchPrefix(context.Id, prefix);
            StorageIterator iterator = new StorageIterator(Snapshot.Storages.Find(prefix_key).Where(p => p.Key.Key.AsSpan().StartsWith(prefix)).GetEnumerator());
            Disposables.Add(iterator);
            return iterator;
        }

        protected internal void Put(StorageContext context, byte[] key, byte[] value)
        {
            PutExInternal(context, key, value, StorageFlags.None);
        }

        protected internal void PutEx(StorageContext context, byte[] key, byte[] value, StorageFlags flags)
        {
            PutExInternal(context, key, value, flags);
        }

        private void PutExInternal(StorageContext context, byte[] key, byte[] value, StorageFlags flags)
        {
            if (key.Length > MaxStorageKeySize || value.Length > MaxStorageValueSize || context.IsReadOnly)
                throw new ArgumentException();

            int newDataSize;
            StorageKey skey = new StorageKey
            {
                Id = context.Id,
                Key = key
            };
            StorageItem item = Snapshot.Storages.GetAndChange(skey);
            if (item is null)
            {
                newDataSize = key.Length + value.Length;
                Snapshot.Storages.Add(skey, item = new StorageItem());
            }
            else
            {
                if (item.IsConstant) throw new InvalidOperationException();
                if (value.Length == 0)
                    newDataSize = 1;
                else if (value.Length <= item.Value.Length)
                    newDataSize = (value.Length - 1) / 4 + 1;
                else
                    newDataSize = (item.Value.Length - 1) / 4 + 1 + value.Length - item.Value.Length;
            }
            AddGas(newDataSize * StoragePrice, false);

            item.Value = value;
            item.IsConstant = flags.HasFlag(StorageFlags.Constant);
        }

        protected internal void Delete(StorageContext context, byte[] key)
        {
            if (context.IsReadOnly) throw new ArgumentException();
            StorageKey skey = new StorageKey
            {
                Id = context.Id,
                Key = key
            };
            if (Snapshot.Storages.TryGet(skey)?.IsConstant == true)
                throw new InvalidOperationException();
            Snapshot.Storages.Delete(skey);
        }
    }
}
