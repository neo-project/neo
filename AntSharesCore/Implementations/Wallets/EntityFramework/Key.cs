namespace AntShares.Implementations.Wallets.EntityFramework
{
    internal class Key
    {
        public const string MasterKey = "MasterKey";
        public const string IV = "IV";

        public string Name { get; set; }
        public byte[] Value { get; set; }
    }
}
