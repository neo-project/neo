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

namespace Neo.Hosting.App.Extensions
{
    internal static class TaskExtensions
    {
        public static ValueTask<TResult> DefaultTimeoutAsync<TResult>(this ValueTask<TResult> valueTask) =>
            TimeoutAfterAsync(valueTask, TimeSpan.FromSeconds(5));

        public static ValueTask DefaultTimeoutAsync(this ValueTask valueTask) =>
            TimeoutAfterAsync(valueTask, TimeSpan.FromSeconds(5));

        public static Task<TResult> DefaultTimeoutAsync<TResult>(this Task<TResult> task) =>
            TimeoutAfterAsync(task, TimeSpan.FromSeconds(5));

        public static Task DefaultTimeoutAsync(this Task task) =>
            TimeoutAfterAsync(task, TimeSpan.FromSeconds(5));

        public static async ValueTask<TResult> TimeoutAfterAsync<TResult>(this ValueTask<TResult> valueTask, TimeSpan timeout) =>
            await valueTask.AsTask().WaitAsync(timeout).ConfigureAwait(false);

        public static async ValueTask TimeoutAfterAsync(this ValueTask valueTask, TimeSpan timeout) =>
            await valueTask.AsTask().WaitAsync(timeout).ConfigureAwait(false);

        public static async Task<TResult> TimeoutAfterAsync<TResult>(this Task<TResult> task, TimeSpan timeout) =>
            await task.WaitAsync(timeout).ConfigureAwait(false);

        public static async Task TimeoutAfterAsync(this Task task, TimeSpan timeout) =>
            await task.WaitAsync(timeout).ConfigureAwait(false);
    }
}
