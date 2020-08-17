using Neo.IO;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        [ContractMethod(0, CallFlags.None)]
        public abstract string Name { get; }
        public byte[] Script { get; }
        public UInt160 Hash { get; }
        public abstract int Id { get; }
        public ContractManifest Manifest { get; }

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
            foreach (MemberInfo member in GetType().GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                ContractMethodAttribute attribute = member.GetCustomAttribute<ContractMethodAttribute>();
                if (attribute is null) continue;
                ContractMethodMetadata metadata = new ContractMethodMetadata(member, attribute);
                descriptors.Add(new ContractMethodDescriptor
                {
                    Name = metadata.Name,
                    ReturnType = ToParameterType(metadata.Handler.ReturnType),
                    Parameters = metadata.Parameters.Select(p => new ContractParameterDefinition { Type = ToParameterType(p.Type), Name = p.Name }).ToArray()
                });
                if (!attribute.RequiredCallFlags.HasFlag(CallFlags.AllowModifyStates)) safeMethods.Add(metadata.Name);
                methods.Add(metadata.Name, metadata);
            }
            this.Manifest = new ContractManifest
            {
                Groups = System.Array.Empty<ContractGroup>(),
                Features = ContractFeatures.NoProperty,
                SupportedStandards = new string[0],
                Abi = new ContractAbi()
                {
                    Hash = Hash,
                    Events = System.Array.Empty<ContractEventDescriptor>(),
                    Methods = descriptors.ToArray()
                },
                Permissions = new[] { ContractPermission.DefaultPermission },
                Trusts = WildcardContainer<UInt160>.Create(),
                SafeMethods = WildcardContainer<string>.Create(safeMethods.ToArray()),
                Extra = null
            };
            contractsList.Add(this);
            contractsNameDictionary.Add(Name, this);
            contractsHashDictionary.Add(Hash, this);
        }

        private protected KeyBuilder CreateStorageKey(byte prefix)
        {
            return new KeyBuilder(Id, prefix);
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

        internal void Invoke(ApplicationEngine engine)
        {
            if (!engine.CurrentScriptHash.Equals(Hash))
                throw new InvalidOperationException("It is not allowed to use Neo.Native.Call directly to call native contracts. System.Contract.Call should be used.");
            string operation = engine.Pop().GetString();
            Array args = engine.Pop<Array>();
            ContractMethodMetadata method = methods[operation];
            ExecutionContextState state = engine.CurrentContext.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(method.RequiredCallFlags))
                throw new InvalidOperationException($"Cannot call this method with the flag {state.CallFlags}.");
            engine.AddGas(method.Price);
            List<object> parameters = new List<object>();
            if (method.NeedApplicationEngine) parameters.Add(engine);
            if (method.NeedSnapshot) parameters.Add(engine.Snapshot);
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                StackItem item = i < args.Count ? args[i] : StackItem.Null;
                parameters.Add(engine.Convert(item, method.Parameters[i]));
            }
            object returnValue = method.Handler.Invoke(this, parameters.ToArray());
            if (method.Handler.ReturnType != typeof(void))
                engine.Push(engine.Convert(returnValue));
        }

        public static bool IsNative(UInt160 hash)
        {
            return contractsHashDictionary.ContainsKey(hash);
        }

        internal virtual void Initialize(ApplicationEngine engine)
        {
        }

        [ContractMethod(0, CallFlags.AllowModifyStates)]
        protected virtual void OnPersist(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.System)
                throw new InvalidOperationException();
        }

        public ApplicationEngine TestCall(string operation, params object[] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(Hash, operation, args);
                return ApplicationEngine.Run(sb.ToArray());
            }
        }

        private static ContractParameterType ToParameterType(Type type)
        {
            if (type == typeof(void)) return ContractParameterType.Void;
            if (type == typeof(bool)) return ContractParameterType.Boolean;
            if (type == typeof(sbyte)) return ContractParameterType.Integer;
            if (type == typeof(byte)) return ContractParameterType.Integer;
            if (type == typeof(short)) return ContractParameterType.Integer;
            if (type == typeof(ushort)) return ContractParameterType.Integer;
            if (type == typeof(int)) return ContractParameterType.Integer;
            if (type == typeof(uint)) return ContractParameterType.Integer;
            if (type == typeof(long)) return ContractParameterType.Integer;
            if (type == typeof(ulong)) return ContractParameterType.Integer;
            if (type == typeof(BigInteger)) return ContractParameterType.Integer;
            if (type == typeof(byte[])) return ContractParameterType.ByteArray;
            if (type == typeof(string)) return ContractParameterType.String;
            if (type == typeof(VM.Types.Boolean)) return ContractParameterType.Boolean;
            if (type == typeof(Integer)) return ContractParameterType.Integer;
            if (type == typeof(ByteString)) return ContractParameterType.ByteArray;
            if (type == typeof(VM.Types.Buffer)) return ContractParameterType.ByteArray;
            if (type == typeof(Array)) return ContractParameterType.Array;
            if (type == typeof(Struct)) return ContractParameterType.Array;
            if (type == typeof(Map)) return ContractParameterType.Map;
            if (type == typeof(StackItem)) return ContractParameterType.Any;
            if (typeof(IInteroperable).IsAssignableFrom(type)) return ContractParameterType.Array;
            if (typeof(ISerializable).IsAssignableFrom(type)) return ContractParameterType.ByteArray;
            if (type.IsArray) return ContractParameterType.Array;
            if (type.IsEnum) return ContractParameterType.Integer;
            return ContractParameterType.Any;
        }
    }
}
