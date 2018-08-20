using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
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
    public class StateReader : InteropService, IDisposable
    {
        public static event EventHandler<NotifyEventArgs> Notify;
        public static event EventHandler<LogEventArgs> Log;

        private readonly List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public IReadOnlyList<NotifyEventArgs> Notifications => notifications;

        private DataCache<UInt160, AccountState> _accounts;
        protected virtual DataCache<UInt160, AccountState> Accounts
        {
            get
            {
                if (_accounts == null)
                    _accounts = Blockchain.Default.GetStates<UInt160, AccountState>();
                return _accounts;
            }
        }

        private DataCache<UInt256, AssetState> _assets;
        protected virtual DataCache<UInt256, AssetState> Assets
        {
            get
            {
                if (_assets == null)
                    _assets = Blockchain.Default.GetStates<UInt256, AssetState>();
                return _assets;
            }
        }

        private DataCache<UInt160, ContractState> _contracts;
        protected virtual DataCache<UInt160, ContractState> Contracts
        {
            get
            {
                if (_contracts == null)
                    _contracts = Blockchain.Default.GetStates<UInt160, ContractState>();
                return _contracts;
            }
        }

        private DataCache<StorageKey, StorageItem> _storages;
        protected virtual DataCache<StorageKey, StorageItem> Storages
        {
            get
            {
                if (_storages == null)
                    _storages = Blockchain.Default.GetStates<StorageKey, StorageItem>();
                return _storages;
            }
        }

        public StateReader()
        {
            //Standard Library
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
            Register("System.Storage.GetContext", Storage_GetContext);
            Register("System.Storage.GetReadOnlyContext", Storage_GetReadOnlyContext);
            Register("System.Storage.Get", Storage_Get);
            Register("System.StorageContext.AsReadOnly", StorageContext_AsReadOnly);

            //Neo Specified
            Register("Neo.Blockchain.GetAccount", Blockchain_GetAccount);
            Register("Neo.Blockchain.GetValidators", Blockchain_GetValidators);
            Register("Neo.Blockchain.GetAsset", Blockchain_GetAsset);
            Register("Neo.Header.GetVersion", Header_GetVersion);
            Register("Neo.Header.GetMerkleRoot", Header_GetMerkleRoot);
            Register("Neo.Header.GetConsensusData", Header_GetConsensusData);
            Register("Neo.Header.GetNextConsensus", Header_GetNextConsensus);
            Register("Neo.Transaction.GetType", Transaction_GetType);
            Register("Neo.Transaction.GetAttributes", Transaction_GetAttributes);
            Register("Neo.Transaction.GetInputs", Transaction_GetInputs);
            Register("Neo.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("Neo.Transaction.GetReferences", Transaction_GetReferences);
            Register("Neo.Transaction.GetUnspentCoins", Transaction_GetUnspentCoins);
            Register("Neo.InvocationTransaction.GetScript", InvocationTransaction_GetScript);
            Register("Neo.Attribute.GetUsage", Attribute_GetUsage);
            Register("Neo.Attribute.GetData", Attribute_GetData);
            Register("Neo.Input.GetHash", Input_GetHash);
            Register("Neo.Input.GetIndex", Input_GetIndex);
            Register("Neo.Output.GetAssetId", Output_GetAssetId);
            Register("Neo.Output.GetValue", Output_GetValue);
            Register("Neo.Output.GetScriptHash", Output_GetScriptHash);
            Register("Neo.Account.GetScriptHash", Account_GetScriptHash);
            Register("Neo.Account.GetVotes", Account_GetVotes);
            Register("Neo.Account.GetBalance", Account_GetBalance);
            Register("Neo.Asset.GetAssetId", Asset_GetAssetId);
            Register("Neo.Asset.GetAssetType", Asset_GetAssetType);
            Register("Neo.Asset.GetAmount", Asset_GetAmount);
            Register("Neo.Asset.GetAvailable", Asset_GetAvailable);
            Register("Neo.Asset.GetPrecision", Asset_GetPrecision);
            Register("Neo.Asset.GetOwner", Asset_GetOwner);
            Register("Neo.Asset.GetAdmin", Asset_GetAdmin);
            Register("Neo.Asset.GetIssuer", Asset_GetIssuer);
            Register("Neo.Contract.GetScript", Contract_GetScript);
            Register("Neo.Contract.IsPayable", Contract_IsPayable);
            Register("Neo.Storage.Find", Storage_Find);
            Register("Neo.Enumerator.Create", Enumerator_Create);
            Register("Neo.Enumerator.Next", Enumerator_Next);
            Register("Neo.Enumerator.Value", Enumerator_Value);
            Register("Neo.Enumerator.Concat", Enumerator_Concat);
            Register("Neo.Iterator.Create", Iterator_Create);
            Register("Neo.Iterator.Key", Iterator_Key);
            Register("Neo.Iterator.Keys", Iterator_Keys);
            Register("Neo.Iterator.Values", Iterator_Values);

            #region Aliases
            Register("Neo.Iterator.Next", Enumerator_Next);
            Register("Neo.Iterator.Value", Enumerator_Value);
            #endregion

            #region Old APIs
            Register("Neo.Runtime.GetTrigger", Runtime_GetTrigger);
            Register("Neo.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("AntShares.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("Neo.Runtime.Notify", Runtime_Notify);
            Register("AntShares.Runtime.Notify", Runtime_Notify);
            Register("Neo.Runtime.Log", Runtime_Log);
            Register("AntShares.Runtime.Log", Runtime_Log);
            Register("Neo.Runtime.GetTime", Runtime_GetTime);
            Register("Neo.Runtime.Serialize", Runtime_Serialize);
            Register("Neo.Runtime.Deserialize", Runtime_Deserialize);
            Register("Neo.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("AntShares.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("Neo.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("AntShares.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("Neo.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("AntShares.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("Neo.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("AntShares.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("Neo.Blockchain.GetTransactionHeight", Blockchain_GetTransactionHeight);
            Register("AntShares.Blockchain.GetAccount", Blockchain_GetAccount);
            Register("AntShares.Blockchain.GetValidators", Blockchain_GetValidators);
            Register("AntShares.Blockchain.GetAsset", Blockchain_GetAsset);
            Register("Neo.Blockchain.GetContract", Blockchain_GetContract);
            Register("AntShares.Blockchain.GetContract", Blockchain_GetContract);
            Register("Neo.Header.GetIndex", Header_GetIndex);
            Register("Neo.Header.GetHash", Header_GetHash);
            Register("AntShares.Header.GetHash", Header_GetHash);
            Register("AntShares.Header.GetVersion", Header_GetVersion);
            Register("Neo.Header.GetPrevHash", Header_GetPrevHash);
            Register("AntShares.Header.GetPrevHash", Header_GetPrevHash);
            Register("AntShares.Header.GetMerkleRoot", Header_GetMerkleRoot);
            Register("Neo.Header.GetTimestamp", Header_GetTimestamp);
            Register("AntShares.Header.GetTimestamp", Header_GetTimestamp);
            Register("AntShares.Header.GetConsensusData", Header_GetConsensusData);
            Register("AntShares.Header.GetNextConsensus", Header_GetNextConsensus);
            Register("Neo.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("AntShares.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("Neo.Block.GetTransactions", Block_GetTransactions);
            Register("AntShares.Block.GetTransactions", Block_GetTransactions);
            Register("Neo.Block.GetTransaction", Block_GetTransaction);
            Register("AntShares.Block.GetTransaction", Block_GetTransaction);
            Register("Neo.Transaction.GetHash", Transaction_GetHash);
            Register("AntShares.Transaction.GetHash", Transaction_GetHash);
            Register("AntShares.Transaction.GetType", Transaction_GetType);
            Register("AntShares.Transaction.GetAttributes", Transaction_GetAttributes);
            Register("AntShares.Transaction.GetInputs", Transaction_GetInputs);
            Register("AntShares.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("AntShares.Transaction.GetReferences", Transaction_GetReferences);
            Register("AntShares.Attribute.GetUsage", Attribute_GetUsage);
            Register("AntShares.Attribute.GetData", Attribute_GetData);
            Register("AntShares.Input.GetHash", Input_GetHash);
            Register("AntShares.Input.GetIndex", Input_GetIndex);
            Register("AntShares.Output.GetAssetId", Output_GetAssetId);
            Register("AntShares.Output.GetValue", Output_GetValue);
            Register("AntShares.Output.GetScriptHash", Output_GetScriptHash);
            Register("AntShares.Account.GetScriptHash", Account_GetScriptHash);
            Register("AntShares.Account.GetVotes", Account_GetVotes);
            Register("AntShares.Account.GetBalance", Account_GetBalance);
            Register("AntShares.Asset.GetAssetId", Asset_GetAssetId);
            Register("AntShares.Asset.GetAssetType", Asset_GetAssetType);
            Register("AntShares.Asset.GetAmount", Asset_GetAmount);
            Register("AntShares.Asset.GetAvailable", Asset_GetAvailable);
            Register("AntShares.Asset.GetPrecision", Asset_GetPrecision);
            Register("AntShares.Asset.GetOwner", Asset_GetOwner);
            Register("AntShares.Asset.GetAdmin", Asset_GetAdmin);
            Register("AntShares.Asset.GetIssuer", Asset_GetIssuer);
            Register("AntShares.Contract.GetScript", Contract_GetScript);
            Register("Neo.Storage.GetContext", Storage_GetContext);
            Register("AntShares.Storage.GetContext", Storage_GetContext);
            Register("Neo.Storage.GetReadOnlyContext", Storage_GetReadOnlyContext);
            Register("Neo.Storage.Get", Storage_Get);
            Register("AntShares.Storage.Get", Storage_Get);
            Register("Neo.StorageContext.AsReadOnly", StorageContext_AsReadOnly);
            #endregion
        }

        internal bool CheckStorageContext(StorageContext context)
        {
            ContractState contract = Contracts.TryGet(context.ScriptHash);
            if (contract == null) return false;
            if (!contract.HasStorage) return false;
            return true;
        }

        public void Dispose()
        {
            foreach (IDisposable disposable in disposables)
                disposable.Dispose();
            disposables.Clear();
        }

        protected virtual bool Runtime_GetTrigger(ExecutionEngine engine)
        {
            ApplicationEngine app_engine = (ApplicationEngine)engine;
            engine.CurrentContext.EvaluationStack.Push((int)app_engine.Trigger);
            return true;
        }

        protected bool CheckWitness(ExecutionEngine engine, UInt160 hash)
        {
            IVerifiable container = (IVerifiable)engine.ScriptContainer;
            UInt160[] _hashes_for_verifying = container.GetScriptHashesForVerifying();
            return _hashes_for_verifying.Contains(hash);
        }

        protected bool CheckWitness(ExecutionEngine engine, ECPoint pubkey)
        {
            return CheckWitness(engine, Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
        }

        protected virtual bool Runtime_CheckWitness(ExecutionEngine engine)
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

        protected virtual bool Runtime_Notify(ExecutionEngine engine)
        {
            StackItem state = engine.CurrentContext.EvaluationStack.Pop();
            NotifyEventArgs notification = new NotifyEventArgs(engine.ScriptContainer, new UInt160(engine.CurrentContext.ScriptHash), state);
            Notify?.Invoke(this, notification);
            notifications.Add(notification);
            return true;
        }

        protected virtual bool Runtime_Log(ExecutionEngine engine)
        {
            string message = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            Log?.Invoke(this, new LogEventArgs(engine.ScriptContainer, new UInt160(engine.CurrentContext.ScriptHash), message));
            return true;
        }

        protected virtual bool Runtime_GetTime(ExecutionEngine engine)
        {
            BlockBase header = Blockchain.Default?.GetHeader(Blockchain.Default.Height);
            if (header == null) header = Blockchain.GenesisBlock;
            engine.CurrentContext.EvaluationStack.Push(header.Timestamp + Blockchain.SecondsPerBlock);
            return true;
        }

        private void SerializeStackItem(StackItem item, BinaryWriter writer)
        {
            List<StackItem> serialized = new List<StackItem>();
            Stack<StackItem> unserialized = new Stack<StackItem>();
            unserialized.Push(item);
            while (unserialized.Count > 0)
            {
                item = unserialized.Pop();
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
                        if (serialized.Any(p => ReferenceEquals(p, array)))
                            throw new NotSupportedException();
                        serialized.Add(array);
                        if (array is Struct)
                            writer.Write((byte)StackItemType.Struct);
                        else
                            writer.Write((byte)StackItemType.Array);
                        writer.WriteVarInt(array.Count);
                        for (int i = array.Count - 1; i >= 0; i--)
                            unserialized.Push(array[i]);
                        break;
                    case Map map:
                        if (serialized.Any(p => ReferenceEquals(p, map)))
                            throw new NotSupportedException();
                        serialized.Add(map);
                        writer.Write((byte)StackItemType.Map);
                        writer.WriteVarInt(map.Count);
                        foreach (var pair in map.Reverse())
                        {
                            unserialized.Push(pair.Value);
                            unserialized.Push(pair.Key);
                        }
                        break;
                }
            }
        }

        protected virtual bool Runtime_Serialize(ExecutionEngine engine)
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
            Stack<StackItem> deserialized = new Stack<StackItem>();
            int undeserialized = 1;
            while (undeserialized-- > 0)
            {
                StackItemType type = (StackItemType)reader.ReadByte();
                switch (type)
                {
                    case StackItemType.ByteArray:
                        deserialized.Push(new ByteArray(reader.ReadVarBytes()));
                        break;
                    case StackItemType.Boolean:
                        deserialized.Push(new VMBoolean(reader.ReadBoolean()));
                        break;
                    case StackItemType.Integer:
                        deserialized.Push(new Integer(new BigInteger(reader.ReadVarBytes())));
                        break;
                    case StackItemType.Array:
                    case StackItemType.Struct:
                        {
                            int count = (int)reader.ReadVarInt(ApplicationEngine.MaxArraySize);
                            deserialized.Push(new ContainerPlaceholder
                            {
                                Type = type,
                                ElementCount = count
                            });
                            undeserialized += count;
                        }
                        break;
                    case StackItemType.Map:
                        {
                            int count = (int)reader.ReadVarInt(ApplicationEngine.MaxArraySize);
                            deserialized.Push(new ContainerPlaceholder
                            {
                                Type = type,
                                ElementCount = count
                            });
                            undeserialized += count * 2;
                        }
                        break;
                    default:
                        throw new FormatException();
                }
            }
            Stack<StackItem> stack_temp = new Stack<StackItem>();
            while (deserialized.Count > 0)
            {
                StackItem item = deserialized.Pop();
                if (item is ContainerPlaceholder placeholder)
                {
                    switch (placeholder.Type)
                    {
                        case StackItemType.Array:
                            VMArray array = new VMArray();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                                array.Add(stack_temp.Pop());
                            item = array;
                            break;
                        case StackItemType.Struct:
                            Struct @struct = new Struct();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                                @struct.Add(stack_temp.Pop());
                            item = @struct;
                            break;
                        case StackItemType.Map:
                            Map map = new Map();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                            {
                                StackItem key = stack_temp.Pop();
                                StackItem value = stack_temp.Pop();
                                map.Add(key, value);
                            }
                            item = map;
                            break;
                    }
                }
                stack_temp.Push(item);
            }
            return stack_temp.Peek();
        }

        protected virtual bool Runtime_Deserialize(ExecutionEngine engine)
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

        protected virtual bool Blockchain_GetHeight(ExecutionEngine engine)
        {
            if (Blockchain.Default == null)
                engine.CurrentContext.EvaluationStack.Push(0);
            else
                engine.CurrentContext.EvaluationStack.Push(Blockchain.Default.Height);
            return true;
        }

        protected virtual bool Blockchain_GetHeader(ExecutionEngine engine)
        {
            byte[] data = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            Header header;
            if (data.Length <= 5)
            {
                uint height = (uint)new BigInteger(data);
                if (Blockchain.Default != null)
                    header = Blockchain.Default.GetHeader(height);
                else if (height == 0)
                    header = Blockchain.GenesisBlock.Header;
                else
                    header = null;
            }
            else if (data.Length == 32)
            {
                UInt256 hash = new UInt256(data);
                if (Blockchain.Default != null)
                    header = Blockchain.Default.GetHeader(hash);
                else if (hash == Blockchain.GenesisBlock.Hash)
                    header = Blockchain.GenesisBlock.Header;
                else
                    header = null;
            }
            else
            {
                return false;
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(header));
            return true;
        }

        protected virtual bool Blockchain_GetBlock(ExecutionEngine engine)
        {
            byte[] data = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            Block block;
            if (data.Length <= 5)
            {
                uint height = (uint)new BigInteger(data);
                if (Blockchain.Default != null)
                    block = Blockchain.Default.GetBlock(height);
                else if (height == 0)
                    block = Blockchain.GenesisBlock;
                else
                    block = null;
            }
            else if (data.Length == 32)
            {
                UInt256 hash = new UInt256(data);
                if (Blockchain.Default != null)
                    block = Blockchain.Default.GetBlock(hash);
                else if (hash == Blockchain.GenesisBlock.Hash)
                    block = Blockchain.GenesisBlock;
                else
                    block = null;
            }
            else
            {
                return false;
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(block));
            return true;
        }

        protected virtual bool Blockchain_GetTransaction(ExecutionEngine engine)
        {
            byte[] hash = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            Transaction tx = Blockchain.Default?.GetTransaction(new UInt256(hash));
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        protected virtual bool Blockchain_GetTransactionHeight(ExecutionEngine engine)
        {
            byte[] hash = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            int height;
            if (Blockchain.Default == null)
                height = -1;
            else
                Blockchain.Default.GetTransaction(new UInt256(hash), out height);
            engine.CurrentContext.EvaluationStack.Push(height);
            return true;
        }

        protected virtual bool Blockchain_GetAccount(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            AccountState account = Accounts.GetOrAdd(hash, () => new AccountState(hash));
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(account));
            return true;
        }

        protected virtual bool Blockchain_GetValidators(ExecutionEngine engine)
        {
            ECPoint[] validators = Blockchain.Default.GetValidators();
            engine.CurrentContext.EvaluationStack.Push(validators.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
            return true;
        }

        protected virtual bool Blockchain_GetAsset(ExecutionEngine engine)
        {
            UInt256 hash = new UInt256(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            AssetState asset = Assets.TryGet(hash);
            if (asset == null) return false;
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        protected virtual bool Blockchain_GetContract(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            ContractState contract = Contracts.TryGet(hash);
            if (contract == null)
                engine.CurrentContext.EvaluationStack.Push(new byte[0]);
            else
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        protected virtual bool Header_GetIndex(ExecutionEngine engine)
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

        protected virtual bool Header_GetHash(ExecutionEngine engine)
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

        protected virtual bool Header_GetVersion(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.Version);
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetPrevHash(ExecutionEngine engine)
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

        protected virtual bool Header_GetMerkleRoot(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.MerkleRoot.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetTimestamp(ExecutionEngine engine)
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

        protected virtual bool Header_GetConsensusData(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.ConsensusData);
                return true;
            }
            return false;
        }

        protected virtual bool Header_GetNextConsensus(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.NextConsensus.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Block_GetTransactionCount(ExecutionEngine engine)
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

        protected virtual bool Block_GetTransactions(ExecutionEngine engine)
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

        protected virtual bool Block_GetTransaction(ExecutionEngine engine)
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

        protected virtual bool Transaction_GetHash(ExecutionEngine engine)
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

        protected virtual bool Transaction_GetType(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)tx.Type);
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetAttributes(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Attributes.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetInputs(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Outputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetReferences(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(tx.References[p])).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Transaction_GetUnspentCoins(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(Blockchain.Default.GetUnspent(tx.Hash).Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool InvocationTransaction_GetScript(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                InvocationTransaction tx = _interface.GetInterface<InvocationTransaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Script);
                return true;
            }
            return false;
        }

        protected virtual bool Attribute_GetUsage(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionAttribute attr = _interface.GetInterface<TransactionAttribute>();
                if (attr == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)attr.Usage);
                return true;
            }
            return false;
        }

        protected virtual bool Attribute_GetData(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionAttribute attr = _interface.GetInterface<TransactionAttribute>();
                if (attr == null) return false;
                engine.CurrentContext.EvaluationStack.Push(attr.Data);
                return true;
            }
            return false;
        }

        protected virtual bool Input_GetHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                CoinReference input = _interface.GetInterface<CoinReference>();
                if (input == null) return false;
                engine.CurrentContext.EvaluationStack.Push(input.PrevHash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Input_GetIndex(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                CoinReference input = _interface.GetInterface<CoinReference>();
                if (input == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)input.PrevIndex);
                return true;
            }
            return false;
        }

        protected virtual bool Output_GetAssetId(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.AssetId.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Output_GetValue(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.Value.GetData());
                return true;
            }
            return false;
        }

        protected virtual bool Output_GetScriptHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.ScriptHash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Account_GetScriptHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                if (account == null) return false;
                engine.CurrentContext.EvaluationStack.Push(account.ScriptHash.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Account_GetVotes(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                if (account == null) return false;
                engine.CurrentContext.EvaluationStack.Push(account.Votes.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Account_GetBalance(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                UInt256 asset_id = new UInt256(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
                if (account == null) return false;
                Fixed8 balance = account.Balances.TryGetValue(asset_id, out Fixed8 value) ? value : Fixed8.Zero;
                engine.CurrentContext.EvaluationStack.Push(balance.GetData());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAssetId(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.AssetId.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAssetType(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)asset.AssetType);
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAmount(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Amount.GetData());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAvailable(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Available.GetData());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetPrecision(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)asset.Precision);
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetOwner(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Owner.EncodePoint(true));
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetAdmin(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Admin.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Asset_GetIssuer(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Issuer.ToArray());
                return true;
            }
            return false;
        }

        protected virtual bool Contract_GetScript(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.CurrentContext.EvaluationStack.Push(contract.Script);
                return true;
            }
            return false;
        }

        protected virtual bool Contract_IsPayable(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.CurrentContext.EvaluationStack.Push(contract.Payable);
                return true;
            }
            return false;
        }

        protected virtual bool Storage_GetContext(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
            {
                ScriptHash = new UInt160(engine.CurrentContext.ScriptHash),
                IsReadOnly = false
            }));
            return true;
        }

        protected virtual bool Storage_GetReadOnlyContext(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
            {
                ScriptHash = new UInt160(engine.CurrentContext.ScriptHash),
                IsReadOnly = true
            }));
            return true;
        }

        protected virtual bool Storage_Get(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(context)) return false;
                byte[] key = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                StorageItem item = Storages.TryGet(new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                });
                engine.CurrentContext.EvaluationStack.Push(item?.Value ?? new byte[0]);
                return true;
            }
            return false;
        }

        protected virtual bool Storage_Find(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(context)) return false;
                byte[] prefix = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                byte[] prefix_key;
                using (MemoryStream ms = new MemoryStream())
                {
                    int index = 0;
                    int remain = prefix.Length;
                    while (remain >= 16)
                    {
                        ms.Write(prefix, index, 16);
                        ms.WriteByte(0);
                        index += 16;
                        remain -= 16;
                    }
                    if (remain > 0)
                        ms.Write(prefix, index, remain);
                    prefix_key = context.ScriptHash.ToArray().Concat(ms.ToArray()).ToArray();
                }
                StorageIterator iterator = new StorageIterator(Storages.Find(prefix_key).Where(p => p.Key.Key.Take(prefix.Length).SequenceEqual(prefix)).GetEnumerator());
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                disposables.Add(iterator);
                return true;
            }
            return false;
        }

        protected virtual bool StorageContext_AsReadOnly(ExecutionEngine engine)
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

        protected virtual bool Enumerator_Create(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is VMArray array)
            {
                IEnumerator enumerator = new ArrayWrapper(array);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(enumerator));
                return true;
            }
            return false;
        }

        protected virtual bool Enumerator_Next(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Next());
                return true;
            }
            return false;
        }

        protected virtual bool Enumerator_Value(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Value());
                return true;
            }
            return false;
        }

        protected virtual bool Enumerator_Concat(ExecutionEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IEnumerator first = _interface1.GetInterface<IEnumerator>();
            IEnumerator second = _interface2.GetInterface<IEnumerator>();
            IEnumerator result = new ConcatenatedEnumerator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

        protected virtual bool Iterator_Create(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is Map map)
            {
                IIterator iterator = new MapWrapper(map);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                return true;
            }
            return false;
        }

        protected virtual bool Iterator_Key(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(iterator.Key());
                return true;
            }
            return false;
        }

        protected virtual bool Iterator_Keys(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorKeysWrapper(iterator)));
                return true;
            }
            return false;
        }

        protected virtual bool Iterator_Values(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorValuesWrapper(iterator)));
                return true;
            }
            return false;
        }
    }
}
