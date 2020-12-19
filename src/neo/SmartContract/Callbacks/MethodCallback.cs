using Neo.IO;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using System;
using System.Linq;

namespace Neo.SmartContract.Callbacks
{
    public class MethodCallback : SyscallCallback
    {
        private readonly ContractState contract;
        private readonly ContractMethodDescriptor method;

        public override int ParametersCount => method.Parameters.Length;

        public MethodCallback(ApplicationEngine engine, UInt160 hash, string method)
            : base(ApplicationEngine.System_Contract_Call, false)
        {
            if (method.StartsWith('_')) throw new ArgumentException();
            this.contract = NativeContract.ContractManagement.GetContract(engine.Snapshot, hash);
            ContractState currentContract = NativeContract.ContractManagement.GetContract(engine.Snapshot, engine.CurrentScriptHash);
            if (currentContract?.CanCall(this.contract, method) == false)
                throw new InvalidOperationException();
            this.method = this.contract.Manifest.Abi.Methods.First(p => p.Name == method);
        }

        public override void LoadContext(ApplicationEngine engine)
        {
            engine.Push(ParametersCount);
            engine.Push(method.ReturnType == ContractParameterType.Void ? 0 : 1);
            engine.Push((byte)CallFlags.All);
            engine.Push(method.Name);
            engine.Push(contract.Hash.ToArray());
        }
    }
}
