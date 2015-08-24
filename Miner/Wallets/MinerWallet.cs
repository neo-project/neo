using AntShares.Core;
using AntShares.Cryptography;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    internal class MinerWallet
    {
        private readonly byte[] key_exported;
        public readonly ECCPublicKey PublicKey;

        private MinerWallet(byte[] key_exported)
        {
            this.key_exported = key_exported;
            this.PublicKey = new ECCPublicKey(key_exported);
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

        public static MinerWallet Open(string path, SecureString password)
        {
            byte[] data = File.ReadAllBytes(path);
            byte[] masterKey = password.ToArray().Sha256().Sha256();
            byte[] iv = masterKey.Take(16).ToArray();
            using (AesManaged aes = new AesManaged())
            using (ICryptoTransform decryptor = aes.CreateDecryptor(masterKey, iv))
            {
                return new MinerWallet(decryptor.TransformFinalBlock(data, 0, data.Length));
            }
        }

        private void Save(string path, SecureString password)
        {
            byte[] masterKey = password.ToArray().Sha256().Sha256();
            byte[] iv = masterKey.Take(16).ToArray();
            byte[] data;
            using (AesManaged aes = new AesManaged())
            using (ICryptoTransform encryptor = aes.CreateEncryptor(masterKey, iv))
            {
                ProtectedMemory.Unprotect(key_exported, MemoryProtectionScope.SameProcess);
                data = encryptor.TransformFinalBlock(key_exported, 0, key_exported.Length);
                ProtectedMemory.Protect(key_exported, MemoryProtectionScope.SameProcess);
            }
            File.WriteAllBytes(path, data);
        }

        public bool Sign(SignatureContext context, byte[] redeemScript)
        {
            byte[] signature;
            ProtectedMemory.Unprotect(key_exported, MemoryProtectionScope.SameProcess);
            using (CngKey key = CngKey.Import(key_exported, CngKeyBlobFormat.EccPrivateBlob))
            using (ECDsaCng ecdsa = new ECDsaCng(key))
            {
                signature = ecdsa.SignHash(context.Signable.GetHashForSigning());
            }
            ProtectedMemory.Protect(key_exported, MemoryProtectionScope.SameProcess);
            return context.Add(redeemScript, PublicKey, signature);
        }
    }
}
