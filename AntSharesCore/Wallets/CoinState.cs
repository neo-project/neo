namespace AntShares.Wallets
{
    public enum CoinState : byte
    {
        Unconfirmed,
        Unspent,
        Spending,
        Spent,
        SpentAndClaimed
    }
}
