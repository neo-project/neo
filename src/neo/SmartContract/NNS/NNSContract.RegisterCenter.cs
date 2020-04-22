using Neo.SmartContract.Native;
using System.Collections.Generic;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        public class RegisterCenter
        {
            private static readonly Dictionary<string, RegisterTable> registerTable = new Dictionary<string, RegisterTable>();

            public static void RegisterNewName(string name, UInt160 owner, ulong TTL)
            {
                if (registerTable.ContainsKey(name)) return;
                RegisterTable table = new RegisterTable(owner, TTL);
                registerTable.Add(name, table);
            }
        }
    }
}
