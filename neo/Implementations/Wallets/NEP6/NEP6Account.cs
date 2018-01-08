using Neo.IO.Json;
using Neo.Wallets;
using System;

namespace Neo.Implementations.Wallets.NEP6
{
    internal class NEP6Account : WalletAccount
    {
        private readonly NEP6Wallet wallet;
        private readonly string nep2key;
        private KeyPair key;
        public JObject Extra;

        public bool Decrypted => nep2key == null || key != null;
        public override bool HasKey => nep2key != null;

        public NEP6Account(NEP6Wallet wallet, UInt160 scriptHash, string nep2key = null)
            : base(scriptHash)
        {
            this.wallet = wallet;
            this.nep2key = nep2key;
        }

        public NEP6Account(NEP6Wallet wallet, UInt160 scriptHash, KeyPair key, string password)
            : this(wallet, scriptHash, key.Export(password, wallet.Scrypt.N, wallet.Scrypt.R, wallet.Scrypt.P))
        {
            this.key = key;
        }

        public static NEP6Account FromJson(JObject json, NEP6Wallet wallet)
        {
            return new NEP6Account(wallet, Wallet.ToScriptHash(json["address"].AsString()), json["key"]?.AsString())
            {
                Label = json["label"]?.AsString(),
                IsDefault = json["isDefault"].AsBoolean(),
                Lock = json["lock"].AsBoolean(),
                Contract = NEP6Contract.FromJson(json["contract"]),
                Extra = json["extra"]
            };
        }

        public override KeyPair GetKey()
        {
            if (nep2key == null) return null;
            if (key == null)
            {
                key = wallet.DecryptKey(nep2key);
            }
            return key;
        }

        public KeyPair GetKey(string password)
        {
            if (nep2key == null) return null;
            if (key == null)
            {
                key = new KeyPair(Wallet.GetPrivateKeyFromNEP2(nep2key, password, wallet.Scrypt.N, wallet.Scrypt.R, wallet.Scrypt.P));
            }
            return key;
        }

        public JObject ToJson()
        {
            JObject account = new JObject();
            account["address"] = Wallet.ToAddress(ScriptHash);
            account["label"] = Label;
            account["isDefault"] = IsDefault;
            account["lock"] = Lock;
            account["key"] = nep2key;
            account["contract"] = ((NEP6Contract)Contract)?.ToJson();
            account["extra"] = Extra;
            return account;
        }

        public bool VerifyPassword(string password)
        {
            try
            {
                Wallet.GetPrivateKeyFromNEP2(nep2key, password, wallet.Scrypt.N, wallet.Scrypt.R, wallet.Scrypt.P);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
