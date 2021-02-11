using Neo.IO;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.SmartContract.Native
{
    public abstract class NativeContract
    {
        private static readonly List<NativeContract> contractsList = new List<NativeContract>();
        private static readonly Dictionary<UInt160, NativeContract> contractsDictionary = new Dictionary<UInt160, NativeContract>();
        private readonly Dictionary<int, ContractMethodMetadata> methods = new Dictionary<int, ContractMethodMetadata>();
        private static int id_counter = 0;

        #region Named Native Contracts
        public static ContractManagement ContractManagement { get; } = new ContractManagement();
        public static StdLib StdLib { get; } = new StdLib();
        public static CryptoLib CryptoLib { get; } = new CryptoLib();
        public static LedgerContract Ledger { get; } = new LedgerContract();
        public static NeoToken NEO { get; } = new NeoToken();
        public static GasToken GAS { get; } = new GasToken();
        public static PolicyContract Policy { get; } = new PolicyContract();
        public static RoleManagement RoleManagement { get; } = new RoleManagement();
        public static OracleContract Oracle { get; } = new OracleContract();
        public static NameService NameService { get; } = new NameService();
        #endregion

        public static IReadOnlyCollection<NativeContract> Contracts { get; } = contractsList;
        public string Name => GetType().Name;
        public NefFile Nef { get; }
        public UInt160 Hash { get; }
        public int Id { get; } = --id_counter;
        public ContractManifest Manifest { get; }

        protected NativeContract()
        {
            List<ContractMethodMetadata> descriptors = new List<ContractMethodMetadata>();
            foreach (MemberInfo member in GetType().GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                ContractMethodAttribute attribute = member.GetCustomAttribute<ContractMethodAttribute>();
                if (attribute is null) continue;
                descriptors.Add(new ContractMethodMetadata(member, attribute));
            }
            descriptors = descriptors.OrderBy(p => p.Name).ThenBy(p => p.Parameters.Length).ToList();
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                foreach (ContractMethodMetadata method in descriptors)
                {
                    method.Descriptor.Offset = sb.Length;
                    sb.EmitPush(0); //version
                    methods.Add(sb.Length, method);
                    sb.EmitSysCall(ApplicationEngine.System_Contract_CallNative);
                    sb.Emit(OpCode.RET);
                }
                script = sb.ToArray();
            }
            this.Nef = new NefFile
            {
                Compiler = "neo-core-v3.0",
                Tokens = Array.Empty<MethodToken>(),
                Script = script
            };
            this.Nef.CheckSum = NefFile.ComputeChecksum(Nef);
            this.Hash = Helper.GetContractHash(UInt160.Zero, 0, Name);
            this.Manifest = new ContractManifest
            {
                Name = Name,
                Groups = Array.Empty<ContractGroup>(),
                SupportedStandards = Array.Empty<string>(),
                Abi = new ContractAbi()
                {
                    Events = Array.Empty<ContractEventDescriptor>(),
                    Methods = descriptors.Select(p => p.Descriptor).ToArray()
                },
                Permissions = new[] { ContractPermission.DefaultPermission },
                Trusts = WildcardContainer<UInt160>.Create(),
                Extra = null
            };
            contractsList.Add(this);
            contractsDictionary.Add(Hash, this);
        }

        protected bool CheckCommittee(ApplicationEngine engine)
        {
            UInt160 committeeMultiSigAddr = NEO.GetCommitteeAddress(engine.Snapshot);
            return engine.CheckWitnessInternal(committeeMultiSigAddr);
        }

        private protected KeyBuilder CreateStorageKey(byte prefix)
        {
            return new KeyBuilder(Id, prefix);
        }

        public static NativeContract GetContract(UInt160 hash)
        {
            contractsDictionary.TryGetValue(hash, out var contract);
            return contract;
        }

        internal void Invoke(ApplicationEngine engine, byte version)
        {
            uint activeIndex = engine.ProtocolSettings.NativeUpdateHistory[Name][0];
            if (activeIndex > Ledger.CurrentIndex(engine.Snapshot))
                throw new InvalidOperationException($"The native contract {Name} is not active.");
            if (version != 0)
                throw new InvalidOperationException($"The native contract of version {version} is not active.");
            ExecutionContext context = engine.CurrentContext;
            ContractMethodMetadata method = methods[context.InstructionPointer];
            ExecutionContextState state = context.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(method.RequiredCallFlags))
                throw new InvalidOperationException($"Cannot call this method with the flag {state.CallFlags}.");
            engine.AddGas(method.Price);
            List<object> parameters = new List<object>();
            if (method.NeedApplicationEngine) parameters.Add(engine);
            if (method.NeedSnapshot) parameters.Add(engine.Snapshot);
            for (int i = 0; i < method.Parameters.Length; i++)
                parameters.Add(engine.Convert(context.EvaluationStack.Pop(), method.Parameters[i]));
            object returnValue = method.Handler.Invoke(this, parameters.ToArray());
            if (method.Handler.ReturnType != typeof(void))
                context.EvaluationStack.Push(engine.Convert(returnValue));
        }

        public static bool IsNative(UInt160 hash)
        {
            return contractsDictionary.ContainsKey(hash);
        }

        internal virtual void Initialize(ApplicationEngine engine)
        {
        }

        internal virtual void OnPersist(ApplicationEngine engine)
        {
        }

        internal virtual void PostPersist(ApplicationEngine engine)
        {
        }
    }
}
