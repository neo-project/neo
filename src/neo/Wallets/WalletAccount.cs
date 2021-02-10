using Neo.SmartContract;

namespace Neo.Wallets
{
    public abstract class WalletAccount
    {
        protected readonly ProtocolSettings ProtocolSettings;
        public readonly UInt160 ScriptHash;
        public string Label;
        public bool IsDefault;
        public bool Lock;
        public Contract Contract;

        public string Address => ScriptHash.ToAddress(ProtocolSettings.AddressVersion);
        public abstract bool HasKey { get; }
        public bool WatchOnly => Contract == null;

        public abstract KeyPair GetKey();

        protected WalletAccount(UInt160 scriptHash, ProtocolSettings settings)
        {
            this.ProtocolSettings = settings;
            this.ScriptHash = scriptHash;
        }
    }
}
