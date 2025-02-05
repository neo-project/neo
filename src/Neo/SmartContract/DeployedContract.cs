// Copyright (C) 2015-2025 The Neo Project.
//
// DeployedContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            ContractMethodDescriptor descriptor = contract.Manifest.Abi.GetMethod(ContractBasicMethod.Verify, ContractBasicMethod.VerifyPCount);
            if (descriptor is null) throw new NotSupportedException("The smart contract haven't got verify method.");

            ParameterList = descriptor.Parameters.Select(u => u.Type).ToArray();
        }
    }
}
