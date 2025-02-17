// Copyright (C) 2015-2025 The Neo Project.
//
// ObjectExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using System;

namespace Neo.Build.Core.Extensions
{
    public static class ObjectExtensions
    {
        public static string TryCatchThrow<TCatch, TThrow>(this object source, Func<string> func)
            where TCatch : Exception
            where TThrow : NeoBuildException, new()
        {
            try
            {
                return func();
            }
            catch (TCatch)
            {
                throw new TThrow();
            }
        }
    }
}
