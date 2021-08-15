// Copyright (C) 2015-2021 NEO GLOBAL DEVELOPMENT.
// 
// The Neo project is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the type of <see cref="ContractParameter"/>.
    /// </summary>
    public enum ContractParameterType : byte
    {
        /// <summary>
        /// Indicates that the parameter can be of any type.
        /// </summary>
        Any = 0x00,

        /// <summary>
        /// Indicates that the parameter is of Boolean type.
        /// </summary>
        Boolean = 0x10,

        /// <summary>
        /// Indicates that the parameter is an integer.
        /// </summary>
        Integer = 0x11,

        /// <summary>
        /// Indicates that the parameter is a byte array.
        /// </summary>
        ByteArray = 0x12,

        /// <summary>
        /// Indicates that the parameter is a string.
        /// </summary>
        String = 0x13,

        /// <summary>
        /// Indicates that the parameter is a 160-bit hash.
        /// </summary>
        Hash160 = 0x14,

        /// <summary>
        /// Indicates that the parameter is a 256-bit hash.
        /// </summary>
        Hash256 = 0x15,

        /// <summary>
        /// Indicates that the parameter is a public key.
        /// </summary>
        PublicKey = 0x16,

        /// <summary>
        /// Indicates that the parameter is a signature.
        /// </summary>
        Signature = 0x17,

        /// <summary>
        /// Indicates that the parameter is an array.
        /// </summary>
        Array = 0x20,

        /// <summary>
        /// Indicates that the parameter is a map.
        /// </summary>
        Map = 0x22,

        /// <summary>
        /// Indicates that the parameter is an interoperable interface.
        /// </summary>
        InteropInterface = 0x30,

        /// <summary>
        /// It can be only used as the return type of a method, meaning that the method has no return value.
        /// </summary>
        Void = 0xff
    }
}
