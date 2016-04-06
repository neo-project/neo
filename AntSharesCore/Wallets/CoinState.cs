namespace AntShares.Wallets
{
    public enum CoinState : byte
    {
        Unconfirmed,
        Unspent,
        Unclaimed,
        Spent
    }
}
