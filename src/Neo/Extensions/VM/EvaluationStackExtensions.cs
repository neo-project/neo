// Copyright (C) 2015-2025 The Neo Project.
//
// EvaluationStackExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.VM;
using System;

namespace Neo.Extensions
{
    public static class EvaluationStackExtensions
    {
        /// <summary>
        /// Converts the <see cref="EvaluationStack"/> to a JSON object.
        /// </summary>
        /// <param name="stack">The <see cref="EvaluationStack"/> to convert.</param>
        /// <param name="maxSize">The maximum size in bytes of the result.</param>
        /// <returns>The <see cref="EvaluationStack"/> represented by a JSON object.</returns>
        public static JArray ToJson(this EvaluationStack stack, int maxSize = int.MaxValue)
        {
            if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize));
            maxSize -= 2/*[]*/+ Math.Max(0, (stack.Count - 1))/*,*/;
            JArray result = [];
            foreach (var item in stack)
                result.Add(item.ToJson(null, ref maxSize));
            if (maxSize < 0) throw new InvalidOperationException("Max size reached.");
            return result;
        }
    }
}
