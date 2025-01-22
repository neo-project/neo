// Copyright (C) 2015-2025 The Neo Project.
//
// Result.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System;

namespace Neo.Plugins.RpcServer
{
    public static class Result
    {
        /// <summary>
        /// Checks the execution result of a function and throws an exception if it is null or throw an exception.
        /// </summary>
        /// <param name="function">The function to execute</param>
        /// <param name="err">The rpc error</param>
        /// <param name="withData">Append extra base exception message</param>
        /// <typeparam name="T">The return type</typeparam>
        /// <returns>The execution result</returns>
        /// <exception cref="RpcException">The Rpc exception</exception>
        public static T Ok_Or<T>(Func<T?> function, RpcError err, bool withData = false)
        {
            try
            {
                var result = function();
                return result == null ? throw new RpcException(err) : result;
            }
            catch (Exception ex)
            {
                if (withData)
                    throw new RpcException(err.WithData(ex.GetBaseException().Message));
                throw new RpcException(err);
            }
        }

        /// <summary>
        /// Checks the execution result and throws an exception if it is null.
        /// </summary>
        /// <param name="result">The execution result</param>
        /// <param name="err">The rpc error</param>
        /// <typeparam name="T">The return type</typeparam>
        /// <returns>The execution result</returns>
        /// <exception cref="RpcException">The Rpc exception</exception>
        public static T NotNull_Or<T>(this T? result, RpcError err)
        {
            if (result == null) throw new RpcException(err);
            return result;
        }

        /// <summary>
        /// The execution result is true or throws an exception or null.
        /// </summary>
        /// <param name="function">The function to execute</param>
        /// <param name="err">the rpc exception code</param>
        /// <returns>the execution result</returns>
        /// <exception cref="RpcException">The rpc exception</exception>
        public static bool True_Or(Func<bool> function, RpcError err)
        {
            try
            {
                var result = function();
                if (!result.Equals(true)) throw new RpcException(err);
                return result;
            }
            catch
            {
                throw new RpcException(err);
            }
        }

        /// <summary>
        /// Checks if the execution result is true or throws an exception.
        /// </summary>
        /// <param name="result">the execution result</param>
        /// <param name="err">the rpc exception code</param>
        /// <returns>the execution result</returns>
        /// <exception cref="RpcException">The rpc exception</exception>
        public static bool True_Or(this bool result, RpcError err)
        {
            if (!result.Equals(true)) throw new RpcException(err);
            return result;
        }

        /// <summary>
        /// Checks if the execution result is false or throws an exception.
        /// </summary>
        /// <param name="result">the execution result</param>
        /// <param name="err">the rpc exception code</param>
        /// <returns>the execution result</returns>
        /// <exception cref="RpcException">The rpc exception</exception>
        public static bool False_Or(this bool result, RpcError err)
        {
            if (!result.Equals(false)) throw new RpcException(err);
            return result;
        }

        /// <summary>
        /// Check if the execution result is null or throws an exception.
        /// </summary>
        /// <param name="result">The execution result</param>
        /// <param name="err">the rpc error</param>
        /// <typeparam name="T">The execution result type</typeparam>
        /// <returns>The execution result</returns>
        /// <exception cref="RpcException">the rpc exception</exception>
        public static void Null_Or<T>(this T? result, RpcError err)
        {
            if (result != null) throw new RpcException(err);
        }
    }
}
