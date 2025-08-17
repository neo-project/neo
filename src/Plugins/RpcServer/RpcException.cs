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

#nullable enable

using System;
using System.Runtime.CompilerServices;

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
            if (value is null) throw new RpcException(string.IsNullOrWhiteSpace(error.Data) ? error.WithData($"Parameter '{paramName}' is null") : error);
        }

        /// <summary>
        /// Throws an exception if the condition is true.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="error">The error to throw.</param>
        /// <param name="conditionExpresion">The expresion to evaluate.</param>
        public static void ThrowIfTrue(bool condition, RpcError error, [CallerArgumentExpression(nameof(condition))] string? conditionExpresion = null)
        {
            if (condition) throw new RpcException(string.IsNullOrWhiteSpace(error.Data) ? error.WithData($"Condition {conditionExpresion} is true") : error);
        }

        /// <summary>
        /// Throws an exception if the value is false.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="error">The condition evaluated.</param>
        /// <param name="conditionExpresion">The error to throw.</param>
        public static void ThrowIfFalse(bool condition, RpcError error, [CallerArgumentExpression(nameof(condition))] string? conditionExpresion = null)
        {
            if (!condition) throw new RpcException(string.IsNullOrWhiteSpace(error.Data) ? error.WithData($"Condition {conditionExpresion} is false") : error);
        }
    }
}
