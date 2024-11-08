// Copyright (C) 2015-2024 The Neo Project.
//
// ConstantTimeUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Neo.Cryptography.BLS12_381
{
    public static class ConstantTimeUtility
    {
        public static bool ConstantTimeEq<T>(in T a, in T b) where T : unmanaged
        {
            var a_bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in a), 1));
            var b_bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in b), 1));

            return CryptographicOperations.FixedTimeEquals(a_bytes, b_bytes);
        }

        public static T ConditionalSelect<T>(in T a, in T b, bool choice) where T : unmanaged
        {
            return choice ? b : a;
        }

        public static void ConditionalAssign<T>(this ref T self, in T other, bool choice) where T : unmanaged
        {
            self = ConditionalSelect(in self, in other, choice);
        }
    }
}
