using Neo.SmartContract;
using Neo.Wallets;
using System.Collections.Generic;
using System.Security.Cryptography;
using static Neo.SDK.Wallet.WalletFile;

namespace Neo.SDK.Wallet
{
    public class WalletHelper
    {
        public const string DefaultLabel = "NeoSdkAccount";
        public const string DefaultWalletName = "NeoSdkWallet";
        public const string DefaultVersion = "1.0";

        public const int ScryptN = 16384;
        public const int ScryptR = 8;
        public const int ScryptP = 8;

        public KeyPair CreateKeyPair()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            return new KeyPair(privateKey);
        }

        public Account CreateAccount(string label, string password, KeyPair pair, int n, int r, int p)
        {
            return new Account
            {
                Address = pair.PublicKeyHash.ToAddress(),
                Label = label,
                Key = pair.Export(password, n, r, p),
                Contract = new WalletFile.Contract
                {
                    Script = SmartContract.Contract.CreateSignatureRedeemScript(pair.PublicKey).ToHexString(),
                    Parameters = new[] { new WalletFile.ContractParameter {
                        Name = "signature",
                        Type = ContractParameterType.Signature.ToString()
                    } },
                    Deployed = false
                },
                Extra = null,
                IsDefault = true,
                Lock = false
            };
        }

        public Account CreateAccount(string password, KeyPair pair)
        {
            return CreateAccount(DefaultLabel, password, pair, ScryptN, ScryptR, ScryptP);
        }

        public WalletFile CreateWallet(string name, string version, int n, int r, int p, object extra = null)
        {
            return new WalletFile
            {
                Name = name,
                Version = version,
                Scrypt = new ScryptParams { N = n, R = r, P = p },
                Accounts = new List<Account>(),
                Extra = extra
            };
        }

        public WalletFile CreateStandardWallet()
        {
            return CreateWallet(DefaultWalletName, DefaultVersion, ScryptN, ScryptR, ScryptP);
        }


    }
}
