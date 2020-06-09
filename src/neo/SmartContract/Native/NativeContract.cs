#pragma warning disable IDE0060

using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    public abstract class NativeContract
    {
        private static readonly List<NativeContract> contractsList = new List<NativeContract>();
        private static readonly Dictionary<string, NativeContract> contractsNameDictionary = new Dictionary<string, NativeContract>();
        private static readonly Dictionary<UInt160, NativeContract> contractsHashDictionary = new Dictionary<UInt160, NativeContract>();
        private readonly Dictionary<string, ContractMethodMetadata> methods = new Dictionary<string, ContractMethodMetadata>();

        public static IReadOnlyCollection<NativeContract> Contracts { get; } = contractsList;
        public static NeoToken NEO { get; } = new NeoToken();
        public static GasToken GAS { get; } = new GasToken();
        public static PolicyContract Policy { get; } = new PolicyContract();

        public abstract string Name { get; }
        public byte[] Script { get; }
        public UInt160 Hash { get; }
        public abstract int Id { get; }
        public ContractManifest Manifest { get; }
        public virtual string[] SupportedStandards { get; } = { "NEP-10" };

        protected NativeContract()
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(Name);
                sb.EmitSysCall(ApplicationEngine.Neo_Native_Call);
                this.Script = sb.ToArray();
            }
            this.Hash = Script.ToScriptHash();
            List<ContractMethodDescriptor> descriptors = new List<ContractMethodDescriptor>();
            List<string> safeMethods = new List<string>();
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                ContractMethodAttribute attribute = method.GetCustomAttribute<ContractMethodAttribute>();
                if (attribute is null) continue;
                string name = attribute.Name ?? (method.Name.ToLower()[0] + method.Name.Substring(1));
                descriptors.Add(new ContractMethodDescriptor
                {
                    Name = name,
                    ReturnType = attribute.ReturnType,
                    Parameters = attribute.ParameterTypes.Zip(attribute.ParameterNames, (t, n) => new ContractParameterDefinition { Type = t, Name = n }).ToArray()
                });
                if (!attribute.RequiredCallFlags.HasFlag(CallFlags.AllowModifyStates)) safeMethods.Add(name);
                methods.Add(name, new ContractMethodMetadata
                {
                    Delegate = (Func<ApplicationEngine, Array, StackItem>)method.CreateDelegate(typeof(Func<ApplicationEngine, Array, StackItem>), this),
                    Price = attribute.Price,
                    RequiredCallFlags = attribute.RequiredCallFlags
                });
            }
            this.Manifest = new ContractManifest
            {
                Permissions = new[] { ContractPermission.DefaultPermission },
                Abi = new ContractAbi()
                {
                    Hash = Hash,
                    Events = new ContractEventDescriptor[0],
                    Methods = descriptors.ToArray()
                },
                Features = ContractFeatures.NoProperty,
                Groups = new ContractGroup[0],
                SafeMethods = WildcardContainer<string>.Create(safeMethods.ToArray()),
                Trusts = WildcardContainer<UInt160>.Create(),
                Extra = null,
            };
            contractsList.Add(this);
            contractsNameDictionary.Add(Name, this);
            contractsHashDictionary.Add(Hash, this);
        }

        protected StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                Id = Id,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }

        internal protected StorageKey CreateStorageKey(byte prefix, ISerializable key)
        {
            return CreateStorageKey(prefix, key.ToArray());
        }

        public static NativeContract GetContract(UInt160 hash)
        {
            contractsHashDictionary.TryGetValue(hash, out var contract);
            return contract;
        }

        public static NativeContract GetContract(string name)
        {
            contractsNameDictionary.TryGetValue(name, out var contract);
            return contract;
        }

        internal bool Invoke(ApplicationEngine engine)
        {
            if (!engine.CurrentScriptHash.Equals(Hash))
                return false;
            string operation = engine.CurrentContext.EvaluationStack.Pop().GetString();
            Array args = (Array)engine.CurrentContext.EvaluationStack.Pop();
            if (!methods.TryGetValue(operation, out ContractMethodMetadata method))
                return false;
            ExecutionContextState state = engine.CurrentContext.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(method.RequiredCallFlags))
                return false;
            if (!engine.AddGas(method.Price))
                return false;
            StackItem result = method.Delegate(engine, args);
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        public static bool IsNative(UInt160 hash)
        {
            return contractsHashDictionary.ContainsKey(hash);
        }

        internal virtual void Initialize(ApplicationEngine engine)
        {
        }

        [ContractMethod(0, ContractParameterType.Boolean, CallFlags.AllowModifyStates)]
        protected StackItem OnPersist(ApplicationEngine engine, Array args)
        {
            if (engine.Trigger != TriggerType.System) return false;
            return OnPersist(engine);
        }

        protected virtual bool OnPersist(ApplicationEngine engine)
        {
            return true;
        }

        [ContractMethod(0, ContractParameterType.Array, CallFlags.None, Name = "supportedStandards")]
        protected StackItem SupportedStandardsMethod(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, SupportedStandards.Select(p => (StackItem)p));
        }

        public ApplicationEngine TestCall(string operation, params object[] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(Hash, operation, args);
                return ApplicationEngine.Run(sb.ToArray(), testMode: true);
            }
        }
    }
}
