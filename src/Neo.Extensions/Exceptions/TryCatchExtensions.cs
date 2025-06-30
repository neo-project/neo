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
        public static void TryCatch(this object _, Action action)
        {
            try
            {
                action();
            }
            catch
            {
            }
        }

        public static void TryCatch<TException>(this object _, Action action, Action<TException> onError)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                onError?.Invoke(ex);
            }
        }

        public static TResult? TryCatch<TException, TResult>(this object _, Func<TResult?> func, Func<TException, TResult?> onError)
            where TException : Exception
            where TResult : class?
        {
            try
            {
                return func();
            }
            catch (TException ex)
            {
                return onError?.Invoke(ex);
            }
        }

        public static void TryCatchThrow<TException>(this object _, Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                throw;
            }
        }

        public static TResult? TryCatchThrow<TException, TResult>(this object _, Func<TResult?> func)
            where TException : Exception
            where TResult : class?
        {
            try
            {
                return func();
            }
            catch (TException)
            {
                throw;
            }
        }

        public static void TryCatchThrow<TException>(this object _, Action action, string? errorMessage = default)
            where TException : Exception, new()
        {
            try
            {
                action();
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

        public static TResult? TryCatchThrow<TException, TResult>(this object _, Func<TResult?> func, string? errorMessage = default)
            where TException : Exception
            where TResult : class?
        {
            try
            {
                return func();
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
