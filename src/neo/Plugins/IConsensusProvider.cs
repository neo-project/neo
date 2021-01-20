using Neo.Wallets;

namespace Neo.Plugins
{
    public interface IConsensusProvider
    {
        void Start(Wallet wallet);
    }
}
