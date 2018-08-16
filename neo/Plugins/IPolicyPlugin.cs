using Neo.Network.P2P.Payloads;
using System.Collections.Generic;

namespace Neo.Plugins
{
    public interface IPolicyPlugin
    {
        bool CheckPolicy(Transaction tx);
        IEnumerable<Transaction> Filter(IEnumerable<Transaction> transactions);
    }
}
