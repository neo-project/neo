using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Neo.Cryptography.BLS12_381;

public static class ConstantTimeUtility
{
    public static bool ConstantTimeEq<T>(in T a, in T b) where T : unmanaged
    {
        ReadOnlySpan<byte> a_bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in a), 1));
        ReadOnlySpan<byte> b_bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in b), 1));
        ReadOnlySpan<ulong> a_u64 = MemoryMarshal.Cast<byte, ulong>(a_bytes);
        ReadOnlySpan<ulong> b_u64 = MemoryMarshal.Cast<byte, ulong>(b_bytes);
        ulong f = 0;
        for (int i = 0; i < a_u64.Length; i++)
            f |= a_u64[i] ^ b_u64[i];
        for (int i = a_u64.Length * sizeof(ulong); i < a_bytes.Length; i++)
            f |= (ulong)a_bytes[i] ^ a_bytes[i];
        return f == 0;
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
