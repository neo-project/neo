using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace AntShares.Wallets
{
    public static class Helper
    {
        public static bool CompareTo(this SecureString s1, SecureString s2)
        {
            if (s1.Length != s2.Length)
                return false;
            IntPtr p1 = IntPtr.Zero;
            IntPtr p2 = IntPtr.Zero;
            try
            {
                p1 = Marshal.SecureStringToGlobalAllocAnsi(s1);
                p2 = Marshal.SecureStringToGlobalAllocAnsi(s2);
                int i = 0;
                while (true)
                {
                    byte b1 = Marshal.ReadByte(p1, i);
                    byte b2 = Marshal.ReadByte(p2, i++);
                    if (b1 == 0 && b2 == 0)
                        return true;
                    if (b1 != b2)
                        return false;
                    if (b1 == 0 || b2 == 0)
                        return false;
                }
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(p1);
                Marshal.ZeroFreeGlobalAllocAnsi(p2);
            }
        }

        public static byte[] ToArray(this SecureString s)
        {
            if (s == null)
                throw new NullReferenceException();
            if (s.Length == 0)
                return new byte[0];
            List<byte> result = new List<byte>();
            IntPtr ptr = Marshal.SecureStringToGlobalAllocAnsi(s);
            try
            {
                int i = 0;
                do
                {
                    byte b = Marshal.ReadByte(ptr, i++);
                    if (b == 0)
                        break;
                    result.Add(b);
                } while (true);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
            return result.ToArray();
        }
    }
}
