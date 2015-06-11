using AntShares.Core;
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
            using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextArchiving }))
            {
                byte[] privateKey = key.Export(CngKeyBlobFormat.EccPrivateBlob);
                byte[] redeemScript = CreateRedeemScript(1, privateKey);
                WalletEntry entry = new WalletEntry(redeemScript, privateKey);
                SaveEntry(entry);
                Array.Clear(privateKey, 0, privateKey.Length);
                return entry;
            }
        }

        private static byte[] CreateRedeemScript(byte m, params byte[][] publicKeys)
        {
            if (!(1 <= m && m <= publicKeys.Length && publicKeys.Length <= 16))
                throw new ArgumentException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Add(ScriptOp.OP_1 - 1 + m);
                for (int i = 0; i < publicKeys.Length; i++)
                {
                    sb.Push(publicKeys[i].ToCompressedPublicKey());
                }
                sb.Add(ScriptOp.OP_1 - 1 + (byte)publicKeys.Length);
                sb.Add(ScriptOp.OP_CHECKMULTISIG);
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
            if ((redeemScript.Length - 3) % 34 != 0 || encryptedPrivateKey.Length % 96 != 0) throw new IOException();
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            byte[] iv = new byte[16];
            Buffer.BlockCopy(masterKey, 0, iv, 0, iv.Length);
            byte[] decryptedPrivateKey;
            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(masterKey, iv))
                {
                    decryptedPrivateKey = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
                }
            }
            Array.Clear(iv, 0, iv.Length);
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            byte[][] privateKeys = new byte[encryptedPrivateKey.Length / 96][];
            for (int i = 0; i < privateKeys.Length; i++)
            {
                privateKeys[i] = new byte[96];
                Buffer.BlockCopy(decryptedPrivateKey, i * 96, privateKeys[i], 0, 96);
            }
            WalletEntry entry = new WalletEntry(redeemScript, privateKeys);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            for (int i = 0; i < privateKeys.Length; i++)
            {
                Array.Clear(privateKeys[i], 0, privateKeys[i].Length);
            }
            return entry;
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
            Secp256r1Point p = Secp256r1Curve.G * privateKey;
            byte[] redeemScript = CreateRedeemScript(1, p.EncodePoint(true));
            WalletEntry entry = new WalletEntry(redeemScript, privateKey);
            SaveEntry(entry);
            Array.Clear(privateKey, 0, privateKey.Length);
            Array.Clear(data, 0, data.Length);
            return entry;
        }

        protected abstract void SaveEncryptedEntry(UInt160 scriptHash, byte[] redeemScript, byte[] encryptedPrivateKey);

        private void SaveEntry(WalletEntry entry)
        {
            byte[] decryptedPrivateKey = new byte[entry.PrivateKeys.Length * 96];
            for (int i = 0; i < entry.PrivateKeys.Length; i++)
            {
                Buffer.BlockCopy(entry.PublicKeys[i], 0, decryptedPrivateKey, i * 96, 64);
                using (entry.Decrypt(i))
                {
                    Buffer.BlockCopy(entry.PrivateKeys[i], 0, decryptedPrivateKey, i * 96 + 64, 32);
                }
            }
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            byte[] iv = new byte[16];
            Buffer.BlockCopy(masterKey, 0, iv, 0, iv.Length);
            byte[] encryptedPrivateKey;
            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform encryptor = aes.CreateEncryptor(masterKey, iv))
                {
                    encryptedPrivateKey = encryptor.TransformFinalBlock(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
                }
            }
            Array.Clear(iv, 0, iv.Length);
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            SaveEncryptedEntry(entry.ScriptHash, entry.RedeemScript, encryptedPrivateKey);
        }

        public bool Sign(SignatureContext context)
        {
            bool fSuccess = false;
            for (int i = 0; i < context.ScriptHashes.Length; i++)
            {
                WalletEntry entry = GetEntry(context.ScriptHashes[i]);
                if (entry == null) continue;
                for (int j = 0; j < entry.PrivateKeys.Length; j++)
                {
                    byte[] signature;
                    using (entry.Decrypt(j))
                    {
                        signature = context.Signable.Sign(entry.PrivateKeys[j], entry.PublicKeys[j]);
                    }
                    fSuccess |= context.Add(entry.RedeemScript, entry.PublicKeys[j].ToPublicKeyHash(), signature);
                }
            }
            return fSuccess;
        }
    }
}
