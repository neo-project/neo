// Copyright (C) 2015-2022 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// Indicates which contracts are authorized to be called.
    /// </summary>
    public class ContractPermissionDescriptor : IEquatable<ContractPermissionDescriptor>
    {
        /// <summary>
        /// The hash of the contract. It can't be set with <see cref="Group"/>.
        /// </summary>
        public UInt160 Hash { get; }

        /// <summary>
        /// The group of the contracts. It can't be set with <see cref="Hash"/>.
        /// </summary>
        public ECPoint Group { get; }

        /// <summary>
        /// Indicates whether <see cref="Hash"/> is set.
        /// </summary>
        public bool IsHash => Hash != null;

        /// <summary>
        /// Indicates whether <see cref="Group"/> is set.
        /// </summary>
        public bool IsGroup => Group != null;

        /// <summary>
        /// Indicates whether it is a wildcard.
        /// </summary>
        public bool IsWildcard => Hash is null && Group is null;

        private ContractPermissionDescriptor(UInt160 hash, ECPoint group)
        {
            this.Hash = hash;
            this.Group = group;
        }

        internal ContractPermissionDescriptor(ReadOnlySpan<byte> span)
        {
            switch (span.Length)
            {
                case UInt160.Length:
                    Hash = new UInt160(span);
                    break;
                case 33:
                    Group = ECPoint.DecodePoint(span, ECCurve.Secp256r1);
                    break;
                default:
                    throw new ArgumentException(null, nameof(span));
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ContractPermissionDescriptor"/> class with the specified contract hash.
        /// </summary>
        /// <param name="hash">The contract to be called.</param>
        /// <returns>The created permission descriptor.</returns>
        public static ContractPermissionDescriptor Create(UInt160 hash)
        {
            return new ContractPermissionDescriptor(hash, null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ContractPermissionDescriptor"/> class with the specified group.
        /// </summary>
        /// <param name="group">The group of the contracts to be called.</param>
        /// <returns>The created permission descriptor.</returns>
        public static ContractPermissionDescriptor Create(ECPoint group)
        {
            return new ContractPermissionDescriptor(null, group);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ContractPermissionDescriptor"/> class with wildcard.
        /// </summary>
        /// <returns>The created permission descriptor.</returns>
        public static ContractPermissionDescriptor CreateWildcard()
        {
            return new ContractPermissionDescriptor(null, null);
        }

        public override bool Equals(object obj)
        {
            if (obj is not ContractPermissionDescriptor other) return false;
            return Equals(other);
        }

        public bool Equals(ContractPermissionDescriptor other)
        {
            if (other is null) return false;
            if (this == other) return true;
            if (IsWildcard == other.IsWildcard) return true;
            if (IsHash) return Hash.Equals(other.Hash);
            return Group.Equals(other.Group);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hash, Group);
        }

        /// <summary>
        /// Converts the permission descriptor from a JSON object.
        /// </summary>
        /// <param name="json">The permission descriptor represented by a JSON object.</param>
        /// <returns>The converted permission descriptor.</returns>
        public static ContractPermissionDescriptor FromJson(JObject json)
        {
            string str = json.GetString();
            if (str.Length == 42)
                return Create(UInt160.Parse(str));
            if (str.Length == 66)
                return Create(ECPoint.Parse(str, ECCurve.Secp256r1));
            if (str == "*")
                return CreateWildcard();
            throw new FormatException();
        }

        /// <summary>
        /// Converts the permission descriptor to a JSON object.
        /// </summary>
        /// <returns>The permission descriptor represented by a JSON object.</returns>
        public JObject ToJson()
        {
            if (IsHash) return Hash.ToString();
            if (IsGroup) return Group.ToString();
            return "*";
        }

        /// <summary>
        /// Converts the permission descriptor to byte array.
        /// </summary>
        /// <returns>The converted byte array. Or <see langword="null"/> if it is a wildcard.</returns>
        public byte[] ToArray()
        {
            return Hash?.ToArray() ?? Group?.EncodePoint(true);
        }
    }
}
