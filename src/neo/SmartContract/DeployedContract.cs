using Neo.Ledger;
using Neo.SmartContract.Manifest;
using System;

namespace Neo.SmartContract
{
    public class DeployedContract : Contract
    {
        public override UInt160 ScriptHash { get; }

        public ContractParameterDefinition[] VerifyArguments { get; }

        public DeployedContract(ContractState contract)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));

            Script = null;
            ScriptHash = contract.ScriptHash;
            ContractMethodDescriptor descriptor = contract.Manifest.Abi.GetMethod("verify");
            if (descriptor == null) throw new ArgumentNullException("The smart contract haven't got verify method.");

            VerifyArguments = descriptor.Parameters;
            ParameterList = new ContractParameterType[] { ContractParameterType.Array };
        }
    }
}
