using AntShares.Core;
using System.ComponentModel;

namespace AntShares.Network
{
    public class AddingTransactionEventArgs : CancelEventArgs
    {
        public Transaction Transaction { get; private set; }

        public AddingTransactionEventArgs(Transaction tx)
        {
            Transaction = tx;
        }
    }
}
