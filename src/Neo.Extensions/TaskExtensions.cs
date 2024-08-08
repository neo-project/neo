// Copyright (C) 2015-2024 The Neo Project.
//
// TaskExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading.Tasks;

namespace Neo.Extensions
{
    public static class TaskExtensions
    {
        private const int DefaultTimeoutSeconds = 10;

        public static ValueTask<TResult> DefaultTimeout<TResult>(this ValueTask<TResult> valueTask) =>
            TimeoutAfter(valueTask, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        public static ValueTask DefaultTimeout(this ValueTask valueTask) =>
            TimeoutAfter(valueTask, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        public static Task<TResult> DefaultTimeout<TResult>(this Task<TResult> task) =>
            TimeoutAfter(task, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        public static Task DefaultTimeout(this Task task)
            => TimeoutAfter(task, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        public static async ValueTask<TResult> TimeoutAfter<TResult>(this ValueTask<TResult> valueTask, TimeSpan timeout)
        {
#if NET5_0_OR_GREATER
            return await valueTask.AsTask().WaitAsync(timeout).ConfigureAwait(false);
#else
            var task = valueTask.AsTask();
            if (task.Wait(timeout))
                return await task.ConfigureAwait(false);
            else
                throw new TimeoutException();
#endif
        }

        public static async ValueTask TimeoutAfter(this ValueTask valueTask, TimeSpan timeout)
        {
#if NET5_0_OR_GREATER
            await valueTask.AsTask().WaitAsync(timeout).ConfigureAwait(false);
#else
            var task = valueTask.AsTask();
            if (task.Wait(timeout))
                await task.ConfigureAwait(false);
            else
                throw new TimeoutException();
#endif
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
#if NET5_0_OR_GREATER
            return await task.WaitAsync(timeout).ConfigureAwait(false);
#else
            if (task.Wait(timeout))
                return await task.ConfigureAwait(false);
            else
                throw new TimeoutException();
#endif
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
#if NET5_0_OR_GREATER
            await task.WaitAsync(timeout).ConfigureAwait(false);
#else
            if (task.Wait(timeout))
                await task.ConfigureAwait(false);
            else
                throw new TimeoutException();
#endif
        }
    }
}
