// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// Represents the manifest of a smart contract.
    /// When a smart contract is deployed, it must explicitly declare the features and permissions it will use.
    /// When it is running, it will be limited by its declared list of features and permissions, and cannot make any behavior beyond the scope of the list.
    /// </summary>
    /// <remarks>For more details, see NEP-15.</remarks>
    public class ContractManifest : IInteroperable
    {
        /// <summary>
        /// The maximum length of a manifest.
        /// </summary>
        public const int MaxLength = ushort.MaxValue;

        /// <summary>
        /// The name of the contract.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The groups of the contract.
        /// </summary>
        public ContractGroup[] Groups { get; set; }

        /// <summary>
        /// Indicates which standards the contract supports. It can be a list of NEPs.
        /// </summary>
        public string[] SupportedStandards { get; set; }

        /// <summary>
        /// The ABI of the contract.
        /// </summary>
        public ContractAbi Abi { get; set; }

        /// <summary>
        /// The permissions of the contract.
        /// </summary>
        public ContractPermission[] Permissions { get; set; }

        /// <summary>
        /// The trusted contracts and groups of the contract.
        /// If a contract is trusted, the user interface will not give any warnings when called by the contract.
        /// </summary>
        public WildcardContainer<ContractPermissionDescriptor> Trusts { get; set; }

        /// <summary>
        /// Custom user data.
        /// </summary>
        public JObject Extra { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Name = @struct[0].GetString();
            Groups = ((Array)@struct[1]).Select(p => p.ToInteroperable<ContractGroup>()).ToArray();
            if (((Map)@struct[2]).Count != 0)
                throw new ArgumentException(null, nameof(stackItem));
            SupportedStandards = ((Array)@struct[3]).Select(p => p.GetString()).ToArray();
            Abi = @struct[4].ToInteroperable<ContractAbi>();
            Permissions = ((Array)@struct[5]).Select(p => p.ToInteroperable<ContractPermission>()).ToArray();
            Trusts = @struct[6] switch
            {
                Null => WildcardContainer<ContractPermissionDescriptor>.CreateWildcard(),
                Array array => WildcardContainer<ContractPermissionDescriptor>.Create(array.Select(p => new ContractPermissionDescriptor(p.GetSpan())).ToArray()),
                _ => throw new ArgumentException(null, nameof(stackItem))
            };
            Extra = JObject.Parse(@struct[7].GetSpan());
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter)
            {
                Name,
                new Array(referenceCounter, Groups.Select(p => p.ToStackItem(referenceCounter))),
                new Map(referenceCounter),
                new Array(referenceCounter, SupportedStandards.Select(p => (StackItem)p)),
                Abi.ToStackItem(referenceCounter),
                new Array(referenceCounter, Permissions.Select(p => p.ToStackItem(referenceCounter))),
                Trusts.IsWildcard ? StackItem.Null : new Array(referenceCounter, Trusts.Select(p => (StackItem)p.ToArray())),
                Extra is null ? "null" : Extra.ToByteArray(false)
            };
        }

        /// <summary>
        /// Converts the manifest from a JSON object.
        /// </summary>
        /// <param name="json">The manifest represented by a JSON object.</param>
        /// <returns>The converted manifest.</returns>
        public static ContractManifest FromJson(JObject json)
        {
            ContractManifest manifest = new()
            {
                Name = json["name"].GetString(),
                Groups = ((JArray)json["groups"]).Select(u => ContractGroup.FromJson(u)).ToArray(),
                SupportedStandards = ((JArray)json["supportedstandards"]).Select(u => u.GetString()).ToArray(),
                Abi = ContractAbi.FromJson(json["abi"]),
                Permissions = ((JArray)json["permissions"]).Select(u => ContractPermission.FromJson(u)).ToArray(),
                Trusts = WildcardContainer<ContractPermissionDescriptor>.FromJson(json["trusts"], u => ContractPermissionDescriptor.FromJson(u)),
                Extra = json["extra"]
            };
            if (string.IsNullOrEmpty(manifest.Name))
                throw new FormatException();
            _ = manifest.Groups.ToDictionary(p => p.PubKey);
            if (json["features"].Properties.Count != 0)
                throw new FormatException();
            if (manifest.SupportedStandards.Any(p => string.IsNullOrEmpty(p)))
                throw new FormatException();
            _ = manifest.SupportedStandards.ToDictionary(p => p);
            _ = manifest.Permissions.ToDictionary(p => p.Contract);
            _ = manifest.Trusts.ToDictionary(p => p);
            return manifest;
        }

        /// <summary>
        /// Parse the manifest from a byte array containing JSON data.
        /// </summary>
        /// <param name="json">The byte array containing JSON data.</param>
        /// <returns>The parsed manifest.</returns>
        public static ContractManifest Parse(ReadOnlySpan<byte> json)
        {
            if (json.Length > MaxLength) throw new ArgumentException(null, nameof(json));
            return FromJson(JObject.Parse(json));
        }

        /// <summary>
        /// Parse the manifest from a JSON <see cref="string"/>.
        /// </summary>
        /// <param name="json">The JSON <see cref="string"/>.</param>
        /// <returns>The parsed manifest.</returns>
        public static ContractManifest Parse(string json) => Parse(Utility.StrictUTF8.GetBytes(json));

        /// <summary>
        /// Converts the manifest to a JSON object.
        /// </summary>
        /// <returns>The manifest represented by a JSON object.</returns>
        public JObject ToJson()
        {
            return new JObject
            {
                ["name"] = Name,
                ["groups"] = Groups.Select(u => u.ToJson()).ToArray(),
                ["features"] = new JObject(),
                ["supportedstandards"] = SupportedStandards.Select(u => new JString(u)).ToArray(),
                ["abi"] = Abi.ToJson(),
                ["permissions"] = Permissions.Select(p => p.ToJson()).ToArray(),
                ["trusts"] = Trusts.ToJson(p => p.ToJson()),
                ["extra"] = Extra
            };
        }

        /// <summary>
        /// Determines whether the manifest is valid.
        /// </summary>
        /// <param name="hash">The hash of the contract.</param>
        /// <returns><see langword="true"/> if the manifest is valid; otherwise, <see langword="false"/>.</returns>
        public bool IsValid(UInt160 hash)
        {
            return Groups.All(u => u.IsValid(hash));
        }
    }
}
