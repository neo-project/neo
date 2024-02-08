// Copyright (C) 2015-2024 The Neo Project.
//
// ContractState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Json;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents a deployed contract.
    /// </summary>
    public class ContractState : IInteroperable
    {
        /// <summary>
        /// The id of the contract.
        /// </summary>
        public int Id;

        /// <summary>
        /// Indicates the number of times the contract has been updated.
        /// </summary>
        public ushort UpdateCounter;

        /// <summary>
        /// The hash of the contract.
        /// </summary>
        public UInt160 Hash;

        /// <summary>
        /// The nef of the contract.
        /// </summary>
        public NefFile Nef;

        /// <summary>
        /// The manifest of the contract.
        /// </summary>
        public ContractManifest Manifest;

        /// <summary>
        /// The script of the contract.
        /// </summary>
        public ReadOnlyMemory<byte> Script => Nef.Script;

        IInteroperable IInteroperable.Clone()
        {
            return new ContractState
            {
                Id = Id,
                UpdateCounter = UpdateCounter,
                Hash = Hash,
                Nef = Nef,
                Manifest = Manifest
            };
        }

        void IInteroperable.FromReplica(IInteroperable replica)
        {
            ContractState from = (ContractState)replica;
            Id = from.Id;
            UpdateCounter = from.UpdateCounter;
            Hash = from.Hash;
            Nef = from.Nef;
            Manifest = from.Manifest;
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Array array = (Array)stackItem;
            Id = (int)array[0].GetInteger();
            UpdateCounter = (ushort)array[1].GetInteger();
            Hash = new UInt160(array[2].GetSpan());
            Nef = ((ByteString)array[3]).Memory.AsSerializable<NefFile>();
            Manifest = array[4].ToInteroperable<ContractManifest>();
        }

        /// <summary>
        /// Determines whether the current contract has the permission to call the specified contract.
        /// </summary>
        /// <param name="targetContract">The contract to be called.</param>
        /// <param name="targetMethod">The method to be called.</param>
        /// <returns><see langword="true"/> if the contract allows to be called; otherwise, <see langword="false"/>.</returns>
        public bool CanCall(ContractState targetContract, string targetMethod)
        {
            return Manifest.Permissions.Any(u => u.IsAllowed(targetContract, targetMethod));
        }

        /// <summary>
        /// Converts the contract to a JSON object.
        /// </summary>
        /// <returns>The contract represented by a JSON object.</returns>
        public JObject ToJson()
        {
            return new JObject
            {
                ["id"] = Id,
                ["updatecounter"] = UpdateCounter,
                ["hash"] = Hash.ToString(),
                ["nef"] = Nef.ToJson(),
                ["manifest"] = Manifest.ToJson()
            };
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Id, (int)UpdateCounter, Hash.ToArray(), Nef.ToArray(), Manifest.ToStackItem(referenceCounter) });
        }
    }
}
