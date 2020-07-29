using Neo.Ledger;
using Neo.SmartContract.Manifest;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    public class DeployedContract : Contract
    {
        public override UInt160 ScriptHash { get; }

        public DeployedContract(ContractState contract)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));

            Script = null;
            ScriptHash = contract.ScriptHash;
            ContractMethodDescriptor descriptor = contract.Manifest.Abi.GetMethod("verify");
            ParameterList = descriptor.Parameters.Select(u => u.Type).ToArray();
        }
    }
}
