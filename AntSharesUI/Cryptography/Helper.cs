using System;
using System.Security.Cryptography;
using System.Text;

namespace AntShares.Cryptography
{
    internal static class Helper
    {
        public static void AesDecrypt(this byte[] data, byte[] key, byte[] iv)
        {
            if (data == null || key == null || iv == null) throw new ArgumentNullException();
            if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentException();
            byte[] buffer;
            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(key, iv))
                {
                    buffer = decryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
            Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
            Array.Clear(iv, 0, iv.Length);
            Array.Clear(buffer, 0, buffer.Length);
        }

        public static void AesEncrypt(this byte[] data, byte[] key, byte[] iv)
        {
            if (data == null || key == null || iv == null) throw new ArgumentNullException();
            if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentException();
            byte[] buffer;
            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform encryptor = aes.CreateEncryptor(key, iv))
                {
                    buffer = encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
            Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
            Array.Clear(iv, 0, iv.Length);
        }

        public static byte[] ToAesKey(this string password)
        {
            using (SHA256Cng sha256 = new SHA256Cng())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] passwordHash = sha256.ComputeHash(passwordBytes);
                byte[] passwordHash2 = sha256.ComputeHash(passwordHash);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
                Array.Clear(passwordHash, 0, passwordHash.Length);
                return passwordHash2;
            }
        }
    }
}
