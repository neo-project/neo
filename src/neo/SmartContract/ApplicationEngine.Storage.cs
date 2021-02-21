using Neo.SmartContract.Iterators;
using Neo.SmartContract.Native;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const int MaxStorageKeySize = 64;
        public const int MaxStorageValueSize = ushort.MaxValue;

        public static readonly InteropDescriptor System_Storage_GetContext = Register("System.Storage.GetContext", nameof(GetStorageContext), 1 << 4, CallFlags.ReadStates);
        public static readonly InteropDescriptor System_Storage_GetReadOnlyContext = Register("System.Storage.GetReadOnlyContext", nameof(GetReadOnlyContext), 1 << 4, CallFlags.ReadStates);
        public static readonly InteropDescriptor System_Storage_AsReadOnly = Register("System.Storage.AsReadOnly", nameof(AsReadOnly), 1 << 4, CallFlags.ReadStates);
        public static readonly InteropDescriptor System_Storage_Get = Register("System.Storage.Get", nameof(Get), 1 << 15, CallFlags.ReadStates);
        public static readonly InteropDescriptor System_Storage_Find = Register("System.Storage.Find", nameof(Find), 1 << 15, CallFlags.ReadStates);
        public static readonly InteropDescriptor System_Storage_Put = Register("System.Storage.Put", nameof(Put), 1 << 15, CallFlags.WriteStates);
        public static readonly InteropDescriptor System_Storage_Delete = Register("System.Storage.Delete", nameof(Delete), 1 << 15, CallFlags.WriteStates);

        protected internal StorageContext GetStorageContext()
        {
            ContractState contract = NativeContract.ContractManagement.GetContract(Snapshot, CurrentScriptHash);
            return new StorageContext
            {
                Id = contract.Id,
                IsReadOnly = false
            };
        }

        protected internal StorageContext GetReadOnlyContext()
        {
            ContractState contract = NativeContract.ContractManagement.GetContract(Snapshot, CurrentScriptHash);
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
            return Snapshot.TryGet(new StorageKey
            {
                Id = context.Id,
                Key = key.ToArray()
            })?.Value;
        }

        protected internal IIterator Find(StorageContext context, byte[] prefix, FindOptions options)
        {
            if ((options & ~FindOptions.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(options));
            if (options.HasFlag(FindOptions.KeysOnly) && (options.HasFlag(FindOptions.ValuesOnly) || options.HasFlag(FindOptions.DeserializeValues) || options.HasFlag(FindOptions.PickField0) || options.HasFlag(FindOptions.PickField1)))
                throw new ArgumentException();
            if (options.HasFlag(FindOptions.ValuesOnly) && (options.HasFlag(FindOptions.KeysOnly) || options.HasFlag(FindOptions.RemovePrefix)))
                throw new ArgumentException();
            if (options.HasFlag(FindOptions.PickField0) && options.HasFlag(FindOptions.PickField1))
                throw new ArgumentException();
            if ((options.HasFlag(FindOptions.PickField0) || options.HasFlag(FindOptions.PickField1)) && !options.HasFlag(FindOptions.DeserializeValues))
                throw new ArgumentException();
            byte[] prefix_key = StorageKey.CreateSearchPrefix(context.Id, prefix);
            return new StorageIterator(Snapshot.Find(prefix_key).GetEnumerator(), options, ReferenceCounter);
        }

        protected internal void Put(StorageContext context, byte[] key, byte[] value)
        {
            if (key.Length > MaxStorageKeySize || value.Length > MaxStorageValueSize || context.IsReadOnly)
                throw new ArgumentException();

            int newDataSize;
            StorageKey skey = new StorageKey
            {
                Id = context.Id,
                Key = key
            };
            StorageItem item = Snapshot.GetAndChange(skey);
            if (item is null)
            {
                newDataSize = key.Length + value.Length;
                Snapshot.Add(skey, item = new StorageItem());
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
            AddGas(newDataSize * StoragePrice);

            item.Value = value;
        }

        protected internal void Delete(StorageContext context, byte[] key)
        {
            if (context.IsReadOnly) throw new ArgumentException();
            Snapshot.Delete(new StorageKey
            {
                Id = context.Id,
                Key = key
            });
        }
    }
}
