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
            public UInt160 Owner { set; get; }
            public UInt160 Admin { set; get; }
            public string Name { get; }
            public ulong TTL { set; get; }
            public Resolver Resolver { set; get; }

            public RegisterTable(string name, UInt160 owner, UInt160 admin, ulong ttl)
            {
                this.Owner = admin;
                this.TTL = ttl;
                Resolver = new Resolver(name, owner, admin);
            }

        }
    }
}
