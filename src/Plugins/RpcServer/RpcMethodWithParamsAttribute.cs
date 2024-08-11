// Copyright (C) 2015-2024 The Neo Project.
//
// RpcMethodWithParamsAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Org.BouncyCastle.Asn1;
using System;

namespace Neo.Plugins.RpcServer;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RpcMethodWithParamsAttribute : Attribute
{
    public string Name { get; set; }
}
