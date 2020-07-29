using Neo.Ledger;
using Neo.SmartContract.Manifest;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    public class DeployedContract : Contract
    {
        private readonly UInt160 _scriptHash;
        public readonly ContractMethodDescriptor Verify;

        public override UInt160 ScriptHash => _scriptHash;

        public DeployedContract(ContractState contract)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));

            Script = null;
            _scriptHash = contract.ScriptHash;
            Verify = contract.Manifest.Abi.GetMethod("verify");
            ParameterList = Verify.Parameters.Select(u => u.Type).ToArray();
        }
    }
}
