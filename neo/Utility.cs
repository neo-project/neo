using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.Wallets;
using System;

namespace Neo
{
    public static class Utility
    {
        /// <summary>
        /// Parse WIF or private key hex string to KeyPair
        /// </summary>
        /// <param name="key">WIF or private key hex string
        /// Example: WIF ("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"), PrivateKey ("450d6c2a04b5b470339a745427bae6828400cf048400837d73c415063835e005")</param>
        /// <returns></returns>
        public static KeyPair GetKeyPair(string key)
        {
            if (string.IsNullOrEmpty(key)) { throw new ArgumentNullException(nameof(key)); }
            if (key.StartsWith("0x")) { key = key.Substring(2); }

            if (key.Length == 52)
            {
                return new KeyPair(Wallet.GetPrivateKeyFromWIF(key));
            }
            else if (key.Length == 64)
            {
                return new KeyPair(key.HexToBytes());
            }

            throw new FormatException();
        }

        /// <summary>
        /// Parse address, scripthash or public key string to UInt160
        /// </summary>
        /// <param name="account">account address, scripthash or public key string
        /// Example: address ("AV556nYUwyJKNv8Xy7hVMLQnkmKPukw6x5"), scripthash ("0x6a38cd693b615aea24dd00de12a9f5836844da91"), public key ("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575")</param>
        /// <returns></returns>
        public static UInt160 GetScriptHash(string account)
        {
            if (string.IsNullOrEmpty(account)) { throw new ArgumentNullException(nameof(account)); }
            if (account.StartsWith("0x")) { account = account.Substring(2); }

            if (account.Length == 34)
            {
                return Wallets.Helper.ToScriptHash(account);
            }
            else if (account.Length == 40)
            {
                return UInt160.Parse(account);
            }
            else if (account.Length == 66)
            {
                var pubKey = ECPoint.Parse(account, ECCurve.Secp256r1);
                return Contract.CreateSignatureRedeemScript(pubKey).ToScriptHash();
            }

            throw new FormatException();
        }
    }
}
