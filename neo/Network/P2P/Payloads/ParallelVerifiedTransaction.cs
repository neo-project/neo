namespace Neo.Network.P2P.Payloads
{
    public class ParallelVerifiedTransaction
    {
        public Transaction Transaction { get; set; }

        public bool ShouldRelay { get; set; } = true;

        public ParallelVerifiedTransaction(Transaction tx)
        {
            Transaction = tx;
        }

        public ParallelVerifiedTransaction(Transaction tx, bool shouldRelay)
        {
            ShouldRelay = shouldRelay;
            Transaction = tx;
        }
    }
}
