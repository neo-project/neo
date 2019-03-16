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
    class NativeContract_Nep5Neo : INativeContract
    {
        public string Name => throw new NotImplementedException();

        public long Price => throw new NotImplementedException();

        public ContractParameterType[] Parameter_list => throw new NotImplementedException();

        public ContractParameterType Return_type => throw new NotImplementedException();

        public ContractPropertyState Contract_properties => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public string Author => throw new NotImplementedException();

        public string Email => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public bool Contract_Main(ExecutionEngine engine)
        {
            throw new NotImplementedException();
        }
    }
}
