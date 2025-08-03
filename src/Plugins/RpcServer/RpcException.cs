// Copyright (C) 2015-2025 The Neo Project.
//
// RpcException.cs file belongs to the neo project and is free
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

        /// <summary>
        /// Throws an exception if the value is null.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="error">The error to throw.</param>
        public static void ThrowIfNull<T>(T value, string paramName, RpcError error)
        {
            if (value is null) throw new RpcException(error.WithData($"Parameter '{paramName}' is null"));
        }
    }
}
