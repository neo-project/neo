// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using System.Buffers.Binary;
using System.Collections.Generic;
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
        public string Name { get; init; }

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
        public MethodInfo Handler { get; init; }

        private IReadOnlyList<InteropParameterDescriptor> _parameters;
        /// <summary>
        /// The parameters of the interoperable service.
        /// </summary>
        public IReadOnlyList<InteropParameterDescriptor> Parameters => _parameters ??= Handler.GetParameters().Select(p => new InteropParameterDescriptor(p)).ToList().AsReadOnly();

        /// <summary>
        /// The fixed price for calling the interoperable service. It can be 0 if the interoperable service has a variable price.
        /// </summary>
        public long FixedPrice { get; init; }

        /// <summary>
        /// The required <see cref="CallFlags"/> for the interoperable service.
        /// </summary>
        public CallFlags RequiredCallFlags { get; init; }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
