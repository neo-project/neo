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
        private const int DefaultTimeoutSeconds = 10;

        public static ValueTask<TResult> DefaultTimeout<TResult>(this ValueTask<TResult> valueTask) =>
            TimeoutAfter(valueTask, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        public static ValueTask DefaultTimeout(this ValueTask valueTask) =>
            TimeoutAfter(valueTask, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        public static Task<TResult> DefaultTimeout<TResult>(this Task<TResult> task) =>
            TimeoutAfter(task, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        public static Task DefaultTimeout(this Task task)
            => TimeoutAfter(task, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        public static async ValueTask<TResult> TimeoutAfter<TResult>(this ValueTask<TResult> valueTask, TimeSpan timeout) =>
            await valueTask.AsTask().WaitAsync(timeout).ConfigureAwait(false);

        public static async ValueTask TimeoutAfter(this ValueTask valueTask, TimeSpan timeout) =>
            await valueTask.AsTask().WaitAsync(timeout).ConfigureAwait(false);

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) =>
            await task.WaitAsync(timeout).ConfigureAwait(false);

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout) =>
            await task.WaitAsync(timeout).ConfigureAwait(false);
    }
}
