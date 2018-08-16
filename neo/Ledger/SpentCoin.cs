using Neo.Network.P2P.Payloads;

namespace Neo.Ledger
{
    public class SpentCoin
    {
        public TransactionOutput Output;
        public uint StartHeight;
        public uint EndHeight;

        public Fixed8 Value => Output.Value;
    }
}
