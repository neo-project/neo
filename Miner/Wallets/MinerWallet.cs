using AntShares.Core;
using AntShares.Cryptography;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    internal class MinerWallet
    {
        private readonly byte[] key_exported;
        public readonly Secp256r1Point PublicKey;

        private MinerWallet(byte[] key_exported)
        {
            this.key_exported = key_exported;
            this.PublicKey = Secp256r1Point.FromBytes(key_exported);
            ProtectedMemory.Protect(key_exported, MemoryProtectionScope.SameProcess);
        }

        public static MinerWallet Create(string path, SecureString password)
        {
            MinerWallet wallet;
            using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextArchiving }))
            {
                wallet = new MinerWallet(key.Export(CngKeyBlobFormat.EccPrivateBlob));
            }
            wallet.Save(path, password);
            return wallet;
        }

        public byte[] GetAesKey(Secp256r1Point pubkey)
        {
            byte[] prikey = new byte[32];
            ProtectedMemory.Unprotect(key_exported, MemoryProtectionScope.SameProcess);
            Buffer.BlockCopy(key_exported, 8 + 64, prikey, 0, 32);
            ProtectedMemory.Protect(key_exported, MemoryProtectionScope.SameProcess);
            byte[] aeskey = (pubkey * prikey).EncodePoint(false).Skip(1).Sha256();
            Array.Clear(prikey, 0, prikey.Length);
            return aeskey;
        }

        public static MinerWallet Open(string path, SecureString password)
        {
            byte[] data;
            byte[] iv = new byte[16];
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Read(iv, 0, iv.Length);
                data = new byte[fs.Length - iv.Length];
                fs.Read(data, 0, data.Length);
            }
            byte[] masterKey = password.ToArray().Sha256().Sha256();
            using (AesManaged aes = new AesManaged())
            using (ICryptoTransform decryptor = aes.CreateDecryptor(masterKey, iv))
            {
                return new MinerWallet(decryptor.TransformFinalBlock(data, 0, data.Length));
            }
        }

        private void Save(string path, SecureString password)
        {
            byte[] masterKey = password.ToArray().Sha256().Sha256();
            byte[] iv = new byte[16];
            byte[] data;
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetNonZeroBytes(iv);
            }
            using (AesManaged aes = new AesManaged())
            using (ICryptoTransform encryptor = aes.CreateEncryptor(masterKey, iv))
            {
                ProtectedMemory.Unprotect(key_exported, MemoryProtectionScope.SameProcess);
                data = encryptor.TransformFinalBlock(key_exported, 0, key_exported.Length);
                ProtectedMemory.Protect(key_exported, MemoryProtectionScope.SameProcess);
            }
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(iv, 0, iv.Length);
                fs.Write(data, 0, data.Length);
            }
        }

        public bool Sign(SignatureContext context, byte[] redeemScript)
        {
            return context.Add(redeemScript, PublicKey, Sign(context.Signable));
        }

        public byte[] Sign(ISignable signable)
        {
            byte[] signature;
            ProtectedMemory.Unprotect(key_exported, MemoryProtectionScope.SameProcess);
            using (CngKey key = CngKey.Import(key_exported, CngKeyBlobFormat.EccPrivateBlob))
            using (ECDsaCng ecdsa = new ECDsaCng(key))
            {
                signature = ecdsa.SignHash(signable.GetHashForSigning());
            }
            ProtectedMemory.Protect(key_exported, MemoryProtectionScope.SameProcess);
            return signature;
        }
    }
}
