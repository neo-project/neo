// Copyright (C) 2015-2024 The Neo Project.
//
// ExceptionExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CommandLine.Exceptions;
using Neo.CommandLine.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CommandLine.Extensions
{
    internal static class ExceptionExtensions
    {
        public static void TryCatch<TSource>(this TSource sourceObject, Action<TSource> action)
        {
            try
            {
                action(sourceObject);
            }
            catch
            {
            }
        }

        public static void TryCatchThrow<TSource>(this TSource sourceObject, Action<TSource> action)
        {
            try
            {
                action(sourceObject);
            }
            catch
            {
                throw;
            }
        }

        public static void TryCatchHandle<TSource>(this TSource sourceObject, Action<TSource> action)
        {
            try
            {
                action(sourceObject);
            }
            catch (EndOfStreamException)
            {
                Console.Error.WriteLine("Could not connect to Remote Service.");
            }
            catch (Exception ex)
            {
                ex = ex.InnerException ?? ex;
                Console.Error.WriteLine("{0}::{1}: {2}", nameof(RemoteCommandClient), ex.GetType().Name, ex.Message);
            }
        }

        public static async Task<TResult?> TryCatchHandle<TSource, TResult>(this TSource sourceObject, Func<Task<TResult?>> func, CancellationToken cancellationToken = default)
        {
            try
            {
                var waitTaskResult = await Task.WhenAny(Task.Run(func, cancellationToken));
                if (waitTaskResult.IsCanceled == false)
                    return waitTaskResult.Result;
                else
                    return await Task.FromException<TResult?>(new RequestTaskTimeoutException());
            }
            catch (EndOfStreamException)
            {
                return await Task.FromException<TResult?>(new HostServiceDisconnectException());
            }
            catch (Exception ex)
            {
                ex = ex.InnerException ?? ex;
                return await Task.FromException<TResult?>(ex);
            }
        }
    }
}
