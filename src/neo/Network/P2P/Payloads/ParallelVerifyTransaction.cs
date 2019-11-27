namespace Neo.Network.P2P.Payloads
{
    public class ParallelVerifyTransaction
    {
        public Transaction Transaction { get; set; }

        public bool ShouldRelay { get; set; }

        public ParallelVerifyTransaction(Transaction tx, bool shouldRelay = true)
        {
            ShouldRelay = shouldRelay;
            Transaction = tx;
        }
    }
}
