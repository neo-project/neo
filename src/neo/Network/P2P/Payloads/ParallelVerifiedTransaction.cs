namespace Neo.Network.P2P.Payloads
{
    public class ParallelVerifiedTransaction
    {
        public Transaction Transaction;
        public bool ShouldRelay;
        public bool VerifyResult;

        public ParallelVerifiedTransaction(Transaction tx, bool result, bool shouldRelay = true)
        {
            ShouldRelay = shouldRelay;
            Transaction = tx;
            VerifyResult = result;
        }
    }
}
