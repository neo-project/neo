// Copyright (C) 2015-2025 The Neo Project.
//
// TryCatchExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Extensions.Exceptions
{
    internal static class TryCatchExtensions
    {
        public static TSource TryCatch<TSource>(this TSource obj, Action<TSource?> action)
            where TSource : class?
        {
            try
            {
                action(obj);
            }
            catch
            {
            }

            return obj;
        }

        public static TSource TryCatch<TSource, TException>(this TSource obj, Action<TSource?> action, Action<TSource?, TException> onError)
            where TSource : class?
            where TException : Exception
        {
            try
            {
                action(obj);
            }
            catch (TException ex)
            {
                onError?.Invoke(obj, ex);
            }

            return obj;
        }

        public static TResult? TryCatch<TSource, TException, TResult>(this TSource obj, Func<TSource?, TResult?> func, Func<TSource?, TException, TResult?> onError)
            where TSource : class?
            where TException : Exception
            where TResult : class?
        {
            try
            {
                return func(obj);
            }
            catch (TException ex)
            {
                return onError?.Invoke(obj, ex);
            }
        }

        public static TSource TryCatchThrow<TSource, TException>(this TSource obj, Action<TSource?> action)
            where TSource : class?
            where TException : Exception
        {
            try
            {
                action(obj);

                return obj;
            }
            catch (TException)
            {
                throw;
            }
        }

        public static TResult? TryCatchThrow<TSource, TException, TResult>(this TSource obj, Func<TSource?, TResult?> func)
            where TSource : class?
            where TException : Exception
            where TResult : class?
        {
            try
            {
                return func(obj);
            }
            catch (TException)
            {
                throw;
            }
        }

        public static TSource TryCatchThrow<TSource, TException>(this TSource obj, Action<TSource?> action, string? errorMessage = default)
            where TSource : class?
            where TException : Exception, new()
        {
            try
            {
                action(obj);

                return obj;
            }
            catch (TException innerException)
            {
                if (string.IsNullOrEmpty(errorMessage))
                    throw;
                else
                {
                    if (Activator.CreateInstance(typeof(TException), errorMessage, innerException) is not TException ex)
                        throw;
                    else
                        throw ex;
                }

            }
        }

        public static TResult? TryCatchThrow<TSource, TException, TResult>(this TSource obj, Func<TSource?, TResult?> func, string? errorMessage = default)
            where TSource : class?
            where TException : Exception
            where TResult : class?
        {
            try
            {
                return func(obj);
            }
            catch (TException innerException)
            {
                if (string.IsNullOrEmpty(errorMessage))
                    throw;
                else
                {
                    if (Activator.CreateInstance(typeof(TException), errorMessage, innerException) is not TException ex)
                        throw;
                    else
                        throw ex;
                }

            }
        }
    }
}
