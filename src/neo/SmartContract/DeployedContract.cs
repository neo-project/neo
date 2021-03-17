using Neo.SmartContract.Manifest;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents a deployed contract that can be invoked.
    /// </summary>
    public class DeployedContract : Contract
    {
        public override UInt160 ScriptHash { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeployedContract"/> class with the specified <see cref="ContractState"/>.
        /// </summary>
        /// <param name="contract">The <see cref="ContractState"/> corresponding to the contract.</param>
        public DeployedContract(ContractState contract)
        {
            if (contract is null) throw new ArgumentNullException(nameof(contract));

            Script = null;
            ScriptHash = contract.Hash;
            ContractMethodDescriptor descriptor = contract.Manifest.Abi.GetMethod("verify", -1);
            if (descriptor is null) throw new NotSupportedException("The smart contract haven't got verify method.");

            ParameterList = descriptor.Parameters.Select(u => u.Type).ToArray();
        }
    }
}
