using Neo.Cryptography;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    static partial class InteropService
    {
        public static readonly uint Neo_Native_Deploy = Register("Neo.Native.Deploy", Native_Deploy, 0, TriggerType.Application);
        public static readonly uint Neo_Crypto_CheckSig = Register("Neo.Crypto.CheckSig", Crypto_CheckSig, 0_01000000, TriggerType.All);
        public static readonly uint Neo_Crypto_CheckMultiSig = Register("Neo.Crypto.CheckMultiSig", Crypto_CheckMultiSig, GetCheckMultiSigPrice, TriggerType.All);
        public static readonly uint Neo_Header_GetVersion = Register("Neo.Header.GetVersion", Header_GetVersion, 0_00000400, TriggerType.Application);
        public static readonly uint Neo_Header_GetMerkleRoot = Register("Neo.Header.GetMerkleRoot", Header_GetMerkleRoot, 0_00000400, TriggerType.Application);
        public static readonly uint Neo_Header_GetNextConsensus = Register("Neo.Header.GetNextConsensus", Header_GetNextConsensus, 0_00000400, TriggerType.Application);
        public static readonly uint Neo_Transaction_GetScript = Register("Neo.Transaction.GetScript", Transaction_GetScript, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Transaction_GetWitnesses = Register("Neo.Transaction.GetWitnesses", Transaction_GetWitnesses, 0_00010000, TriggerType.All);
        public static readonly uint Neo_Witness_GetVerificationScript = Register("Neo.Witness.GetVerificationScript", Witness_GetVerificationScript, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Account_IsStandard = Register("Neo.Account.IsStandard", Account_IsStandard, 0_00030000, TriggerType.All);
        public static readonly uint Neo_Contract_Create = Register("Neo.Contract.Create", Contract_Create, GetDeploymentPrice, TriggerType.Application);
        public static readonly uint Neo_Contract_Update = Register("Neo.Contract.Update", Contract_Update, GetDeploymentPrice, TriggerType.Application);
        public static readonly uint Neo_Contract_GetScript = Register("Neo.Contract.GetScript", Contract_GetScript, 0_00000400, TriggerType.Application);
        public static readonly uint Neo_Contract_IsPayable = Register("Neo.Contract.IsPayable", Contract_IsPayable, 0_00000400, TriggerType.Application);
        public static readonly uint Neo_Storage_Find = Register("Neo.Storage.Find", Storage_Find, 0_01000000, TriggerType.Application);
        public static readonly uint Neo_Enumerator_Create = Register("Neo.Enumerator.Create", Enumerator_Create, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Enumerator_Next = Register("Neo.Enumerator.Next", Enumerator_Next, 0_01000000, TriggerType.All);
        public static readonly uint Neo_Enumerator_Value = Register("Neo.Enumerator.Value", Enumerator_Value, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Enumerator_Concat = Register("Neo.Enumerator.Concat", Enumerator_Concat, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Create = Register("Neo.Iterator.Create", Iterator_Create, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Key = Register("Neo.Iterator.Key", Iterator_Key, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Keys = Register("Neo.Iterator.Keys", Iterator_Keys, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Values = Register("Neo.Iterator.Values", Iterator_Values, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Concat = Register("Neo.Iterator.Concat", Iterator_Concat, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Json_Serialize = Register("Neo.Json.Serialize", Json_Serialize, 0_00100000, TriggerType.All);
        public static readonly uint Neo_Json_Deserialize = Register("Neo.Json.Deserialize", Json_Deserialize, 0_00500000, TriggerType.All);

        static InteropService()
        {
            foreach (NativeContract contract in NativeContract.Contracts)
                Register(contract.ServiceName, contract.Invoke, contract.GetPrice, TriggerType.System | TriggerType.Application);
        }

        private static long GetCheckMultiSigPrice(RandomAccessStack<StackItem> stack)
        {
            if (stack.Count == 0) return 0;
            var item = stack.Peek();
            int n;
            if (item is VMArray array) n = array.Count;
            else n = (int)item.GetBigInteger();
            if (n < 1) return 0;
            return GetPrice(Neo_Crypto_CheckSig, stack) * n;
        }

        private static long GetDeploymentPrice(RandomAccessStack<StackItem> stack)
        {
            int size = stack.Peek(0).GetByteLength() + stack.Peek(1).GetByteLength();
            return GasPerByte * size;
        }

        private static bool Native_Deploy(ApplicationEngine engine)
        {
            if (engine.Snapshot.PersistingBlock.Index != 0) return false;
            foreach (NativeContract contract in NativeContract.Contracts)
            {
                engine.Snapshot.Contracts.Add(contract.Hash, new ContractState
                {
                    Script = contract.Script,
                    Manifest = contract.Manifest
                });
                contract.Initialize(engine);
            }
            return true;
        }

        private static bool Crypto_CheckSig(ApplicationEngine engine)
        {
            byte[] pubkey = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            byte[] signature = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();

            try
            {
                engine.CurrentContext.EvaluationStack.Push(Crypto.Default.VerifySignature(engine.ScriptContainer.GetHashData(), signature, pubkey));
            }
            catch (ArgumentException)
            {
                engine.CurrentContext.EvaluationStack.Push(false);
            }
            return true;
        }

        private static bool Crypto_CheckMultiSig(ApplicationEngine engine)
        {
            int n;
            byte[][] pubkeys;
            StackItem item = engine.CurrentContext.EvaluationStack.Pop();

            if (item is VMArray array1)
            {
                pubkeys = array1.Select(p => p.GetByteArray()).ToArray();
                n = pubkeys.Length;
                if (n == 0) return false;
            }
            else
            {
                n = (int)item.GetBigInteger();
                if (n < 1 || n > engine.CurrentContext.EvaluationStack.Count) return false;
                pubkeys = new byte[n][];
                for (int i = 0; i < n; i++)
                    pubkeys[i] = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            }

            int m;
            byte[][] signatures;
            item = engine.CurrentContext.EvaluationStack.Pop();
            if (item is VMArray array2)
            {
                signatures = array2.Select(p => p.GetByteArray()).ToArray();
                m = signatures.Length;
                if (m == 0 || m > n) return false;
            }
            else
            {
                m = (int)item.GetBigInteger();
                if (m < 1 || m > n || m > engine.CurrentContext.EvaluationStack.Count) return false;
                signatures = new byte[m][];
                for (int i = 0; i < m; i++)
                    signatures[i] = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            }
            byte[] message = engine.ScriptContainer.GetHashData();
            bool fSuccess = true;
            try
            {
                for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                {
                    if (Crypto.Default.VerifySignature(message, signatures[i], pubkeys[j]))
                        i++;
                    j++;
                    if (m - i > n - j)
                        fSuccess = false;
                }
            }
            catch (ArgumentException)
            {
                fSuccess = false;
            }
            engine.CurrentContext.EvaluationStack.Push(fSuccess);
            return true;
        }

        private static bool Header_GetVersion(ApplicationEngine engine)
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

        private static bool Header_GetMerkleRoot(ApplicationEngine engine)
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

        private static bool Header_GetNextConsensus(ApplicationEngine engine)
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

        private static bool Transaction_GetScript(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Script);
                return true;
            }
            return false;
        }

        private static bool Transaction_GetWitnesses(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Witnesses.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(WitnessWrapper.Create(tx, engine.Snapshot).Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private static bool Witness_GetVerificationScript(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                WitnessWrapper witness = _interface.GetInterface<WitnessWrapper>();
                if (witness == null) return false;
                engine.CurrentContext.EvaluationStack.Push(witness.VerificationScript);
                return true;
            }
            return false;
        }

        private static bool Account_IsStandard(ApplicationEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            bool isStandard = contract is null || contract.Script.IsStandardContract();
            engine.CurrentContext.EvaluationStack.Push(isStandard);
            return true;
        }

        private static bool Contract_Create(ApplicationEngine engine)
        {
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;

            var manifest = engine.CurrentContext.EvaluationStack.Pop().GetString();
            if (manifest.Length > ContractManifest.MaxLength) return false;

            UInt160 hash = script.ToScriptHash();
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            if (contract != null) return false;
            contract = new ContractState
            {
                Script = script,
                Manifest = ContractManifest.Parse(manifest)
            };

            if (!contract.Manifest.IsValid(hash)) return false;

            engine.Snapshot.Contracts.Add(hash, contract);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private static bool Contract_Update(ApplicationEngine engine)
        {
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            var manifest = engine.CurrentContext.EvaluationStack.Pop().GetString();
            if (manifest.Length > ContractManifest.MaxLength) return false;

            var contract = engine.Snapshot.Contracts.TryGet(engine.CurrentScriptHash);
            if (contract is null) return false;

            if (script.Length > 0)
            {
                UInt160 hash_new = script.ToScriptHash();
                if (hash_new.Equals(engine.CurrentScriptHash)) return false;
                if (engine.Snapshot.Contracts.TryGet(hash_new) != null) return false;
                contract = new ContractState
                {
                    Script = script,
                    Manifest = contract.Manifest
                };
                contract.Manifest.Abi.Hash = hash_new;
                engine.Snapshot.Contracts.Add(hash_new, contract);
                if (contract.HasStorage)
                {
                    foreach (var pair in engine.Snapshot.Storages.Find(engine.CurrentScriptHash.ToArray()).ToArray())
                    {
                        engine.Snapshot.Storages.Add(new StorageKey
                        {
                            ScriptHash = hash_new,
                            Key = pair.Key.Key
                        }, new StorageItem
                        {
                            Value = pair.Value.Value,
                            IsConstant = false
                        });
                    }
                }
                Contract_Destroy(engine);
            }
            if (manifest.Length > 0)
            {
                contract = engine.Snapshot.Contracts.GetAndChange(contract.ScriptHash);
                contract.Manifest = ContractManifest.Parse(manifest);
                if (!contract.Manifest.IsValid(contract.ScriptHash)) return false;
            }

            return true;
        }

        private static bool Contract_GetScript(ApplicationEngine engine)
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

        private static bool Contract_IsPayable(ApplicationEngine engine)
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

        private static bool Storage_Find(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(engine, context)) return false;
                byte[] prefix = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                byte[] prefix_key = StorageKey.CreateSearchPrefix(context.ScriptHash, prefix);
                StorageIterator iterator = engine.AddDisposable(new StorageIterator(engine.Snapshot.Storages.Find(prefix_key).Where(p => p.Key.Key.Take(prefix.Length).SequenceEqual(prefix)).GetEnumerator()));
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                return true;
            }
            return false;
        }

        private static bool Enumerator_Create(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is VMArray array)
            {
                IEnumerator enumerator = new ArrayWrapper(array);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(enumerator));
                return true;
            }
            return false;
        }

        private static bool Enumerator_Next(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Next());
                return true;
            }
            return false;
        }

        private static bool Enumerator_Value(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Value());
                return true;
            }
            return false;
        }

        private static bool Enumerator_Concat(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IEnumerator first = _interface1.GetInterface<IEnumerator>();
            IEnumerator second = _interface2.GetInterface<IEnumerator>();
            IEnumerator result = new ConcatenatedEnumerator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

        private static bool Iterator_Create(ApplicationEngine engine)
        {
            IIterator iterator;
            switch (engine.CurrentContext.EvaluationStack.Pop())
            {
                case VMArray array:
                    iterator = new ArrayWrapper(array);
                    break;
                case Map map:
                    iterator = new MapWrapper(map);
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
            return true;
        }

        private static bool Iterator_Key(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(iterator.Key());
                return true;
            }
            return false;
        }

        private static bool Iterator_Keys(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorKeysWrapper(iterator)));
                return true;
            }
            return false;
        }

        private static bool Iterator_Values(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorValuesWrapper(iterator)));
                return true;
            }
            return false;
        }

        private static bool Iterator_Concat(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IIterator first = _interface1.GetInterface<IIterator>();
            IIterator second = _interface2.GetInterface<IIterator>();
            IIterator result = new ConcatenatedIterator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

        private static bool Json_Deserialize(ApplicationEngine engine)
        {
            var json = engine.CurrentContext.EvaluationStack.Pop().GetString();
            var obj = JObject.Parse(json, 10);
            var item = JsonSerializer.Deserialize(obj);

            engine.CurrentContext.EvaluationStack.Push(item);
            return true;
        }

        private static bool Json_Serialize(ApplicationEngine engine)
        {
            var item = engine.CurrentContext.EvaluationStack.Pop();
            var json = JsonSerializer.Serialize(item);

            engine.CurrentContext.EvaluationStack.Push(json.ToString());
            return true;
        }
    }
}
