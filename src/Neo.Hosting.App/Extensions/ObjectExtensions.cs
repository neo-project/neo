// Copyright (C) 2015-2024 The Neo Project.
//
// ObjectExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Hosting.App.Extensions
{
    internal static class ObjectExtensions
    {
        public static TResult TryCatch<TSource, TResult>(this TSource obj, Func<TSource, TResult> func, TResult defaultOnError)
        {
            try
            {
                return func(obj);
            }
            catch
            {
                return defaultOnError;
            }
        }
    }
}
