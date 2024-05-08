// Copyright (C) 2015-2024 The Neo Project.
//
// EnumExtensions.cs file belongs to the neo project and is free
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
    internal static class EnumExtensions
    {
        public static bool Is<TEnum>(this int i, TEnum value)
            where TEnum : struct, Enum =>
            Enum.IsDefined(typeof(TEnum), i)
            ? (int)Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())) == i
            : throw new InvalidCastException();
    }
}
