using Neo.Wallets.NEP6;

namespace Neo.Wallets.SQLite
{
    internal class Account
    {
        public byte[] PublicKeyHash { get; set; }
        public string Nep2key { get; set; }

        public int ScryptN { get; set; } = ScryptParameters.Default.N;
        public int ScryptR { get; set; } = ScryptParameters.Default.R;
        public int ScryptP { get; set; } = ScryptParameters.Default.P;
    }
}
