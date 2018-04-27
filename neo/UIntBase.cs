using System;

namespace Neo
{
    public abstract class UIntBase
    {
        public static UIntBase Parse(string s)
        {
            int uint160Length = UInt160.Zero.Size * 2;

            if (s.Length == uint160Length || s.Length == uint160Length + 2)
                return UInt160.Parse(s);

            int uint256Length = UInt256.Zero.Size * 2;

            if (s.Length == uint256Length || s.Length == uint256Length + 2)
                return UInt256.Parse(s);

            throw new FormatException();
        }

        public static bool TryParse<T>(string s, out T result) where T : UIntBase
        {
            int uint160Length = UInt160.Zero.Size * 2;
            int uint256Length = UInt256.Zero.Size * 2;

            if (s.Length == uint160Length || s.Length == uint160Length + 2)
            {
                if (UInt160.TryParse(s, out var r))
                {
                    result = (T)(UIntBase)r;
                    return true;
                }
            }
            else if (s.Length == uint256Length || s.Length == uint256Length + 2)
            {
                if (UInt256.TryParse(s, out var r))
                {
                    result = (T)(UIntBase)r;
                    return true;
                }
            }

            result = default(T);
            return false;
        }
    }
}
