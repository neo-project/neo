// Copyright (C) 2015-2025 The Neo Project.
//
// RpcMethodAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Plugins.RpcServer
{
    /// <summary>
    /// Indicates that the method is an RPC method.
    /// Parameter type can be JArray, and if the parameter is a JArray,
    /// the method will be called with raw parameters from jsonrpc request.
    /// <para>
    /// Or one of the following types:
    /// </para>
    /// string, byte[], byte, sbyte, short, ushort, int, uint, long, ulong, double, bool,
    /// Guid, UInt160, UInt256, ContractNameOrHashOrId, BlockHashOrIndex, ContractParameter[],
    /// Address, SignersAndWitnesses
    /// <para>
    /// The return type can be one of JToken or Task&lt;JToken&gt;.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RpcMethodAttribute : Attribute
    {
        public string Name { get; set; } = string.Empty;
    }
}
