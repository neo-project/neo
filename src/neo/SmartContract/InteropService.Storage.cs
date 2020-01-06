using Neo.Ledger;
using Neo.SmartContract.Iterators;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Storage
        {
            public const long GasPerByte = 100000;
            public const int MaxKeySize = 64;
            public const int MaxValueSize = ushort.MaxValue;

            public static readonly InteropDescriptor GetContext = Register("System.Storage.GetContext", Storage_GetContext, 0_00000400, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor GetReadOnlyContext = Register("System.Storage.GetReadOnlyContext", Storage_GetReadOnlyContext, 0_00000400, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor AsReadOnly = Register("System.Storage.AsReadOnly", Storage_AsReadOnly, 0_00000400, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor Get = Register("System.Storage.Get", Storage_Get, 0_01000000, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor Find = Register("System.Storage.Find", Storage_Find, 0_01000000, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor Put = Register("System.Storage.Put", Storage_Put, GetStoragePrice, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor PutEx = Register("System.Storage.PutEx", Storage_PutEx, GetStoragePrice, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor Delete = Register("System.Storage.Delete", Storage_Delete, 0_01000000, TriggerType.Application, CallFlags.AllowModifyStates);

            private static bool CheckStorageContext(ApplicationEngine engine, StorageContext context)
            {
                ContractState contract = engine.Snapshot.Contracts.TryGet(context.ScriptHash);
                if (contract == null) return false;
                if (!contract.HasStorage) return false;
                return true;
            }

            private static long GetStoragePrice(EvaluationStack stack)
            {
                return (stack.Peek(1).GetByteLength() + stack.Peek(2).GetByteLength()) * GasPerByte;
            }

            private static bool PutExInternal(ApplicationEngine engine, StorageContext context, byte[] key, byte[] value, StorageFlags flags)
            {
                if (key.Length > MaxKeySize) return false;
                if (value.Length > MaxValueSize) return false;
                if (context.IsReadOnly) return false;
                if (!CheckStorageContext(engine, context)) return false;

                StorageKey skey = new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                };

                if (engine.Snapshot.Storages.TryGet(skey)?.IsConstant == true) return false;

                if (value.Length == 0 && !flags.HasFlag(StorageFlags.Constant))
                {
                    // If put 'value' is empty (and non-const), we remove it (implicit `Storage.Delete`)
                    engine.Snapshot.Storages.Delete(skey);
                }
                else
                {
                    StorageItem item = engine.Snapshot.Storages.GetAndChange(skey, () => new StorageItem());
                    item.Value = value;
                    item.IsConstant = flags.HasFlag(StorageFlags.Constant);
                }
                return true;
            }

            private static bool Storage_GetContext(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
                {
                    ScriptHash = engine.CurrentScriptHash,
                    IsReadOnly = false
                }));
                return true;
            }

            private static bool Storage_GetReadOnlyContext(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
                {
                    ScriptHash = engine.CurrentScriptHash,
                    IsReadOnly = true
                }));
                return true;
            }

            private static bool Storage_AsReadOnly(ApplicationEngine engine)
            {
                if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
                {
                    StorageContext context = _interface.GetInterface<StorageContext>();
                    if (!context.IsReadOnly)
                        context = new StorageContext
                        {
                            ScriptHash = context.ScriptHash,
                            IsReadOnly = true
                        };
                    engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(context));
                    return true;
                }
                return false;
            }

            private static bool Storage_Get(ApplicationEngine engine)
            {
                if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
                {
                    StorageContext context = _interface.GetInterface<StorageContext>();
                    if (!CheckStorageContext(engine, context)) return false;
                    byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                    StorageItem item = engine.Snapshot.Storages.TryGet(new StorageKey
                    {
                        ScriptHash = context.ScriptHash,
                        Key = key
                    });
                    engine.CurrentContext.EvaluationStack.Push(item?.Value ?? StackItem.Null);
                    return true;
                }
                return false;
            }

            private static bool Storage_Find(ApplicationEngine engine)
            {
                if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
                {
                    StorageContext context = _interface.GetInterface<StorageContext>();
                    if (!CheckStorageContext(engine, context)) return false;
                    byte[] prefix = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                    byte[] prefix_key = StorageKey.CreateSearchPrefix(context.ScriptHash, prefix);
                    StorageIterator iterator = engine.AddDisposable(new StorageIterator(engine.Snapshot.Storages.Find(prefix_key).Where(p => p.Key.Key.AsSpan().StartsWith(prefix)).GetEnumerator()));
                    engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                    return true;
                }
                return false;
            }

            private static bool Storage_Put(ApplicationEngine engine)
            {
                if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface))
                    return false;
                StorageContext context = _interface.GetInterface<StorageContext>();
                byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                byte[] value = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                return PutExInternal(engine, context, key, value, StorageFlags.None);
            }

            private static bool Storage_PutEx(ApplicationEngine engine)
            {
                if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface))
                    return false;
                StorageContext context = _interface.GetInterface<StorageContext>();
                byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                byte[] value = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                StorageFlags flags = (StorageFlags)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
                return PutExInternal(engine, context, key, value, flags);
            }

            private static bool Storage_Delete(ApplicationEngine engine)
            {
                if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
                {
                    StorageContext context = _interface.GetInterface<StorageContext>();
                    if (context.IsReadOnly) return false;
                    if (!CheckStorageContext(engine, context)) return false;
                    StorageKey key = new StorageKey
                    {
                        ScriptHash = context.ScriptHash,
                        Key = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray()
                    };
                    if (engine.Snapshot.Storages.TryGet(key)?.IsConstant == true) return false;
                    engine.Snapshot.Storages.Delete(key);
                    return true;
                }
                return false;
            }
        }
    }
}
