using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        public class RegisterCenter
        {
            private static readonly Dictionary<string, RegisterTable> registerTable = new Dictionary<string, RegisterTable>();

            //注册域名
            [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "name", "owner", "admin", "ttl" })]
            public StackItem RegisterNewName(ApplicationEngine engine, Array args)
            {
                string name = args[0].GetString();
                UInt160 owner = new UInt160(args[1].GetSpan());
                UInt160 admin = new UInt160(args[2].GetSpan());
                uint ttl = (uint)args[3].GetBigInteger();
                string[] namelevel = name.Split(".");
                if (!NNS.GetRootNames(engine.Snapshot).Contains(namelevel[namelevel.Length - 1])) return false;
                //if (namelevel.Length == 3 && !NNS.GetFirstNames(engine.Snapshot).Contains(namelevel[1] + "." + namelevel[2])) return false;
                //if (namelevel.Length == 4 && !NNS.GetSecondNames(engine.Snapshot).Contains(namelevel[1] + "." + namelevel[2] + "." + namelevel[3])) return false;
                RegisterTable table = new RegisterTable(name, owner, admin, ttl);
                return true;
            }

            public void setOwner(string name, UInt160 owner)
            {
                if (registerTable.TryGetValue(name, out RegisterTable table))
                {
                    table.Owner = owner;
                }
            }

            public void setResolver(string name, UInt160 owner, UInt160 admin)
            {
                if (registerTable.TryGetValue(name, out RegisterTable table))
                {
                    table.Owner = owner;
                    table.Admin = admin;
                }
            }

            public void setTTL(string name, ulong ttl)
            {
                if (registerTable.TryGetValue(name, out RegisterTable table))
                {
                    table.TTL = ttl;
                }
            }
        }
    }
}
