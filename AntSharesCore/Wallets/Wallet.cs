using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    public abstract class Wallet
    {
        public const byte CoinVersion = 0x17;

        private byte[] masterKey;
        private byte[] iv;

        protected Wallet(byte[] masterKey, byte[] iv)
        {
            this.masterKey = masterKey;
            this.iv = iv;
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
        }

        public WalletEntry CreateEntry()
        {
            using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextArchiving }))
            {
                byte[] privateKey = key.Export(CngKeyBlobFormat.EccPrivateBlob);
                byte[] redeemScript = ScriptBuilder.CreateSignatureRedeemScript(ECPoint.FromBytes(privateKey, ECCurve.Secp256r1));
                WalletEntry entry = new WalletEntry(redeemScript, privateKey);
                SaveEntry(entry);
                Array.Clear(privateKey, 0, privateKey.Length);
                return entry;
            }
        }

        protected abstract void DeleteEntry(UInt160 scriptHash);

        protected abstract void GetEncryptedEntry(UInt160 scriptHash, out byte[] redeemScript, out byte[] encryptedPrivateKey);

        public WalletEntry GetEntry(UInt160 scriptHash)
        {
            byte[] redeemScript, encryptedPrivateKey;
            GetEncryptedEntry(scriptHash, out redeemScript, out encryptedPrivateKey);
            if (redeemScript == null || encryptedPrivateKey == null) return null;
            if (encryptedPrivateKey.Length != 96) throw new IOException();
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            byte[] decryptedPrivateKey;
            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(masterKey, iv))
                {
                    decryptedPrivateKey = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
                }
            }
            ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
            WalletEntry entry = new WalletEntry(redeemScript, decryptedPrivateKey);
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
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
            byte[] redeemScript = ScriptBuilder.CreateMultiSigRedeemScript(1, ECCurve.Secp256r1.G * privateKey);
            WalletEntry entry = new WalletEntry(redeemScript, privateKey);
            SaveEntry(entry);
            Array.Clear(privateKey, 0, privateKey.Length);
            Array.Clear(data, 0, data.Length);
            return entry;
        }

        protected abstract void SaveEncryptedEntry(UInt160 scriptHash, byte[] redeemScript, byte[] encryptedPrivateKey);

        private void SaveEntry(WalletEntry entry)
        {
            byte[] decryptedPrivateKey = new byte[96];
            Buffer.BlockCopy(entry.PublicKey, 0, decryptedPrivateKey, 0, 64);
            using (entry.Decrypt())
            {
                Buffer.BlockCopy(entry.PrivateKey, 0, decryptedPrivateKey, 64, 32);
            }
            ProtectedMemory.Unprotect(masterKey, MemoryProtectionScope.SameProcess);
            byte[] encryptedPrivateKey;
            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform encryptor = aes.CreateEncryptor(masterKey, iv))
                {
                    encryptedPrivateKey = encryptor.TransformFinalBlock(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
                }
            }
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
                byte[] signature;
                using (entry.Decrypt())
                {
                    signature = context.Signable.Sign(entry.PrivateKey, entry.PublicKey);
                }
                fSuccess |= context.Add(entry.RedeemScript, ECPoint.FromBytes(entry.PublicKey, ECCurve.Secp256r1), signature);
            }
            return fSuccess;
        }

        public static string ToAddress(UInt160 hash)
        {
            byte[] data = new byte[] { CoinVersion }.Concat(hash.ToArray()).ToArray();
            return Base58.Encode(data.Concat(data.Sha256().Sha256().Take(4)).ToArray());
        }

        public static UInt160 ToScriptHash(string address)
        {
            byte[] data = Base58.Decode(address);
            if (data.Length != 25)
                throw new FormatException();
            if (data[0] != CoinVersion)
                throw new FormatException();
            if (!data.Take(21).Sha256().Sha256().Take(4).SequenceEqual(data.Skip(21)))
                throw new FormatException();
            return new UInt160(data.Skip(1).Take(20).ToArray());
        }
    }
}
