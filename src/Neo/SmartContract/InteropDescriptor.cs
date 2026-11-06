// Copyright (C) 2015-2026 The Neo Project.
//
// InteropDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents a descriptor of an interoperable service.
    /// </summary>
    public record InteropDescriptor
    {
        /// <summary>
        /// The name of the interoperable service.
        /// </summary>
        public required string Name { get; init; }

        private uint _hash;
        /// <summary>
        /// The hash of the interoperable service.
        /// </summary>
        public uint Hash
        {
            get
            {
                if (_hash == 0)
                    _hash = BinaryPrimitives.ReadUInt32LittleEndian(Encoding.ASCII.GetBytes(Name).Sha256());
                return _hash;
            }
        }

        /// <summary>
        /// The <see cref="MethodInfo"/> used to handle the interoperable service.
        /// </summary>
        public required MethodInfo Handler { get; init; }

        /// <summary>
        /// The parameters of the interoperable service.
        /// </summary>
        public IReadOnlyList<InteropParameterDescriptor> Parameters => field ??= Handler.GetParameters().Select(p => new InteropParameterDescriptor(p)).ToList().AsReadOnly();

        /// <summary>
        /// The fixed prices for calling the interoperable service in the unit of 1e-11 GAS mapped from the
        /// hardforks they're applied from, ordered from the latest hardfork to the earliest one. The last entry
        /// with <see langword="null"/> hardfork is applied if none of the other hardforks is enabled.
        /// </summary>
        public IReadOnlyList<KeyValuePair<Hardfork?, long>> Prices { get; init; } = [];

        /// <summary>
        /// Required Hardfork to be active.
        /// </summary>
        public Hardfork? Hardfork { get; init; }

        /// <summary>
        /// The required <see cref="CallFlags"/> for the interoperable service.
        /// </summary>
        public CallFlags RequiredCallFlags { get; init; }

        /// <summary>
        /// Gets the fixed price of the interoperable service. It's taken from the latest enabled <see cref="Prices"/> entry.
        /// </summary>
        /// <param name="isHardforkEnabled">Checks whether the specified hardfork is enabled.</param>
        /// <returns>The price in the unit of 1e-11 GAS.</returns>
        public long GetPrice(Func<Hardfork, bool> isHardforkEnabled)
        {
            foreach (var (hardfork, price) in Prices)
                if (hardfork is null || isHardforkEnabled(hardfork.Value))
                    return price;
            throw new UnreachableException($"Price of {Name} isn't found.");
        }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
