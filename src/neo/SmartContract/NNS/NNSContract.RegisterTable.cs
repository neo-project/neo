using Neo.Cryptography;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        public class RegisterTable
        {
            public UInt160 Owner { get; }
            public ulong TTL { get; }
            public Resolver resolver { get; }

            public RegisterTable(UInt160 owner, ulong ttl)
            {
                this.Owner = owner;
                this.TTL = ttl;
            }
        }
    }
}
