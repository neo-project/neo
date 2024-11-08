// Copyright (C) 2015-2024 The Neo Project.
//
// BlockHashOrIndex.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics.CodeAnalysis;

namespace Neo.Plugins.RpcServer.Model
{
    public class BlockHashOrIndex
    {
        private readonly object _value;

        public BlockHashOrIndex(uint index)
        {
            _value = index;
        }

        public BlockHashOrIndex(UInt256 hash)
        {
            _value = hash;
        }

        public bool IsIndex => _value is uint;

        public static bool TryParse(string value, [NotNullWhen(true)] out BlockHashOrIndex blockHashOrIndex)
        {
            if (uint.TryParse(value, out var index))
            {
                blockHashOrIndex = new BlockHashOrIndex(index);
                return true;
            }
            if (UInt256.TryParse(value, out var hash))
            {
                blockHashOrIndex = new BlockHashOrIndex(hash);
                return true;
            }
            blockHashOrIndex = null;
            return false;
        }

        public uint AsIndex()
        {
            if (_value is uint intValue)
                return intValue;
            throw new RpcException(RpcError.InvalidParams.WithData($"Value {_value} is not a valid block index"));
        }

        public UInt256 AsHash()
        {
            if (_value is UInt256 hash)
                return hash;
            throw new RpcException(RpcError.InvalidParams.WithData($"Value {_value} is not a valid block hash"));
        }
    }
}
