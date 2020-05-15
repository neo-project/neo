using Neo.Ledger;
using Neo.SmartContract.Iterators;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const long StoragePrice = 100000;
        public const int MaxStorageKeySize = 64;
        public const int MaxStorageValueSize = ushort.MaxValue;

        private bool PutExInternal(StorageContext context, byte[] key, byte[] value, StorageFlags flags)
        {
            if (key.Length > MaxStorageKeySize) return false;
            if (value.Length > MaxStorageValueSize) return false;
            if (context.IsReadOnly) return false;

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
                Snapshot.Storages.Add(skey, new StorageItem(value));
            }
            else
            {
                if (item.IsConstant) return false;
                if (value.Length <= item.Value.Length)
                    newDataSize = 1;
                else
                    newDataSize = value.Length - item.Value.Length;
                item.Value = value;
                item.IsConstant = flags.HasFlag(StorageFlags.Constant);
            }

            return AddGas(newDataSize * StoragePrice);
        }

        [InteropService("System.Storage.GetContext", 0_00000400, TriggerType.Application, CallFlags.AllowStates)]
        private bool Storage_GetContext()
        {
            ContractState contract = Snapshot.Contracts.TryGet(CurrentScriptHash);
            if (contract == null) return false;
            if (!contract.HasStorage) return false;
            Push(StackItem.FromInterface(new StorageContext
            {
                Id = contract.Id,
                IsReadOnly = false
            }));
            return true;
        }

        [InteropService("System.Storage.GetReadOnlyContext", 0_00000400, TriggerType.Application, CallFlags.AllowStates)]
        private bool Storage_GetReadOnlyContext()
        {
            ContractState contract = Snapshot.Contracts.TryGet(CurrentScriptHash);
            if (contract == null) return false;
            if (!contract.HasStorage) return false;
            Push(StackItem.FromInterface(new StorageContext
            {
                Id = contract.Id,
                IsReadOnly = true
            }));
            return true;
        }

        [InteropService("System.Storage.AsReadOnly", 0_00000400, TriggerType.Application, CallFlags.AllowStates)]
        private bool Storage_AsReadOnly()
        {
            if (!TryPopInterface(out StorageContext context)) return false;
            if (!context.IsReadOnly)
                context = new StorageContext
                {
                    Id = context.Id,
                    IsReadOnly = true
                };
            Push(StackItem.FromInterface(context));
            return true;
        }

        [InteropService("System.Storage.Get", 0_01000000, TriggerType.Application, CallFlags.AllowStates)]
        private bool Storage_Get()
        {
            if (!TryPopInterface(out StorageContext context)) return false;
            if (!TryPop(out ReadOnlySpan<byte> key)) return false;
            StorageItem item = Snapshot.Storages.TryGet(new StorageKey
            {
                Id = context.Id,
                Key = key.ToArray()
            });
            Push(item?.Value ?? StackItem.Null);
            return true;
        }

        [InteropService("System.Storage.Find", 0_01000000, TriggerType.Application, CallFlags.AllowStates)]
        private bool Storage_Find()
        {
            if (!TryPopInterface(out StorageContext context)) return false;
            byte[] prefix = Pop().GetSpan().ToArray();
            byte[] prefix_key = StorageKey.CreateSearchPrefix(context.Id, prefix);
            StorageIterator iterator = new StorageIterator(Snapshot.Storages.Find(prefix_key).Where(p => p.Key.Key.AsSpan().StartsWith(prefix)).GetEnumerator());
            disposables.Add(iterator);
            Push(StackItem.FromInterface(iterator));
            return true;
        }

        [InteropService("System.Storage.Put", 0, TriggerType.Application, CallFlags.AllowModifyStates)]
        private bool Storage_Put()
        {
            if (!TryPopInterface(out StorageContext context)) return false;
            byte[] key = Pop().GetSpan().ToArray();
            byte[] value = Pop().GetSpan().ToArray();
            return PutExInternal(context, key, value, StorageFlags.None);
        }

        [InteropService("System.Storage.PutEx", 0, TriggerType.Application, CallFlags.AllowModifyStates)]
        private bool Storage_PutEx()
        {
            if (!TryPopInterface(out StorageContext context)) return false;
            byte[] key = Pop().GetSpan().ToArray();
            byte[] value = Pop().GetSpan().ToArray();
            if (!TryPop(out int flags)) return false;
            return PutExInternal(context, key, value, (StorageFlags)flags);
        }

        [InteropService("System.Storage.Delete", 1 * StoragePrice, TriggerType.Application, CallFlags.AllowModifyStates)]
        private bool Storage_Delete()
        {
            if (!TryPopInterface(out StorageContext context)) return false;
            if (context.IsReadOnly) return false;
            StorageKey key = new StorageKey
            {
                Id = context.Id,
                Key = Pop().GetSpan().ToArray()
            };
            if (Snapshot.Storages.TryGet(key)?.IsConstant == true) return false;
            Snapshot.Storages.Delete(key);
            return true;
        }
    }
}
