// Copyright (C) 2015-2024 The Neo Project.
//
// ContractNameOrHashOrId.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics.CodeAnalysis;

namespace Neo.Plugins.RpcServer.Model;

public class ContractNameOrHashOrId
{
    private readonly object _value;

    public ContractNameOrHashOrId(int id)
    {
        _value = id;
    }

    public ContractNameOrHashOrId(UInt160 hash)
    {
        _value = hash;
    }

    public ContractNameOrHashOrId(string name)
    {
        _value = name;
    }

    public bool IsId => _value is int;
    public bool IsHash => _value is UInt160;
    public bool IsName => _value is string;

    public static bool TryParse(string value, [NotNullWhen(true)] out ContractNameOrHashOrId? contractNameOrHashOrId)
    {
        if (int.TryParse(value, out var id))
        {
            contractNameOrHashOrId = new ContractNameOrHashOrId(id);
            return true;
        }
        if (UInt160.TryParse(value, out var hash))
        {
            contractNameOrHashOrId = new ContractNameOrHashOrId(hash);
            return true;
        }

        if (value.Length > 0)
        {
            contractNameOrHashOrId = new ContractNameOrHashOrId(value);
            return true;
        }
        contractNameOrHashOrId = null;
        return false;
    }

    public int AsId()
    {
        if (_value is int intValue)
            return intValue;
        throw new RpcException(RpcError.InvalidParams.WithData($"Value {_value} is not a valid contract id"));
    }

    public UInt160 AsHash()
    {
        if (_value is UInt160 hash)
            return hash;
        throw new RpcException(RpcError.InvalidParams.WithData($"Value {_value} is not a valid contract hash"));
    }
    public string AsName()
    {
        if (_value is string name)
            return name;
        throw new RpcException(RpcError.InvalidParams.WithData($"Value {_value} is not a valid contract name"));
    }
}
