using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    public abstract class NativeContract
    {
        private static readonly List<NativeContract> contracts = new List<NativeContract>();

        public static IReadOnlyCollection<NativeContract> Contracts { get; } = contracts;
        public static NeoToken NEO { get; } = new NeoToken();
        public static GasToken GAS { get; } = new GasToken();

        public abstract string ServiceName { get; }
        public uint ServiceHash { get; }
        public byte[] Script { get; }
        public UInt160 Hash { get; }
        public virtual ContractPropertyState Properties => ContractPropertyState.NoProperty;
        public virtual string[] SupportedStandards { get; } = { "NEP-10" };

        protected NativeContract()
        {
            this.ServiceHash = ServiceName.ToInteropMethodHash();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ServiceHash);
                this.Script = sb.ToArray();
            }
            this.Hash = Script.ToScriptHash();
            contracts.Add(this);
        }

        protected StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = Hash,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            if (key != null)
                Buffer.BlockCopy(key, 0, storageKey.Key, 1, key.Length);
            return storageKey;
        }

        protected StorageKey CreateStorageKey(byte prefix, ISerializable key)
        {
            return CreateStorageKey(prefix, key.ToArray());
        }

        internal bool Invoke(ApplicationEngine engine)
        {
            if (!engine.CurrentScriptHash.Equals(Hash))
                return false;
            string operation = engine.CurrentContext.EvaluationStack.Pop().GetString();
            VMArray args = (VMArray)engine.CurrentContext.EvaluationStack.Pop();
            StackItem result = Main(engine, operation, args);
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        protected virtual StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            switch (operation)
            {
                case "onPersist":
                    return OnPersist(engine);
                case "supportedStandards":
                    return SupportedStandards.Select(p => (StackItem)p).ToList();
            }
            throw new NotSupportedException();
        }

        internal virtual bool Initialize(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.Application)
                throw new InvalidOperationException();
            return true;
        }

        protected virtual bool OnPersist(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.System)
                throw new InvalidOperationException();
            return true;
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
