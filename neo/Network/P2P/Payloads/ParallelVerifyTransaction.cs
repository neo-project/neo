namespace Neo.Network.P2P.Payloads
{
    public class ParallelVerifyTransaction
    {
        public Transaction Transaction { get; set; }

        public bool ShouldRelay { get; set; } = true;

        public ParallelVerifyTransaction(Transaction tx)
        {
            Transaction = tx;
        }

        public ParallelVerifyTransaction(Transaction tx, bool shouldRelay)
        {
            ShouldRelay = shouldRelay;
            Transaction = tx;
        }
    }
}
