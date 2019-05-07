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
    public abstract class NativeContractBase
    {
        public static IReadOnlyDictionary<string, NativeContractBase> Contracts { get; }
        public static NeoToken NEO { get; }
        public static GasToken GAS { get; }

        public abstract string ServiceName { get; }
        public byte[] Script { get; }
        public UInt160 ScriptHash { get; }
        public virtual ContractPropertyState Properties => ContractPropertyState.NoProperty;
        public virtual string[] SupportedStandards { get; } = { "NEP-10" };

        static NativeContractBase()
        {
            Type t = typeof(NativeContractBase);
            Contracts = t.Assembly.GetTypes().Where(p => !p.IsAbstract && p.IsSubclassOf(t)).Select(p => (NativeContractBase)Activator.CreateInstance(p, true)).ToDictionary(p => p.ServiceName);
            NEO = (NeoToken)Contracts["Neo.Native.Tokens.NEO"];
            GAS = (GasToken)Contracts["Neo.Native.Tokens.GAS"];
        }

        protected NativeContractBase()
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ServiceName);
                this.Script = sb.ToArray();
            }
            this.ScriptHash = Script.ToScriptHash();
        }

        protected StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = ScriptHash,
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
            if (!new UInt160(engine.CurrentContext.ScriptHash).Equals(ScriptHash))
                return false;
            string operation = engine.CurrentContext.EvaluationStack.Pop().GetString();
            VMArray args = (VMArray)engine.CurrentContext.EvaluationStack.Pop();
            StackItem result = Main(engine, operation, args);
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        protected virtual StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            if (operation == "supportedStandards")
                return SupportedStandards.Select(p => (StackItem)p).ToList();
            throw new NotSupportedException();
        }
    }
}
