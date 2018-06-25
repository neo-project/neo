using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract
{
    public class StandardService : InteropService, IDisposable
    {
        public static event EventHandler<NotifyEventArgs> Notify;
        public static event EventHandler<LogEventArgs> Log;

        protected readonly TriggerType Trigger;
        protected readonly Snapshot Snapshot;
        protected readonly List<IDisposable> Disposables = new List<IDisposable>();
        protected readonly Dictionary<UInt160, UInt160> ContractsCreated = new Dictionary<UInt160, UInt160>();
        private readonly List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();

        public IReadOnlyList<NotifyEventArgs> Notifications => notifications;

        public StandardService(TriggerType trigger, Snapshot snapshot)
        {
            this.Trigger = trigger;
            this.Snapshot = snapshot;
            Register("System.Runtime.GetTrigger", Runtime_GetTrigger);
            Register("System.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("System.Runtime.Notify", Runtime_Notify);
            Register("System.Runtime.Log", Runtime_Log);
            Register("System.Runtime.GetTime", Runtime_GetTime);
            Register("System.Runtime.Serialize", Runtime_Serialize);
            Register("System.Runtime.Deserialize", Runtime_Deserialize);
            Register("System.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("System.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("System.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("System.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("System.Blockchain.GetTransactionHeight", Blockchain_GetTransactionHeight);
            Register("System.Blockchain.GetContract", Blockchain_GetContract);
            Register("System.Header.GetIndex", Header_GetIndex);
            Register("System.Header.GetHash", Header_GetHash);
            Register("System.Header.GetPrevHash", Header_GetPrevHash);
            Register("System.Header.GetTimestamp", Header_GetTimestamp);
            Register("System.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("System.Block.GetTransactions", Block_GetTransactions);
            Register("System.Block.GetTransaction", Block_GetTransaction);
            Register("System.Transaction.GetHash", Transaction_GetHash);
            Register("System.Contract.Destroy", Contract_Destroy);
            Register("System.Contract.GetStorageContext", Contract_GetStorageContext);
            Register("System.Storage.GetContext", Storage_GetContext);
            Register("System.Storage.GetReadOnlyContext", Storage_GetReadOnlyContext);
            Register("System.Storage.Get", Storage_Get);
            Register("System.Storage.Put", Storage_Put);
            Register("System.Storage.Delete", Storage_Delete);
            Register("System.StorageContext.AsReadOnly", StorageContext_AsReadOnly);
        }

        internal bool CheckStorageContext(StorageContext context)
        {
            ContractState contract = Snapshot.Contracts.TryGet(context.ScriptHash);
            if (contract == null) return false;
            if (!contract.HasStorage) return false;
            return true;
        }

        public void Commit()
        {
            Snapshot.Commit();
        }

        public void Dispose()
        {
            foreach (IDisposable disposable in Disposables)
                disposable.Dispose();
            Disposables.Clear();
        }

        protected bool Runtime_GetTrigger(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push((int)Trigger);
            return true;
        }

        protected bool CheckWitness(ExecutionEngine engine, UInt160 hash)
        {
            IVerifiable container = (IVerifiable)engine.ScriptContainer;
            UInt160[] _hashes_for_verifying = container.GetScriptHashesForVerifying(Snapshot);
            return _hashes_for_verifying.Contains(hash);
        }

        protected bool CheckWitness(ExecutionEngine engine, ECPoint pubkey)
        {
            return CheckWitness(engine, Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
        }

        protected bool Runtime_CheckWitness(ExecutionEngine engine)
        {
            byte[] hashOrPubkey = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            bool result;
            if (hashOrPubkey.Length == 20)
                result = CheckWitness(engine, new UInt160(hashOrPubkey));
            else if (hashOrPubkey.Length == 33)
                result = CheckWitness(engine, ECPoint.DecodePoint(hashOrPubkey, ECCurve.Secp256r1));
            else
                return false;
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        protected bool Runtime_Notify(ExecutionEngine engine)
        {
            StackItem state = engine.CurrentContext.EvaluationStack.Pop();
            NotifyEventArgs notification = new NotifyEventArgs(engine.ScriptContainer, new UInt160(engine.CurrentContext.ScriptHash), state);
            Notify?.Invoke(this, notification);
            notifications.Add(notification);
            return true;
        }

        protected bool Runtime_Log(ExecutionEngine engine)
        {
            string message = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            Log?.Invoke(this, new LogEventArgs(engine.ScriptContainer, new UInt160(engine.CurrentContext.ScriptHash), message));
            return true;
        }

        protected bool Runtime_GetTime(ExecutionEngine engine)
        {
            if (Snapshot.PersistingBlock == null)
            {
                Header header = Snapshot.GetHeader(Snapshot.CurrentBlockHash);
                engine.CurrentContext.EvaluationStack.Push(header.Timestamp + Blockchain.SecondsPerBlock);
            }
            else
            {
                engine.CurrentContext.EvaluationStack.Push(Snapshot.PersistingBlock.Timestamp);
            }
            return true;
        }

        private void SerializeStackItem(StackItem item, BinaryWriter writer)
        {
            switch (item)
            {
                case ByteArray _:
                    writer.Write((byte)StackItemType.ByteArray);
                    writer.WriteVarBytes(item.GetByteArray());
                    break;
                case VMBoolean _:
                    writer.Write((byte)StackItemType.Boolean);
                    writer.Write(item.GetBoolean());
                    break;
                case Integer _:
                    writer.Write((byte)StackItemType.Integer);
                    writer.WriteVarBytes(item.GetByteArray());
                    break;
                case InteropInterface _:
                    throw new NotSupportedException();
                case VMArray array:
                    if (array is Struct)
                        writer.Write((byte)StackItemType.Struct);
                    else
                        writer.Write((byte)StackItemType.Array);
                    writer.WriteVarInt(array.Count);
                    foreach (StackItem subitem in array)
                        SerializeStackItem(subitem, writer);
                    break;
                case Map map:
                    writer.Write((byte)StackItemType.Map);
                    writer.WriteVarInt(map.Count);
                    foreach (var pair in map)
                    {
                        SerializeStackItem(pair.Key, writer);
                        SerializeStackItem(pair.Value, writer);
                    }
                    break;
            }
        }

        protected bool Runtime_Serialize(ExecutionEngine engine)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                try
                {
                    SerializeStackItem(engine.CurrentContext.EvaluationStack.Pop(), writer);
                }
                catch (NotSupportedException)
                {
                    return false;
                }
                writer.Flush();
                engine.CurrentContext.EvaluationStack.Push(ms.ToArray());
            }
            return true;
        }

        private StackItem DeserializeStackItem(BinaryReader reader)
        {
            StackItemType type = (StackItemType)reader.ReadByte();
            switch (type)
            {
                case StackItemType.ByteArray:
                    return new ByteArray(reader.ReadVarBytes());
                case StackItemType.Boolean:
                    return new VMBoolean(reader.ReadBoolean());
                case StackItemType.Integer:
                    return new Integer(new BigInteger(reader.ReadVarBytes()));
                case StackItemType.Array:
                case StackItemType.Struct:
                    {
                        VMArray array = type == StackItemType.Struct ? new Struct() : new VMArray();
                        ulong count = reader.ReadVarInt();
                        while (count-- > 0)
                            array.Add(DeserializeStackItem(reader));
                        return array;
                    }
                case StackItemType.Map:
                    {
                        Map map = new Map();
                        ulong count = reader.ReadVarInt();
                        while (count-- > 0)
                        {
                            StackItem key = DeserializeStackItem(reader);
                            StackItem value = DeserializeStackItem(reader);
                            map[key] = value;
                        }
                        return map;
                    }
                default:
                    throw new FormatException();
            }
        }

        protected bool Runtime_Deserialize(ExecutionEngine engine)
        {
            byte[] data = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                StackItem item;
                try
                {
                    item = DeserializeStackItem(reader);
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (IOException)
                {
                    return false;
                }
                engine.CurrentContext.EvaluationStack.Push(item);
            }
            return true;
        }

        protected bool Blockchain_GetHeight(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(Snapshot.Height);
            return true;
        }

        protected bool Blockchain_GetHeader(ExecutionEngine engine)
        {
            byte[] data = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            UInt256 hash;
            if (data.Length <= 5)
                hash = Blockchain.Singleton.GetBlockHash((uint)new BigInteger(data));
            else if (data.Length == 32)
                hash = new UInt256(data);
            else
                return false;
            if (hash == null)
            {
                engine.CurrentContext.EvaluationStack.Push(new byte[0]);
            }
            else
            {
                Header header = Snapshot.GetHeader(hash);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(header));
            }
            return true;
        }

        protected bool Blockchain_GetBlock(ExecutionEngine engine)
        {
            byte[] data = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            UInt256 hash;
            if (data.Length <= 5)
                hash = Blockchain.Singleton.GetBlockHash((uint)new BigInteger(data));
            else if (data.Length == 32)
                hash = new UInt256(data);
            else
                return false;
            if (hash == null)
            {
                engine.CurrentContext.EvaluationStack.Push(new byte[0]);
            }
            else
            {
                Block block = Snapshot.GetBlock(hash);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(block));
            }
            return true;
        }

        protected bool Blockchain_GetTransaction(ExecutionEngine engine)
        {
            byte[] hash = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            Transaction tx = Snapshot.GetTransaction(new UInt256(hash));
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        protected bool Blockchain_GetTransactionHeight(ExecutionEngine engine)
        {
            byte[] hash = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            int? height = (int?)Snapshot.Transactions.TryGet(new UInt256(hash))?.BlockIndex;
            engine.CurrentContext.EvaluationStack.Push(height ?? -1);
            return true;
        }

        protected bool Blockchain_GetContract(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null)
                engine.CurrentContext.EvaluationStack.Push(new byte[0]);
            else
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        protected bool Header_GetIndex(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.Index);
                return true;
            }
            return false;
        }

        protected bool Header_GetHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.Hash.ToArray());
                return true;
            }
            return false;
        }

        protected bool Header_GetPrevHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.PrevHash.ToArray());
                return true;
            }
            return false;
        }

        protected bool Header_GetTimestamp(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.Timestamp);
                return true;
            }
            return false;
        }

        protected bool Block_GetTransactionCount(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Block block = _interface.GetInterface<Block>();
                if (block == null) return false;
                engine.CurrentContext.EvaluationStack.Push(block.Transactions.Length);
                return true;
            }
            return false;
        }

        protected bool Block_GetTransactions(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Block block = _interface.GetInterface<Block>();
                if (block == null) return false;
                engine.CurrentContext.EvaluationStack.Push(block.Transactions.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected bool Block_GetTransaction(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Block block = _interface.GetInterface<Block>();
                int index = (int)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
                if (block == null) return false;
                if (index < 0 || index >= block.Transactions.Length) return false;
                Transaction tx = block.Transactions[index];
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(tx));
                return true;
            }
            return false;
        }

        protected bool Transaction_GetHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Hash.ToArray());
                return true;
            }
            return false;
        }

        protected bool Storage_GetContext(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
            {
                ScriptHash = new UInt160(engine.CurrentContext.ScriptHash),
                IsReadOnly = false
            }));
            return true;
        }

        protected bool Storage_GetReadOnlyContext(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
            {
                ScriptHash = new UInt160(engine.CurrentContext.ScriptHash),
                IsReadOnly = true
            }));
            return true;
        }

        protected bool Storage_Get(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(context)) return false;
                byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                StorageItem item = Snapshot.Storages.TryGet(new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                });
                engine.CurrentContext.EvaluationStack.Push(item?.Value ?? new byte[0]);
                return true;
            }
            return false;
        }

        protected bool StorageContext_AsReadOnly(ExecutionEngine engine)
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

        protected bool Contract_GetStorageContext(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (!ContractsCreated.TryGetValue(contract.ScriptHash, out UInt160 created)) return false;
                if (!created.Equals(new UInt160(engine.CurrentContext.ScriptHash))) return false;
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
                {
                    ScriptHash = contract.ScriptHash,
                    IsReadOnly = false
                }));
                return true;
            }
            return false;
        }

        protected bool Contract_Destroy(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            UInt160 hash = new UInt160(engine.CurrentContext.ScriptHash);
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null) return true;
            Snapshot.Contracts.Delete(hash);
            if (contract.HasStorage)
                foreach (var pair in Snapshot.Storages.Find(hash.ToArray()))
                    Snapshot.Storages.Delete(pair.Key);
            return true;
        }

        protected bool Storage_Put(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application && Trigger != TriggerType.ApplicationR)
                return false;
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (context.IsReadOnly) return false;
                if (!CheckStorageContext(context)) return false;
                byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                if (key.Length > 1024) return false;
                byte[] value = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                Snapshot.Storages.GetAndChange(new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                }, () => new StorageItem()).Value = value;
                return true;
            }
            return false;
        }

        protected bool Storage_Delete(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application && Trigger != TriggerType.ApplicationR)
                return false;
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (context.IsReadOnly) return false;
                if (!CheckStorageContext(context)) return false;
                byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                Snapshot.Storages.Delete(new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                });
                return true;
            }
            return false;
        }
    }
}
