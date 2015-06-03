using AntShares.Core.Scripts;
using AntShares.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    public abstract class Wallet
    {
        private byte[] masterKey;

        protected Wallet(byte[] masterKey)
        {
            this.masterKey = masterKey;
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
        }

        public WalletEntry CreateEntry()
        {
            byte[] privateKey = new byte[32];
            byte[] publicKey = new byte[33];
            using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextArchiving }))
            {
                byte[] data = key.Export(CngKeyBlobFormat.EccPrivateBlob);
                Buffer.BlockCopy(data, 8 + 64, privateKey, 0, 32);
                Buffer.BlockCopy(data, 8, publicKey, 1, 32);
                publicKey[0] = (byte)(data[8 + 64 - 1] % 2 + 2);
                Array.Clear(data, 0, data.Length);
            }
            byte[] redeemScript = CreateRedeemScript(1, publicKey);
            WalletEntry entry = new WalletEntry(redeemScript, privateKey);
            SaveEntry(entry);
            return entry;
        }

        private static byte[] CreateRedeemScript(byte m, params byte[][] publicKey)
        {
            if (!(1 <= m && m <= publicKey.Length && publicKey.Length <= 16))
                throw new ArgumentException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.WriteOp((ScriptOp)(0x50 + m));
                for (int i = 0; i < publicKey.Length; i++)
                {
                    sb.WritePushData(publicKey[i]);
                }
                sb.WriteOp((ScriptOp)(0x50 + publicKey.Length));
                sb.WriteOp(ScriptOp.OP_CHECKMULTISIG);
                return sb.ToArray();
            }
        }

        protected abstract void DeleteEntry(UInt160 scriptHash);

        protected abstract void GetEncryptedEntry(UInt160 scriptHash, out byte[] redeemScript, out byte[] encryptedPrivateKey);

        private WalletEntry GetEntry(UInt160 scriptHash)
        {
            byte[] redeemScript, encryptedPrivateKey;
            GetEncryptedEntry(scriptHash, out redeemScript, out encryptedPrivateKey);
            if (redeemScript == null || encryptedPrivateKey == null) return null;
            if ((redeemScript.Length - 3) % 33 != 0 || encryptedPrivateKey.Length % 32 != 0) throw new IOException();
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            byte[] iv = new byte[16];
            Buffer.BlockCopy(masterKey, 0, iv, 0, iv.Length);
            byte[] decryptedPrivateKey;
            using (AesManaged aes = new AesManaged())
            using (ICryptoTransform decryptor = aes.CreateDecryptor(masterKey, iv))
            {
                decryptedPrivateKey = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
            }
            Array.Clear(iv, 0, iv.Length);
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            byte[][] privateKey = new byte[encryptedPrivateKey.Length / 32][];
            for (int i = 0; i < privateKey.Length; i++)
            {
                Buffer.BlockCopy(decryptedPrivateKey, i * 32, privateKey[i], 0, 32);
            }
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            return new WalletEntry(redeemScript, privateKey);
        }

        public abstract IEnumerable<UInt160> GetAddresses();

        public WalletEntry Import(string wif)
        {
            if (wif == null)
                throw new ArgumentNullException();
            byte[] data = Base58.Decode(wif);
            if (data.Length != 38 || data[0] != 0x80 || data[33] != 0x01)
                throw new FormatException();
            byte[] checksum = data.Sha256(0, data.Length - 4).Sha256();
            if (!data.Skip(data.Length - 4).SequenceEqual(checksum.Take(4)))
                throw new FormatException();
            byte[] privateKey = new byte[32];
            Buffer.BlockCopy(data, 1, privateKey, 0, privateKey.Length);
            Array.Clear(data, 0, data.Length);
            Secp256r1Point p = Secp256r1Curve.G * privateKey;
            byte[] redeemScript = CreateRedeemScript(1, p.EncodePoint(true));
            WalletEntry entry = new WalletEntry(redeemScript, privateKey);
            SaveEntry(entry);
            return entry;
        }

        protected abstract void SaveEncryptedEntry(UInt160 scriptHash, byte[] redeemScript, byte[] encryptedPrivateKey);

        private void SaveEntry(WalletEntry entry)
        {
            byte[] decryptedPrivateKey = new byte[entry.PrivateKey.Length * 32];
            for (int i = 0; i < entry.PrivateKey.Length; i++)
            {
                using (entry.Decrypt(i))
                {
                    Buffer.BlockCopy(entry.PrivateKey[i], 0, decryptedPrivateKey, i * 32, 32);
                }
            }
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            byte[] iv = new byte[16];
            Buffer.BlockCopy(masterKey, 0, iv, 0, iv.Length);
            byte[] encryptedPrivateKey;
            using (AesManaged aes = new AesManaged())
            using (ICryptoTransform encryptor = aes.CreateEncryptor(masterKey, iv))
            {
                encryptedPrivateKey = encryptor.TransformFinalBlock(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            }
            Array.Clear(iv, 0, iv.Length);
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            SaveEncryptedEntry(entry.ScriptHash, entry.RedeemScript, encryptedPrivateKey);
        }
    }
}
