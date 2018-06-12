using Neo.Core;
using System.Collections.Generic;

namespace Neo.Plugins
{
    public abstract class PolicyPlugin : Plugin
    {
        private static readonly List<PolicyPlugin> instances = new List<PolicyPlugin>();

        public new static IEnumerable<PolicyPlugin> Instances => instances;

        protected PolicyPlugin()
        {
            instances.Add(this);
        }

        internal protected virtual bool CheckPolicy(Transaction tx) => true;
        internal protected virtual IEnumerable<Transaction> Filter(IEnumerable<Transaction> transactions) => transactions;
    }
}
