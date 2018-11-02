using Neo.IO.Json;
using System;

namespace Neo.Wallets.NEP6
{
    internal class NEP6Account : WalletAccount
    {
        private readonly Wallet wallet;
        private readonly string nep2key;
        private KeyPair key;
        public JObject Extra;

        public bool Decrypted => nep2key == null || key != null;
        public override bool HasKey => nep2key != null;

        private readonly ScryptParameters Scrypt;
        public string password;

        public NEP6Account(Wallet wallet, UInt160 scriptHash, string nep2key = null)
            : base(scriptHash)
        {
            this.wallet = wallet;
            this.nep2key = nep2key;
        }

        public NEP6Account(Wallet wallet, UInt160 scriptHash, KeyPair key, string password, ScryptParameters Scrypt)
            : this(wallet, scriptHash, key.Export(password, Scrypt.N, Scrypt.R, Scrypt.P))
        {
            this.key = key;
            this.Scrypt = Scrypt;
            this.password = password;
        }

        public static NEP6Account FromJson(JObject json, Wallet wallet)
        {
            return new NEP6Account(wallet, json["address"].AsString().ToScriptHash(), json["key"]?.AsString())
            {
                Label = json["label"]?.AsString(),
                IsDefault = json["isDefault"].AsBoolean(),
                Lock = json["lock"].AsBoolean(),
                Contract = NEP6Contract.FromJson(json["contract"]),
                Extra = json["extra"]
            };
        }

        public KeyPair DecryptKey(string nep2key)
        {
            return new KeyPair(Wallet.GetPrivateKeyFromNEP2(nep2key, password, Scrypt.N, Scrypt.R, Scrypt.P));
        }

        public override KeyPair GetKey()
        {
            if (nep2key == null) return null;
            if (key == null)
            {
                key = DecryptKey(nep2key);
            }
            return key;
        }

        public KeyPair GetKey(string password, ScryptParameters Scrypt)
        {
            if (nep2key == null) return null;
            if (key == null)
            {
                key = new KeyPair(Wallet.GetPrivateKeyFromNEP2(nep2key, password, Scrypt.N, Scrypt.R, Scrypt.P));
            }
            return key;
        }

        public JObject ToJson()
        {
            JObject account = new JObject();
            account["address"] = ScriptHash.ToAddress();
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
                Wallet.GetPrivateKeyFromNEP2(nep2key, password, Scrypt.N, Scrypt.R, Scrypt.P);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
