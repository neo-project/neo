// Copyright (C) 2015-2025 The Neo Project.
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

namespace Neo.Cryptography.BN254
{
    static class ConstantTimeUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ConditionalSelect<T>(in T a, in T b, bool choice) where T : struct
        {
            return choice ? b : a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConditionalAssign<T>(ref T a, in T b, bool choice) where T : struct
        {
            a = ConditionalSelect(in a, in b, choice);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConditionalSwap<T>(ref T a, ref T b, bool choice) where T : struct
        {
            T tmp = a;
            ConditionalAssign(ref a, in b, choice);
            ConditionalAssign(ref b, in tmp, choice);
        }
    }
}