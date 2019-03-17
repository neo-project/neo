using Neo.Ledger;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.NativeContract
{
    class NativeContractList
    {
        public static Dictionary<string, INativeContract> Contracts;
        static NativeContractList()
        {
            Contracts = new Dictionary<string, INativeContract>();


            RegContract(new NativeContract_Nep5Neo());
        }
        static void RegContract(INativeContract contract)
        {
            Contracts[contract.Name] = contract;
        }
    }
    class NativeContractTool
    {
        public static StorageItem Storage_Get_Current(ExecutionEngine engine, byte[] key)
        {
            var app = engine as ApplicationEngine;
            UInt160 ScriptHash = new UInt160(engine.CurrentContext.ScriptHash);
            StorageItem item = app.snapshot.Storages.TryGet(new StorageKey() { ScriptHash = ScriptHash, Key = key });
            return item;
        }
        public static void Storage_Put_Current(ExecutionEngine engine, byte[] key, byte[] value)
        {
            var app = engine as ApplicationEngine;
            UInt160 ScriptHash = new UInt160(engine.CurrentContext.ScriptHash);
            app.snapshot.Storages.Add(
                new StorageKey() { ScriptHash = ScriptHash, Key = key },
                new StorageItem() { Value = value });
        }
    }
    interface INativeContract
    {
        string Name
        {
            get;
        }
        long Price
        {
            get;
        }
        ContractParameterType[] Parameter_list
        {
            get;
        }
        ContractParameterType Return_type
        {
            get;
        }
        Ledger.ContractPropertyState Contract_properties
        {
            get;
        }
        string Version
        {
            get;
        }
        string Author
        {
            get;
        }
        string Email
        {
            get;
        }
        string Description
        {
            get;
        }
        bool Contract_Main(ExecutionEngine engine);
    }
  
}
