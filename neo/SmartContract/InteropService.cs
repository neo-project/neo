using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
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

namespace Neo.SmartContract
{
    public static partial class InteropService
    {
        private static readonly Dictionary<uint, Func<ApplicationEngine, bool>> methods = new Dictionary<uint, Func<ApplicationEngine, bool>>();
        private static readonly Dictionary<uint, long> prices = new Dictionary<uint, long>();

        public static readonly uint System_ExecutionEngine_GetScriptContainer = Register("System.ExecutionEngine.GetScriptContainer", ExecutionEngine_GetScriptContainer, 1);
        public static readonly uint System_ExecutionEngine_GetExecutingScriptHash = Register("System.ExecutionEngine.GetExecutingScriptHash", ExecutionEngine_GetExecutingScriptHash, 1);
        public static readonly uint System_ExecutionEngine_GetCallingScriptHash = Register("System.ExecutionEngine.GetCallingScriptHash", ExecutionEngine_GetCallingScriptHash, 1);
        public static readonly uint System_ExecutionEngine_GetEntryScriptHash = Register("System.ExecutionEngine.GetEntryScriptHash", ExecutionEngine_GetEntryScriptHash, 1);
        public static readonly uint System_Runtime_Platform = Register("System.Runtime.Platform", Runtime_Platform, 1);
        public static readonly uint System_Runtime_GetTrigger = Register("System.Runtime.GetTrigger", Runtime_GetTrigger, 1);
        public static readonly uint System_Runtime_CheckWitness = Register("System.Runtime.CheckWitness", Runtime_CheckWitness, 200);
        public static readonly uint System_Runtime_Notify = Register("System.Runtime.Notify", Runtime_Notify, 1);
        public static readonly uint System_Runtime_Log = Register("System.Runtime.Log", Runtime_Log, 1);
        public static readonly uint System_Runtime_GetTime = Register("System.Runtime.GetTime", Runtime_GetTime, 1);
        public static readonly uint System_Runtime_Serialize = Register("System.Runtime.Serialize", Runtime_Serialize, 1);
        public static readonly uint System_Runtime_Deserialize = Register("System.Runtime.Deserialize", Runtime_Deserialize, 1);
        public static readonly uint System_Crypto_Verify = Register("System.Crypto.Verify", Crypto_Verify, 100);
        public static readonly uint System_Blockchain_GetHeight = Register("System.Blockchain.GetHeight", Blockchain_GetHeight, 1);
        public static readonly uint System_Blockchain_GetHeader = Register("System.Blockchain.GetHeader", Blockchain_GetHeader, 100);
        public static readonly uint System_Blockchain_GetBlock = Register("System.Blockchain.GetBlock", Blockchain_GetBlock, 200);
        public static readonly uint System_Blockchain_GetTransaction = Register("System.Blockchain.GetTransaction", Blockchain_GetTransaction, 200);
        public static readonly uint System_Blockchain_GetTransactionHeight = Register("System.Blockchain.GetTransactionHeight", Blockchain_GetTransactionHeight, 100);
        public static readonly uint System_Blockchain_GetContract = Register("System.Blockchain.GetContract", Blockchain_GetContract, 100);
        public static readonly uint System_Header_GetIndex = Register("System.Header.GetIndex", Header_GetIndex, 1);
        public static readonly uint System_Header_GetHash = Register("System.Header.GetHash", Header_GetHash, 1);
        public static readonly uint System_Header_GetPrevHash = Register("System.Header.GetPrevHash", Header_GetPrevHash, 1);
        public static readonly uint System_Header_GetTimestamp = Register("System.Header.GetTimestamp", Header_GetTimestamp, 1);
        public static readonly uint System_Block_GetTransactionCount = Register("System.Block.GetTransactionCount", Block_GetTransactionCount, 1);
        public static readonly uint System_Block_GetTransactions = Register("System.Block.GetTransactions", Block_GetTransactions, 1);
        public static readonly uint System_Block_GetTransaction = Register("System.Block.GetTransaction", Block_GetTransaction, 1);
        public static readonly uint System_Transaction_GetHash = Register("System.Transaction.GetHash", Transaction_GetHash, 1);
        public static readonly uint System_Contract_Call = Register("System.Contract.Call", Contract_Call, 10);
        public static readonly uint System_Contract_Destroy = Register("System.Contract.Destroy", Contract_Destroy, 1);
        public static readonly uint System_Storage_GetContext = Register("System.Storage.GetContext", Storage_GetContext, 1);
        public static readonly uint System_Storage_GetReadOnlyContext = Register("System.Storage.GetReadOnlyContext", Storage_GetReadOnlyContext, 1);
        public static readonly uint System_Storage_Get = Register("System.Storage.Get", Storage_Get, 100);
        public static readonly uint System_Storage_Put = Register("System.Storage.Put", Storage_Put);
        public static readonly uint System_Storage_PutEx = Register("System.Storage.PutEx", Storage_PutEx);
        public static readonly uint System_Storage_Delete = Register("System.Storage.Delete", Storage_Delete, 100);
        public static readonly uint System_StorageContext_AsReadOnly = Register("System.StorageContext.AsReadOnly", StorageContext_AsReadOnly, 1);

        private static bool CheckStorageContext(ApplicationEngine engine, StorageContext context)
        {
            ContractState contract = engine.Snapshot.Contracts.TryGet(context.ScriptHash);
            if (contract == null) return false;
            if (!contract.HasStorage) return false;
            return true;
        }

        public static long GetPrice(uint hash)
        {
            prices.TryGetValue(hash, out long price);
            return price;
        }

        internal static bool Invoke(ApplicationEngine engine, uint method)
        {
            if (!methods.TryGetValue(method, out Func<ApplicationEngine, bool> func))
                return false;
            return func(engine);
        }

        private static uint Register(string method, Func<ApplicationEngine, bool> handler)
        {
            uint hash = method.ToInteropMethodHash();
            methods.Add(hash, handler);
            return hash;
        }

        private static uint Register(string method, Func<ApplicationEngine, bool> handler, long price)
        {
            uint hash = Register(method, handler);
            prices.Add(hash, price);
            return hash;
        }

        private static bool ExecutionEngine_GetScriptContainer(ApplicationEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(engine.ScriptContainer));
            return true;
        }

        private static bool ExecutionEngine_GetExecutingScriptHash(ApplicationEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(engine.CurrentScriptHash.ToArray());
            return true;
        }

        private static bool ExecutionEngine_GetCallingScriptHash(ApplicationEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(engine.CallingScriptHash?.ToArray() ?? new byte[0]);
            return true;
        }

        private static bool ExecutionEngine_GetEntryScriptHash(ApplicationEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(engine.EntryScriptHash.ToArray());
            return true;
        }

        private static bool Runtime_Platform(ApplicationEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(Encoding.ASCII.GetBytes("NEO"));
            return true;
        }

        private static bool Runtime_GetTrigger(ApplicationEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push((int)engine.Trigger);
            return true;
        }

        internal static bool CheckWitness(ApplicationEngine engine, UInt160 hash)
        {
            UInt160[] _hashes_for_verifying = engine.ScriptContainer.GetScriptHashesForVerifying(engine.Snapshot);
            return _hashes_for_verifying.Contains(hash);
        }

        private static bool CheckWitness(ApplicationEngine engine, ECPoint pubkey)
        {
            return CheckWitness(engine, Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
        }

        private static bool Runtime_CheckWitness(ApplicationEngine engine)
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

        private static bool Runtime_Notify(ApplicationEngine engine)
        {
            engine.SendNotification(engine.CurrentScriptHash, engine.CurrentContext.EvaluationStack.Pop());
            return true;
        }

        private static bool Runtime_Log(ApplicationEngine engine)
        {
            string message = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            engine.SendLog(engine.CurrentScriptHash, message);
            return true;
        }

        private static bool Runtime_GetTime(ApplicationEngine engine)
        {
            if (engine.Snapshot.PersistingBlock == null)
            {
                Header header = engine.Snapshot.GetHeader(engine.Snapshot.CurrentBlockHash);
                engine.CurrentContext.EvaluationStack.Push(header.Timestamp + Blockchain.SecondsPerBlock);
            }
            else
            {
                engine.CurrentContext.EvaluationStack.Push(engine.Snapshot.PersistingBlock.Timestamp);
            }
            return true;
        }

        private static bool Runtime_Serialize(ApplicationEngine engine)
        {
            byte[] serialized;
            try
            {
                serialized = engine.CurrentContext.EvaluationStack.Pop().Serialize();
            }
            catch (NotSupportedException)
            {
                return false;
            }
            if (serialized.Length > engine.MaxItemSize)
                return false;
            engine.CurrentContext.EvaluationStack.Push(serialized);
            return true;
        }

        private static bool Runtime_Deserialize(ApplicationEngine engine)
        {
            StackItem item;
            try
            {
                item = engine.CurrentContext.EvaluationStack.Pop().GetByteArray().DeserializeStackItem(engine.MaxArraySize);
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
            return true;
        }

        private static bool Crypto_Verify(ApplicationEngine engine)
        {
            StackItem item0 = engine.CurrentContext.EvaluationStack.Pop();
            byte[] message;
            if (item0 is InteropInterface _interface)
                message = _interface.GetInterface<IVerifiable>().GetHashData();
            else
                message = item0.GetByteArray();
            byte[] pubkey = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (pubkey[0] != 2 && pubkey[0] != 3 && pubkey[0] != 4)
                return false;
            byte[] signature = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            bool result = Crypto.Default.VerifySignature(message, signature, pubkey);
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        private static bool Blockchain_GetHeight(ApplicationEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(engine.Snapshot.Height);
            return true;
        }

        private static bool Blockchain_GetHeader(ApplicationEngine engine)
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
                Header header = engine.Snapshot.GetHeader(hash);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(header));
            }
            return true;
        }

        private static bool Blockchain_GetBlock(ApplicationEngine engine)
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
                Block block = engine.Snapshot.GetBlock(hash);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(block));
            }
            return true;
        }

        private static bool Blockchain_GetTransaction(ApplicationEngine engine)
        {
            byte[] hash = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            Transaction tx = engine.Snapshot.GetTransaction(new UInt256(hash));
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        private static bool Blockchain_GetTransactionHeight(ApplicationEngine engine)
        {
            byte[] hash = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            int? height = (int?)engine.Snapshot.Transactions.TryGet(new UInt256(hash))?.BlockIndex;
            engine.CurrentContext.EvaluationStack.Push(height ?? -1);
            return true;
        }

        private static bool Blockchain_GetContract(ApplicationEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            if (contract == null)
                engine.CurrentContext.EvaluationStack.Push(new byte[0]);
            else
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private static bool Header_GetIndex(ApplicationEngine engine)
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

        private static bool Header_GetHash(ApplicationEngine engine)
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

        private static bool Header_GetPrevHash(ApplicationEngine engine)
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

        private static bool Header_GetTimestamp(ApplicationEngine engine)
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

        private static bool Block_GetTransactionCount(ApplicationEngine engine)
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

        private static bool Block_GetTransactions(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Block block = _interface.GetInterface<Block>();
                if (block == null) return false;
                if (block.Transactions.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(block.Transactions.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private static bool Block_GetTransaction(ApplicationEngine engine)
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

        private static bool Transaction_GetHash(ApplicationEngine engine)
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

        private static bool Storage_Get(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(engine, context)) return false;
                byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                StorageItem item = engine.Snapshot.Storages.TryGet(new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                });
                engine.CurrentContext.EvaluationStack.Push(item?.Value ?? new byte[0]);
                return true;
            }
            return false;
        }

        private static bool StorageContext_AsReadOnly(ApplicationEngine engine)
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

        private static bool Contract_Call(ApplicationEngine engine)
        {
            StackItem item0 = engine.CurrentContext.EvaluationStack.Pop();
            ContractState contract;
            if (item0 is InteropInterface<ContractState> _interface)
                contract = _interface;
            else
                contract = engine.Snapshot.Contracts.TryGet(new UInt160(item0.GetByteArray()));
            if (contract is null) return false;
            StackItem item1 = engine.CurrentContext.EvaluationStack.Pop();
            StackItem item2 = engine.CurrentContext.EvaluationStack.Pop();
            ExecutionContext context_new = engine.LoadScript(contract.Script, 1);
            context_new.EvaluationStack.Push(item2);
            context_new.EvaluationStack.Push(item1);
            return true;
        }

        private static bool Contract_Destroy(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            UInt160 hash = engine.CurrentScriptHash;
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            if (contract == null) return true;
            engine.Snapshot.Contracts.Delete(hash);
            if (contract.HasStorage)
                foreach (var pair in engine.Snapshot.Storages.Find(hash.ToArray()))
                    engine.Snapshot.Storages.Delete(pair.Key);
            return true;
        }

        private static bool PutEx(ApplicationEngine engine, StorageContext context, byte[] key, byte[] value, StorageFlags flags)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (key.Length > 1024) return false;
            if (context.IsReadOnly) return false;
            if (!CheckStorageContext(engine, context)) return false;
            StorageKey skey = new StorageKey
            {
                ScriptHash = context.ScriptHash,
                Key = key
            };
            StorageItem item = engine.Snapshot.Storages.GetAndChange(skey, () => new StorageItem());
            if (item.IsConstant) return false;
            item.Value = value;
            item.IsConstant = flags.HasFlag(StorageFlags.Constant);
            return true;
        }

        private static bool Storage_Put(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface))
                return false;
            StorageContext context = _interface.GetInterface<StorageContext>();
            byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            byte[] value = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            return PutEx(engine, context, key, value, StorageFlags.None);
        }

        private static bool Storage_PutEx(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface))
                return false;
            StorageContext context = _interface.GetInterface<StorageContext>();
            byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            byte[] value = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            StorageFlags flags = (StorageFlags)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            return PutEx(engine, context, key, value, flags);
        }

        private static bool Storage_Delete(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (context.IsReadOnly) return false;
                if (!CheckStorageContext(engine, context)) return false;
                StorageKey key = new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = engine.CurrentContext.EvaluationStack.Pop().GetByteArray()
                };
                if (engine.Snapshot.Storages.TryGet(key)?.IsConstant == true) return false;
                engine.Snapshot.Storages.Delete(key);
                return true;
            }
            return false;
        }
    }
}
