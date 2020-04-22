using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        public class Resolver
        {
            public UInt160 manager { get; }

            private UInt160 resolve(string name)
            {
                return UInt160.Zero;
            }
        }
    }
}
