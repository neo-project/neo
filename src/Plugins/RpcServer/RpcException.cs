// Copyright (C) 2015-2025 The Neo Project.
//
// RpcException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Plugins.RpcServer
{
    public class RpcException : Exception
    {
        private readonly RpcError _rpcError;

        public RpcException(RpcError error) : base(error.ErrorMessage)
        {
            HResult = error.Code;
            _rpcError = error;
        }

        public RpcError GetError()
        {
            return _rpcError;
        }
    }
}
