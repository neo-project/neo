using System;
using System.Globalization;
using System.Linq;

namespace AntShares
{
    public class UInt160 : UIntBase
    {
        public static readonly UInt160 Zero = new UInt160();

        public UInt160()
            : this(null)
        {
        }

        public UInt160(byte[] value)
            : base(20, value)
        {
        }

        public static UInt160 Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException();
            if (value.StartsWith("0x"))
                value = value.Substring(2);
            if (value.Length != 40)
                throw new FormatException();
            return new UInt160(value.HexToBytes().Reverse().ToArray());
        }

        public static bool TryParse(string s, out UInt160 result)
        {
            if (s == null)
            {
                result = null;
                return false;
            }
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 40)
            {
                result = null;
                return false;
            }
            byte[] data = new byte[20];
            for (int i = 0; i < 20; i++)
                if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[i]))
                {
                    result = null;
                    return false;
                }
            result = new UInt160(data.Reverse().ToArray());
            return true;
        }
    }
}
